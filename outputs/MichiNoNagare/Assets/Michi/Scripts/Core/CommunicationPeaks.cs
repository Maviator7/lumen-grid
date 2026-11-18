using System;
using System.Collections.Generic;
namespace Michi.Core
{
    public sealed partial class NetworkSimulation
    {
        public const double CommunicationCycle=120,CommunicationPreparation=90,CenterBoostPower=2,CenterBoostOutput=11;
        public bool CommunicationPeaksEnabled {get;private set;}
        public double CommunicationEpoch {get;private set;}
        bool communicationReferenceStress;
        readonly List<int> boostedCenters=new List<int>();
        IReadOnlyList<int> boostedCenterView;
        public IReadOnlyList<int> BoostedCenters {get{return boostedCenterView??(boostedCenterView=boostedCenters.AsReadOnly());}}
        public int AvailableCenterBoosts {get{return Continuous?1+ExpansionCount/2:0;}}
        double CommunicationPhase {get{return Math.Max(0,Time-CommunicationEpoch)%CommunicationCycle;}}
        public bool DataPeakActive {get{return CommunicationPeaksEnabled&&(communicationReferenceStress||CommunicationPhase>=CommunicationPreparation);}}
        public double DataPeakChangeIn {get{return !CommunicationPeaksEnabled?0:DataPeakActive?CommunicationCycle-CommunicationPhase:CommunicationPreparation-CommunicationPhase;}}
        public bool HasResearchDistrict
        {
            get{foreach(var sector in sectors)if(Nodes[sector.District].DemandProfile==DistrictDemandProfile.Research)return true;return false;}
        }
        double ResearchPhase {get{return Time%CommunicationCycle;}}
        public bool ResearchBurstActive {get{return HasResearchDistrict&&(communicationReferenceStress||ResearchPhase>=CommunicationPreparation);}}
        public double ResearchBurstChangeIn {get{return !HasResearchDistrict?0:ResearchBurstActive?CommunicationCycle-ResearchPhase:CommunicationPreparation-ResearchPhase;}}
        public bool IsCenterBoosted(int node){return boostedCenters.Contains(node);}
        public double NominalDataOutput(int node){return IsCenterBoosted(node)?CenterBoostOutput:Nodes[node].DataSupply;}
        public static double DistrictPeakDataRate(int role){return 3;}
        public static double DistrictBaseDataRate(int role,DistrictDemandProfile profile){return profile==DistrictDemandProfile.Research?ResearchBaseData:DistrictDataRate(role);}
        public static double DistrictPeakDataRate(int role,DistrictDemandProfile profile){return profile==DistrictDemandProfile.Research?ResearchPeakData:DistrictPeakDataRate(role);}
        double DistrictReferenceDataRate(GridNode n){return CommunicationPeaksEnabled||n.DemandProfile==DistrictDemandProfile.Research?DistrictPeakDataRate(n.Role,n.DemandProfile):DistrictBaseDataRate(n.Role,n.DemandProfile);}
        public double PeakDataDemandTotal {get{double total=0;foreach(var n in Nodes)if(n.Kind==NetworkNodeKind.District)total+=DistrictReferenceDataRate(n);return total;}}
        bool PowerConsumer(GridNode n){return n.Kind==NetworkNodeKind.District||n.Kind==NetworkNodeKind.DataCore&&IsCenterBoosted(n.Id);}
        public int UnderpoweredCenterCount {get{int count=0;foreach(int id in boostedCenters)if(Nodes[id].PowerFraction<.98)count++;return count;}}
        public bool SetCommunicationPeaks(bool enabled)
        {
            if(!CanEdit||!Continuous||!Map.Generated||CommunicationPeaksEnabled==enabled)return false;
            CommunicationPeaksEnabled=enabled;CommunicationEpoch=enabled?Time:0;RefreshCommunicationDemand(true);dataBlocks.Clear();StableSeconds=0;offerCache=null;return true;
        }
        public bool CanBoostCenter(int node){return CanEdit&&Continuous&&Map.Generated&&node>=0&&node<Nodes.Length&&Nodes[node].Kind==NetworkNodeKind.DataCore&&!IsCenterBoosted(node)&&boostedCenters.Count<AvailableCenterBoosts;}
        public bool SetCenterBoost(int node,bool enabled)
        {
            if(enabled){if(!CanBoostCenter(node))return false;boostedCenters.Add(node);boostedCenters.Sort();}
            else{if(!CanEdit||!Continuous||!Map.Generated||!boostedCenters.Remove(node))return false;Nodes[node].PowerDemand=0;Nodes[node].DataSupply=7;}
            RefreshCommunicationDemand();AllocatePower();dataBlocks.Clear();StableSeconds=0;offerCache=null;return true;
        }
        void RefreshCommunicationDemand(bool restoreBase=false)
        {
            if(CommunicationPeaksEnabled||restoreBase||HasResearchDistrict){
                bool researchBurst=ResearchBurstActive,cityPeak=DataPeakActive;
                foreach(var n in Nodes)if(n.Kind==NetworkNodeKind.District){
                    if(n.DemandProfile==DistrictDemandProfile.Research)n.DataDemand=researchBurst?ResearchPeakData:ResearchBaseData;
                    else if(CommunicationPeaksEnabled||restoreBase)n.DataDemand=cityPeak?DistrictPeakDataRate(n.Role):DistrictDataRate(n.Role);
                }
            }
            foreach(int id in boostedCenters)Nodes[id].PowerDemand=CenterBoostPower;
        }
        void RefreshCenterOutput(){foreach(int id in boostedCenters)Nodes[id].DataSupply=7+4*Nodes[id].PowerFraction;}
        void ResetCommunicationEquipment(){boostedCenters.Clear();CommunicationEpoch=0;communicationReferenceStress=false;}
        void CopyCommunicationTo(NetworkSimulation copy){copy.CommunicationPeaksEnabled=CommunicationPeaksEnabled;copy.CommunicationEpoch=0;copy.boostedCenters.AddRange(boostedCenters);}
        void PrepareCommunicationReference()
        {
            if(!CommunicationPeaksEnabled&&!HasResearchDistrict)return;
            double capacity=0;foreach(var n in Nodes)if(n.Kind==NetworkNodeKind.DataCore)capacity+=NominalDataOutput(n.Id);
            foreach(var n in Nodes)if(capacity<PeakDataDemandTotal&&CanBoostCenter(n.Id)){SetCenterBoost(n.Id,true);capacity+=4;}
            if(batteries.Count<AvailableBatteries&&CanInstallBattery(5))InstallBattery(5);
            AllocatePower();RelieveCommunicationReference();
        }
        bool ValidateExpansionOperation(int legacySteps)
        {
            if(!ReferenceValid)return false;
            bool hasVariedDemand=sectors.Exists(sector=>Nodes[sector.District].DemandProfile!=DistrictDemandProfile.Standard);
            if(!CommunicationPeaksEnabled&&!hasVariedDemand){for(int i=0;i<legacySteps&&!ExpansionReady;i++)Step(1.0/60);return ExpansionReady;}
            // Sustained worst data demand plus two real electrical peaks and the recharge interval.
            communicationReferenceStress=true;
            for(int i=0;i<10800;i++)Step(1.0/60);
            return ExpansionReady&&RecentData>=.9&&RecentPower>=.98;
        }
    }
}

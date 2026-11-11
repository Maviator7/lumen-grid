using System;
using System.Collections.Generic;
namespace Michi.Core
{
    public enum ExpansionDirection { None, North, South, East, West }
    public enum SectorTerrain { Riverbank, Dense, Detour }
    public sealed class CityPoint {public double X,Z;public CityPoint(double x,double z){X=x;Z=z;}}
    public sealed class ExpansionOffer
    {
        public ExpansionDirection Direction;public SectorTerrain Terrain;public string UnavailableReason="安定運転できる経路を確保できません。";
        public CityPoint DistrictPoint,ReactorPoint,DataPoint;public readonly List<CityObstacle> Obstacles=new List<CityObstacle>();public List<CityPoint> DistrictRoute;
        public bool North,Available,Utilities;public int Role;public double Z,Demand,CableGrant;
        public string Title;public List<CityPoint> WestRoute,EastRoute;public int WestParent,EastParent;
    }
    public sealed class CitySector
    {
        public ExpansionDirection GrowthDirection;public ExpansionDirection Direction {get{return GrowthDirection==ExpansionDirection.None?(North?ExpansionDirection.North:ExpansionDirection.South):GrowthDirection;}}
        public SectorTerrain Terrain;public readonly List<CityObstacle> Obstacles=new List<CityObstacle>();public readonly List<int[]> DemandRoute=new List<int[]>();
        public bool North;public double Z;public int West,East,District,Reactor=-1,DataCore=-1;
        public readonly List<int[]> Feeders=new List<int[]>();
    }
    public sealed partial class NetworkSimulation
    {
        public int ExpansionCount {get;private set;}
        public double StableSeconds {get;private set;}
        public double RecentPower {get;private set;}
        public double RecentData {get;private set;}
        public double RecentLatency {get;private set;}
        public int RecentDelivered {get;private set;}
        public bool ExpansionReady {get{return Continuous&&StableSeconds>=45;}}
        public int DistrictCount {get{int count=0;foreach(var n in Nodes)if(n.Kind==NetworkNodeKind.District)count++;return count;}}
        public bool ReferenceValid {get;private set;}=true;
        readonly List<CitySector> sectors=new List<CitySector>();
        public IReadOnlyList<CitySector> Sectors {get{return sectors;}}
        double expansionCable,qualityClock;
        ExpansionOffer[] offerCache;
        sealed class QualitySample {public double Time,Power,Delay;public int Good,Bad;public int[] Dispatched,Delivered;}
        readonly List<QualitySample> samples=new List<QualitySample>();
        void ResetExpansionState(){ClearBatteries();ExpansionCount=0;expansionCable=0;sectors.Clear();offerCache=null;}
        void ResetQuality(){StableSeconds=qualityClock=0;RecentPower=PowerCoverage;RecentData=RecentLatency=0;RecentDelivered=0;samples.Clear();samples.Add(QualitySnapshot());}
        QualitySample QualitySnapshot(){var sample=new QualitySample{Time=Time,Power=powerIntegral,Delay=delaySum,Good=Successful,Bad=Expired,Dispatched=new int[Nodes.Length],Delivered=new int[Nodes.Length]};foreach(var n in Nodes){sample.Dispatched[n.Id]=n.DataDispatched;sample.Delivered[n.Id]=n.Delivered;}return sample;}
        void UpdateQuality(double dt)
        {
            qualityClock+=dt;if(qualityClock<1)return;double elapsed=qualityClock;qualityClock=0;
            samples.Add(QualitySnapshot());
            while(samples.Count>2&&samples[1].Time<=Time-15)samples.RemoveAt(0);
            var first=samples[0];int good=Successful-first.Good,bad=Expired-first.Bad;RecentDelivered=good;RecentPower=(powerIntegral-first.Power)/Math.Max(.001,Time-first.Time);RecentData=good+bad>0?(double)good/(good+bad):0;RecentLatency=good>0?(delaySum-first.Delay)/good:0;
            foreach(var n in Nodes){double elapsedWindow=Math.Max(.001,Time-first.Time);n.DataOutputRate=(n.DataDispatched-(n.Id<first.Dispatched.Length?first.Dispatched[n.Id]:0))/elapsedWindow;n.DataDeliveryRate=(n.Delivered-(n.Id<first.Delivered.Length?first.Delivered[n.Id]:0))/elapsedWindow;}
            bool all=true;foreach(var n in Nodes)if(n.Kind==NetworkNodeKind.District&&(n.LastDelivered<0||Time-n.LastDelivered>15||n.PowerFraction<.98))all=false;
            if(all&&PowerCoverage>=.98&&RecentPower>=.98&&RecentData>=.9&&RecentLatency<=2.8)StableSeconds=Math.Min(45,StableSeconds+elapsed);else StableSeconds=0;
        }
        public ExpansionOffer[] ExpansionOffers
        {
            get{if(offerCache==null)offerCache=new[]{BuildOffer(true),BuildOffer(false),BuildLateralOffer(ExpansionDirection.East),BuildLateralOffer(ExpansionDirection.West)};return offerCache;}
        }
        ExpansionOffer BuildOffer(bool north)
        {
            int wp=north?(Nodes[2].Z>Nodes[3].Z?2:3):(Nodes[2].Z<Nodes[3].Z?2:3),ep=north?(Nodes[5].Z>Nodes[6].Z?5:6):(Nodes[5].Z<Nodes[6].Z?5:6);
            foreach(var sector in sectors)if(sector.Direction==(north?ExpansionDirection.North:ExpansionDirection.South)){wp=sector.West;ep=sector.East;}
            double z=(north?Math.Max(Nodes[wp].Z,Nodes[ep].Z)+6:Math.Min(Nodes[wp].Z,Nodes[ep].Z)-6);
            var random=new CityRandom(unchecked(Map.Seed+ExpansionCount*7919+(north?103:407)));int role=7+random.Int(3);double demand=ModernRules?DistrictBasePower(role):role==7?6:role==8?3:4;
            var offer=new ExpansionOffer{Direction=north?ExpansionDirection.North:ExpansionDirection.South,Terrain=SectorTerrain.Riverbank,North=north,Z=z,Role=role,Demand=demand,Title=(north?"北":"南")+"の"+(role==7?"工業地区":role==8?"住宅地区":"医療地区"),WestParent=wp,EastParent=ep,Utilities=ExpansionCount%2==1};
            offer.WestRoute=PlanCorridor(wp,Nodes[2].X,z);offer.EastRoute=PlanCorridor(ep,Nodes[5].X,z);
            double outward=Math.Sign(Nodes[5].X-Nodes[2].X);offer.DistrictPoint=new CityPoint(Nodes[5].X+outward*4,z+1.2);
            if(offer.WestRoute==null||offer.EastRoute==null)return offer;
            offer.CableGrant=36+CorridorLength(wp,offer.WestRoute)+CorridorLength(ep,offer.EastRoute);
            // Validate before committing any change to the player's city.
            for(int attempt=0;attempt<2;attempt++){
                var test=PlanningClone();test.CommitExpansion(offer);test.LoadSolution();
                if(test.ReferenceValid){for(int i=0;i<7200&&!test.ExpansionReady;i++)test.Step(1.0/60);if(test.ExpansionReady){offer.Available=true;break;}}
                offer.Utilities=true;
            }
            return offer;
        }
        GridNode PointNode(CityPoint p){return new GridNode(-1,"","",p.X,p.Z,24,NetworkNodeKind.Relay);}
        bool LegalSegment(GridNode a,GridNode b){return Distance(a,b)<=MaxLinkLength+.00001&&!IsBlocked(a,b)&&!Map.CrossesRiver(a,b);}
        List<CityPoint> PlanCorridor(int parent,double x,double z)
        {
            var start=Nodes[parent];var target=new CityPoint(x,z);if(LegalSegment(start,PointNode(target)))return new List<CityPoint>{target};
            foreach(double shift in new[]{-2.0,2.0,-4.0,4.0,-6.0,6.0,-8.0,8.0}){
                var points=new List<CityPoint>{new CityPoint(start.X+shift,start.Z),new CityPoint(start.X+shift,z),target};var last=start;bool safe=true;
                foreach(var p in points){var next=PointNode(p);if(Math.Abs(next.X-Map.RiverX)<Map.RiverWidth/2+.4||!LegalSegment(last,next))safe=false;last=next;}
                if(safe)return points;
            }
            return null;
        }
        double CorridorLength(int parent,List<CityPoint> points){double total=0;var last=Nodes[parent];foreach(var p in points){var next=PointNode(p);total+=Distance(last,next);last=next;}return total;}
        NetworkSimulation PlanningClone()
        {
            var copy=new NetworkSimulation(Map,true,RulesVersion);copy.Nodes=new GridNode[Nodes.Length];
            for(int i=0;i<Nodes.Length;i++){var n=Nodes[i];copy.Nodes[i]=new GridNode(n.Id,n.Code,n.Name,n.X,n.Z,n.Capacity,n.Kind){Role=n.Role,PowerDemand=n.PowerDemand,PowerSupply=n.PowerSupply,DataDemand=n.DataDemand,DataSupply=n.DataSupply};}
            foreach(var b in batteries)copy.AddBattery(new BatteryState(b.Node));copy.nodeCredit=new double[Nodes.Length];copy.ExpansionCount=ExpansionCount;copy.expansionCable=expansionCable;foreach(var sector in sectors)copy.sectors.Add(CopySector(sector));copy.Links.Clear();foreach(var l in Links.Values)copy.Add(l.A,l.B,l.Kind);copy.ReturnToDesign();copy.BeginRunning();return copy;
        }
        int AppendNode(string code,string name,double x,double z,NetworkNodeKind kind,int role,double demand=0)
        {
            int id=Nodes.Length;var grown=new GridNode[id+1];Array.Copy(Nodes,grown,id);grown[id]=new GridNode(id,code,name,x,z,kind==NetworkNodeKind.District?100:kind==NetworkNodeKind.Reactor?24:32,kind){Role=role,PowerDemand=demand};Nodes=grown;ConfigureMainNode(grown[id]);Array.Resize(ref nodeCredit,Nodes.Length);return id;
        }
        int AddCorridor(int parent,List<CityPoint> points,CitySector sector,string prefix,bool numbered=false)
        {
            int last=parent,index=0;foreach(var p in points){int next=AppendNode(prefix+"."+Nodes.Length,prefix+(numbered?"-"+(++index):"")+" 中継",p.X,p.Z,NetworkNodeKind.Relay,2);Add(last,next,LinkKind.Shared);sector.Feeders.Add(new[]{last,next});last=next;}return last;
        }
        void CommitExpansion(ExpansionOffer offer)
        {
            if(offer.Direction==ExpansionDirection.East||offer.Direction==ExpansionDirection.West){CommitLateralExpansion(offer);return;}
            expansionCable+=offer.CableGrant;ExpansionCount++;string prefix=(offer.North?"北":"南")+ExpansionCount;
            var sector=new CitySector{GrowthDirection=offer.Direction,Terrain=offer.Terrain,North=offer.North,Z=offer.Z};sector.West=AddCorridor(offer.WestParent,offer.WestRoute,sector,prefix+"西");sector.East=AddCorridor(offer.EastParent,offer.EastRoute,sector,prefix+"東");
            double direction=Math.Sign(Nodes[5].X-Nodes[2].X),x=Nodes[sector.East].X+direction*4;
            sector.District=AppendNode(prefix+".D",prefix+(offer.Role==7?" 工業区":offer.Role==8?" 居住区":" 医療区"),x,offer.Z+1.2,NetworkNodeKind.District,offer.Role,offer.Demand);
            if(offer.Utilities){double ux=Nodes[sector.West].X-direction*3;sector.Reactor=AppendNode(prefix+".P",prefix+" 発電所",Nodes[sector.East].X+direction*3,offer.Z-1.8,NetworkNodeKind.Reactor,0);sector.DataCore=AppendNode(prefix+".I",prefix+" データセンター",ux,offer.Z+1.8,NetworkNodeKind.DataCore,1);}
            Nodes[0].PowerSupply=18+ExpansionCount*4;Nodes[0].Capacity=Nodes[0].PowerSupply+6;sectors.Add(sector);history.Clear();offerCache=null;RefreshLiveNetwork();
        }
        public bool Expand(bool north){return Expand(north?ExpansionDirection.North:ExpansionDirection.South);}
        public bool Expand(ExpansionDirection direction)
        {
            if(!ExpansionReady||direction<ExpansionDirection.North||direction>ExpansionDirection.West)return false;
            var offer=ExpansionOffers[(int)direction-1];if(!offer.Available)return false;CommitExpansion(offer);return true;
        }
        void ReferenceConnect(int a,int b,LinkKind kind){if(Connect(a,b,kind)!=NetworkEditError.None)ReferenceValid=false;}
        void ApplySectorReference()
        {
            foreach(var sector in sectors){if(sector.Direction==ExpansionDirection.East||sector.Direction==ExpansionDirection.West){ApplyLateralReference(sector);continue;}foreach(var e in sector.Feeders)ReferenceConnect(e[0],e[1],e[1]==sector.East?LinkKind.PowerOnly:LinkKind.Shared);ReferenceConnect(sector.West,sector.East,LinkKind.DataOnly);ReferenceConnect(sector.East,sector.District,LinkKind.Shared);
                if(sector.Reactor>=0){ReferenceConnect(sector.Reactor,sector.East,LinkKind.Shared);ReferenceConnect(sector.DataCore,sector.West,LinkKind.Shared);}

            }
        }
    }
}

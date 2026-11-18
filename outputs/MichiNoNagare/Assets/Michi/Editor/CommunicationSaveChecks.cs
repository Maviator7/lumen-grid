using System;
using UnityEngine;
using Michi.Core;
using Michi.Runtime;
namespace Michi.Checks
{
    public static class CommunicationSaveChecks
    {
        static int checks;
        static void Check(bool ok,string why){checks++;if(!ok)throw new Exception("COMMUNICATION JSON: "+why);}
        public static string Run()
        {
            checks=0;var s=new NetworkSimulation(CityMap.Generate(42),true,1);s.LoadSolution();for(int i=0;i<900;i++)s.Step(1.0/60);s.SetCommunicationPeaks(true);s.SetCenterBoost(1,true);s.InstallBattery(5);for(int i=0;i<5700;i++)s.Step(1.0/60);
            var city=new SavedCity{Version=1,SavedAt=DateTime.UtcNow.ToString("O"),Network=s.CaptureSave(),Angle=0,Zoom=1,Paused=true,Speed=4,Tool=4};
            string envelope=CitySaveStore.Encode(city);var copy=NetworkSimulation.RestoreSave(CitySaveStore.Decode(envelope).Network);
            Check(copy.CommunicationPeaksEnabled&&copy.DataPeakActive&&copy.IsCenterBoosted(1),"mode, real epoch and upgrade survive JSON");Check(Math.Abs(copy.DataPeakChangeIn-s.DataPeakChangeIn)<1e-8,"exact countdown within JSON rounding");Check(Math.Abs(copy.BatteryAt(5).Energy-s.BatteryAt(5).Energy)<1e-8,"equipment energy kept");
            for(int i=0;i<600;i++){s.Step(1.0/60);copy.Step(1.0/60);Check(Math.Abs(s.Nodes[1].DataSupply-copy.Nodes[1].DataSupply)<1e-8&&s.Successful==copy.Successful&&s.Expired==copy.Expired,"resumed effective output and packets");}
            var legacy=new NetworkSimulation(CityMap.Generate(291026),true);legacy.LoadSolution();string old=JsonUtility.ToJson(legacy.CaptureSave()).Replace("\"CommunicationPeaksEnabled\":false,","").Replace("\"CommunicationEpoch\":0.0,","").Replace("\"CommunicationEpoch\":0,","").Replace("\"BoostedCenters\":[],","");
            var restored=NetworkSimulation.RestoreSave(JsonUtility.FromJson<NetworkSave>(old));Check(!old.Contains("\"CommunicationPeaksEnabled\"")&&!old.Contains("\"BoostedCenters\""),"actual fields omitted");Check(!restored.CommunicationPeaksEnabled&&restored.BoostedCenters.Count==0&&restored.Nodes[1].DataSupply==7,"legacy stays opt-out");
            var research=new NetworkSimulation(CityMap.Generate(291026),true,1);research.LoadSolution();for(int i=0;i<6060;i++)research.Step(1.0/60);
            Check(research.Expand(ExpansionDirection.East),"research district available for JSON check");
            int researchId=research.Sectors[0].District;
            var researchSave=research.CaptureSave();
            var researchCopy=NetworkSimulation.RestoreSave(JsonUtility.FromJson<NetworkSave>(JsonUtility.ToJson(researchSave)));
            Check(researchCopy.Nodes[researchId].DemandProfile==DistrictDemandProfile.Research&&researchCopy.ResearchBurstActive&&researchCopy.Nodes[researchId].DataDemand==5,"research contract and independent clock survive JSON");
            researchSave.Nodes[researchId].DataDemand=1.5;
            string omittedProfile=System.Text.RegularExpressions.Regex.Replace(JsonUtility.ToJson(researchSave),"\"DemandProfile\":[0-9]+,","");
            var standardCopy=NetworkSimulation.RestoreSave(JsonUtility.FromJson<NetworkSave>(omittedProfile));
            Check(!omittedProfile.Contains("\"DemandProfile\"")&&!standardCopy.HasResearchDistrict&&standardCopy.Nodes[researchId].DemandProfile==DistrictDemandProfile.Standard&&standardCopy.Nodes[researchId].DataDemand==1.5,"older modern saves retain standard expansion demand");
            string fixture=Environment.GetEnvironmentVariable("MICHI_COMMUNICATION_FIXTURE");if(!string.IsNullOrEmpty(fixture)){var preview=new NetworkSimulation(CityMap.Generate(42),true,1);preview.LoadSolution();var data=new SavedCity{Version=1,SavedAt=DateTime.UtcNow.ToString("O"),Network=preview.CaptureSave(),Angle=0,Zoom=.9f,TopDown=false,Paused=true,Speed=4,Tool=4};System.IO.File.WriteAllText(fixture,CitySaveStore.Encode(data));}
            return "PASS: "+checks+" communication JSON compatibility checks";
        }
    }
}

using System;
using UnityEngine;
using Michi.Core;
using Michi.Runtime;
namespace Michi.Checks
{
    public static class BatterySaveChecks
    {
        static int checks;
        static void Check(bool ok,string why){checks++;if(!ok)throw new Exception("BATTERY JSON: "+why);}
        public static string Run()
        {
            checks=0;var s=new NetworkSimulation(CityMap.Generate(42),true,1);s.LoadSolution();Check(s.InstallBattery(5),"install");for(int i=0;i<600;i++)s.Step(1.0/60);
            var city=new SavedCity{Version=1,SavedAt=DateTime.UtcNow.ToString("O"),Network=s.CaptureSave(),Angle=-18,Zoom=1,Paused=true,Speed=2,Tool=4};
            var json=CitySaveStore.Encode(city);var copy=NetworkSimulation.RestoreSave(CitySaveStore.Decode(json).Network);
            Check(copy.Batteries.Count==1&&copy.BatteryAt(5)!=null,"equipment survives envelope JSON");Check(Math.Abs(copy.BatteryAt(5).Energy-s.BatteryAt(5).Energy)<1e-9&&copy.BatteryAt(5).Energy>0,"charge survives: before="+s.BatteryAt(5).Energy.ToString("R")+" after="+copy.BatteryAt(5).Energy.ToString("R"));
            for(int i=0;i<120;i++){s.Step(1.0/60);copy.Step(1.0/60);Check(Math.Abs(s.BatteryAt(5).Energy-copy.BatteryAt(5).Energy)<1e-9&&s.Successful==copy.Successful,"resumed storage and traffic match");}
            s.RemoveBattery(5);string old=JsonUtility.ToJson(s.CaptureSave()).Replace("\"Batteries\":[],","");Check(!old.Contains("\"Batteries\""),"actual omitted battery field");
            var legacy=NetworkSimulation.RestoreSave(JsonUtility.FromJson<NetworkSave>(old));Check(legacy.Batteries.Count==0&&legacy.Links.Count==s.Links.Count,"legacy city is unchanged");
            string fixture=Environment.GetEnvironmentVariable("MICHI_BATTERY_FIXTURE");
            if(!string.IsNullOrEmpty(fixture)){var preview=new NetworkSimulation(CityMap.Generate(42),true,1);preview.Nodes[0].PowerSupply=14;preview.LoadSolution();var data=new SavedCity{Version=1,SavedAt=DateTime.UtcNow.ToString("O"),Network=preview.CaptureSave(),Angle=0,Zoom=.9f,TopDown=true,Paused=true,Speed=4,Tool=4};System.IO.File.WriteAllText(fixture,CitySaveStore.Encode(data));}
            return "PASS: "+checks+" battery JSON compatibility checks";
        }
    }
}

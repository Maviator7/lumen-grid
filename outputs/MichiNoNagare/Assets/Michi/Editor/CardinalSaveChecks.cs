using System;
using System.Text.RegularExpressions;
using UnityEngine;
using Michi.Core;
using Michi.Runtime;
namespace Michi.Checks
{
    public static class CardinalSaveChecks
    {
        static int checks;
        static void Check(bool ok,string message){checks++;if(!ok)throw new Exception("CARDINAL JSON: "+message);}
        static void Stable(NetworkSimulation s){for(int i=0;i<15000&&!s.ExpansionReady;i++)s.Step(1.0/60);Check(s.ExpansionReady,"test setup ready");}
        public static string Run()
        {
            checks=0;var s=new NetworkSimulation(CityMap.Generate(664020296),true,1);s.LoadSolution();Stable(s);Check(s.Expand(ExpansionDirection.East),"lateral setup");
            var city=new SavedCity{Version=1,SavedAt=DateTime.UtcNow.ToString("O"),Network=s.CaptureSave(),Angle=-18,Zoom=1,Paused=true,Speed=4,Tool=4};
            string envelope=CitySaveStore.Encode(city);var decoded=CitySaveStore.Decode(envelope);var restored=NetworkSimulation.RestoreSave(decoded.Network);
            var a=s.Sectors[0];var b=restored.Sectors[0];Check(b.Direction==ExpansionDirection.East&&b.Terrain==a.Terrain,"real JSON keeps terrain and exact direction");Check(b.Obstacles.Count==a.Obstacles.Count&&b.DemandRoute.Count==a.DemandRoute.Count,"real JSON keeps rectangles and route edges");
            for(int i=0;i<a.Obstacles.Count;i++)Check(a.Obstacles[i].X==b.Obstacles[i].X&&a.Obstacles[i].Z==b.Obstacles[i].Z&&a.Obstacles[i].Width==b.Obstacles[i].Width&&a.Obstacles[i].Depth==b.Obstacles[i].Depth,"real JSON keeps exact restriction geometry");
            Check(restored.Links.Count==s.Links.Count&&restored.CableCost==s.CableCost,"immediately after expansion remains saveable");restored.LoadSolution();Stable(restored);
            var old=new NetworkSimulation(CityMap.Generate(291026),true);old.LoadSolution();Stable(old);old.Expand(false);
            string legacy=JsonUtility.ToJson(old.CaptureSave());legacy=Regex.Replace(legacy,"\\\"Direction\\\":\\d+,","");legacy=Regex.Replace(legacy,"\\\"Terrain\\\":\\d+,","");legacy=legacy.Replace("\"Obstacles\":[],","").Replace("\"DemandRoute\":[],","");
            var older=NetworkSimulation.RestoreSave(JsonUtility.FromJson<NetworkSave>(legacy));Check(older.Sectors[0].Direction==ExpansionDirection.South,"actual omitted-field legacy JSON infers south");Check(older.Sectors[0].Obstacles.Count==0&&older.Nodes.Length==old.Nodes.Length,"legacy layout is unchanged");
            return "PASS: "+checks+" cardinal JSON compatibility checks";
        }
    }
}

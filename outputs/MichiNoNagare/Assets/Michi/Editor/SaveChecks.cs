using System;
using Michi.Core;
namespace Michi.Checks
{
    public static class SaveChecks
    {
        static int checks;
        static void Check(bool ok,string why){checks++;if(!ok)throw new Exception("SAVE: "+why);}
        static void Advance(NetworkSimulation s,int frames){for(int i=0;i<frames;i++)s.Step(1.0/60);}
        static NetworkSimulation RoundTrip(NetworkSimulation s){var state=s.CaptureSave();
#if UNITY_EDITOR
            state=UnityEngine.JsonUtility.FromJson<NetworkSave>(UnityEngine.JsonUtility.ToJson(state));
#endif
            return NetworkSimulation.RestoreSave(state);}
        public static string Run()
        {
            checks=0;var s=new NetworkSimulation(CityMap.Generate(291026),true);s.LoadSolution();for(int sector=0;sector<2;sector++){for(int i=0;i<10800&&!s.ExpansionReady;i++)s.Step(1.0/60);Check(RoundTrip(s).ExpansionReady,"ready-to-expand stable progress preserved");Check(s.Expand(sector==0),"expansion setup");s.LoadSolution();}
            var last=s.Sectors[s.Sectors.Count-1];s.BeginEdit();s.Remove(NetworkSimulation.EdgeId(last.DataCore,last.West));s.EndEdit();Advance(s,901);Check(s.WaitingData>0&&s.Packets.Count>0,"overloaded city has queues and in-flight packets");
            var restored=RoundTrip(s);Check(restored.Time==s.Time&&restored.ExpansionCount==2,"time and expansions restored");Check(restored.AvailableCable==s.AvailableCable&&restored.AvailableDedicated==s.AvailableDedicated&&restored.AvailableBridges==s.AvailableBridges,"all budgets restored");Check(restored.Nodes.Length==s.Nodes.Length&&restored.Links.Count==s.Links.Count&&restored.WaitingData==s.WaitingData,"expanded graph and pending work restored");
            for(int i=0;i<600;i++){s.Step(1.0/60);restored.Step(1.0/60);Check(s.Successful==restored.Successful&&s.Expired==restored.Expired&&s.Packets.Count==restored.Packets.Count&&s.WaitingData==restored.WaitingData,"same demand outcomes after resume");Check(Math.Abs(s.RecentLatency-restored.RecentLatency)<1e-10&&s.StableSeconds==restored.StableSeconds,"quality history continues");}
            var captured=s.CaptureSave();double x=captured.Nodes[0].X;s.Nodes[0].X+=1;Check(captured.Nodes[0].X==x,"snapshot is independent of future edits");s.Nodes[0].X=x;
            var bad=s.CaptureSave();bad.Version=999;bool rejected=false;try{NetworkSimulation.RestoreSave(bad);}catch(ArgumentException){rejected=true;}Check(rejected,"unknown version rejected");bad=s.CaptureSave();bad.Links[0].A=999999;rejected=false;try{NetworkSimulation.RestoreSave(bad);}catch(ArgumentException){rejected=true;}Check(rejected,"broken graph rejected");
            return "PASS: "+checks+" save checks";
        }
#if SAVE_STANDALONE
        public static void Main(){Console.WriteLine(Run());}
#endif
    }
}

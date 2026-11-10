using System;
using System.Collections.Generic;
using Michi.Core;
namespace Michi.Checks
{
    public static class ContinuousCityChecks
    {
        static int checks;
        static void Check(bool value,string message){checks++;if(!value)throw new Exception(message);}
        static void Stabilize(NetworkSimulation s){for(int i=0;i<9000&&!s.ExpansionReady;i++)s.Step(1.0/60);Check(s.ExpansionReady,"Reference city must stabilize: "+s.Map.Seed+" expansion "+s.ExpansionCount);}
        public static string Run()
        {
            checks=0;
            foreach(int seed in new[]{291026,42,-19,0,12345,-2147483648}){
                var s=new NetworkSimulation(CityMap.Generate(seed),true);Check(s.Phase==RoundPhase.Running,"Main city starts live");Check(!s.Expand(true),"Unstable expansion rejected");s.LoadSolution();Stabilize(s);
                for(int round=0;round<6;round++){
                    double time=s.Time,cable=s.AvailableCable;int good=s.Successful,oldCount=s.Nodes.Length,districts=s.DistrictCount;var links=new Dictionary<int,LinkKind>();foreach(var e in s.Links.Values)links.Add(e.Id,e.Kind);
                    var offers=s.ExpansionOffers;Check(s.Nodes.Length==oldCount&&s.Time==time,"Planning never mutates live city");Check(offers[0].Available&&offers[1].Available,"Both directions have validated solutions: "+seed+" round "+round);
                    Check(s.Expand(round%2==0),"Ready expansion succeeds");Check(s.Time==time&&s.Successful==good,"Expansion preserves running history");Check(s.Nodes.Length>oldCount&&s.DistrictCount==districts+1,"Adds one district");Check(s.StableSeconds==0&&!s.ExpansionReady,"New demand requires stability again");
                    Check(s.AvailableCable>cable&&s.AvailableDedicated==3+2*(round+1)&&s.AvailableBridges==s.Map.BridgeLimit+round+1,"Growing budgets");foreach(var e in links)Check(s.Links.ContainsKey(e.Key)&&s.Links[e.Key].Kind==e.Value,"Old wiring preserved");
                    int district=s.Sectors[round].District;Check(s.Path(1,district,SignalKind.Data)==null,"New district starts disconnected");s.LoadSolution();Check(s.ReferenceValid,"Reference wiring obeys constraints");Stabilize(s);
                }
                double before=s.Time;int delivered=s.Successful;s.BeginEdit();s.Paint(NetworkSimulation.EdgeId(2,4),LinkKind.Shared);s.EndEdit();Check(s.Time==before&&s.Successful==delivered,"Live edit preserves history");Check(s.Undo()&&s.Time==before,"Live undo preserves time");
                for(int i=0;i<3600;i++)s.Step(1.0/60);Check(s.Phase==RoundPhase.Running&&s.Time>45&&!s.Passed,"No terminal clear state");
                s.LoadInitial();Check(s.Nodes.Length==10&&s.DistrictCount==3&&s.ExpansionCount==0&&s.Time==0,"Reset returns seed to original sector");Check(s.AvailableDedicated==3&&s.AvailableBridges==s.Map.BridgeLimit,"Reset restores budgets");
            }
            var tutorial=new NetworkSimulation();tutorial.LoadSolution();tutorial.Start();for(int i=0;i<2800;i++)tutorial.Step(1.0/60);Check(tutorial.Phase==RoundPhase.Result&&tutorial.Passed,"Tutorial remains a fixed 45 second lesson");
            return "PASS: "+checks+" continuous city checks; 6 seeds expanded 6 times each";
        }
#if CONTINUOUS_STANDALONE
        public static void Main(){Console.WriteLine(Run());}
#endif
    }
}

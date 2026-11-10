using System;
using Michi.Core;
namespace Michi.Checks
{
    public static class StrategyChecks
    {
        static int checks;
        static void Check(bool ok,string why){checks++;if(!ok)throw new Exception("STRATEGY: "+why);}
        static void Advance(NetworkSimulation s,double seconds){for(int i=0;i<(int)Math.Ceiling(seconds*60);i++)s.Step(1.0/60);}
        static CityMap TestMap(double cable,int role=9)
        {
            var nodes=CityMap.Tutorial().CreateNodes();
            for(int i=0;i<10;i++){nodes[i].Kind=NetworkNodeKind.Relay;nodes[i].Capacity=100;nodes[i].PowerDemand=0;nodes[i].X=i;nodes[i].Z=20;}
            nodes[0].Kind=NetworkNodeKind.Reactor;nodes[0].X=36;nodes[0].Z=2;
            nodes[1].Kind=NetworkNodeKind.DataCore;nodes[1].X=0;nodes[1].Z=0;
            nodes[2].X=9;nodes[2].Z=0;nodes[3].X=18;nodes[3].Z=0;nodes[4].X=27;nodes[4].Z=0;
            nodes[9].Kind=NetworkNodeKind.District;nodes[9].Role=role;nodes[9].X=36;nodes[9].Z=0;nodes[9].PowerDemand=3;
            return new CityMap(291026,true,nodes,new int[0][],-100,1,cable,99,new CityObstacle[0]);
        }
        static NetworkSimulation LongRoute(int rules,int role)
        {
            var s=new NetworkSimulation(TestMap(500,role),true,rules);s.BeginEdit();
            foreach(var e in new[]{new[]{0,9},new[]{1,2},new[]{2,3},new[]{3,4},new[]{4,9}})Check(s.Connect(e[0],e[1],LinkKind.Shared)==NetworkEditError.None,"controlled route legal");
            s.EndEdit();return s;
        }
        public static string Run()
        {
            checks=0;
            var legacy=new NetworkSimulation(CityMap.Generate(42),true);legacy.LoadSolution();Advance(legacy,30);
            var old=legacy.CaptureSave();Check(old.RulesVersion==0,"old default remains classic");
            var restored=NetworkSimulation.RestoreSave(old);Check(restored.RulesVersion==0&&restored.CableCost==legacy.CableLength,"old save keeps costs");
            for(int i=0;i<600;i++){legacy.Step(1.0/60);restored.Step(1.0/60);Check(legacy.Successful==restored.Successful&&legacy.Expired==restored.Expired&&legacy.PowerCoverage==restored.PowerCoverage,"old save deterministic");}
            var small=new NetworkSimulation(TestMap(8),true,1);small.BeginEdit();Check(small.Connect(1,2,LinkKind.Shared)==NetworkEditError.None,"cheap shared route fits");small.EndEdit();
            var estimate=small.PreviewConnect(1,2,LinkKind.PowerOnly);Check(estimate.Error==NetworkEditError.CableBudget&&Math.Abs(estimate.AddedCost-6.75)<.00001,"conversion includes surcharge and blocks before mutation");
            Check(small.Paint(NetworkSimulation.EdgeId(1,2),LinkKind.PowerOnly)==NetworkEditError.CableBudget&&small.Links[NetworkSimulation.EdgeId(1,2)].Kind==LinkKind.Shared,"failed conversion leaves wiring intact");
            var budget=new NetworkSimulation(TestMap(100),true,1);budget.BeginEdit();budget.Connect(1,2,LinkKind.Shared);budget.EndEdit();double cheap=budget.CableCost;
            budget.BeginEdit();Check(budget.Paint(NetworkSimulation.EdgeId(1,2),LinkKind.DataOnly)==NetworkEditError.None,"upgrade legal");budget.EndEdit();Check(Math.Abs(budget.CableCost-cheap*1.75)<1e-8,"dedicated costs 1.75x");
            Check(Math.Abs(budget.PreviewConnect(1,2,LinkKind.Shared).AddedCost+cheap*.75)<1e-8,"downgrade refunds surcharge");Check(budget.Undo()&&Math.Abs(budget.CableCost-cheap)<1e-8,"undo restores cost");
            budget.BeginEdit();budget.Remove(NetworkSimulation.EdgeId(1,2));budget.EndEdit();Check(budget.CableCost==0,"remove returns all cost");
            var medical=LongRoute(1,9);Advance(medical,20);Check(medical.Expired>0&&medical.Successful==0,"medical rejects >2.8s route");
            var factory=LongRoute(1,7);Advance(factory,20);Check(factory.Successful>30,"factory can accept longer supply path");
            var classic=LongRoute(0,9);Advance(classic,20);Check(classic.Successful>30,"legacy medical deadline unchanged");
            var housing=new NetworkSimulation(TestMap(100,8),true,1);Check(housing.Nodes[9].PowerDemand==3,"housing off-peak 3MW");Advance(housing,60.1);Check(housing.Nodes[9].PowerDemand==5&&housing.HousingPeak,"predictable 5MW peak");housing.LoadInitial();Check(housing.Nodes[9].PowerDemand==3&&!housing.HousingPeak,"reset clears peak demand while paused");Advance(housing,90.1);Check(housing.Nodes[9].PowerDemand==3&&!housing.HousingPeak,"peak ends after 30s");
            foreach(int seed in new[]{291026,42,-19,0,12345,int.MinValue,621576904,2147483647}){
                var s=new NetworkSimulation(CityMap.Generate(seed),true,1);s.LoadSolution();Check(s.ReferenceValid,"new reference legal seed="+seed+" cost="+s.CableCost+" budget="+s.CableCostLimit);Advance(s,180);
                Check(s.ExpansionReady&&s.RecentData>=.9,"new city survives peaks seed="+seed+" "+s.Summary());
                for(int expansion=0;expansion<4;expansion++){
                    Check(s.ExpansionOffers[0].Available&&s.ExpansionOffers[1].Available,"both new-rule offers solvable seed="+seed+" expansion="+expansion);
                    Check(s.Expand(expansion%2==0),"new expansion allowed");s.LoadSolution();Check(s.ReferenceValid,"expanded budget legal");Advance(s,180);Check(s.ExpansionReady,"expanded peak stable seed="+seed+" expansion="+expansion+" "+s.Summary());
                }
                var saved=s.CaptureSave();var copy=NetworkSimulation.RestoreSave(saved);Check(copy.RulesVersion==1&&copy.CableCost==s.CableCost,"new rules saved");
                for(int i=0;i<600;i++){s.Step(1.0/60);copy.Step(1.0/60);Check(s.Successful==copy.Successful&&s.Expired==copy.Expired&&s.PowerCoverage==copy.PowerCoverage,"new save resumes same demand");}
                saved.RulesVersion=999;bool rejected=false;try{NetworkSimulation.RestoreSave(saved);}catch(ArgumentException){rejected=true;}Check(rejected,"unknown rule rejected");
            }
            return "PASS: "+checks+" strategy checks (legacy, costs, deadlines, peaks, 8 seeds x 4 expansions, save resume)";
        }
#if STRATEGY_STANDALONE
        public static void Main(){Console.WriteLine(Run());}
#endif
    }
}

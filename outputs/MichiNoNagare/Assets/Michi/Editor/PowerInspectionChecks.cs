using System;
using Michi.Core;
namespace Michi.Checks
{
    public static class PowerInspectionChecks
    {
        static int checks;
        static void Check(bool ok,string reason){checks++;if(!ok)throw new Exception("POWER INSPECTION: "+reason);}
        static NetworkSimulation Branches(double firstSupply=18,bool second=false)
        {
            var nodes=CityMap.Tutorial().CreateNodes();
            for(int i=0;i<nodes.Length;i++){nodes[i].X=0;nodes[i].Z=20+i;nodes[i].PowerDemand=0;}
            nodes[0].X=0;nodes[0].Z=0;nodes[0].PowerSupply=firstSupply;
            nodes[2].X=2;nodes[2].Z=0;nodes[3].X=2;nodes[3].Z=2;
            nodes[7].X=4;nodes[7].Z=0;nodes[7].PowerDemand=7;
            nodes[1].X=4;nodes[1].Z=2;if(second){nodes[1].Kind=NetworkNodeKind.Reactor;nodes[1].PowerSupply=3;}
            int[][] edges=second?new[]{new[]{0,2},new[]{2,7},new[]{0,3},new[]{3,7},new[]{1,7}}:new[]{new[]{0,2},new[]{2,7},new[]{0,3},new[]{3,7}};
            var sim=new NetworkSimulation(new CityMap(42,true,nodes,edges,-100,1,100,99,new CityObstacle[0]),true);
            sim.Nodes[0].PowerSupply=firstSupply;if(second)sim.Nodes[1].PowerSupply=3;
            sim.BeginEdit();sim.Paint(NetworkSimulation.EdgeId(0,2),LinkKind.PowerOnly);sim.EndEdit();return sim;
        }
        static double SourceAmount(PowerInspection info,int id){foreach(var source in info.Sources)if(source.Source==id)return source.Amount;return 0;}
        public static string Run()
        {
            checks=0;var s=Branches();
            var active=s.InspectPower(2);Check(active.State==PowerUse.InUse&&active.Throughput==7&&SourceAmount(active,0)==7,"active relay reports actual source and MW");
            var standby=s.InspectPower(3);Check(standby.State==PowerUse.Standby&&standby.Throughput==0&&!standby.SourcesPassThroughSelection&&SourceAmount(standby,0)==7,"same source on another route is not misreported as another reactor");
            double time=s.Time,coverage=s.PowerCoverage;int edges=s.Links.Count;for(int i=0;i<100;i++)s.InspectPower(i%10);Check(s.Time==time&&s.PowerCoverage==coverage&&s.Links.Count==edges,"inspection does not edit or advance city");
            s.BeginEdit();s.Remove(NetworkSimulation.EdgeId(0,2));s.EndEdit();Check(s.InspectPower(3).State==PowerUse.InUse&&s.InspectPower(3).Throughput==7,"editing invalidates old diagnostics");Check(s.Undo()&&s.InspectPower(3).State==PowerUse.Standby,"undo recomputes actual flows");
            var noSource=Branches();noSource.BeginEdit();noSource.Remove(NetworkSimulation.EdgeId(0,2));noSource.Remove(NetworkSimulation.EdgeId(0,3));noSource.EndEdit();Check(noSource.InspectPower(2).State==PowerUse.NoSource,"unpowered input distinguished");
            var noDestination=Branches();noDestination.BeginEdit();noDestination.Remove(NetworkSimulation.EdgeId(2,7));noDestination.Remove(NetworkSimulation.EdgeId(3,7));noDestination.EndEdit();Check(noDestination.InspectPower(2).State==PowerUse.NoDestination,"reactor connection alone does not mean power flows");
            var limited=Branches(3);Check(limited.InspectPower(3).State==PowerUse.SupplyLimited,"does not claim downstream supply is satisfied when source capacity is short");
            var unpoweredDistrict=Branches();unpoweredDistrict.Nodes[8].X=6;unpoweredDistrict.Nodes[8].Z=0;unpoweredDistrict.Nodes[8].PowerDemand=3;
            unpoweredDistrict.BeginEdit();unpoweredDistrict.Connect(7,8,LinkKind.Shared);unpoweredDistrict.EndEdit();
            Check(unpoweredDistrict.Nodes[8].PowerReceived==0&&unpoweredDistrict.Nodes[7].PowerReceived==7,"district-to-district wire does not carry through a district");
            Check(unpoweredDistrict.InspectPower(8).Sources.Count==0,"unpowered district must not list a neighbor district's source or MW");
            var split=Branches(18,true);var district=split.InspectPower(7);Check(district.Sources.Count==2&&SourceAmount(district,1)==3&&SourceAmount(district,0)==4,"reports real capacity-based split rather than only closest reactor");Check(split.InspectPower(2).Sources.Count==1&&SourceAmount(split.InspectPower(2),0)==4,"relay only lists flows actually through it");
            var live=new NetworkSimulation(CityMap.Generate(664020296),true,1);live.LoadSolution();for(int i=0;i<10000&&!live.ExpansionReady;i++)live.Step(1.0/60);Check(live.Expand(false),"reproduction city expands south");live.LoadSolution();
            var reported=live.InspectPower(2);int south=live.Sectors[0].Reactor;Check(reported.State==PowerUse.Standby&&reported.Throughput==0&&SourceAmount(reported,south)>0,"reported seed: original relay idle while southern reactor supplies districts");
            var restored=NetworkSimulation.RestoreSave(live.CaptureSave());var read=restored.InspectPower(2);Check(read.State==reported.State&&read.Throughput==reported.Throughput&&SourceAmount(read,south)==SourceAmount(reported,south),"derived flows recomputed after loading existing save format");
            double total=0;foreach(var f in live.PowerFlows){Check(f.Amount>0&&f.Path[0]==f.Source&&f.Path[f.Path.Count-1]==f.District,"flow path has real endpoints");total+=f.Amount;}
            double received=0;foreach(var node in live.Nodes)if(node.Kind==NetworkNodeKind.District)received+=node.PowerReceived;Check(total==received,"flow accounting equals delivered power");
            return "PASS: "+checks+" power inspection checks";
        }
#if POWER_INSPECTION_STANDALONE
        public static void Main(){Console.WriteLine(Run());}
#endif
    }
}

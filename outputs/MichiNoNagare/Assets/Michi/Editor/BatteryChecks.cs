using System;
using Michi.Core;
namespace Michi.Checks
{
    public static class BatteryChecks
    {
        static int checks;
        static void Check(bool ok,string why){checks++;if(!ok)throw new Exception("BATTERY: "+why);}
        static void Near(double a,double b,string why){Check(Math.Abs(a-b)<.00001,why+" actual="+a+" expected="+b);}
        // Fixture-only seeding; live state remains publicly read-only, including in the separate Editor assembly.
        static void SetEnergy(BatteryState b,double energy){typeof(BatteryState).GetProperty("Energy").GetSetMethod(true).Invoke(b,new object[]{energy});}
        static void Advance(NetworkSimulation s,double seconds){for(int i=0;i<(int)Math.Round(seconds*60);i++)s.Step(1.0/60);}
        static NetworkSimulation Fixture(int rules=1)
        {
            var nodes=CityMap.Tutorial().CreateNodes();
            for(int i=0;i<nodes.Length;i++){nodes[i].Kind=NetworkNodeKind.Relay;nodes[i].Role=2;nodes[i].PowerDemand=0;nodes[i].Capacity=24;nodes[i].X=i;nodes[i].Z=20;}
            nodes[0].Kind=NetworkNodeKind.Reactor;nodes[0].Role=0;nodes[0].X=0;nodes[0].Z=0;
            nodes[1].Kind=NetworkNodeKind.DataCore;nodes[1].Role=1;nodes[1].X=1;nodes[1].Z=-2;
            nodes[2].X=2;nodes[2].Z=0;nodes[3].X=2;nodes[3].Z=2;
            nodes[8].Kind=NetworkNodeKind.District;nodes[8].Role=8;nodes[8].PowerDemand=3;nodes[8].X=4;nodes[8].Z=0;
            var map=new CityMap(42,true,nodes,new[]{new[]{0,2},new[]{2,8},new[]{1,2}},-100,1,500,99,new CityObstacle[0]);
            var s=new NetworkSimulation(map,true,rules);s.Nodes[0].PowerSupply=4;return s;
        }
        static void Edit(NetworkSimulation s,int a,int b,LinkKind kind){s.BeginEdit();Check(s.Connect(a,b,kind)==NetworkEditError.None,"fixture connection");s.EndEdit();}
        static bool Rejected(NetworkSave data){try{NetworkSimulation.RestoreSave(data);return false;}catch(ArgumentException){return true;}}
        public static string Run()
        {
            checks=0;Check(typeof(BatteryState).GetField("Node")==null&&typeof(BatteryState).GetProperty("Node")!=null&&typeof(BatteryState).GetProperty("Node").GetSetMethod()==null,"live node is publicly read-only");Check(typeof(BatteryState).GetField("Energy")==null&&typeof(BatteryState).GetProperty("Energy")!=null&&typeof(BatteryState).GetProperty("Energy").GetSetMethod()==null,"live energy is publicly read-only");var s=Fixture();Check(s.AvailableBatteries==1&&s.Batteries.Count==0,"one initial empty slot");
            Check(!s.InstallBattery(0)&&!s.InstallBattery(8)&&!s.InstallBattery(-1),"only valid relay can be converted");
            Check(s.InstallBattery(2),"relay installed");var b=s.BatteryAt(2);Check(b!=null,"lookup installed relay");Near(b.Energy,0,"starts empty");
            Check(!s.InstallBattery(2)&&!s.InstallBattery(3),"duplicate and exhausted slot rejected");
            Near(b.ChargeRate,1,"only spare reactor power is charged");Near(s.Nodes[8].PowerReceived,3,"district is supplied before storage");
            s.Step(0);Near(b.Energy,0,"zero step does not charge");s.Step(1);Near(b.Energy,.9,"1MW-second with 90 percent efficiency");Near(s.Nodes[0].PowerLoad,4,"reactor supplies district and charging");
            double recorded=0;foreach(var flow in s.PowerFlows)recorded+=flow.Amount;Near(recorded,3,"district flow trace excludes storage charging");
            Check(s.InspectPower(2).Throughput>=4,"charging consumes relay throughput");
            s.Nodes[0].PowerSupply=18;Advance(s,60);Check(b.Energy<=120&&b.Energy>100,"finite charge with2MW cap");Advance(s,90);Near(b.Energy,120,"never exceeds capacity");Near(b.ChargeRate,0,"full battery stops charging");
            double stored=b.Energy;s.BeginEdit();s.Paint(NetworkSimulation.EdgeId(1,2),LinkKind.Shared);s.EndEdit();Near(b.Energy,stored,"edit preview does not integrate energy");
            s.Nodes[0].PowerSupply=0;s.Step(.5);Near(b.DischargeRate,4,"max4MW output");Near(b.Energy,118,"discharge energy conserved");Near(s.Nodes[8].PowerReceived,4,"output limit leaves shortfall at5MW peak");
            Check(s.InspectPower(8).Sources.Count==1&&s.InspectPower(8).Sources[0].Source==2,"district identifies battery source");
            Advance(s,40);Near(b.Energy,0,"depleted battery is finite");s.Step(1.0/60);Near(b.DischargeRate,0,"empty cannot power city");
            Check(s.RemoveBattery(2)&&s.BatteryAt(2)==null,"remove releases slot");Check(s.InstallBattery(3),"slot reusable elsewhere");Near(s.BatteryAt(3).Energy,0,"reinstall cannot generate stored energy");
            Check(s.RemoveBattery(3),"remove second location");Check(s.InstallBattery(2),"reinstall first location");s.Nodes[0].PowerSupply=18;Advance(s,2);stored=s.BatteryAt(2).Energy;
            s.BeginEdit();s.Remove(NetworkSimulation.EdgeId(0,2));s.EndEdit();Near(s.BatteryAt(2).Energy,stored,"disconnect does not change energy");s.Step(.25);Check(s.BatteryAt(2).DischargeRate>0,"disconnected source can still supply reachable district");
            s.BeginEdit();s.Remove(NetworkSimulation.EdgeId(2,8));s.EndEdit();stored=s.BatteryAt(2).Energy;Advance(s,1);Near(s.BatteryAt(2).Energy,stored,"isolated battery cannot supply");
            Edit(s,0,2,LinkKind.DataOnly);Advance(s,1);Near(s.BatteryAt(2).Energy,stored,"communication-only connection cannot charge");
            var transit=Fixture();transit.Nodes[0].PowerSupply=3;Check(transit.InstallBattery(2),"transit battery installed");SetEnergy(transit.BatteryAt(2),20);Advance(transit,60.1);
            Near(transit.Nodes[8].PowerReceived,5,"battery adds to reactor transit rather than subtracting its output");Near(transit.BatteryAt(2).DischargeRate,2,"own output separated from transit load");Near(transit.Nodes[2].PowerLoad,5,"transit plus output share node budget");
            transit.Nodes[2].Capacity=4;transit.Step(.1);Near(transit.Nodes[8].PowerReceived,4,"battery shares relay capacity");
            // Classic fixture allows a demand above the one-link limit.
            var saturated=Fixture(0);saturated.Nodes[8].PowerDemand=17;saturated.Nodes[0].PowerSupply=15;saturated.InstallBattery(2);SetEnergy(saturated.BatteryAt(2),20);saturated.Step(.1);Near(saturated.Nodes[8].PowerReceived,16,"wire limit applies to combined supply");
            Near(saturated.BatteryAt(2).DischargeRate,1,"only spare link capacity discharged");
            var tiny=Fixture(0);tiny.Nodes[0].PowerSupply=0;tiny.InstallBattery(2);SetEnergy(tiny.BatteryAt(2),.001);tiny.Step(1);Near(tiny.Nodes[8].PowerReceived,.001,"small residual cannot multiply across paths");Near(tiny.BatteryAt(2).Energy,0,"small residual drained exactly");
            tiny.Nodes[0].PowerSupply=18;SetEnergy(tiny.BatteryAt(2),119.999);tiny.Step(1);Near(tiny.BatteryAt(2).Energy,120,"tiny headroom caps accepted charge");Near(tiny.BatteryAt(2).ChargeRate,.001/.9,"efficiency included in headroom");
            var split=Fixture(0);split.Nodes[0].PowerSupply=0;split.Nodes[9].Kind=NetworkNodeKind.District;split.Nodes[9].PowerDemand=3;split.Nodes[9].X=4;split.Nodes[9].Z=2;Edit(split,2,9,LinkKind.Shared);split.InstallBattery(2);SetEnergy(split.BatteryAt(2),.001);split.Step(1);Near(split.Nodes[8].PowerReceived+split.Nodes[9].PowerReceived,.001,"residual shared across two destinations");Near(split.BatteryAt(2).Energy,0,"two targets cannot overdraft");
            var sources=Fixture(0);sources.Nodes[0].PowerSupply=3.5;sources.Nodes[4].Kind=NetworkNodeKind.Reactor;sources.Nodes[4].PowerSupply=18;sources.Nodes[4].X=0;sources.Nodes[4].Z=2;Edit(sources,4,2,LinkKind.Shared);sources.InstallBattery(2);SetEnergy(sources.BatteryAt(2),119.999);sources.Step(1);Near(sources.BatteryAt(2).Energy,120,"two sources cannot overfill");Near(sources.BatteryAt(2).ChargeRate,.001/.9,"aggregate input headroom");
            var coarse=Fixture(0);var fine=Fixture(0);coarse.InstallBattery(2);fine.InstallBattery(2);coarse.Step(.1);coarse.Step(.2);coarse.Step(.7);Advance(fine,1);Near(coarse.BatteryAt(2).Energy,fine.BatteryAt(2).Energy,"variable step energy matches fixed steps");
            coarse.Nodes[0].PowerSupply=0;coarse.Step(.1);Check(coarse.BatteryAt(2).DischargeRate>0,"input supply change invalidates prior charging rate");coarse.Nodes[2].Capacity=0;coarse.Step(.1);Near(coarse.BatteryAt(2).DischargeRate,0,"changed relay capacity invalidates prior discharge rate");
            var alternate=Fixture(0);alternate.Nodes[0].PowerSupply=18;alternate.Nodes[2].Capacity=3;alternate.Nodes[8].PowerDemand=5;alternate.InstallBattery(3);Edit(alternate,0,3,LinkKind.Shared);Edit(alternate,3,8,LinkKind.Shared);alternate.Step(1.0/60);Near(alternate.Nodes[8].PowerReceived,3,"first tick constrained reactor route");Check(alternate.BatteryAt(3).ChargeRate>0,"empty bypass charges from surplus");alternate.Step(1.0/60);Check(alternate.Nodes[8].PowerReceived>3&&alternate.BatteryAt(3).DischargeRate>0&&alternate.BatteryAt(3).ChargeRate==0,"newly charged energy can discharge on next tick through bypass");
            var pairFixture=Fixture(0);pairFixture.Nodes[0].PowerSupply=18;pairFixture.Nodes[2].Capacity=3;pairFixture.Nodes[8].PowerDemand=5;pairFixture.Nodes[4].X=1;pairFixture.Nodes[4].Z=3;
            Edit(pairFixture,0,3,LinkKind.Shared);Edit(pairFixture,3,8,LinkKind.Shared);Edit(pairFixture,0,4,LinkKind.Shared);Edit(pairFixture,4,8,LinkKind.Shared);
            var pairSave=pairFixture.CaptureSave();pairSave.ExpansionCount=2;pairSave.ExpansionCable=100;pairSave.Sectors=new[]{new SavedSector{North=true,West=2,East=3,District=8,Reactor=-1,DataCore=-1,Feeders=new SavedEdge[0]},new SavedSector{North=false,West=2,East=3,District=8,Reactor=-1,DataCore=-1,Feeders=new SavedEdge[0]}};pairSave.Batteries=new[]{new SavedBattery{Node=3,Energy=0},new SavedBattery{Node=4,Energy=20}};
            var paired=NetworkSimulation.RestoreSave(pairSave);paired.Step(1.0/60);Check(paired.BatteryAt(3).ChargeRate>0&&paired.BatteryAt(4).DischargeRate>0&&paired.PowerCoverage==1,"one battery charges while other covers all shortfall");var fresh=NetworkSimulation.RestoreSave(paired.CaptureSave());paired.Step(1.0/60);fresh.Step(1.0/60);Near(paired.BatteryAt(3).Energy,fresh.BatteryAt(3).Energy,"overlap charges/discharges match fresh allocation");Near(paired.BatteryAt(4).Energy,fresh.BatteryAt(4).Energy,"second battery shares output consistently");for(int i=0;i<1000;i++){fresh=NetworkSimulation.RestoreSave(fresh.CaptureSave());paired.Step(1.0/60);fresh.Step(1.0/60);Near(paired.BatteryAt(3).Energy,fresh.BatteryAt(3).Energy,"multi-storage trajectory matches fresh allocation");Near(paired.BatteryAt(4).Energy,fresh.BatteryAt(4).Energy,"multi-storage second trajectory matches");Near(paired.PowerCoverage,fresh.PowerCoverage,"multi-storage supply matches");}
            var old=Fixture(0);old.InstallBattery(2);Advance(old,65);Check(!old.HousingPeak&&old.HousingChangeIn==0&&old.Nodes[8].PowerDemand==3,"classic demand never changed");
            var real=new NetworkSimulation(CityMap.Generate(42),true,1);real.LoadSolution();real.InstallBattery(5);Advance(real,5);var save=real.CaptureSave();var copy=NetworkSimulation.RestoreSave(save);
            Near(copy.BatteryAt(5).Energy,real.BatteryAt(5).Energy,"capture/restore preserves energy");save.Batteries[0].Energy=0;Check(real.BatteryAt(5).Energy>0&&copy.BatteryAt(5).Energy>0,"save ownership isolated");
            for(int i=0;i<1200;i++){real.Step(1.0/60);copy.Step(1.0/60);Near(real.BatteryAt(5).Energy,copy.BatteryAt(5).Energy,"resume energy matches");Check(real.Successful==copy.Successful&&real.Expired==copy.Expired&&real.PowerCoverage==copy.PowerCoverage,"resume traffic matches");}
            var absent=real.CaptureSave();absent.Batteries=null;Check(NetworkSimulation.RestoreSave(absent).Batteries.Count==0,"old omitted field accepted");
            var invalid=real.CaptureSave();invalid.Batteries[0].Energy=double.NaN;Check(Rejected(invalid),"NaN charge rejected");invalid=real.CaptureSave();invalid.Batteries[0].Energy=121;Check(Rejected(invalid),"overfull rejected");invalid=real.CaptureSave();invalid.Batteries[0].Node=8;Check(Rejected(invalid),"district is not battery");invalid=real.CaptureSave();invalid.Batteries=new[]{invalid.Batteries[0],invalid.Batteries[0]};Check(Rejected(invalid),"duplicate/excess batteries rejected");invalid=real.CaptureSave();invalid.Batteries[0]=null;Check(Rejected(invalid),"null entry rejected");
            var deficit=real.CaptureSave();deficit.StableSeconds=45;deficit.Batteries[0].Energy=0;foreach(var n in deficit.Nodes)if(n.Kind==NetworkNodeKind.Reactor)n.PowerSupply=0;var interrupted=NetworkSimulation.RestoreSave(deficit);Check(interrupted.StableSeconds==45&&interrupted.ExpansionReady,"load preview preserves saved quality state before first tick");interrupted.Step(.001);Check(!interrupted.ExpansionReady,"battery depletion cancels stability immediately");
            real.LoadInitial();Check(real.Batteries.Count==0,"city reset clears equipment");Check(!new NetworkSimulation().InstallBattery(2),"fixed tutorial cannot gain equipment");
            var growth=new NetworkSimulation(CityMap.Generate(42),true,1);growth.LoadSolution();Advance(growth,180);Check(growth.Expand(true),"first growth");growth.LoadSolution();Advance(growth,180);Check(growth.Expand(false),"second growth");Check(growth.AvailableBatteries==2,"two growths add a slot");
            Check(growth.InstallBattery(2)&&growth.InstallBattery(3),"two storage units installed");SetEnergy(growth.BatteryAt(2),30);SetEnergy(growth.BatteryAt(3),0);foreach(var node in growth.Nodes)if(node.Kind==NetworkNodeKind.Reactor)node.PowerSupply=0;Advance(growth,1);Near(growth.BatteryAt(3).Energy,0,"batteries never charge one another");Near(growth.BatteryAt(2).ChargeRate,0,"discharging battery cannot charge");
            var emptyBoundary=real.CaptureSave();emptyBoundary.Batteries=new[]{new SavedBattery{Node=5,Energy=.0001}};foreach(var node in emptyBoundary.Nodes)if(node.Kind==NetworkNodeKind.Reactor)node.PowerSupply=0;var boundaryA=NetworkSimulation.RestoreSave(emptyBoundary);var boundaryB=NetworkSimulation.RestoreSave(boundaryA.CaptureSave());for(int i=0;i<10;i++){boundaryA.Step(.01);boundaryB.Step(.01);Near(boundaryA.BatteryAt(5).Energy,boundaryB.BatteryAt(5).Energy,"near-empty resume");}
            var fullBoundary=real.CaptureSave();fullBoundary.Batteries=new[]{new SavedBattery{Node=5,Energy=119.9999}};boundaryA=NetworkSimulation.RestoreSave(fullBoundary);boundaryB=NetworkSimulation.RestoreSave(boundaryA.CaptureSave());for(int i=0;i<10;i++){boundaryA.Step(.01);boundaryB.Step(.01);Near(boundaryA.BatteryAt(5).Energy,boundaryB.BatteryAt(5).Energy,"near-full resume");}
            foreach(int seed in new[]{42,291026}){var extended=new NetworkSimulation(CityMap.Generate(seed),true,1);extended.LoadSolution();extended.InstallBattery(5);Advance(extended,180);foreach(var offer in extended.ExpansionOffers)Check(offer.Available,"all terrain choices remain available with storage seed="+seed);Check(extended.Expand(ExpansionDirection.East),"expand with installed storage");extended.LoadSolution();Advance(extended,180);Check(extended.ExpansionReady&&extended.BatteryAt(5)!=null,"expanded city with storage is stable");var resumed=NetworkSimulation.RestoreSave(extended.CaptureSave());Near(resumed.BatteryAt(5).Energy,extended.BatteryAt(5).Energy,"expanded storage remains saved");}
            return "PASS: "+checks+" battery checks";
        }
#if BATTERY_STANDALONE
        public static void Main(){Console.WriteLine(Run());}
#endif
    }
}

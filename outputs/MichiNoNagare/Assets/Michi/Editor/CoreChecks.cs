using System;
using Michi.Core;

public static class CoreChecks
{
    static int passed;
    static void Check(bool condition,string message){if(!condition)throw new Exception("FAIL: "+message);passed++;}
    public static string Run()
    {
        passed=0;
        var s=new RoadSimulation();
        Check(s.Roads.Count==50,"initial road budget");
        Check(s.Route(TravelRole.Freight).Contains(new Cell(12,6)),"initial trucks use shortcut");
        s.BeginEdit();s.Edit(new Cell(12,6),RoadKind.CommuteOnly);s.EndEdit();
        Check(!s.Route(TravelRole.Freight).Contains(new Cell(12,6)),"freight respects dedicated road");
        Check(s.Route(TravelRole.Freight).Contains(new Cell(12,11)),"freight uses lower bridge");
        Check(s.Route(TravelRole.Commute).Contains(new Cell(12,6)),"commuters keep shortcut");
        Check(s.DedicatedCount==1,"dedicated tile counted");
        s.Undo();Check(s.DedicatedCount==0,"undo restriction");
        s.LoadPreset(Preset.Empty);s.BeginEdit();
        for(int x=1;x<=6;x++)Check(s.Edit(new Cell(x,1),RoadKind.FreightOnly)==EditError.None,"dedicated placement");
        Check(s.Edit(new Cell(1,2),RoadKind.CommuteOnly)==EditError.DedicatedBudget,"dedicated budget");
        s.Edit(new Cell(1,1),RoadKind.General);Check(s.Edit(new Cell(1,2),RoadKind.CommuteOnly)==EditError.None,"tile refund");s.EndEdit();
        s.LoadPreset(Preset.Empty);s.BeginEdit();s.Edit(new Cell(12,1),RoadKind.General);s.Edit(new Cell(12,2),RoadKind.General);
        Check(s.Edit(new Cell(12,3),RoadKind.General)==EditError.BridgeBudget,"bridge budget");
        Check(s.Edit(new Cell(3,3),RoadKind.General)==EditError.Obstacle,"building blocks roads");s.EndEdit();
        var mixed=Play(Preset.Shared);var solved=Play(Preset.Solution);
        Check(!mixed.Evaluate().Passed,"mixed layout fails");Check(solved.Evaluate().Passed,"reference layout passes");
        Check(solved.Arrived[0]==80,"browser parity: 80 commuters");Check(solved.FreightDelivered==186,"browser parity: 186 freight");
        Check(Math.Abs(solved.Time-60)<.000001,"round ends at exactly60");
        Check(solved.CommuteAverage<mixed.CommuteAverage,"separation improves commute");
        Check(solved.Edit(new Cell(5,5),RoadKind.General)==EditError.NotDesigning,"editing locked after test");
        solved.ReturnToDesign();Check(solved.Roads.Count==50&&solved.DedicatedCount==1,"redesign retains layout");
        Check(solved.Time==0&&solved.Phase==RoundPhase.Design,"redesign clears traffic");
        s.LoadPreset(Preset.Shared);s.StartTest();
        for(int i=0;i<3600;i++){s.StepRound(1.0/60);for(int a=0;a<s.Cars.Count;a++)for(int b=a+1;b<s.Cars.Count;b++)Check(!s.BodyOverlaps(s.Cars[a],s.Cars[b],-.005),"body separation");}
        return "PASS: "+passed+" checks; mixed "+mixed.Arrived[0]+" commuters / "+mixed.FreightDelivered+" freight; solved "+80+" commuters / "+186+" freight.";
    }
    static RoadSimulation Play(Preset preset){var s=new RoadSimulation();s.LoadPreset(preset);s.StartTest();for(int i=0;i<3700;i++)s.StepRound(1.0/60);return s;}
#if STANDALONE_CHECKS
    public static void Main(){Console.WriteLine(Run());}
#endif
}

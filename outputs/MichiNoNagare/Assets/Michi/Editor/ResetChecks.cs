using System;
using Michi.Core;
public static class ResetChecks
{
    static int checks;
    static void Check(bool ok,string label){if(!ok)throw new Exception("RESET FAIL: "+label);checks++;}
    public static string Run()
    {
        checks=0;var s=new NetworkSimulation(CityMap.Generate(42));s.LoadSolution();var gate=new ResetConfirmation();
        Check(!gate.Confirm(s)&&s.Links.Count==11,"reset cannot occur without a request");gate.Request();Check(gate.Pending&&s.Links.Count==11,"opening prompt preserves wiring");gate.Cancel();Check(!gate.Pending&&!gate.Confirm(s)&&s.Links.Count==11,"cancel preserves all edits");
        s.Start();s.Step(1);double time=s.Time;gate.Request();gate.Cancel();Check(s.Phase==RoundPhase.Running&&s.Time==time,"cancel preserves current round");gate.Request();Check(gate.Confirm(s),"explicit confirmation resets");Check(s.Map.Seed==42&&s.Map.Generated,"same city and seed retained");Check(s.Phase==RoundPhase.Design&&s.Time==0&&s.Successful==0,"round statistics reset");Check(s.Links.Count==10&&s.DedicatedCount==0&&s.BridgeCount==2,"initial seed wiring restored");Check(!s.Undo(),"pre-reset undo history cleared");Check(!gate.Pending&&!gate.Confirm(s),"confirmation consumed once");return "PASS: "+checks+" reset confirmation checks";
    }
#if RESET_STANDALONE
    public static void Main(){Console.WriteLine(Run());}
#endif
}

using System;
using Michi.Core;
public static class TutorialChecks
{
    static int checks;
    static void Check(bool ok,string label){if(!ok)throw new Exception("TUTORIAL FAIL: "+label);checks++;}
    public static string Run()
    {
        checks=0;var s=new NetworkSimulation();var t=new NetworkTutorial();t.Begin(s);
        Check(t.Step==0&&s.PowerCoverage==0,"first lesson begins unpowered");Check(!t.AllowsLaunch&&!t.AllowsTool(1)&&!t.AllowsNode(0),"intro cannot be bypassed by input");
        t.Next();Check(t.Step==1&&t.AllowsTool(1)&&!t.AllowsTool(3),"only the requested tool is accepted");t.Tick(s,1,-1,-1);Check(t.TargetNode==0,"start at reactor");t.Tick(s,1,0,0);Check(t.TargetNode==2&&!t.AllowsNode(7),"next target is west relay");
        s.BeginEdit();s.Connect(0,2,LinkKind.PowerOnly);s.EndEdit();t.Tick(s,1,-1,2);Check(t.Step==4&&s.PowerCoverage>.99,"connecting one cable immediately powers districts");
        t.Next();Check(t.TargetNode==4&&!t.CanAdvance&&!t.ShowsHubDetails,"inspect congestion next");t.Next();Check(t.Step==5,"cannot skip observation before selecting hub");t.Tick(s,4,-1,4);Check(t.Step==5&&t.CanAdvance&&t.ShowsHubDetails,"hub inspection waits for explicit next");Check(t.HubPowerBefore==14&&t.HubCapacityBefore==16,"observation records actual hub load and capacity");t.Tick(s,4,-1,-1);Check(t.Step==5&&t.ShowsHubDetails,"observation remains readable after deselection");t.Next();t.Tick(s,1,-1,4);Check(t.TargetNode==2,"bypass starts at relay1");t.Tick(s,1,2,2);Check(t.TargetNode==5,"bypass ends at relay3");s.BeginEdit();s.Connect(2,5,LinkKind.PowerOnly);s.EndEdit();t.Tick(s,1,-1,5);Check(t.Step==9&&t.AllowsTool(2),"ask for data tool after actual connection");
        t.Tick(s,2,-1,5);Check(t.AllowsLink(NetworkSimulation.EdgeId(2,4))&&!t.AllowsLink(NetworkSimulation.EdgeId(4,5)),"only central feeder may be repainted");s.BeginEdit();s.Paint(NetworkSimulation.EdgeId(2,4),LinkKind.DataOnly);s.EndEdit();t.Tick(s,2,-1,-1);Check(t.AllowsLaunch&&t.Step==11&&t.ShowsHubDetails,"launch only after topology is correct");Check(s.Nodes[4].PowerLoad<t.HubPowerBefore,"bypass reduces observed hub load");
        Check(s.DedicatedCount==3,"lesson solution fits three dedicated lines");s.Start();t.Tick(s,2,-1,-1);Check(t.Step==12,"wait for actual simulation");for(int i=0;i<2701;i++)s.Step(1.0/60);t.Tick(s,2,-1,-1);Check(t.Step==13&&s.Passed,"guided topology clears the stage");t.Next();Check(!t.Active&&t.AllowsNode(7)&&t.AllowsTool(3),"completion unlocks free play");
        t.Begin(s);t.Next();t.Tick(s,1,-1,-1);t.Tick(s,1,0,0);t.Tick(s,1,-1,-1);Check(t.Step==2,"escape from pending selection can recover");t.Skip();Check(!t.Active&&t.AllowsLaunch,"skip removes all input gates");t.Begin(s);Check(t.Step==0&&s.PowerCoverage==0&&!t.HubInspected&&!t.ShowsHubDetails,"replay resets lesson and observation");return "PASS: "+checks+" tutorial checks";
    }
#if TUTORIAL_STANDALONE
    public static void Main(){Console.WriteLine(Run());}
#endif
}

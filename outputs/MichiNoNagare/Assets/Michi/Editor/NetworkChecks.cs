using System;
using Michi.Core;
public static class NetworkChecks
{
    static int checks;
    static void Check(bool value,string name){if(!value)throw new Exception("NETWORK FAIL: "+name);checks++;}
    static NetworkSimulation Play(bool solution){var s=new NetworkSimulation();if(solution)s.LoadSolution();s.Start();for(int i=0;i<2900;i++)s.Step(1.0/60);return s;}
    static void CheckPreview()
    {
        var s=new NetworkSimulation();double cable=s.CableLength;int links=s.Links.Count;
        var p=s.PreviewConnect(2,5,LinkKind.PowerOnly);
        Check(p.Error==NetworkEditError.None&&p.AddedCable>0&&p.DedicatedDelta==1,"preview quotes new dedicated cable");
        Check(s.CableLength==cable&&s.Links.Count==links&&!s.Undo(),"preview changes neither network nor undo history");
        s.BeginEdit();Check(s.Connect(2,5,LinkKind.PowerOnly)==p.Error,"commit matches preview");s.EndEdit();
        Check(Math.Abs(s.CableLength-cable-p.AddedCable)<.00001,"quoted cable equals actual delta");
        p=s.PreviewConnect(2,5,LinkKind.Shared);Check(p.Existing&&p.AddedCable==0&&p.DedicatedDelta==-1&&p.BridgeDelta==0,"conversion refunds slot without charging cable or bridge");
        Check(s.PreviewConnect(-1,2,LinkKind.Shared).Error==NetworkEditError.Invalid,"invalid endpoints safely rejected");
        Check(s.PreviewConnect(0,9,LinkKind.Shared).Error==NetworkEditError.TooLong,"length limit visible before commit");
        s=new NetworkSimulation();s.BeginEdit();s.Connect(0,3,LinkKind.Shared);s.EndEdit();
        Check(s.PreviewConnect(1,2,LinkKind.Shared).Error==NetworkEditError.CableBudget,"cable shortage visible before commit");
        s=new NetworkSimulation();s.BeginEdit();s.Paint(NetworkSimulation.EdgeId(0,2),LinkKind.PowerOnly);s.Paint(NetworkSimulation.EdgeId(1,3),LinkKind.DataOnly);s.Paint(NetworkSimulation.EdgeId(2,4),LinkKind.PowerOnly);s.EndEdit();
        Check(s.PreviewConnect(3,4,LinkKind.DataOnly).Error==NetworkEditError.DedicatedBudget,"conversion detects full dedicated budget");
        Check(s.PreviewConnect(0,2,LinkKind.DataOnly).Error==NetworkEditError.None,"dedicated to dedicated conversion uses no new slot");
        var m=CityMap.Tutorial();s=new NetworkSimulation(new CityMap(1,true,m.CreateNodes(),m.InitialEdges,1,1.5,100,2,new CityObstacle[0]));
        p=s.PreviewConnect(2,5,LinkKind.Shared);Check(p.BridgeDelta==1&&p.Error==NetworkEditError.BridgeBudget,"bridge consumption and shortage visible");
        s=new NetworkSimulation(new CityMap(2,true,m.CreateNodes(),m.InitialEdges,1,1.5,100,5,new[]{new CityObstacle{X=-2.5,Z=-2,Width=1,Depth=1}}));s.LoadEmpty();
        Check(s.PreviewConnect(2,4,LinkKind.Shared).Error==NetworkEditError.Obstacle,"blocked route visible before commit");
        s=new NetworkSimulation();s.Start();Check(s.PreviewConnect(2,5,LinkKind.Shared).Error==NetworkEditError.NotDesigning,"preview respects locked simulation");
    }
    public static string Run()
    {
        checks=0;CheckPreview();var s=new NetworkSimulation();Check(s.CableLength<NetworkSimulation.CableBudget,"initial budget");Check(s.PowerCoverage>.99,"continuous power available");
        Check(s.Path(0,9,SignalKind.Power).Contains(4),"initial power traverses centre");
        s.LoadSolution();Check(!s.Path(0,9,SignalKind.Power).Contains(4),"power bypasses centre");Check(s.Path(1,9,SignalKind.Data).Contains(4),"data retains central route");
        Check(s.DedicatedCount==2,"two dedicated links");s.Undo();Check(s.DedicatedCount==0,"solution undo");
        Check(s.Connect(0,9,LinkKind.Shared)==NetworkEditError.TooLong,"long links rejected");
        s.BeginEdit();s.Paint(NetworkSimulation.EdgeId(0,2),LinkKind.DataOnly);s.EndEdit();Check(s.PowerCoverage==0,"data-only blocks power");s.Undo();Check(s.PowerCoverage>.99,"undo restores power");
        s=new NetworkSimulation();s.BeginEdit();Check(s.Connect(0,3,LinkKind.Shared)==NetworkEditError.None,"extra link accepted");double length=s.CableLength;Check(s.Connect(1,2,LinkKind.Shared)==NetworkEditError.CableBudget,"length budget enforced");Check(Math.Abs(s.CableLength-length)<.001,"failed edit does not spend cable");s.EndEdit();s.Undo();Check(s.Links.Count==10,"undo removes extra link");
        s=new NetworkSimulation();foreach(var node in s.Nodes)if(node.Kind==NetworkNodeKind.District)node.PowerDemand=20;s.ReturnToDesign();double min=100,max=0;foreach(var node in s.Nodes)if(node.Kind==NetworkNodeKind.District){min=Math.Min(min,node.PowerReceived);max=Math.Max(max,node.PowerReceived);}Check(max-min<=.126,"max-min fair power under saturation");Check(s.Nodes[4].PowerLoad<=s.Nodes[4].Capacity+.001,"hub capacity respected");
        var bad=Play(false);var good=Play(true);Console.WriteLine("NETWORK METRICS bad="+bad.Summary()+" good="+good.Summary());
        Check(!bad.Passed,"contended network fails");Check(good.Passed,"segregated network passes");Check(good.DataReliability>bad.DataReliability+.2,"data performance is topology dependent");Check(good.AverageLatency<bad.AverageLatency,"separation reduces latency");
        Check(good.Connect(0,3,LinkKind.Shared)==NetworkEditError.NotDesigning,"live graph locked");Check(good.PowerCoverage>.99,"solution retains all power");Check(Math.Abs(good.Time-45)<.00001,"round ends at45");Check(good.Packets.Count<80,"finite in-flight queue");Check(bad.Expired>0,"congested packets expire");
        string final=good.Summary();good.ReturnToDesign();Check(good.Time==0&&good.Links.Count==11,"redesign retains graph");
        s=new NetworkSimulation();s.BeginEdit();s.Paint(NetworkSimulation.EdgeId(0,2),LinkKind.PowerOnly);s.Paint(NetworkSimulation.EdgeId(1,3),LinkKind.DataOnly);s.Paint(NetworkSimulation.EdgeId(2,4),LinkKind.PowerOnly);Check(s.Paint(NetworkSimulation.EdgeId(3,4),LinkKind.DataOnly)==NetworkEditError.DedicatedBudget,"dedicated budget enforced");s.EndEdit();s.Undo();Check(s.DedicatedCount==0,"undo refunds dedicated budget");
        foreach(var link in good.Links.Values)Check(link.PowerLoad<=NetworkSimulation.LinkCapacity+.001,"line power capacity");
        return "PASS: "+checks+" network checks. "+final;
    }
#if NETWORK_STANDALONE
    public static void Main(){Console.WriteLine(Run());}
#endif
}

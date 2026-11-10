using System;
using System.Globalization;
using System.Text;
using Michi.Core;
public static class SeededCityChecks
{
    static int checks;
    static void Check(bool ok,string why){if(!ok)throw new Exception("SEEDED CITY FAIL: "+why);checks++;}
    static string Fingerprint(CityMap map){var b=new StringBuilder();b.Append(map.Seed).Append('|').Append(map.CableAllowance).Append('|').Append(map.RiverX.ToString("R",CultureInfo.InvariantCulture));foreach(var n in map.CreateNodes())b.Append('|').Append(n.X.ToString("R",CultureInfo.InvariantCulture)).Append(',').Append(n.Z.ToString("R",CultureInfo.InvariantCulture)).Append(',').Append(n.PowerDemand);foreach(var e in map.InitialEdges)b.Append('|').Append(e[0]).Append(',').Append(e[1]);foreach(var o in map.Obstacles)b.Append('|').Append(o.X.ToString("R",CultureInfo.InvariantCulture)).Append(',').Append(o.Z.ToString("R",CultureInfo.InvariantCulture)).Append(',').Append(o.Width).Append(',').Append(o.Depth);return b.ToString();}
    public static string Run()
    {
        checks=0;int[] extreme={0,-1,1,int.MinValue,int.MaxValue,291026};foreach(int seed in extreme)Check(Fingerprint(CityMap.Generate(seed))==Fingerprint(CityMap.Generate(seed)),"repeatable seed "+seed);
        Check(Fingerprint(CityMap.Generate(42))!=Fingerprint(CityMap.Generate(43)),"different seeds vary");
        for(int seed=-24;seed<40;seed++){
            var map=CityMap.Generate(seed);var s=new NetworkSimulation(map);Check(map.Obstacles.Length>=2,"constraints generated "+seed);Check(s.PowerCoverage>.99,"initial graph supplied "+seed);Check(s.CableLength<=s.AvailableCable&&s.BridgeCount<=map.BridgeLimit,"initial budgets "+seed);
            foreach(var l in s.Links.Values)Check(l.Length<=NetworkSimulation.MaxLinkLength&&!map.Blocked(s.Nodes[l.A],s.Nodes[l.B]),"initial edge legal "+seed);
            s.LoadSolution();Check(s.DedicatedCount<=3&&s.CableLength<=s.AvailableCable&&s.BridgeCount<=map.BridgeLimit,"solution budgets "+seed);s.Start();for(int i=0;i<2701;i++)s.Step(1.0/60);Check(s.Passed,"actual seeded round passes "+seed);s.ReturnToDesign();Check(s.Links.Count==11,"redesign retains seed wiring");s.LoadInitial();Check(s.Links.Count==10&&s.DedicatedCount==0,"reset restores seed initial graph");
        }
        var baseMap=CityMap.Tutorial();var custom=new CityMap(1,true,baseMap.CreateNodes(),baseMap.InitialEdges,1,1.5,100,2,new CityObstacle[0]);var test=new NetworkSimulation(custom);Check(test.BridgeCount==2,"two initial crossings");test.BeginEdit();Check(test.Connect(2,5,LinkKind.PowerOnly)==NetworkEditError.BridgeBudget,"crossing limit enforced");test.EndEdit();test.BeginEdit();test.Remove(NetworkSimulation.EdgeId(4,5));test.EndEdit();Check(test.BridgeCount==1,"removal refunds bridge");test.BeginEdit();Check(test.Connect(2,5,LinkKind.PowerOnly)==NetworkEditError.None,"refunded bridge reusable");test.EndEdit();test.Undo();Check(test.BridgeCount==1&&!test.Links.ContainsKey(NetworkSimulation.EdgeId(2,5)),"undo refunds crossing");
        var obstacle=new CityObstacle{X=-2.5,Z=-2,Width=1,Depth=1};Check(obstacle.Intersects(-5,-4,0,0)&&!obstacle.Intersects(-5,4,0,4),"segment collision geometry");var blocked=new NetworkSimulation(new CityMap(2,true,baseMap.CreateNodes(),baseMap.InitialEdges,1,1.5,100,5,new[]{obstacle}));blocked.LoadEmpty();blocked.BeginEdit();Check(blocked.Connect(2,4,LinkKind.Shared)==NetworkEditError.Obstacle&&blocked.Links.Count==0,"blocked line does not spend resources");blocked.EndEdit();
        return "PASS: "+checks+" seeded city checks; 64 generated reference rounds clear";
    }
#if CITY_STANDALONE
    public static void Main(){Console.WriteLine(Run());}
#endif
}

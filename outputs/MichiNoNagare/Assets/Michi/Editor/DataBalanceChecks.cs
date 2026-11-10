using System;
using System.Collections.Generic;
using Michi.Core;
namespace Michi.Checks
{
    public static class DataBalanceChecks
    {
        static int checks;
        static void Check(bool value,string message){checks++;if(!value)throw new Exception("DATA BALANCE: "+message);}
        static void Advance(NetworkSimulation s,double seconds){for(int i=0;i<(int)(seconds*60);i++)s.Step(1.0/60);}
        static void Stable(NetworkSimulation s){for(int i=0;i<10800&&!s.ExpansionReady;i++)s.Step(1.0/60);Check(s.ExpansionReady,"connected reference stabilizes");}
        static CityMap SmallMap(bool shared)
        {
            var nodes=shared?new[]{new GridNode(0,"A","",-3,0,100,NetworkNodeKind.DataCore),new GridNode(1,"B","",-2,0,100,NetworkNodeKind.DataCore),new GridNode(2,"R","",0,0,100,NetworkNodeKind.Relay),new GridNode(3,"D","",2,0,100,NetworkNodeKind.District)}:new[]{new GridNode(0,"A","",-3,0,100,NetworkNodeKind.DataCore),new GridNode(1,"B","",3,0,100,NetworkNodeKind.DataCore),new GridNode(2,"D1","",-4,0,100,NetworkNodeKind.District),new GridNode(3,"D2","",4,0,100,NetworkNodeKind.District)};
            var edges=shared?new[]{new[]{0,2},new[]{1,2},new[]{2,3}}:new[]{new[]{0,2},new[]{1,3}};
            return new CityMap(0,false,nodes,edges,0,1,100,99,new CityObstacle[0]);
        }
        static void CheckIndependentCenters()
        {
            var s=new NetworkSimulation(SmallMap(false),true);s.Nodes[0].DataSupply=0;Advance(s,2);var oldest=new List<double>();foreach(var r in s.PendingData)oldest.Add(r.Born);Check(oldest.Count>0,"zero output still generates real waiting requests");Check(s.Nodes[3].Delivered>0,"blocked FIFO request cannot starve independent center");Check(s.Nodes[0].DataDispatched==0,"zero output cannot dispatch");
            s.Nodes[0].DataSupply=7;Advance(s,.2);bool original=false;foreach(var packet in s.Packets)if(oldest.Contains(packet.Born)&&packet.Destination==2)original=true;Check(original,"dispatch from queue keeps original deadline");Advance(s,20);Check(s.RecentData>=.9,"restored source recovers after outcome window rolls over");
        }
        static void CheckSharedCenters()
        {
            var s=new NetworkSimulation(SmallMap(true),true);s.Nodes[0].DataSupply=s.Nodes[1].DataSupply=1;Advance(s,30);Check(s.Nodes[0].DataDispatched>0&&s.Nodes[1].DataDispatched>0,"saturated nearest center falls back to reachable spare source");Check(s.RecentData>=.9,"shared capacity can meet demand");foreach(var n in s.Nodes)if(n.Kind==NetworkNodeKind.DataCore)Check(n.DataDispatched<=31,"one packet per second budget remains exact");
        }
        public static string Run()
        {
            checks=0;var s=new NetworkSimulation(CityMap.Generate(291026),true);s.LoadSolution();Stable(s);Check(s.UnconnectedDataDistrictCount==0&&s.UnderpoweredDistrictCount==0,"stable city has no live connection warnings");Check(s.Expand(true),"north opens");
            int newDistrict=s.Sectors[0].District;
            Check(!s.DistrictDataConnected(newDistrict)&&s.UnconnectedDataDistrictCount==1,"new district is visibly disconnected before any simulation tick");
            Check(s.UnderpoweredDistrictCount==1,"new district immediately reports missing power");
            Check(!s.DistrictDataConnected(-1)&&!s.DistrictDataConnected(1),"non district IDs are not reported connected");
            s.LoadSolution();Check(s.DistrictDataConnected(newDistrict)&&s.UnconnectedDataDistrictCount==0&&s.UnderpoweredDistrictCount==0,"connection warnings clear immediately after wiring");
            var sector=s.Sectors[0];Check(sector.DataCore>=0,"first growing district offers extra source");var disconnected=new List<int>();foreach(var e in s.Links.Values)if(e.A==sector.DataCore||e.B==sector.DataCore)disconnected.Add(e.Id);
            s.BeginEdit();foreach(int id in disconnected)s.Remove(id);s.EndEdit();Advance(s,90);
            Check(s.PowerCoverage>=.99,"disconnecting source retains all power");Check(s.UnconnectedDataDistrictCount==0,"output shortage is distinct from missing district routes");Check(s.RecentData>.55,"overloaded center still delivers a useful share instead of wasting all output on doomed requests");Check(s.RecentData<.9,"one old center cannot serve the expanded city at target quality; actual="+s.RecentData);Check(!s.ExpansionReady,"output shortage prevents expansion");Console.WriteLine("DATA BALANCE before connection: "+s.Summary()+", recent="+s.RecentData+", waiting="+s.WaitingData);
            Check(s.Nodes[1].DataOutputRate>=6.9&&s.Nodes[1].DataOutputRate<=7.1,"fractional source credit preserves seven packets per second under load");
            Check(s.WaitingData>0&&s.WaitingData+s.Packets.Count<100,"overload queue is visible and bounded by deadlines");
            foreach(var request in s.PendingData)Check(s.Time-request.Born<=NetworkSimulation.Deadline+1.0/60,"waiting request retains deadline");
            double before=s.Time;var born=new List<double>();foreach(var request in s.PendingData)born.Add(request.Born);
            s.BeginEdit();s.Paint(NetworkSimulation.EdgeId(sector.DataCore,sector.West),LinkKind.DataOnly);s.EndEdit();int index=0;foreach(var request in s.PendingData)Check(request.Born==born[index++],"live edits retain pending birth times");Check(s.Time==before&&index==born.Count,"live edit does not clear or regenerate demand");
            s.LoadSolution();Stable(s);Check(s.RecentData>=.9&&s.RecentLatency<=2.8,"connecting additional center recovers service");Check(s.Nodes[sector.DataCore].DataOutputRate>0,"extra center actually sends data");
            int requested=0;foreach(var n in s.Nodes){requested+=n.DataRequested;if(n.Kind==NetworkNodeKind.DataCore)Check(n.DataDispatched<=s.Time*n.DataSupply+1.001,"source output never exceeds production plus one stored token");}
            Check(requested==s.Successful+s.Expired+s.WaitingData+s.Packets.Count,"no request vanishes from delivery accounting");
            Console.WriteLine("DATA BALANCE after connection: recent="+s.RecentData+", latency="+s.RecentLatency+", north output="+s.Nodes[sector.DataCore].DataOutputRate);
            s.LoadInitial();Check(s.WaitingData==0&&s.Packets.Count==0,"reset clears outstanding work");Check(Math.Abs(s.DataDemandTotal-6.5)<.001&&s.DataCapacityTotal==7,"root demand and capacity restored");
            CheckIndependentCenters();CheckSharedCenters();
            return "PASS: "+checks+" data balance checks";
        }
#if DATA_BALANCE_STANDALONE
        public static void Main(){Console.WriteLine(Run());}
#endif
    }
}

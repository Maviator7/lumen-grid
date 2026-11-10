using System;
using System.Collections.Generic;

namespace Michi.Core
{
    public enum SignalKind { Power, Data }
    public enum LinkKind { Shared, PowerOnly, DataOnly }
    public enum NetworkNodeKind { Reactor, DataCore, Relay, District }
    public enum NetworkEditError { None, TooLong, CableBudget, DedicatedBudget, NotDesigning, Invalid, Obstacle, BridgeBudget }
    public sealed class NetworkEditPreview
    {
        public NetworkEditError Error;public double AddedCable,AddedCost;public int DedicatedDelta,BridgeDelta;public bool Existing;
    }
    [Serializable] public sealed class GridNode
    {
        public int Id;public string Code,Name;public double X,Z,Capacity,PowerLoad,PowerReceived,PowerDemand;public NetworkNodeKind Kind;public int Delivered,Failed,Processed;
        public int Role;public double PowerSupply=18,LastDelivered=-1;
        public double DataDemand,DataSupply,DataOutputRate,DataDeliveryRate;public int DataRequested,DataDispatched;internal double SourceCredit,RequestCredit;
        public double PowerFraction {get{return PowerDemand>0?Math.Min(1,PowerReceived/PowerDemand):1;}}
        public double Reliability {get{return Delivered+Failed>0?(double)Delivered/(Delivered+Failed):0;}}
        internal GridNode SaveCopy(){return (GridNode)MemberwiseClone();}
        public GridNode(int id,string code,string name,double x,double z,double capacity,NetworkNodeKind kind){Id=id;Code=code;Name=name;X=x;Z=z;Capacity=capacity;Kind=kind;Role=kind==NetworkNodeKind.Reactor?0:kind==NetworkNodeKind.DataCore?1:kind==NetworkNodeKind.District?(id>=7&&id<=9?id:8):2;}
    }
    [Serializable] public sealed class GridLink
    {
        public int A,B;public LinkKind Kind;public double Length,PowerLoad,Credit;public int Sent;
        public int Id {get{return NetworkSimulation.EdgeId(A,B);}}
        internal GridLink SaveCopy(){return (GridLink)MemberwiseClone();}
        public GridLink(int a,int b,LinkKind kind,double length){A=a;B=b;Kind=kind;Length=length;}
        public bool Allows(SignalKind type){return Kind==LinkKind.Shared||(Kind==LinkKind.PowerOnly&&type==SignalKind.Power)||(Kind==LinkKind.DataOnly&&type==SignalKind.Data);}
    }
    [Serializable] public sealed class DataPacket
    {
        public int Id,Destination,Index;public List<int> Path;public bool Travelling;public double Born,Progress;
        internal DataPacket SaveCopy(){var p=(DataPacket)MemberwiseClone();p.Path=new List<int>(Path);return p;}
        public int Node {get{return Path[Index];}}public int Next {get{return Path[Index+1];}}
    }
    public sealed partial class NetworkSimulation
    {
        public const double CableBudget=78,MaxLinkLength=11.5,LinkCapacity=16,Duration=45,Deadline=5;
        public const int DedicatedLimit=3;
        public readonly CityMap Map;
        public readonly bool Continuous;
        public GridNode[] Nodes {get;private set;}
        public readonly Dictionary<int,GridLink> Links=new Dictionary<int,GridLink>();
        public readonly List<DataPacket> Packets=new List<DataPacket>();
        public RoundPhase Phase {get;private set;}
        public double Time {get;private set;}
        public int Successful {get;private set;}public int Expired {get;private set;}
        double delaySum,powerIntegral,spawnClock;int serial;double[] nodeCredit;bool edited;
        readonly Stack<List<GridLink>> history=new Stack<List<GridLink>>();
        readonly Dictionary<long,List<int>> routeCache=new Dictionary<long,List<int>>();
        public double AvailableCable {get{return Map.CableAllowance+expansionCable;}}
        public int AvailableDedicated {get{return DedicatedLimit+ExpansionCount*2;}}
        public int AvailableBridges {get{return Map.BridgeLimit+ExpansionCount;}}
        public bool CanEdit {get{return Phase==RoundPhase.Design||(Continuous&&Phase==RoundPhase.Running);}}
        public int BridgeCount {get{int n=0;foreach(var l in Links.Values)if(Map.CrossesRiver(Nodes[l.A],Nodes[l.B]))n++;return n;}}
        public double CableLength {get{double n=0;foreach(var l in Links.Values)n+=l.Length;return n;}}
        public int DedicatedCount {get{int n=0;foreach(var l in Links.Values)if(l.Kind!=LinkKind.Shared)n++;return n;}}
        public double PowerCoverage {get{double d=0,r=0;foreach(var n in Nodes)if(n.Kind==NetworkNodeKind.District){d+=n.PowerDemand;r+=n.PowerReceived;}return d>0?r/d:0;}}
        public double AveragePower {get{return Time>0?powerIntegral/Time:PowerCoverage;}}
        public double DataReliability {get{return Successful+Expired>0?(double)Successful/(Successful+Expired):0;}}
        public double AverageLatency {get{return Successful>0?delaySum/Successful:0;}}
        public bool Passed {get{return !Continuous&&Phase==RoundPhase.Result&&AveragePower>=.98&&DataReliability>=.90&&Successful>=60&&AverageLatency<=2.8;}}
        public NetworkSimulation():this(CityMap.Tutorial(),false){}
        public NetworkSimulation(CityMap map,bool continuous=false,int rulesVersion=0){if(rulesVersion<0||rulesVersion>CurrentRulesVersion)throw new ArgumentException("未知のルールです。");Map=map;Continuous=continuous;RulesVersion=rulesVersion;LoadInitial();}
        // Pairing is unique beyond 100 nodes, unlike the original a*100+b encoding.
        public static int EdgeId(int a,int b){long high=Math.Max(a,b);return checked((int)(high*(high-1)/2+Math.Min(a,b)));}
        public double Distance(int a,int b){return Distance(Nodes[a],Nodes[b]);}
        static double Distance(GridNode a,GridNode b){double x=a.X-b.X,z=a.Z-b.Z;return Math.Sqrt(x*x+z*z);}
        void Add(int a,int b,LinkKind kind){Links[EdgeId(a,b)]=new GridLink(a,b,kind,Distance(a,b));routeCache.Clear();}
        public void LoadInitial(){Nodes=Map.CreateNodes();foreach(var n in Nodes)ConfigureMainNode(n);nodeCredit=new double[Nodes.Length];ResetExpansionState();Links.Clear();history.Clear();foreach(var e in Map.InitialEdges)Add(e[0],e[1],LinkKind.Shared);ReturnToDesign();if(Continuous)BeginRunning();}
        public void LoadEmpty(){Links.Clear();history.Clear();routeCache.Clear();ReturnToDesign();if(Continuous)BeginRunning();}
        public void LoadSolution()
        {
            if(!Continuous)LoadInitial();else{Links.Clear();history.Clear();routeCache.Clear();foreach(var e in Map.InitialEdges)Add(e[0],e[1],LinkKind.Shared);}
            ReferenceValid=true;BeginEdit();ReferenceConnect(2,5,LinkKind.PowerOnly);if(Paint(EdgeId(2,4),LinkKind.DataOnly)!=NetworkEditError.None)ReferenceValid=false;
            if(Continuous&&ExpansionCount>0){ApplySectorReference();}
            EndEdit();
        }
        public void BeginEdit(){if(!CanEdit)return;var copy=new List<GridLink>();foreach(var l in Links.Values)copy.Add(new GridLink(l.A,l.B,l.Kind,l.Length));history.Push(copy);edited=false;}
        public NetworkEditPreview PreviewConnect(int a,int b,LinkKind kind)
        {
            var p=new NetworkEditPreview();
            if(!CanEdit){p.Error=NetworkEditError.NotDesigning;return p;}
            if(a<0||b<0||a>=Nodes.Length||b>=Nodes.Length||a==b){p.Error=NetworkEditError.Invalid;return p;}
            GridLink old;p.Existing=Links.TryGetValue(EdgeId(a,b),out old);
            p.AddedCable=p.Existing?0:Distance(a,b);
            p.AddedCost=LinkCost(Distance(a,b),kind)-(p.Existing?LinkCost(old.Length,old.Kind):0);
            p.DedicatedDelta=(kind==LinkKind.Shared?0:1)-(p.Existing&&old.Kind!=LinkKind.Shared?1:0);
            p.BridgeDelta=!p.Existing&&Map.CrossesRiver(Nodes[a],Nodes[b])?1:0;
            if(!p.Existing&&p.AddedCable>MaxLinkLength)p.Error=NetworkEditError.TooLong;
            else if(!p.Existing&&Map.Blocked(Nodes[a],Nodes[b]))p.Error=NetworkEditError.Obstacle;
            else if(p.BridgeDelta>0&&BridgeCount+p.BridgeDelta>AvailableBridges)p.Error=NetworkEditError.BridgeBudget;
            else if(p.AddedCost>0&&CableCost+p.AddedCost>CableCostLimit+.00001)p.Error=NetworkEditError.CableBudget;
            else if(p.DedicatedDelta>0&&DedicatedCount+p.DedicatedDelta>AvailableDedicated)p.Error=NetworkEditError.DedicatedBudget;
            return p;
        }
        public NetworkEditError Connect(int a,int b,LinkKind kind)
        {
            var p=PreviewConnect(a,b,kind);if(p.Error!=NetworkEditError.None)return p.Error;
            if(p.Existing)return Paint(EdgeId(a,b),kind);
            Add(a,b,kind);edited=true;return NetworkEditError.None;
        }
        public NetworkEditError Paint(int id,LinkKind kind)
        {
            if(!CanEdit)return NetworkEditError.NotDesigning;GridLink l;if(!Links.TryGetValue(id,out l))return NetworkEditError.Invalid;
            var p=PreviewConnect(l.A,l.B,kind);if(p.Error!=NetworkEditError.None)return p.Error;
            if(l.Kind==kind)return NetworkEditError.None;l.Kind=kind;routeCache.Clear();edited=true;return NetworkEditError.None;
        }
        public bool Remove(int id){if(!CanEdit)return false;if(Links.Remove(id)){edited=true;routeCache.Clear();return true;}return false;}
        public void EndEdit(){if(edited){if(Continuous)RefreshLiveNetwork();else ReturnToDesign();}else if(history.Count>0)history.Pop();}
        public bool Undo(){if(!CanEdit||history.Count==0)return false;var old=history.Pop();Links.Clear();foreach(var l in old)Links.Add(l.Id,l);routeCache.Clear();if(Continuous)RefreshLiveNetwork();else ReturnToDesign();return true;}
        public List<int> Path(int source,int target,SignalKind type)
        {
            if(source<0||source>=Nodes.Length||target<0||target>=Nodes.Length)return null;
            long key=((long)source<<33)|((long)target<<1)|(type==SignalKind.Data?1L:0L);List<int> cached;if(routeCache.TryGetValue(key,out cached))return cached;
            int count=Nodes.Length;var dist=new double[count];var prev=new int[count];var used=new bool[count];for(int i=0;i<count;i++){dist[i]=double.PositiveInfinity;prev[i]=-1;}dist[source]=0;
            List<int> result=null;
            for(int pass=0;pass<count;pass++){
                int u=-1;for(int i=0;i<count;i++)if(!used[i]&&(u<0||dist[i]<dist[u]))u=i;if(u<0||double.IsInfinity(dist[u]))break;
                if(u==target){result=new List<int>();for(int p=target;p>=0;p=prev[p])result.Insert(0,p);break;}used[u]=true;
                foreach(var l in Links.Values){if(!l.Allows(type))continue;int v=l.A==u?l.B:l.B==u?l.A:-1;if(v<0||used[v]||(v!=target&&Nodes[v].Kind!=NetworkNodeKind.Relay))continue;double alt=dist[u]+l.Length;if(alt<dist[v]-.000001){dist[v]=alt;prev[v]=u;}}
            }
            routeCache[key]=result;return result;
        }
        double PathLength(List<int> path){double sum=0;for(int i=1;i<path.Count;i++)sum+=Distance(path[i-1],path[i]);return sum;}
        List<List<int>> SourcePaths(GridNode target,SignalKind kind)
        {
            var paths=new List<List<int>>();foreach(var source in Nodes)if(source.Kind==(kind==SignalKind.Power?NetworkNodeKind.Reactor:NetworkNodeKind.DataCore)){var path=Path(source.Id,target.Id,kind);if(path!=null)paths.Add(path);}
            paths.Sort((a,b)=>{int c=PathLength(a).CompareTo(PathLength(b));return c!=0?c:a[0].CompareTo(b[0]);});return paths;
        }
        void AllocatePower()
        {
            powerFlows.Clear();powerInspections=null;
            foreach(var n in Nodes){n.PowerLoad=0;n.PowerReceived=0;}foreach(var l in Links.Values)l.PowerLoad=0;
            var targets=new List<GridNode>();var routes=new List<List<List<int>>>();foreach(var n in Nodes)if(n.Kind==NetworkNodeKind.District){targets.Add(n);routes.Add(SourcePaths(n,SignalKind.Power));}
            bool changed=true;while(changed){changed=false;for(int d=0;d<targets.Count;d++){
                var target=targets[d];if(target.PowerReceived>=target.PowerDemand-.00001)continue;
                foreach(var path in routes[d]){double flow=Math.Min(.125,target.PowerDemand-target.PowerReceived);var source=Nodes[path[0]];flow=Math.Min(flow,source.PowerSupply-source.PowerLoad);
                    foreach(int id in path)if(Nodes[id].Kind==NetworkNodeKind.Relay)flow=Math.Min(flow,Nodes[id].Capacity-Nodes[id].PowerLoad);
                    for(int j=0;j<path.Count-1;j++)flow=Math.Min(flow,LinkCapacity-Links[EdgeId(path[j],path[j+1])].PowerLoad);
                    if(flow<=.00001)continue;RecordPowerFlow(path,target.Id,flow);changed=true;target.PowerReceived+=flow;foreach(int id in path)Nodes[id].PowerLoad+=flow;for(int j=0;j<path.Count-1;j++)Links[EdgeId(path[j],path[j+1])].PowerLoad+=flow;break;
                }
            }}
        }
        void RefreshLiveNetwork()
        {
            routeCache.Clear();AllocatePower();StableSeconds=0;
            var failed=new List<DataPacket>();foreach(var p in Packets){var path=Path(p.Node,p.Destination,SignalKind.Data);if(path==null){Fail(p);failed.Add(p);}else{p.Path=path;p.Index=0;p.Progress=0;p.Travelling=false;}}
            foreach(var p in failed)Packets.Remove(p);edited=false;
        }
        public void ReturnToDesign(){Phase=RoundPhase.Design;Time=0;Successful=Expired=serial=0;spawnClock=delaySum=powerIntegral=0;Packets.Clear();routeCache.Clear();Array.Clear(nodeCredit,0,nodeCredit.Length);foreach(var n in Nodes){n.Delivered=n.Failed=n.Processed=0;n.LastDelivered=-1;}foreach(var l in Links.Values){l.Credit=0;l.Sent=0;}UpdateDistrictDemand();AllocatePower();ResetDataSupply();ResetQuality();edited=false;}
        void BeginRunning(){Phase=RoundPhase.Running;foreach(var l in Links.Values)l.Credit=1;for(int i=0;i<nodeCredit.Length;i++)nodeCredit[i]=1;}
        public void Start(){if(Continuous&&Phase==RoundPhase.Running)return;ReturnToDesign();BeginRunning();}
        void Fail(DataPacket p){Expired++;Nodes[p.Destination].Failed++;}
        public void Step(double dt)
        {
            if(Phase!=RoundPhase.Running||dt<=0)return;if(!Continuous)dt=Math.Min(dt,Duration-Time);Time+=dt;UpdateDistrictDemand();powerIntegral+=PowerCoverage*dt;
            foreach(var n in Nodes){double rate=Math.Max(0,n.Capacity-n.PowerLoad);nodeCredit[n.Id]=Math.Min(Math.Max(1,rate*.5),nodeCredit[n.Id]+rate*dt);}
            foreach(var l in Links.Values){double rate=l.Kind==LinkKind.PowerOnly?0:Math.Max(0,LinkCapacity-l.PowerLoad);l.Credit=Math.Min(Math.Max(1,rate*.5),l.Credit+rate*dt);}
            if(Continuous)SupplyData(dt);else {spawnClock+=dt;while(spawnClock>=.65){spawnClock-=.65;foreach(var target in Nodes)if(target.Kind==NetworkNodeKind.District){var paths=SourcePaths(target,SignalKind.Data);if(paths.Count==0){Expired++;target.Failed++;}else Packets.Add(new DataPacket{Id=serial++,Destination=target.Id,Path=paths[0],Born=Time});}}}
            var finished=new List<DataPacket>();foreach(var p in Packets){
                if(Time-p.Born>RequestDeadline(p.Destination)){Fail(p);finished.Add(p);continue;}
                if(p.Travelling){var link=Links[EdgeId(p.Node,p.Next)];p.Progress+=dt/(link.Length*.07+.10);if(p.Progress>=1){p.Index++;p.Progress=0;p.Travelling=false;if(p.Index==p.Path.Count-1){if(Nodes[p.Destination].PowerFraction>=.95){Successful++;Nodes[p.Destination].Delivered++;Nodes[p.Destination].LastDelivered=Time;delaySum+=Time-p.Born;}else Fail(p);finished.Add(p);continue;}}}
                if(!p.Travelling){if(p.Index>=p.Path.Count-1){finished.Add(p);continue;}var l=Links[EdgeId(p.Node,p.Next)];if(nodeCredit[p.Node]>=1&&l.Credit>=1&&Nodes[p.Node].Capacity-Nodes[p.Node].PowerLoad>0){nodeCredit[p.Node]-=1;l.Credit-=1;l.Sent++;Nodes[p.Node].Processed++;p.Travelling=true;}}
            }
            foreach(var p in finished)Packets.Remove(p);
            if(Continuous)UpdateQuality(dt);else if(Time>=Duration-.00001){Time=Duration;Phase=RoundPhase.Result;}
        }
        public string Summary(){return "power="+AveragePower.ToString("P1")+", data="+DataReliability.ToString("P1")+", latency="+AverageLatency.ToString("F2")+"s, delivered="+Successful+", expired="+Expired;}
    }
}

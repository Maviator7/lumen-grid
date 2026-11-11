using System;
using System.Collections.Generic;
namespace Michi.Core
{
    [Serializable] public sealed class SavedRequest {public int Destination;public double Born;}
    [Serializable] public sealed class SavedEdge {public int A,B;}
    [Serializable] public sealed class SavedSector {public ExpansionDirection Direction;public SectorTerrain Terrain;public CityObstacle[] Obstacles;public SavedEdge[] DemandRoute;public bool North;public double Z;public int West,East,District,Reactor,DataCore;public SavedEdge[] Feeders;}
    [Serializable] public sealed class SavedQuality {public double Time,Power,Delay;public int Good,Bad;public int[] Dispatched,Delivered;}
    [Serializable] public sealed class NetworkSave
    {
        public int RulesVersion;
        public int Version,Generator,Seed,ExpansionCount,Successful,Expired,Serial,RecentDelivered;
        public double Time,DelaySum,PowerIntegral,SpawnClock,ExpansionCable,QualityClock,StableSeconds,RecentPower,RecentData,RecentLatency;
        public GridNode[] Nodes;public GridLink[] Links;public DataPacket[] Packets;public double[] NodeCredit,SourceCredit,RequestCredit;
        public SavedRequest[] Waiting;public SavedSector[] Sectors;public SavedQuality[] Quality;
    }
    public sealed partial class NetworkSimulation
    {
        public NetworkSave CaptureSave()
        {
            if(!Continuous||!Map.Generated)throw new InvalidOperationException("操作ガイドは本編のセーブに上書きしません。");
            var state=new NetworkSave{RulesVersion=RulesVersion,Version=1,Generator=CityMap.GeneratorVersion,Seed=Map.Seed,ExpansionCount=ExpansionCount,Successful=Successful,Expired=Expired,Serial=serial,RecentDelivered=RecentDelivered,Time=Time,DelaySum=delaySum,PowerIntegral=powerIntegral,SpawnClock=spawnClock,ExpansionCable=expansionCable,QualityClock=qualityClock,StableSeconds=StableSeconds,RecentPower=RecentPower,RecentData=RecentData,RecentLatency=RecentLatency,NodeCredit=(double[])nodeCredit.Clone(),Nodes=new GridNode[Nodes.Length],SourceCredit=new double[Nodes.Length],RequestCredit=new double[Nodes.Length]};
            for(int i=0;i<Nodes.Length;i++){state.Nodes[i]=Nodes[i].SaveCopy();state.SourceCredit[i]=Nodes[i].SourceCredit;state.RequestCredit[i]=Nodes[i].RequestCredit;}
            var links=new List<GridLink>();foreach(var l in Links.Values)links.Add(l.SaveCopy());state.Links=links.ToArray();
            var packets=new List<DataPacket>();foreach(var p in Packets)packets.Add(p.SaveCopy());state.Packets=packets.ToArray();
            var requests=new List<SavedRequest>();foreach(var r in waitingData)requests.Add(new SavedRequest{Destination=r.Destination,Born=r.Born});state.Waiting=requests.ToArray();
            var savedSectors=new List<SavedSector>();foreach(var s in sectors){var edges=new List<SavedEdge>();foreach(var e in s.Feeders)edges.Add(new SavedEdge{A=e[0],B=e[1]});var routes=new List<SavedEdge>();foreach(var e in s.DemandRoute)routes.Add(new SavedEdge{A=e[0],B=e[1]});var blocks=new List<CityObstacle>();foreach(var o in s.Obstacles)blocks.Add(CopyObstacle(o));savedSectors.Add(new SavedSector{Direction=s.Direction,Terrain=s.Terrain,Obstacles=blocks.ToArray(),DemandRoute=routes.ToArray(),North=s.North,Z=s.Z,West=s.West,East=s.East,District=s.District,Reactor=s.Reactor,DataCore=s.DataCore,Feeders=edges.ToArray()});}state.Sectors=savedSectors.ToArray();
            var quality=new List<SavedQuality>();foreach(var q in samples)quality.Add(new SavedQuality{Time=q.Time,Power=q.Power,Delay=q.Delay,Good=q.Good,Bad=q.Bad,Dispatched=(int[])q.Dispatched.Clone(),Delivered=(int[])q.Delivered.Clone()});state.Quality=quality.ToArray();return state;
        }
        static bool Finite(double n){return !double.IsNaN(n)&&!double.IsInfinity(n);}
        static void RequireSave(bool ok){if(!ok)throw new ArgumentException("セーブの形式・バージョン、または都市データが不正です。");}
        public static NetworkSimulation RestoreSave(NetworkSave d)
        {
            RequireSave(d!=null&&d.Version==1&&d.Generator==CityMap.GeneratorVersion&&d.RulesVersion>=0&&d.RulesVersion<=CurrentRulesVersion);
            RequireSave(d.Nodes!=null&&d.Nodes.Length>=10&&d.Nodes.Length<=4096&&d.Links!=null&&d.Links.Length<=20000&&d.Packets!=null&&d.Packets.Length<=100000&&d.Waiting!=null&&d.Waiting.Length<=100000&&d.Sectors!=null&&d.Quality!=null&&d.Quality.Length>=1&&d.Quality.Length<=32);
            int count=d.Nodes.Length;RequireSave(d.NodeCredit!=null&&d.SourceCredit!=null&&d.RequestCredit!=null&&d.NodeCredit.Length==count&&d.SourceCredit.Length==count&&d.RequestCredit.Length==count);
            RequireSave(d.ExpansionCount==d.Sectors.Length&&d.ExpansionCount>=0&&Finite(d.Time)&&d.Time>=0&&Finite(d.ExpansionCable)&&d.ExpansionCable>=0&&Finite(d.DelaySum)&&d.DelaySum>=0&&Finite(d.PowerIntegral)&&d.PowerIntegral>=0&&Finite(d.SpawnClock)&&Finite(d.QualityClock)&&d.QualityClock>=0&&d.QualityClock<1.1&&Finite(d.StableSeconds)&&d.StableSeconds>=0&&d.StableSeconds<=45&&d.Successful>=0&&d.Expired>=0&&d.Serial>=0&&Finite(d.RecentPower)&&Finite(d.RecentData)&&Finite(d.RecentLatency));
            var s=new NetworkSimulation(CityMap.Generate(d.Seed),true,d.RulesVersion);s.Nodes=new GridNode[count];
            for(int i=0;i<count;i++){var n=d.Nodes[i];RequireSave(n!=null&&n.Id==i&&n.Kind>=NetworkNodeKind.Reactor&&n.Kind<=NetworkNodeKind.District&&Finite(n.X)&&Finite(n.Z)&&Math.Abs(n.X)<100000&&Math.Abs(n.Z)<100000&&Finite(n.Capacity)&&n.Capacity>0&&Finite(n.PowerDemand)&&n.PowerDemand>=0&&Finite(n.PowerSupply)&&n.PowerSupply>=0&&Finite(n.DataDemand)&&n.DataDemand>=0&&Finite(n.DataSupply)&&n.DataSupply>=0&&Finite(d.NodeCredit[i])&&d.NodeCredit[i]>=0&&Finite(d.SourceCredit[i])&&d.SourceCredit[i]>=0&&Finite(d.RequestCredit[i])&&d.RequestCredit[i]>=0);RequireSave(!s.ModernRules||n.Kind!=NetworkNodeKind.District||(n.Role>=7&&n.Role<=9));s.Nodes[i]=n.SaveCopy();s.Nodes[i].SourceCredit=d.SourceCredit[i];s.Nodes[i].RequestCredit=d.RequestCredit[i];}
            s.Links.Clear();foreach(var l in d.Links){RequireSave(l!=null&&l.A>=0&&l.B>=0&&l.A<count&&l.B<count&&l.A!=l.B&&l.Kind>=LinkKind.Shared&&l.Kind<=LinkKind.DataOnly&&Finite(l.Credit)&&l.Credit>=0);int id=EdgeId(l.A,l.B);RequireSave(!s.Links.ContainsKey(id));var copy=l.SaveCopy();copy.Length=s.Distance(l.A,l.B);RequireSave(copy.Length<=MaxLinkLength+.00001&&!s.Map.Blocked(s.Nodes[l.A],s.Nodes[l.B]));s.Links.Add(id,copy);}
            s.ExpansionCount=d.ExpansionCount;s.expansionCable=d.ExpansionCable;RequireSave(s.CableCost<=s.CableCostLimit+.0001&&s.DedicatedCount<=s.AvailableDedicated&&s.BridgeCount<=s.AvailableBridges);
            s.sectors.Clear();foreach(var a in d.Sectors){
                RequireSave(a!=null&&Finite(a.Z)&&a.West>=0&&a.East>=0&&a.District>=0&&a.West<count&&a.East<count&&a.District<count&&a.Reactor>=-1&&a.DataCore>=-1&&a.Reactor<count&&a.DataCore<count&&a.Feeders!=null);
                RequireSave(a.Direction>=ExpansionDirection.None&&a.Direction<=ExpansionDirection.West&&a.Terrain>=SectorTerrain.Riverbank&&a.Terrain<=SectorTerrain.Detour);
                var sector=new CitySector{GrowthDirection=a.Direction,Terrain=a.Terrain,North=a.North,Z=a.Z,West=a.West,East=a.East,District=a.District,Reactor=a.Reactor,DataCore=a.DataCore};
                foreach(var e in a.Feeders){RequireSave(e!=null&&e.A>=0&&e.B>=0&&e.A<count&&e.B<count);sector.Feeders.Add(new[]{e.A,e.B});}
                if(a.Obstacles!=null){RequireSave(a.Obstacles.Length<=64);foreach(var o in a.Obstacles){RequireSave(o!=null&&Finite(o.X)&&Finite(o.Z)&&Math.Abs(o.X)<100000&&Math.Abs(o.Z)<100000&&Finite(o.Width)&&Finite(o.Depth)&&o.Width>0&&o.Depth>0&&o.Width<100&&o.Depth<100);sector.Obstacles.Add(CopyObstacle(o));}}
                if(a.DemandRoute!=null){RequireSave(a.DemandRoute.Length<=256);int previousNode=a.East;var visited=new HashSet<int>{a.East};for(int i=0;i<a.DemandRoute.Length;i++){var e=a.DemandRoute[i];RequireSave(e!=null&&e.A==previousNode&&e.B>=0&&e.B<count&&visited.Add(e.B)&&(i==a.DemandRoute.Length-1?e.B==a.District:s.Nodes[e.B].Kind==NetworkNodeKind.Relay));sector.DemandRoute.Add(new[]{e.A,e.B});previousNode=e.B;}if(a.DemandRoute.Length>0)RequireSave(previousNode==a.District);}
                if(sector.Direction==ExpansionDirection.East||sector.Direction==ExpansionDirection.West)RequireSave(sector.Obstacles.Count>0&&sector.DemandRoute.Count>0&&sector.Terrain!=SectorTerrain.Riverbank);
                s.sectors.Add(sector);
            }
            foreach(var sector in s.sectors)foreach(var edge in sector.DemandRoute)RequireSave(s.Distance(edge[0],edge[1])<=MaxLinkLength+.00001&&!s.IsBlocked(s.Nodes[edge[0]],s.Nodes[edge[1]]));
            foreach(var link in s.Links.Values)RequireSave(!s.IsBlocked(s.Nodes[link.A],s.Nodes[link.B]));
            s.Packets.Clear();foreach(var p in d.Packets){RequireSave(p!=null&&p.Destination>=0&&p.Destination<count&&s.Nodes[p.Destination].Kind==NetworkNodeKind.District&&p.Path!=null&&p.Path.Count>=2&&p.Index>=0&&p.Index<p.Path.Count&&(!p.Travelling||p.Index<p.Path.Count-1)&&Finite(p.Born)&&p.Born>=0&&p.Born<=d.Time&&Finite(p.Progress)&&p.Progress>=0&&p.Progress<1&&p.Path[p.Path.Count-1]==p.Destination);for(int i=0;i<p.Path.Count;i++){RequireSave(p.Path[i]>=0&&p.Path[i]<count);if(i>p.Index){GridLink l;RequireSave(s.Links.TryGetValue(EdgeId(p.Path[i-1],p.Path[i]),out l)&&l.Allows(SignalKind.Data));}}s.Packets.Add(p.SaveCopy());}
            s.waitingData.Clear();foreach(var r in d.Waiting){RequireSave(r!=null&&r.Destination>=0&&r.Destination<count&&s.Nodes[r.Destination].Kind==NetworkNodeKind.District&&Finite(r.Born)&&r.Born>=0&&r.Born<=d.Time);s.waitingData.Enqueue(new DataRequest(r.Destination,r.Born));}
            s.samples.Clear();double previous=-1;foreach(var q in d.Quality){RequireSave(q!=null&&Finite(q.Time)&&q.Time>=previous&&q.Time<=d.Time&&Finite(q.Power)&&Finite(q.Delay)&&q.Good>=0&&q.Bad>=0&&q.Dispatched!=null&&q.Delivered!=null&&q.Dispatched.Length<=count&&q.Delivered.Length==q.Dispatched.Length);previous=q.Time;s.samples.Add(new QualitySample{Time=q.Time,Power=q.Power,Delay=q.Delay,Good=q.Good,Bad=q.Bad,Dispatched=(int[])q.Dispatched.Clone(),Delivered=(int[])q.Delivered.Clone()});}
            s.Time=d.Time;s.Successful=d.Successful;s.Expired=d.Expired;s.serial=d.Serial;s.delaySum=d.DelaySum;s.powerIntegral=d.PowerIntegral;s.spawnClock=d.SpawnClock;s.nodeCredit=(double[])d.NodeCredit.Clone();s.qualityClock=d.QualityClock;s.StableSeconds=d.StableSeconds;s.RecentPower=d.RecentPower;s.RecentData=d.RecentData;s.RecentLatency=d.RecentLatency;s.RecentDelivered=d.RecentDelivered;s.history.Clear();s.routeCache.Clear();s.offerCache=null;s.Phase=RoundPhase.Running;s.AllocatePower();return s;
        }
    }
}

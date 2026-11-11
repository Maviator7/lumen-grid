using System;
using System.Collections.Generic;
namespace Michi.Core
{
    public sealed partial class NetworkSimulation
    {
        public IEnumerable<CityObstacle> AllObstacles {get{foreach(var o in Map.Obstacles)yield return o;foreach(var sector in sectors)foreach(var o in sector.Obstacles)yield return o;}}
        public bool IsBlocked(GridNode a,GridNode b){foreach(var o in AllObstacles)if(o.Intersects(a.X,a.Z,b.X,b.Z,.12))return true;return false;}
        static CityObstacle CopyObstacle(CityObstacle o){return new CityObstacle{X=o.X,Z=o.Z,Width=o.Width,Depth=o.Depth};}
        static CitySector CopySector(CitySector old)
        {
            var copy=new CitySector{North=old.North,GrowthDirection=old.GrowthDirection,Terrain=old.Terrain,Z=old.Z,West=old.West,East=old.East,District=old.District,Reactor=old.Reactor,DataCore=old.DataCore};
            foreach(var e in old.Feeders)copy.Feeders.Add((int[])e.Clone());foreach(var e in old.DemandRoute)copy.DemandRoute.Add((int[])e.Clone());foreach(var o in old.Obstacles)copy.Obstacles.Add(CopyObstacle(o));return copy;
        }
        public static string DirectionName(ExpansionDirection d){return d==ExpansionDirection.North?"北":d==ExpansionDirection.South?"南":d==ExpansionDirection.East?"東":"西";}
        public static string TerrainName(SectorTerrain t){return t==SectorTerrain.Dense?"密集地区":t==SectorTerrain.Detour?"迂回地区":"河岸地区";}
        public static string TerrainHint(SectorTerrain t){return t==SectorTerrain.Dense?"建設不可範囲の間を、中継で抜ける。":t==SectorTerrain.Detour?"障害物を迂回し、距離と負荷を抑える。":"両岸を橋でつなぐ。川越え枠に注意。";}
        static CityObstacle SiteFootprint(GridNode n){return new CityObstacle{X=n.X+(n.Kind==NetworkNodeKind.District?.7:0),Z=n.Z+(n.Kind==NetworkNodeKind.District?.2:0),Width=n.Kind==NetworkNodeKind.District?3.6:1.55,Depth=n.Kind==NetworkNodeKind.District?3.6:1.55};}
        bool ClearLateralParent(GridNode n)
        {
            foreach(var o in AllObstacles)if(o.Intersects(n.X,n.Z,n.X,n.Z,.9))return false;
            foreach(var other in Nodes)if(other.Id!=n.Id&&SiteFootprint(other).Intersects(n.X,n.Z,n.X,n.Z,.9))return false;
            return true;
        }
        ExpansionOffer BuildLateralOffer(ExpansionDirection direction)
        {
            bool east=direction==ExpansionDirection.East;double sign=east?1:-1,outer=Nodes[0].X;
            foreach(var n in Nodes)outer=east?Math.Max(outer,n.X):Math.Min(outer,n.X);
            foreach(var o in AllObstacles)outer=east?Math.Max(outer,o.X+o.Width/2):Math.Min(outer,o.X-o.Width/2);
            double frontier=east?double.NegativeInfinity:double.PositiveInfinity;
            foreach(var n in Nodes)if(n.Kind==NetworkNodeKind.Relay&&ClearLateralParent(n))frontier=east?Math.Max(frontier,n.X):Math.Min(frontier,n.X);
            int lower=-1,upper=-1;foreach(var n in Nodes)if(n.Kind==NetworkNodeKind.Relay&&ClearLateralParent(n)&&Math.Abs(n.X-frontier)<4.5){if(lower<0||n.Z<Nodes[lower].Z)lower=n.Id;if(upper<0||n.Z>Nodes[upper].Z)upper=n.Id;}
            if(lower<0||upper<0)return new ExpansionOffer{Direction=direction,UnavailableReason="外縁の中継に安全な進入路がありません。",Title=DirectionName(direction)+"の地区"};
            var rng=new CityRandom(unchecked(Map.Seed+ExpansionCount*7919+(east?809:1201)));int role=7+rng.Int(3);
            double x=outer+sign*5.5,z=(Nodes[lower].Z+Nodes[upper].Z)/2;
            var terrain=((unchecked(Map.Seed+ExpansionCount)+(east?0:1))&1)==0?SectorTerrain.Dense:SectorTerrain.Detour;
            var offer=new ExpansionOffer{Direction=direction,Terrain=terrain,Z=z,Role=role,Demand=ModernRules?DistrictBasePower(role):role==7?6:role==8?3:4,Title=DirectionName(direction)+"の"+(role==7?"工業地区":role==8?"住宅地区":"医療地区"),WestParent=lower,EastParent=upper,Utilities=ExpansionCount%2==1,DistrictPoint=new CityPoint(x+sign*6.5,z+1),ReactorPoint=new CityPoint(x-sign*2,z-3.8),DataPoint=new CityPoint(x-sign*2,z+3.8)};
            if(terrain==SectorTerrain.Dense){
                offer.Obstacles.Add(new CityObstacle{X=x+sign*2.5,Z=z-2,Width=2.2,Depth=2.8});
                offer.Obstacles.Add(new CityObstacle{X=x+sign*2.5,Z=z+2.9,Width=2.2,Depth=2.4});
                offer.DistrictRoute=new List<CityPoint>{new CityPoint(x+sign,z+.55),offer.DistrictPoint};
            }else{
                offer.Obstacles.Add(new CityObstacle{X=x+sign*2,Z=z+2,Width=1.8,Depth=2.4});
                offer.DistrictRoute=new List<CityPoint>{new CityPoint(x,z+5),new CityPoint(x+sign*4,z+5),offer.DistrictPoint};
            }
            offer.WestRoute=PlanLateralRoute(lower,new CityPoint(x,z-3));offer.EastRoute=PlanLateralRoute(upper,new CityPoint(x,z+3),offer.WestRoute);
            if(offer.WestRoute==null||offer.EastRoute==null){offer.UnavailableReason="建設不可範囲を避ける進入路がありません。";return offer;}
            offer.CableGrant=36+CorridorLength(lower,offer.WestRoute)+CorridorLength(upper,offer.EastRoute);
            for(int attempt=0;attempt<2;attempt++){
                var test=PlanningClone();test.CommitExpansion(offer);test.LoadSolution();
                if(test.ReferenceValid){for(int i=0;i<10800&&!test.ExpansionReady;i++)test.Step(1.0/60);if(test.ExpansionReady){offer.Available=true;break;}}
                offer.Utilities=true;
            }
            return offer;
        }
        // Visibility vertices sit beyond the inclusive padded obstacle boundary.
        List<CityPoint> PlanLateralRoute(int parent,CityPoint target,List<CityPoint> reserved=null)
        {
            var barriers=new List<CityObstacle>();foreach(var o in AllObstacles)barriers.Add(o);
            foreach(var n in Nodes)if(n.Id!=parent)barriers.Add(SiteFootprint(n));
            if(reserved!=null)foreach(var p in reserved)barriers.Add(new CityObstacle{X=p.X,Z=p.Z,Width=1.55,Depth=1.55});
            var vertices=new List<CityPoint>{new CityPoint(Nodes[parent].X,Nodes[parent].Z),target};
            foreach(var o in barriers)foreach(double dx in new[]{-1.0,1.0})foreach(double dz in new[]{-1.0,1.0}){
                var p=new CityPoint(o.X+dx*(o.Width/2+1.0),o.Z+dz*(o.Depth/2+1.0));
                if((p.X-Map.RiverX)*(target.X-Map.RiverX)>0&&(Math.Abs(p.X-Nodes[parent].X)>=1.6||Math.Abs(p.Z-Nodes[parent].Z)>=1.6))vertices.Add(p);
            }
            int count=vertices.Count;var dist=new double[count];var previous=new int[count];var used=new bool[count];for(int i=0;i<count;i++){dist[i]=double.PositiveInfinity;previous[i]=-1;}dist[0]=0;
            for(int pass=0;pass<count;pass++){
                int u=-1;for(int i=0;i<count;i++)if(!used[i]&&(u<0||dist[i]<dist[u]))u=i;if(u<0||double.IsInfinity(dist[u]))return null;if(u==1)break;used[u]=true;
                for(int v=0;v<count;v++)if(v!=u&&!used[v]){var a=PointNode(vertices[u]);var b=PointNode(vertices[v]);if(Map.CrossesRiver(a,b))continue;bool blocked=false;foreach(var o in barriers)if(o.Intersects(a.X,a.Z,b.X,b.Z,.9)){blocked=true;break;}if(blocked)continue;double next=dist[u]+Distance(a,b);if(next<dist[v]-.000001){dist[v]=next;previous[v]=u;}}
            }
            if(previous[1]<0)return null;var reverse=new List<CityPoint>();for(int v=1;v>0;v=previous[v])reverse.Add(vertices[v]);reverse.Reverse();
            var result=new List<CityPoint>();var last=vertices[0];foreach(var next in reverse){double length=Distance(PointNode(last),PointNode(next));int pieces=Math.Max(1,(int)Math.Ceiling(length/8));for(int i=1;i<=pieces;i++)result.Add(new CityPoint(last.X+(next.X-last.X)*i/pieces,last.Z+(next.Z-last.Z)*i/pieces));last=next;}return result;
        }
        void CommitLateralExpansion(ExpansionOffer offer)
        {
            expansionCable+=offer.CableGrant;ExpansionCount++;string prefix=DirectionName(offer.Direction)+ExpansionCount;
            var sector=new CitySector{GrowthDirection=offer.Direction,Terrain=offer.Terrain,Z=offer.Z};
            sector.West=AddCorridor(offer.WestParent,offer.WestRoute,sector,prefix+"A",true);sector.East=AddCorridor(offer.EastParent,offer.EastRoute,sector,prefix+"B",true);
            var d=offer.DistrictPoint;sector.District=AppendNode(prefix+".D",prefix+(offer.Role==7?" 工業区":offer.Role==8?" 居住区":" 医療区"),d.X,d.Z,NetworkNodeKind.District,offer.Role,offer.Demand);
            int last=sector.East;for(int i=0;i<offer.DistrictRoute.Count;i++){
                var p=offer.DistrictRoute[i];int next=i==offer.DistrictRoute.Count-1?sector.District:AppendNode(prefix+".L"+i,prefix+" 通路中継"+(i+1),p.X,p.Z,NetworkNodeKind.Relay,2);
                sector.DemandRoute.Add(new[]{last,next});last=next;
            }
            foreach(var o in offer.Obstacles)sector.Obstacles.Add(CopyObstacle(o));
            if(offer.Utilities){sector.Reactor=AppendNode(prefix+".P",prefix+" 発電所",offer.ReactorPoint.X,offer.ReactorPoint.Z,NetworkNodeKind.Reactor,0);sector.DataCore=AppendNode(prefix+".I",prefix+" データセンター",offer.DataPoint.X,offer.DataPoint.Z,NetworkNodeKind.DataCore,1);}
            Nodes[0].PowerSupply=18+ExpansionCount*4;Nodes[0].Capacity=Nodes[0].PowerSupply+6;sectors.Add(sector);history.Clear();offerCache=null;RefreshLiveNetwork();
        }
        void ApplyLateralReference(CitySector sector)
        {
            foreach(var e in sector.Feeders)ReferenceConnect(e[0],e[1],e[1]==sector.West?LinkKind.PowerOnly:LinkKind.Shared);
            ReferenceConnect(sector.West,sector.East,LinkKind.Shared);foreach(var e in sector.DemandRoute)ReferenceConnect(e[0],e[1],LinkKind.Shared);
            if(sector.Reactor>=0){ReferenceConnect(sector.Reactor,sector.West,LinkKind.Shared);ReferenceConnect(sector.DataCore,sector.East,LinkKind.Shared);}
        }
    }
}

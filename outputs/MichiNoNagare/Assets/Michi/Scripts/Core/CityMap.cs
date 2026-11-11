using System;
using System.Collections.Generic;
namespace Michi.Core
{
    [Serializable] public sealed class CityObstacle
    {
        public double X,Z,Width,Depth;
        public bool Intersects(double ax,double az,double bx,double bz,double padding=0)
        {
            double lo=0,hi=1;return Clip(ax,bx-ax,X-Width/2-padding,X+Width/2+padding,ref lo,ref hi)&&Clip(az,bz-az,Z-Depth/2-padding,Z+Depth/2+padding,ref lo,ref hi);
        }
        static bool Clip(double a,double d,double min,double max,ref double lo,ref double hi){if(Math.Abs(d)<1e-9)return a>=min&&a<=max;double p=(min-a)/d,q=(max-a)/d;if(p>q){double t=p;p=q;q=t;}lo=Math.Max(lo,p);hi=Math.Min(hi,q);return lo<=hi;}
    }
    // Explicit integer PRNG: map version 1 is independent of framework Random implementations.
    public sealed class CityRandom
    {
        uint state;
        public CityRandom(int seed){state=unchecked((uint)seed)^0x9e3779b9u;if(state==0)state=0x6d2b79f5u;}
        public uint Next(){uint x=state;x^=x<<13;x^=x>>17;x^=x<<5;return state=x;}
        public double Unit(){return Next()/4294967296.0;}
        public double Range(double a,double b){return a+(b-a)*Unit();}
        public int Int(int count){return (int)(Next()%(uint)count);}
    }
    public sealed class CityMap
    {
        public const int GeneratorVersion=1;
        public readonly int Seed;public readonly bool Generated;
        public readonly double RiverX,RiverWidth,CableAllowance;
        public readonly int BridgeLimit;
        public readonly CityObstacle[] Obstacles;
        readonly GridNode[] sites;
        readonly int[][] edges;
        static readonly string[] Codes={"PWR.01","DAT.01","R.01","R.02","NEXUS","R.03","R.04","IND.07","RES.08","MED.09"};
        static readonly string[] Names={"発電所","データセンター","中継①","中継②","中央ハブ","中継③","中継④","工業区","居住区","医療区"};
        static readonly double[,] Base={{-10,-4},{-10,4},{-5,-4},{-5,4},{0,0},{5,-4},{5,4},{10,-5},{11,0},{10,5}};
        public CityMap(int seed,bool generated,GridNode[] sites,int[][] edges,double riverX,double riverWidth,double cable,int bridges,CityObstacle[] obstacles){Seed=seed;Generated=generated;this.sites=sites;this.edges=edges;RiverX=riverX;RiverWidth=riverWidth;CableAllowance=cable;BridgeLimit=bridges;Obstacles=obstacles;}
        static GridNode Site(int i,double x,double z,double demand){return new GridNode(i,Codes[i],Names[i],x,z,i==0||i==1||i==2?24:i==5?32:i>=7?100:16,i==0?NetworkNodeKind.Reactor:i==1?NetworkNodeKind.DataCore:i>=7?NetworkNodeKind.District:NetworkNodeKind.Relay){PowerDemand=demand};}
        static int[][] Edges(int a=5,int b=5,int c=6){return new[]{new[]{0,2},new[]{1,3},new[]{2,4},new[]{3,4},new[]{4,5},new[]{4,6},new[]{a,7},new[]{b,8},new[]{c,9},new[]{5,6}};}
        public static CityMap Tutorial(){var n=new GridNode[10];for(int i=0;i<10;i++)n[i]=Site(i,Base[i,0],Base[i,1],i==7?6:i>=8?4:0);return new CityMap(291026,false,n,Edges(),1,2.2,78,99,new CityObstacle[0]);}
        public GridNode[] CreateNodes(){var copy=new GridNode[sites.Length];for(int i=0;i<sites.Length;i++){var n=sites[i];copy[i]=new GridNode(n.Id,n.Code,n.Name,n.X,n.Z,n.Capacity,n.Kind){Role=n.Role,PowerDemand=n.PowerDemand};}return copy;}
        public int[][] InitialEdges {get{return edges;}}
        public bool CrossesRiver(GridNode a,GridNode b){return (a.X-RiverX)*(b.X-RiverX)<0;}
        public bool Blocked(GridNode a,GridNode b){foreach(var o in Obstacles)if(o.Intersects(a.X,a.Z,b.X,b.Z,.12))return true;return false;}
        public static CityMap Generate(int seed)
        {
            var rng=new CityRandom(seed);
            for(int attempt=0;attempt<48;attempt++){var map=Candidate(seed,rng);if(Valid(map))return map;}
            // Known compact geometry; the original seed still drives architecture and skyline.
            var fallback=Tutorial();var sites=fallback.CreateNodes();return new CityMap(seed,true,sites,Edges(),2.2,1.7,82,3,new CityObstacle[0]);
        }
        static CityMap Candidate(int seed,CityRandom r)
        {
            var nodes=new GridNode[10];double mirrorX=r.Int(2)==0?1:-1,mirrorZ=r.Int(2)==0?1:-1,scale=r.Range(.76,.95);
            int[] slots={7,8,9};for(int i=2;i>0;i--){int j=r.Int(i+1),temp=slots[i];slots[i]=slots[j];slots[j]=temp;}
            int[] demand=r.Int(3)==0?new[]{7,4,4}:r.Int(2)==0?new[]{5,5,4}:new[]{6,4,4};
            for(int i=0;i<10;i++){int source=i>=7?slots[i-7]:i;double x=(Base[source,0]*scale+r.Range(-.55,.55))*mirrorX,z=(Base[source,1]*scale+r.Range(-.65,.65))*mirrorZ;nodes[i]=Site(i,x,z,i>=7?demand[i-7]:0);}
            int[][] links=Edges(r.Int(2)==0?5:6,5,r.Int(2)==0?5:6);
            double river=r.Range(1.9,2.6)*scale*mirrorX,width=r.Range(1.35,1.85);
            foreach(var n in nodes)if(Math.Abs(n.X-river)<width/2+.7)return null;
            var reserved=new List<int[]>(links);reserved.Add(new[]{2,5});
            var blocks=new List<CityObstacle>();int wanted=2+r.Int(3);
            for(int trial=0;trial<120&&blocks.Count<wanted;trial++){
                var o=new CityObstacle{X=r.Range(-11,11),Z=r.Range(-6,7),Width=r.Range(1.3,2.8),Depth=r.Range(1.3,2.8)};bool safe=Math.Abs(o.X-river)>o.Width/2+width/2+.3;
                foreach(var n in nodes)if(o.Intersects(n.X,n.Z,n.X,n.Z,n.Kind==NetworkNodeKind.District?2.8:1.1))safe=false;
                foreach(var e in reserved)if(o.Intersects(nodes[e[0]].X,nodes[e[0]].Z,nodes[e[1]].X,nodes[e[1]].Z,.3))safe=false;
                foreach(var old in blocks)if(Math.Abs(old.X-o.X)<(old.Width+o.Width)/2+.5&&Math.Abs(old.Z-o.Z)<(old.Depth+o.Depth)/2+.5)safe=false;
                if(safe)blocks.Add(o);
            }
            double length=0;foreach(var e in reserved){double dx=nodes[e[0]].X-nodes[e[1]].X,dz=nodes[e[0]].Z-nodes[e[1]].Z;length+=Math.Sqrt(dx*dx+dz*dz);}
            return new CityMap(seed,true,nodes,links,river,width,Math.Ceiling(length+r.Range(4,9)),3,blocks.ToArray());
        }
        static bool Valid(CityMap map)
        {
            if(map==null||map.Obstacles.Length<2)return false;var sim=new NetworkSimulation(map);
            foreach(var l in sim.Links.Values)if(l.Length>NetworkSimulation.MaxLinkLength||map.Blocked(sim.Nodes[l.A],sim.Nodes[l.B]))return false;
            if(sim.BridgeCount>map.BridgeLimit||sim.CableLength>map.CableAllowance||sim.PowerCoverage<.99)return false;
            sim.LoadSolution();if(!sim.Links.ContainsKey(NetworkSimulation.EdgeId(2,5))||sim.BridgeCount>map.BridgeLimit)return false;
            sim.Start();for(int i=0;i<2701;i++)sim.Step(1.0/60);return sim.Passed;
        }
    }
}

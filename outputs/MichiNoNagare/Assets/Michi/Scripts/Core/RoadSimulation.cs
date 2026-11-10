using System;
using System.Collections.Generic;

namespace Michi.Core
{
    public enum RoadKind { General, CommuteOnly, FreightOnly }
    public enum TravelRole { Commute, Freight }
    public enum Preset { Shared, Solution, Empty }
    public enum RoundPhase { Design, Running, Result }
    public enum EditError { None, Outside, Obstacle, RoadBudget, BridgeBudget, DedicatedBudget, NotDesigning }
    public struct Cell : IEquatable<Cell>
    {
        public readonly int X,Z;
        public Cell(int x,int z){X=x;Z=z;}
        public bool Equals(Cell other){return X==other.X&&Z==other.Z;}
        public override bool Equals(object obj){return obj is Cell&&Equals((Cell)obj);}
        public override int GetHashCode(){return X*397^Z;}
        public static bool operator ==(Cell a,Cell b){return a.Equals(b);}
        public static bool operator !=(Cell a,Cell b){return !a.Equals(b);}
        public override string ToString(){return X+","+Z;}
    }
    public sealed class Building
    {
        public Cell Position,Entry; public string Name,Kind; public TravelRole Role;
        public Building(int x,int z,int ex,int ez,string name,string kind,TravelRole role){Position=new Cell(x,z);Entry=new Cell(ex,ez);Name=name;Kind=kind;Role=role;}
    }
    public sealed class Vehicle
    {
        public int Id,Index; public TravelRole Role; public List<Cell> Path; public double Progress,Born,Velocity; public bool Done;
        public Cell From {get{return Path[Index];}} public Cell To {get{return Path[Index+1];}}
        public bool IsTruck {get{return Role==TravelRole.Freight;}}
        public double Speed {get{return IsTruck?1.6:2.6;}}
        public double Length {get{return IsTruck?.92:.43;}}
        public double Clearance {get{return IsTruck?.91:.27;}}
        public double X {get{return From.X+(To.X-From.X)*Progress-(To.Z-From.Z)*.17;}}
        public double Z {get{return From.Z+(To.Z-From.Z)*Progress+(To.X-From.X)*.17;}}
    }
    public sealed class PuzzleResult
    {
        public bool Complete,Passed; public bool[] Goals;
    }
    public sealed class RoadSimulation
    {
        public const int Width=24,Height=16,RiverX=12,RoadLimit=56,BridgeLimit=2,DedicatedLimit=6;
        public const double Duration=60;
        public static readonly Building[] Buildings={
            new Building(3,3,3,4,"こもれび住宅","home",TravelRole.Commute),
            new Building(21,3,21,4,"オフィス","office",TravelRole.Commute),
            new Building(3,7,3,8,"工場","factory",TravelRole.Freight),
            new Building(21,7,21,8,"物流倉庫","warehouse",TravelRole.Freight)};
        public static readonly Cell[] Rocks={new Cell(8,2),new Cell(9,2),new Cell(8,3),new Cell(17,13),new Cell(17,14),new Cell(18,14),new Cell(9,14)};
        public readonly Dictionary<Cell,RoadKind> Roads=new Dictionary<Cell,RoadKind>();
        public readonly List<Vehicle> Cars=new List<Vehicle>();
        public readonly Queue<double>[] Queues={new Queue<double>(),new Queue<double>()};
        public readonly int[] Arrived=new int[2];
        public RoundPhase Phase {get;private set;}
        public double Time {get;private set;}
        public int DedicatedCount {get{int n=0;foreach(var k in Roads.Values)if(k!=RoadKind.General)n++;return n;}}
        public int BridgeCount {get{int n=0;foreach(var k in Roads.Keys)if(k.X==RiverX)n++;return n;}}
        public int FreightDelivered {get{return Arrived[1]*6;}}
        public double CommuteAverage {get{return Arrived[0]>0?travel[0]/Arrived[0]:0;}}
        public int Waiting {get{int n=Queues[0].Count+Queues[1].Count;foreach(var c in Cars)if(c.Velocity<.1)n++;return n;}}
        public bool Connected {get{return routes[0]!=null&&routes[1]!=null;}}
        public List<Cell>[] Routes {get{return routes;}}
        readonly double[] clocks=new double[2],travel=new double[2];
        readonly List<Cell>[] routes=new List<Cell>[2];
        readonly Dictionary<Cell,Reservation> reservations=new Dictionary<Cell,Reservation>();
        readonly Stack<Dictionary<Cell,RoadKind>> history=new Stack<Dictionary<Cell,RoadKind>>();
        bool edited;int serial;
        struct Reservation {public int Id;public double Until;public Reservation(int id,double until){Id=id;Until=until;}}
        struct Bounds {public double X,Z,HX,HZ;}
        public RoadSimulation(){LoadPreset(Preset.Shared);}
        public IEnumerable<Cell> Neighbors(Cell p){var ns=new[]{new Cell(p.X+1,p.Z),new Cell(p.X-1,p.Z),new Cell(p.X,p.Z+1),new Cell(p.X,p.Z-1)};foreach(var n in ns)if(Inside(n))yield return n;}
        public static bool Inside(Cell p){return p.X>=0&&p.X<Width&&p.Z>=0&&p.Z<Height;}
        public static bool Blocked(Cell p){foreach(var b in Buildings)if(b.Position==p)return true;foreach(var r in Rocks)if(r==p)return true;return false;}
        void Line(int x1,int z1,int x2,int z2){for(int x=Math.Min(x1,x2);x<=Math.Max(x1,x2);x++)Roads[new Cell(x,z1)]=RoadKind.General;for(int z=Math.Min(z1,z2);z<=Math.Max(z1,z2);z++)Roads[new Cell(x2,z)]=RoadKind.General;}
        public void LoadPreset(Preset preset){Roads.Clear();history.Clear();if(preset!=Preset.Empty){Line(3,4,7,4);Line(7,4,7,11);Line(3,8,7,8);Line(7,6,18,6);Line(18,4,18,11);Line(18,4,21,4);Line(18,8,21,8);Line(7,11,18,11);if(preset==Preset.Solution)Roads[new Cell(12,6)]=RoadKind.CommuteOnly;}ReturnToDesign();}
        bool Allowed(Cell p,TravelRole role){RoadKind kind;return Roads.TryGetValue(p,out kind)&&(kind==RoadKind.General||(kind==RoadKind.CommuteOnly&&role==TravelRole.Commute)||(kind==RoadKind.FreightOnly&&role==TravelRole.Freight));}
        public List<Cell> Route(TravelRole role){int first=role==TravelRole.Commute?0:2;var start=Buildings[first].Entry;var end=Buildings[first+1].Entry;if(!Allowed(start,role)||!Allowed(end,role))return null;var q=new List<Cell>{start};var visited=new HashSet<Cell>{start};var previous=new Dictionary<Cell,Cell>();for(int i=0;i<q.Count;i++){var p=q[i];if(p==end){var path=new List<Cell>{end};while(p!=start){p=previous[p];path.Insert(0,p);}return path;}foreach(var n in Neighbors(p))if(Allowed(n,role)&&visited.Add(n)){previous[n]=p;q.Add(n);}}return null;}
        public void BeginEdit(){if(Phase!=RoundPhase.Design)return;history.Push(new Dictionary<Cell,RoadKind>(Roads));edited=false;}
        public EditError Edit(Cell p,RoadKind? kind)
        {
            if(Phase!=RoundPhase.Design)return EditError.NotDesigning;if(!Inside(p))return EditError.Outside;if(Blocked(p))return EditError.Obstacle;
            if(!kind.HasValue){if(Roads.Remove(p))edited=true;return EditError.None;}
            RoadKind old;bool exists=Roads.TryGetValue(p,out old);
            if(kind.Value!=RoadKind.General&&(!exists||old==RoadKind.General)&&DedicatedCount>=DedicatedLimit)return EditError.DedicatedBudget;
            if(!exists){if(Roads.Count>=RoadLimit)return EditError.RoadBudget;if(p.X==RiverX&&BridgeCount>=BridgeLimit)return EditError.BridgeBudget;}
            if(!exists||old!=kind.Value){Roads[p]=kind.Value;edited=true;}return EditError.None;
        }
        public bool EndEdit(){if(!edited){if(history.Count>0)history.Pop();return false;}ResetTraffic();return true;}
        public bool Undo(){if(Phase!=RoundPhase.Design||history.Count==0)return false;var snapshot=history.Pop();Roads.Clear();foreach(var p in snapshot)Roads.Add(p.Key,p.Value);ResetTraffic();return true;}
        void ResetTraffic(){Cars.Clear();foreach(var q in Queues)q.Clear();Array.Clear(Arrived,0,2);Array.Clear(travel,0,2);Array.Clear(clocks,0,2);Time=0;serial=0;reservations.Clear();routes[0]=Route(TravelRole.Commute);routes[1]=Route(TravelRole.Freight);edited=false;}
        public void ReturnToDesign(){Phase=RoundPhase.Design;ResetTraffic();}
        public void StartTest(){ResetTraffic();Phase=RoundPhase.Running;}
        public void StepRound(double dt){if(Phase!=RoundPhase.Running)return;Simulate(Math.Min(dt,Math.Max(0,Duration-Time)));if(Time>=Duration-.00001){Time=Duration;Phase=RoundPhase.Result;}}
        public PuzzleResult Evaluate(){var goals=new[]{Arrived[0]>=55,FreightDelivered>=150,Arrived[0]>0&&CommuteAverage<=12,Waiting<=15};bool pass=Connected&&Time>=Duration-.001&&Roads.Count<=RoadLimit&&BridgeCount<=BridgeLimit&&DedicatedCount<=DedicatedLimit;foreach(var g in goals)pass&=g;return new PuzzleResult{Complete=Time>=Duration-.001,Passed=pass,Goals=goals};}
        Bounds Body(Vehicle c,double? progress=null,int? index=null){int i=index??c.Index;double p=progress??c.Progress;var a=c.Path[i];var b=c.Path[i+1];int dx=b.X-a.X,dz=b.Z-a.Z;return new Bounds{X=a.X+dx*p-dz*.17,Z=a.Z+dz*p+dx*.17,HX=dx!=0?c.Length/2:(c.IsTruck?.18:.13),HZ=dz!=0?c.Length/2:(c.IsTruck?.18:.13)};}
        public bool BodyOverlaps(Vehicle a,Vehicle b,double margin=.035){var p=Body(a);var q=Body(b);return Math.Abs(p.X-q.X)<p.HX+q.HX+margin&&Math.Abs(p.Z-q.Z)<p.HZ+q.HZ+margin;}
        bool ClearFor(Vehicle c,double p,int index){var a=Body(c,p,index);foreach(var o in Cars){if(o.Id==c.Id||o.Done)continue;var b=Body(o);if(Math.Abs(a.X-b.X)<a.HX+b.HX+.035&&Math.Abs(a.Z-b.Z)<a.HZ+b.HZ+.035)return false;}return true;}
        static bool SameEdge(Vehicle a,Vehicle b){return a.From==b.From&&a.To==b.To;}
        void Simulate(double dt)
        {
            Time+=dt;
            for(int type=0;type<2;type++){double interval=type==0?.64:1.18;clocks[type]+=dt;while(clocks[type]>=interval){clocks[type]-=interval;Queues[type].Enqueue(Time);}var path=routes[type];if(path==null||Queues[type].Count==0)continue;var candidate=new Vehicle{Id=serial,Role=(TravelRole)type,Path=path};bool blocked=false;foreach(var c in Cars)if(SameEdge(c,candidate)&&c.Progress<(c.Length+candidate.Length)/2+.13){blocked=true;break;}if(!blocked&&ClearFor(candidate,0,0)){candidate.Id=serial++;candidate.Born=Queues[type].Dequeue();Cars.Add(candidate);}}
            // Keep insertion order of edges identical to the reference simulation.
            var groups=new List<List<Vehicle>>();var lookup=new Dictionary<string,List<Vehicle>>();foreach(var c in Cars){string edge=c.From+">"+c.To;List<Vehicle> list;if(!lookup.TryGetValue(edge,out list)){list=new List<Vehicle>();lookup.Add(edge,list);groups.Add(list);}list.Add(c);}foreach(var g in groups)g.Sort((a,b)=>b.Progress.CompareTo(a.Progress));
            foreach(var group in groups)for(int ci=0;ci<group.Count;ci++)
            {
                var c=group[ci];c.Velocity=0;double advance=c.Speed*dt;var front=ci>0?group[ci-1]:null;if(front!=null&&!front.Done&&SameEdge(front,c))advance=Math.Min(advance,Math.Max(0,front.Progress-(front.Length+c.Length)/2-.13-c.Progress));
                Cell node=c.To;bool final=c.Index+1==c.Path.Count-1;int degree=0;foreach(var n in Neighbors(node))if(Roads.ContainsKey(n))degree++;double stop=degree>=3?.2:1-c.Length/2-.16;
                if(c.Progress+advance>stop){Reservation reservation;bool busy=reservations.TryGetValue(node,out reservation)&&reservation.Id!=c.Id&&reservation.Until>Time;bool occupied=false;if(!final)foreach(var o in Cars)if(o!=c&&o.From==node&&o.To==c.Path[c.Index+2]&&o.Progress<(o.Length+c.Length)/2+.13){occupied=true;break;}if(busy)advance=Math.Max(0,Math.Min(advance,stop-c.Progress));else{reservations[node]=new Reservation(c.Id,Time+Math.Max(c.Clearance,(1-c.Progress+c.Length/2+.16)/c.Speed));if(occupied)advance=Math.Max(0,Math.Min(advance,stop-c.Progress));}}
                if(c.Progress+advance>=1){if(final){Arrived[(int)c.Role]++;travel[(int)c.Role]+=Time-c.Born;c.Done=true;continue;}double next=c.Progress+advance-1;if(ClearFor(c,next,c.Index+1)){c.Index++;c.Progress=next;c.Velocity=c.Speed;continue;}advance=0;}
                if(advance>0&&!ClearFor(c,c.Progress+advance,c.Index))advance=0;c.Progress+=advance;c.Velocity=advance/dt;
            }
            Cars.RemoveAll(c=>c.Done);
        }
    }
}

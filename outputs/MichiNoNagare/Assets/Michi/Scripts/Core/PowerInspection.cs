using System;
using System.Collections.Generic;
namespace Michi.Core
{
    public enum PowerUse { InUse,Standby,NoSource,NoDestination,SupplyLimited }
    public sealed class PowerFlow
    {
        public readonly int Source,District;public readonly IReadOnlyList<int> Path;
        public double Amount {get;internal set;}
        internal PowerFlow(int source,int district,List<int> path){Source=source;District=district;Path=Array.AsReadOnly(path.ToArray());}
        internal bool Contains(int node){foreach(int id in Path)if(id==node)return true;return false;}
    }
    public sealed class PowerSourceAmount
    {
        public readonly int Source;public readonly double Amount;
        internal PowerSourceAmount(int source,double amount){Source=source;Amount=amount;}
    }
    public sealed class PowerInspection
    {
        public readonly PowerUse State;public readonly double Throughput;
        // False means these sources supply the reachable districts via other routes.
        public readonly bool SourcesPassThroughSelection;
        public readonly IReadOnlyList<PowerSourceAmount> Sources;
        internal PowerInspection(PowerUse state,double amount,bool through,List<PowerSourceAmount> sources){State=state;Throughput=amount;SourcesPassThroughSelection=through;Sources=sources.AsReadOnly();}
    }
    public sealed partial class NetworkSimulation
    {
        readonly Dictionary<long,PowerFlow> powerFlows=new Dictionary<long,PowerFlow>();
        PowerInspection[] powerInspections;
        public IEnumerable<PowerFlow> PowerFlows {get{return powerFlows.Values;}}
        void RecordPowerFlow(List<int> path,int district,double amount)
        {
            long key=((long)path[0]<<32)|(uint)district;PowerFlow flow;
            if(!powerFlows.TryGetValue(key,out flow)){flow=new PowerFlow(path[0],district,path);powerFlows.Add(key,flow);}flow.Amount+=amount;
        }
        public PowerInspection InspectPower(int nodeId)
        {
            if(nodeId<0||nodeId>=Nodes.Length)throw new ArgumentOutOfRangeException("nodeId");
            if(powerInspections==null)powerInspections=new PowerInspection[Nodes.Length];
            if(powerInspections[nodeId]!=null)return powerInspections[nodeId];
            var selected=Nodes[nodeId];bool districtSelection=selected.Kind==NetworkNodeKind.District,hasSource=false,allSatisfied=true;
            foreach(var source in Nodes)if(source.Kind==NetworkNodeKind.Reactor&&source.PowerSupply>0&&Path(source.Id,nodeId,SignalKind.Power)!=null){hasSource=true;break;}
            var districts=new HashSet<int>();foreach(var district in Nodes)if(district.Kind==NetworkNodeKind.District&&district.PowerDemand>0&&(!districtSelection||district.Id==nodeId)&&Path(nodeId,district.Id,SignalKind.Power)!=null){districts.Add(district.Id);if(district.PowerFraction<.98)allSatisfied=false;}
            bool through=selected.PowerLoad>.00001;
            PowerUse state=through?PowerUse.InUse:!hasSource?PowerUse.NoSource:districts.Count==0?PowerUse.NoDestination:allSatisfied?PowerUse.Standby:PowerUse.SupplyLimited;
            var totals=new Dictionary<int,double>();
            foreach(var flow in powerFlows.Values)if(districtSelection?flow.District==nodeId:through?flow.Contains(nodeId):districts.Contains(flow.District)){double amount;totals.TryGetValue(flow.Source,out amount);totals[flow.Source]=amount+flow.Amount;}
            var sources=new List<PowerSourceAmount>();foreach(var pair in totals)sources.Add(new PowerSourceAmount(pair.Key,pair.Value));sources.Sort((a,b)=>{int comparison=b.Amount.CompareTo(a.Amount);return comparison!=0?comparison:a.Source.CompareTo(b.Source);});
            return powerInspections[nodeId]=new PowerInspection(state,selected.PowerLoad,through,sources);
        }
    }
}

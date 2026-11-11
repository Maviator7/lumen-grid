using System;
using System.Collections.Generic;
namespace Michi.Core
{
    [Serializable] public sealed class SavedBattery {public int Node;public double Energy;}
    public sealed class BatteryState
    {
        public int Node {get;private set;}
        public double Energy {get;internal set;}
        public double ChargeRate {get;internal set;}
        public double DischargeRate {get;internal set;}
        internal BatteryState(int node,double energy=0){Node=node;Energy=energy;}
        internal SavedBattery Copy(){return new SavedBattery{Node=Node,Energy=Energy};}
    }
    public sealed partial class NetworkSimulation
    {
        public const double BatteryCapacity=120,BatteryChargeLimit=2,BatteryOutputLimit=4,BatteryEfficiency=.9;
        readonly List<BatteryState> batteries=new List<BatteryState>();
        readonly Dictionary<int,BatteryState> batteryByNode=new Dictionary<int,BatteryState>();
        readonly Dictionary<long,PowerFlow> chargingFlows=new Dictionary<long,PowerFlow>();
        IReadOnlyList<BatteryState> batteryView;
        public IReadOnlyList<BatteryState> Batteries {get{return batteryView??(batteryView=batteries.AsReadOnly());}}
        public int AvailableBatteries {get{return Continuous?1+ExpansionCount/2:0;}}
        public double StoredEnergy {get{double value=0;foreach(var b in batteries)value+=b.Energy;return value;}}
        public BatteryState BatteryAt(int node){BatteryState b;return batteryByNode.TryGetValue(node,out b)?b:null;}
        public bool CanInstallBattery(int node){return CanEdit&&Continuous&&Map.Generated&&node>=0&&node<Nodes.Length&&Nodes[node].Kind==NetworkNodeKind.Relay&&BatteryAt(node)==null&&batteries.Count<AvailableBatteries;}
        public bool InstallBattery(int node)
        {
            if(!CanInstallBattery(node))return false;
            AddBattery(new BatteryState(node));AllocatePower();offerCache=null;return true;
        }
        public bool RemoveBattery(int node)
        {
            if(!CanEdit)return false;var b=BatteryAt(node);if(b==null)return false;
            batteries.Remove(b);batteryByNode.Remove(node);AllocatePower();StableSeconds=0;offerCache=null;return true;
        }
        void AddBattery(BatteryState b){batteries.Add(b);batteryByNode.Add(b.Node,b);}
        void ClearBatteries(){batteryAllocationValid=false;batteries.Clear();batteryByNode.Clear();chargingFlows.Clear();}
        // Amount is accepted rate; energy is integrated once, only at the end of a positive simulation step.
        double StoragePathRoom(List<int> path,double amount)
        {
            foreach(int id in path)if(Nodes[id].Kind==NetworkNodeKind.Relay)amount=Math.Min(amount,Nodes[id].Capacity-Nodes[id].PowerLoad);
            for(int i=1;i<path.Count;i++)amount=Math.Min(amount,LinkCapacity-Links[EdgeId(path[i-1],path[i])].PowerLoad);
            return Math.Max(0,amount);
        }
        void ApplyStorageFlow(List<int> path,double amount,bool charge)
        {
            if(charge){long key=((long)path[0]<<32)|(uint)path[path.Count-1];PowerFlow flow;if(!chargingFlows.TryGetValue(key,out flow)){flow=new PowerFlow(path[0],path[path.Count-1],path);chargingFlows.Add(key,flow);}flow.Amount+=amount;}
            else RecordPowerFlow(path,path[path.Count-1],amount);
            Nodes[path[path.Count-1]].PowerReceived+=amount;
            foreach(int id in path)Nodes[id].PowerLoad+=amount;
            for(int i=1;i<path.Count;i++)Links[EdgeId(path[i-1],path[i])].PowerLoad+=amount;
        }
        void AllocateBatteryFlows(double dt)
        {
            double horizon=dt>0?dt:1.0/60;
            foreach(var b in batteries){b.ChargeRate=b.DischargeRate=0;}
            bool changed=true;
            while(changed){changed=false;foreach(var target in Nodes)if(target.Kind==NetworkNodeKind.District&&target.PowerReceived<target.PowerDemand-.000001){
                foreach(var b in batteries){
                    double amount=Math.Min(.125,Math.Min(target.PowerDemand-target.PowerReceived,Math.Min(BatteryOutputLimit,b.Energy/horizon)-b.DischargeRate));
                    if(amount<=.000001)continue;var path=Path(b.Node,target.Id,SignalKind.Power);if(path==null)continue;amount=StoragePathRoom(path,amount);
                    if(amount<=.000001)continue;ApplyStorageFlow(path,amount,false);b.DischargeRate+=amount;changed=true;break;
                }
            }}
            var chargingRoutes=new List<List<List<int>>>();foreach(var b in batteries)chargingRoutes.Add(SourcePaths(Nodes[b.Node],SignalKind.Power));
            changed=true;
            while(changed){changed=false;foreach(var b in batteries)if(b.DischargeRate<=.000001){
                double room=Math.Min(BatteryChargeLimit,(BatteryCapacity-b.Energy)/(BatteryEfficiency*horizon))-b.ChargeRate;
                if(room<=.000001)continue;
                var paths=chargingRoutes[batteries.IndexOf(b)];
                foreach(var path in paths){var source=Nodes[path[0]];double amount=StoragePathRoom(path,Math.Min(.125,Math.Min(room,source.PowerSupply-source.PowerLoad)));
                    if(amount<=.000001)continue;ApplyStorageFlow(path,amount,true);b.ChargeRate+=amount;changed=true;break;
                }
            }}
            IntegrateBatteryEnergy(dt);RememberBatteryAllocation(dt);
        }
        void IntegrateBatteryEnergy(double dt)
        {
            if(dt>0)foreach(var n in Nodes)if(n.Kind==NetworkNodeKind.District&&n.PowerFraction<.98)StableSeconds=0;
            if(dt>0)foreach(var b in batteries)b.Energy=Math.Max(0,Math.Min(BatteryCapacity,b.Energy+(b.ChargeRate*BatteryEfficiency-b.DischargeRate)*dt));
        }
    }
}

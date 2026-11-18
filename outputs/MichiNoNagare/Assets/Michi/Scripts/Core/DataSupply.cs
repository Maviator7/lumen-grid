using System;
using System.Collections.Generic;
namespace Michi.Core
{
    public sealed class DataRequest
    {
        public readonly int Destination;public readonly double Born;
        public DataRequest(int destination,double born){Destination=destination;Born=born;}
    }
    public sealed partial class NetworkSimulation
    {
        readonly Queue<DataRequest> waitingData=new Queue<DataRequest>();
        public int WaitingData {get{return waitingData.Count;}}
        public IEnumerable<DataRequest> PendingData {get{return waitingData;}}
        public double DataDemandTotal {get{double rate=0;foreach(var n in Nodes)if(n.Kind==NetworkNodeKind.District)rate+=n.DataDemand;return rate;}}
        public double DataCapacityTotal {get{double rate=0;foreach(var n in Nodes)if(n.Kind==NetworkNodeKind.DataCore)rate+=n.DataSupply;return rate;}}
        public static double DistrictDataRate(int role){return role==7?2:role==8?1.5:3;}
        public bool DataSourceConnected(int source){if(source<0||source>=Nodes.Length||Nodes[source].Kind!=NetworkNodeKind.DataCore)return false;foreach(var target in Nodes)if(target.Kind==NetworkNodeKind.District&&Path(source,target.Id,SignalKind.Data)!=null)return true;return false;}
        // Current topology only; separate from the rolling delivery outcomes.
        public bool DistrictDataConnected(int district)
        {
            if(district<0||district>=Nodes.Length||Nodes[district].Kind!=NetworkNodeKind.District)return false;
            foreach(var source in Nodes)if(source.Kind==NetworkNodeKind.DataCore&&source.DataSupply>0&&Path(source.Id,district,SignalKind.Data)!=null)return true;
            return false;
        }
        public int UnconnectedDataDistrictCount {get{int count=0;foreach(var n in Nodes)if(n.Kind==NetworkNodeKind.District&&!DistrictDataConnected(n.Id))count++;return count;}}
        public int UnderpoweredDistrictCount {get{int count=0;foreach(var n in Nodes)if(n.Kind==NetworkNodeKind.District&&n.PowerFraction<.98)count++;return count;}}
        public double ConnectedDataCapacity
        {
            get{double rate=0;foreach(var source in Nodes)if(DataSourceConnected(source.Id))rate+=source.DataSupply;return rate;}
        }
        void ConfigureMainNode(GridNode node)
        {
            if(!Continuous)return;
            if(node.Kind==NetworkNodeKind.Relay)node.Capacity=Math.Max(18,node.Capacity);
            if(node.Kind==NetworkNodeKind.DataCore)node.DataSupply=7;
            if(node.Kind==NetworkNodeKind.District){node.DataDemand=DistrictBaseDataRate(node.Role,node.DemandProfile);if(ModernRules)node.PowerDemand=DistrictPower(node);}
        }
        void ResetDataSupply()
        {
            waitingData.Clear();foreach(var n in Nodes){n.SourceCredit=n.RequestCredit=0;n.DataRequested=n.DataDispatched=0;n.DataOutputRate=n.DataDeliveryRate=0;}
        }
        void FailRequest(DataRequest request){Expired++;Nodes[request.Destination].Failed++;}
        double TransitTime(List<int> path,double dt){double duration=0;for(int i=1;i<path.Count;i++)duration+=Math.Ceiling((Distance(path[i-1],path[i])*.07+.10)/dt)*dt;return duration;}
        void SupplyData(double dt)
        {
            foreach(var n in Nodes){
                if(n.Kind==NetworkNodeKind.DataCore)n.SourceCredit=n.DataSupply>0?n.SourceCredit+n.DataSupply*dt:0;
                if(n.Kind!=NetworkNodeKind.District)continue;
                n.RequestCredit+=Math.Max(0,n.DataDemand)*dt;
                while(n.RequestCredit>=1-.0000001){n.RequestCredit=Math.Max(0,n.RequestCredit-1);n.DataRequested++;waitingData.Enqueue(new DataRequest(n.Id,Time));}
            }
            // Scan the complete FIFO once: a saturated source cannot block other reachable sources.
            int count=waitingData.Count;for(int i=0;i<count;i++){
                var request=waitingData.Dequeue();if(Time-request.Born>RequestDeadline(request.Destination)){FailRequest(request);continue;}
                var paths=SourcePaths(Nodes[request.Destination],SignalKind.Data);if(paths.Count==0){FailRequest(request);continue;}
                List<int> chosen=null, timely=null;foreach(var path in paths){
                    if(Time-request.Born+TransitTime(path,dt)>RequestDeadline(request.Destination))continue;
                    if(timely==null)timely=path;
                    if(Nodes[path[0]].SourceCredit>=1-.0000001){chosen=path;break;}
                }
                if(chosen==null){
                    if (Time - request.Born > .45)
                    {
                        // A fresh request that fits is held by source throughput, even if this older one no longer fits.
                        if (timely == null) foreach (var path in paths)
                            if (TransitTime(path,dt) <= RequestDeadline(request.Destination)) { timely = path; break; }
                        ObserveDataBlock(request.Destination, timely == null ? DataBlock.TransitTime : DataBlock.Source,
                            timely == null ? request.Destination : timely[0], -1, timely);
                    }
                    waitingData.Enqueue(request);continue;
                }
                lastDataRoutes[request.Destination]=chosen;
                var source=Nodes[chosen[0]];source.SourceCredit=Math.Max(0,source.SourceCredit-1);source.DataDispatched++;
                Packets.Add(new DataPacket{Id=serial++,Destination=request.Destination,Path=chosen,Born=request.Born});
            }
            foreach(var source in Nodes)if(source.Kind==NetworkNodeKind.DataCore)source.SourceCredit=Math.Min(1,source.SourceCredit);
        }
    }
}

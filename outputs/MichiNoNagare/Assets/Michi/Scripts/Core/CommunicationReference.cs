using System.Collections.Generic;
namespace Michi.Core
{
    public sealed partial class NetworkSimulation
    {
        // A reference may need to take power around a relay that must forward peak data.
        // Preserve its data wiring and add only legal, budgeted power shortcuts.
        void RelieveCommunicationReference()
        {
            for(int attempt=0;attempt<4;attempt++){
                var demand=new double[Nodes.Length];
                foreach(var n in Nodes)if(n.Kind==NetworkNodeKind.District){var visited=new HashSet<int>();foreach(var path in SourcePaths(n,SignalKind.Data))foreach(int id in path)visited.Add(id);foreach(int id in visited)demand[id]+=DistrictPeakDataRate(n.Role);}
                int from=-1,to=-1;
                foreach(var flow in powerFlows.Values){var path=flow.Path;for(int i=1;i<path.Count-1;i++){
                    var n=Nodes[path[i]];if(n.Kind!=NetworkNodeKind.Relay||n.PowerLoad+demand[n.Id]<=n.Capacity+.00001)continue;
                    int a=path[i-1],b=path[i+1];if(Links.ContainsKey(EdgeId(a,b))||PreviewConnect(a,b,LinkKind.PowerOnly).Error!=NetworkEditError.None)continue;
                    from=a;to=b;break;
                }if(from>=0)break;}
                if(from<0)break;Connect(from,to,LinkKind.PowerOnly);AllocatePower();
            }
        }
    }
}

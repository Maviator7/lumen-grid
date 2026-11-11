using System.Collections.Generic;
using UnityEngine;
namespace Michi.Runtime
{
    public sealed partial class NetworkCityView
    {
        sealed class CenterAccent {public int Node;public Material Material;}
        Transform communicationRoot;
        readonly List<CenterAccent> centerAccents=new List<CenterAccent>();
        public void RefreshCommunicationCenters()
        {
            foreach(var accent in centerAccents)Release(accent.Material);centerAccents.Clear();
            if(communicationRoot==null)communicationRoot=Group("Center expansion modules",city);else Clear(communicationRoot);
            foreach(int id in sim.BoostedCenters){var p=NodePoint(id);for(int side=-1;side<=1;side+=2){float x=p.x+side*.64f;
                Box(communicationRoot,"Boost server module",x,1.13f,p.z,.16f,1.55f,.55f,Metal(Gunmetal));
                var lamp=Glow(Cyan);Box(communicationRoot,"Boost status strip",x,1.15f,p.z-.29f,.08f,1.3f,.025f,lamp);centerAccents.Add(new CenterAccent{Node=id,Material=lamp});
            }}SyncCommunicationCenters();
        }
        void SyncCommunicationCenters()
        {
            foreach(var accent in centerAccents){var node=sim.Nodes[accent.Node];float power=(float)node.PowerFraction;accent.Material.SetColor("_Color",Color.Lerp(Amber,Cyan,power)*(.12f+.8f*power)*(1+.045f*Mathf.Sin((float)sim.Time+node.Id)));}
        }
    }
}

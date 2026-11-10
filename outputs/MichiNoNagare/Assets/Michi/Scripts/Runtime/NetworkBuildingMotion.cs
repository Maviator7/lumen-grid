using System.Collections.Generic;
using UnityEngine;
using Michi.Core;
namespace Michi.Runtime
{
    public sealed partial class NetworkCityView
    {
        enum BuildingAccentKind { ReactorCoil,ServerLamp,MedicalSign,FactoryLamp,Steam }
        sealed class BuildingAccent
        {
            public int Node,Index;public BuildingAccentKind Kind;public Material Material;
            public Transform Part;public Vector3 Position;public float Phase;
        }
        readonly Dictionary<int,Light> infrastructureLights=new Dictionary<int,Light>();
        readonly List<BuildingAccent> buildingAccents=new List<BuildingAccent>();
        void BuildingGlow(int node,BuildingAccentKind kind,Material material,int index=0,Transform part=null)
        {
            buildingAccents.Add(new BuildingAccent{Node=node,Kind=kind,Material=material,Index=index,Part=part,Position=part!=null?part.localPosition:Vector3.zero,Phase=Mathf.Repeat(node*.618034f+index*.381966f,1)});
        }
        void FactorySteam(GridNode node,Vector3 chimneyTop)
        {
            for(int i=0;i<2;i++){
                var material=Glow(new Color(.09f,.14f,.18f));
                var puff=Shape(city,"Soft factory steam",chimneyTop,new Vector3(.16f,.16f,.16f),material,PrimitiveType.Sphere);
                BuildingGlow(node.Id,BuildingAccentKind.Steam,material,i,puff.transform);
            }
        }
        float WindowMotion(GridNode node)
        {
            // Simulation time freezes production motion during pause, menus and tutorials.
            return 1+.045f*(float)node.PowerFraction*(float)System.Math.Sin(sim.Time*.45+node.Id*.73);
        }
        void SyncBuildingMotion()
        {
            float time=(float)sim.Time;
            foreach(var pair in infrastructureLights){
                var source=sim.Nodes[pair.Key];
                float activity=source.Kind==NetworkNodeKind.Reactor?Mathf.Clamp01((float)(source.PowerLoad/System.Math.Max(1,source.PowerSupply))):sim.Continuous?Mathf.Clamp01((float)(source.DataOutputRate/System.Math.Max(.01,source.DataSupply))):source.Processed>0?1:0;
                pair.Value.intensity=(source.Kind==NetworkNodeKind.Reactor?3*(.85f+.15f*activity):2*(.25f+.75f*activity))*(1+.045f*Mathf.Sin(time*.9f+source.Id*.73f));
            }
            foreach(var accent in buildingAccents){
                var node=sim.Nodes[accent.Node];float phase=accent.Phase*Mathf.PI*2,power=(float)node.PowerFraction;
                switch(accent.Kind){
                    case BuildingAccentKind.ReactorCoil:{
                        float load=Mathf.Clamp01((float)(node.PowerLoad/System.Math.Max(1,node.PowerSupply)));
                        float breathing=1+.09f*load*Mathf.Sin(time*.9f+phase+accent.Index*.6f);
                        accent.Material.SetColor("_Color",Amber*(.55f+accent.Index*.1f+load*.25f)*breathing);break;}
                    case BuildingAccentKind.ServerLamp:{
                        float activity=sim.Continuous?Mathf.Clamp01((float)(node.DataOutputRate/System.Math.Max(.01,node.DataSupply))):node.Processed>0?1:0;
                        float scan=Mathf.Repeat(time*.9f+node.Id*.47f+(accent.Index/6)*.28f,6);
                        float distance=Mathf.Abs(scan-accent.Index%6);distance=Mathf.Min(distance,6-distance);
                        float highlight=Mathf.Exp(-distance*distance*1.6f);
                        accent.Material.SetColor("_Color",Cyan*(.14f+activity*(.45f+.45f*highlight)));break;}
                    case BuildingAccentKind.MedicalSign:
                        accent.Material.SetColor("_Color",new Color(.35f,1,.85f)*(.07f+.8f*power)*(1+.045f*Mathf.Sin(time*.7f+phase)));break;
                    case BuildingAccentKind.FactoryLamp:
                        accent.Material.SetColor("_Color",Amber*(.08f+.42f*power)*(1+.06f*Mathf.Sin(time*.6f+phase)));break;
                    case BuildingAccentKind.Steam:{
                        float age=Mathf.Repeat(time*.2f+accent.Index*.5f+accent.Phase*.1f,1);
                        accent.Part.localPosition=accent.Position+new Vector3(Mathf.Sin(time*.4f+phase)*.06f,age*.8f,age*.12f);
                        accent.Part.localScale=Vector3.one*(.12f+age*.2f);
                        accent.Material.SetColor("_Color",new Color(.09f,.14f,.18f)*(Mathf.Sin(age*Mathf.PI)*power));break;}
                }
            }
        }
    }
}

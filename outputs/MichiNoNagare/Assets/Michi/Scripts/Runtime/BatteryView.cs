using System.Collections.Generic;
using UnityEngine;
namespace Michi.Runtime
{
    public sealed partial class NetworkCityView
    {
        sealed class BatteryLamp {public int Node,Cell;public Material Material;}
        Transform batteryRoot;
        readonly List<BatteryLamp> batteryLamps=new List<BatteryLamp>();
        static readonly Color BatteryGreen=RGB(0xa7efb2);
        public void RefreshBatteries()
        {
            foreach(var lamp in batteryLamps)Release(lamp.Material);batteryLamps.Clear();
            if(batteryRoot==null)batteryRoot=Group("Storage retrofits",city);else Clear(batteryRoot);
            foreach(var b in sim.Batteries){var p=NodePoint(b.Node);
                for(int i=0;i<3;i++){
                    float x=p.x+(i-1)*.38f;
                    Shape(batteryRoot,"Storage metal cell",new Vector3(x,.98f,p.z+.12f),new Vector3(.29f,.57f,.29f),Metal(Gunmetal),PrimitiveType.Cylinder);
                    Shape(batteryRoot,"Storage cap",new Vector3(x,1.57f,p.z+.12f),new Vector3(.33f,.04f,.33f),Metal(DarkMetal),PrimitiveType.Cylinder);
                    var light=Glow(BatteryGreen*.2f);Box(batteryRoot,"Charge indicator",x,1.03f,p.z-.035f,.12f,.78f,.03f,light);batteryLamps.Add(new BatteryLamp{Node=b.Node,Cell=i,Material=light});
                }
                Line(batteryRoot,"Storage green ring",Circle(new Vector3(p.x,.4f,p.z),.72f),.04f,BatteryGreen*.55f);
            }
            SyncBatteries();
        }
        void SyncBatteries()
        {
            foreach(var lamp in batteryLamps){var b=sim.BatteryAt(lamp.Node);if(b==null)continue;float charge=(float)(b.Energy/40)-lamp.Cell;
                float activity=b.ChargeRate>0||b.DischargeRate>0?.1f*Mathf.Sin((float)sim.Time*2.2f+lamp.Cell*.8f):0;
                lamp.Material.SetColor("_Color",BatteryGreen*(.07f+Mathf.Clamp01(charge)*(.7f+activity)));
            }
        }
    }
}

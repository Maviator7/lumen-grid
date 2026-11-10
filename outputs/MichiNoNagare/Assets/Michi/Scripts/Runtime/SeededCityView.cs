using UnityEngine;
using Michi.Core;
namespace Michi.Runtime
{
    public sealed partial class NetworkCityView
    {
        void BuildTerrain()
        {
            float river=(float)sim.Map.RiverX,width=(float)sim.Map.RiverWidth;
            Box(city,"Water channel",river,-.015f,(minZ+maxZ)/2,width,.03f,maxZ-minZ,Mat(0x08324a));
            foreach(float x in new[]{river-width/2,river+width/2})Line(city,"Canal edge",new[]{new Vector3(x,.02f,minZ),new Vector3(x,.02f,maxZ)},.035f,new Color(.1f,.5f,.65f));
            int index=0;foreach(var o in sim.Map.Obstacles){float x=(float)o.X,z=(float)o.Z,w=(float)o.Width,d=(float)o.Depth;Box(city,"Restricted foundation "+index,x,.02f,z,w,.12f,d,Mat(0x27313d));var amber=new Color(.65f,.22f,.06f);Line(city,"Restricted perimeter",new[]{new Vector3(x-w/2,.1f,z-d/2),new Vector3(x+w/2,.1f,z-d/2),new Vector3(x+w/2,.1f,z+d/2),new Vector3(x-w/2,.1f,z+d/2),new Vector3(x-w/2,.1f,z-d/2)},.06f,amber);Box(city,"Reserved reactor casing",x,.4f,z,w*.65f,.65f,d*.65f,Mat(0x27313d));for(int i=0;i<3;i++)Box(city,"Warning stripe",x-w*.35f+i*w*.25f,.76f,z,.09f,.025f,d*.6f,Glow(amber));index++;}
        }
        void BuildBridge(GridLink link)
        {
            Vector3 a=NodePoint(link.A),b=NodePoint(link.B);float river=(float)sim.Map.RiverX,width=(float)sim.Map.RiverWidth;
            if(Mathf.Abs(b.x-a.x)<.001f)return;Vector3 left=Vector3.Lerp(a,b,(river-width*.6f-a.x)/(b.x-a.x)),right=Vector3.Lerp(a,b,(river+width*.6f-a.x)/(b.x-a.x));
            var mid=(left+right)/2;var deck=Box(linkRoot,"Bridge deck",mid.x,.22f,mid.z,.5f,.12f,Vector3.Distance(left,right)+.3f,Mat(0x284359));deck.transform.rotation=Quaternion.Euler(0,Mathf.Atan2(right.x-left.x,right.z-left.z)*Mathf.Rad2Deg,0);
            foreach(var p in new[]{left,right})Box(linkRoot,"Bridge pier",p.x,.45f,p.z,.17f,.9f,.17f,Mat(0x35536a));
        }
        void BuildDistrict(GridNode node,Vector3 p)
        {
            var windows=Glow(Amber*.08f);districtWindows[node.Id]=windows;var r=new CityRandom(unchecked(sim.Map.Seed*397+node.Id*7919));var shell=Mat(node.Role==7?0x23313f:0x162d43);
            if(node.Role==7){
                Box(city,"Factory hall",p.x+.8f,.7f,p.z+.45f,2.2f,1.4f,1.15f,shell);Box(city,"Loading wing",p.x+.5f,.32f,p.z-.55f,2.6f,.64f,.7f,shell);
                for(int i=0;i<3;i++){float x=p.x+i*.65f;Box(city,"Factory roof light",x,1.43f,p.z+.45f,.14f,.04f,1.05f,windows);float chimneyHeight=1.8f+(float)r.Range(0,.6);Shape(city,"Industrial chimney",new Vector3(x,.9f,p.z+1.2f),new Vector3(.2f,chimneyHeight,.2f),shell,PrimitiveType.Cylinder);var indicator=Line(city,"Chimney indicator",Circle(new Vector3(x,1.95f,p.z+1.2f),.2f),.045f,Amber*.5f);BuildingGlow(node.Id,BuildingAccentKind.FactoryLamp,indicator.sharedMaterial,i);if(i==1)FactorySteam(node,new Vector3(x,.9f+chimneyHeight+.1f,p.z+1.2f));}
                Shape(city,"Storage tank",new Vector3(p.x+1.8f,.45f,p.z-.6f),new Vector3(.5f,.45f,.5f),shell,PrimitiveType.Cylinder);
            }else if(node.Role==8){
                int count=5+r.Int(4);for(int i=0;i<count;i++){float x=p.x+(i%3)*.65f-.3f,z=p.z+(i/3)*.8f-.4f,h=(float)r.Range(2.3,5.1);Tower(x,z,(float)r.Range(.4,.6),h,(float)r.Range(.4,.65),shell,windows,"Residential tower");}
            }else{
                Box(city,"Medical campus",p.x+.5f,.65f,p.z+.35f,2.25f,1.3f,1.4f,shell);Box(city,"Hospital wing",p.x-.2f,.5f,p.z-.6f,1.8f,1,1,shell);
                var sign=Glow(new Color(.35f,1,.85f));Box(city,"Medical cross X",p.x+.7f,1.34f,p.z+.35f,.9f,.06f,.22f,sign);Box(city,"Medical cross Z",p.x+.7f,1.35f,p.z+.35f,.22f,.06f,.9f,sign);BuildingGlow(node.Id,BuildingAccentKind.MedicalSign,sign);
                Line(city,"Helipad",Circle(new Vector3(p.x-.3f,1.04f,p.z-.6f),.42f),.035f,new Color(.2f,.6f,.8f));for(int i=0;i<3;i++)Box(city,"Medical ward windows",p.x-.25f+i*.7f,.8f,p.z-.37f,.45f,.12f,.02f,windows);
            }
            Line(city,node.Code+" perimeter",new[]{p+new Vector3(-.9f,-.54f,-1.2f),p+new Vector3(2.3f,-.54f,-1.2f),p+new Vector3(2.3f,-.54f,1.6f)},.035f,Cyan*.2f);
        }
    }
}

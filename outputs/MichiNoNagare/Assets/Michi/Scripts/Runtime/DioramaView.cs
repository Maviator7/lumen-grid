using System.Collections.Generic;
using UnityEngine;
using Michi.Core;

namespace Michi.Runtime
{
    public sealed class DioramaView : MonoBehaviour
    {
        public Camera Camera {get;private set;}
        public bool TopDown,ShowRoutes=true;
        public float Angle=-36,Zoom=1;
        RoadSimulation sim;Transform world,roads,trees,routes,traffic;GameObject cursor;
        readonly Dictionary<int,GameObject> vehicles=new Dictionary<int,GameObject>();
        readonly Dictionary<int,Material> materials=new Dictionary<int,Material>();
        int lastWidth,lastHeight;
        public static Vector3 World(Cell p,float y=0){return new Vector3(p.X-11.5f,y,p.Z-7.5f);}
        Material Mat(int rgb){Material m;if(!materials.TryGetValue(rgb,out m)){var template=Resources.Load<Material>("DioramaMaterial");if(template==null)throw new System.InvalidOperationException("Missing DioramaMaterial. Run Michi/Create playable scene.");m=new Material(template);m.color=ColorOf(rgb);m.SetFloat("_Glossiness",.12f);materials.Add(rgb,m);}return m;}
        public static Color ColorOf(int rgb){return new Color(((rgb>>16)&255)/255f,((rgb>>8)&255)/255f,(rgb&255)/255f);}
        GameObject Primitive(Transform parent,string name,Vector3 pos,Vector3 size,int color,PrimitiveType type=PrimitiveType.Cube)
        {
            var go=GameObject.CreatePrimitive(type);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=pos;go.transform.localScale=size;go.GetComponent<Renderer>().sharedMaterial=Mat(color);var c=go.GetComponent<Collider>();c.enabled=false;Destroy(c);return go;
        }
        GameObject Box(Transform p,float x,float y,float z,float w,float h,float d,int color,string name="Block"){return Primitive(p,name,new Vector3(x,y,z),new Vector3(w,h,d),color);}
        Transform Group(string name,Transform parent=null){var g=new GameObject(name).transform;if(parent!=null)g.SetParent(parent,false);return g;}
        void Clear(Transform root){for(int i=root.childCount-1;i>=0;i--){var g=root.GetChild(i).gameObject;g.SetActive(false);Destroy(g);}}
        public void Initialize(RoadSimulation engine)
        {
            sim=engine;QualitySettings.vSyncCount=1;Application.targetFrameRate=60;RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=ColorOf(0xc1cfb5);RenderSettings.fog=false;
            var bg=new GameObject("Background Camera").AddComponent<Camera>();bg.clearFlags=CameraClearFlags.SolidColor;bg.backgroundColor=ColorOf(0xf5f4ec);bg.cullingMask=0;bg.depth=-10;
            Camera=new GameObject("Main Camera").AddComponent<Camera>();Camera.tag="MainCamera";Camera.orthographic=true;Camera.clearFlags=CameraClearFlags.SolidColor;Camera.backgroundColor=ColorOf(0xe7ebdf);Camera.nearClipPlane=.1f;Camera.farClipPlane=120;
            var light=new GameObject("Afternoon Sun").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.05f;light.color=ColorOf(0xfff5df);light.transform.rotation=Quaternion.Euler(52,-28,0);light.shadows=LightShadows.Soft;
            world=Group("Riverside Diorama");world.position=new Vector3(-11.5f,0,-7.5f);
            Box(world,11.5f,-.4f,7.5f,24.6f,.65f,16.6f,0xc5d1b5,"Island foundation");
            Box(world,11.5f,-.02f,7.5f,24.4f,.14f,16.4f,0xd7e1c8,"Landscape");
            for(int x=0;x<24;x++)for(int z=0;z<16;z++)if(x!=12)Box(world,x,.068f,z,.98f,.025f,.98f,(x*19+z*11)%9==0?0xcbd9b6:0xdce5ce,"Land tile");
            Box(world,12,.03f,7.5f,1.08f,.12f,16.42f,0x94c4c0,"River");
            for(int z=0;z<16;z++)Box(world,12+(z%2==0?.14f:-.14f),.096f,z+.12f,.32f,.008f,.028f,0xc0ddd1,"Water ripple");
            foreach(var rock in RoadSimulation.Rocks){var r=Primitive(world,"Rock",new Vector3(rock.X,.3f,rock.Z),new Vector3(.65f,.45f,.6f),0xb5bfaa,PrimitiveType.Sphere);r.transform.localRotation=Quaternion.Euler(10,rock.X*37,12);}
            foreach(var b in RoadSimulation.Buildings)Build(b);
            roads=Group("Editable roads",world);trees=Group("Trees",world);routes=Group("Role route preview",world);traffic=Group("Traffic",world);
            cursor=Box(world,0,.21f,0,.96f,.025f,.96f,0xb8c9a7,"Editing cursor");cursor.SetActive(false);
            RefreshRoads();UpdateCamera();
        }
        void House(Transform g,float x,float z,float scale)
        {
            var h=Group("Home",g);h.localPosition=new Vector3(x,.1f,z);h.localScale=Vector3.one*scale;Box(h,0,.33f,0,.76f,.65f,.7f,0xf0e0c6);
            foreach(int sign in new[]{-1,1}){var r=Box(h,0,.75f,sign*.23f,.94f,.09f,.55f,0xba825e,"Roof");r.transform.localRotation=Quaternion.Euler(sign*31,0,0);}
            Box(h,.16f,.3f,.36f,.16f,.3f,.025f,0x849f99,"Door");Box(h,-.17f,.4f,.36f,.2f,.2f,.025f,0xa1b8ae,"Window");
        }
        void Build(Building b)
        {
            var g=Group(b.Name,world);g.localPosition=new Vector3(b.Position.X,0,b.Position.Z);Box(g,0,.12f,0,1.65f,.09f,1.6f,0xc9d1bd,"Building plot");
            if(b.Kind=="home"){House(g,-.32f,-.1f,.9f);House(g,.42f,.24f,.68f);}
            if(b.Kind=="office"){Box(g,0,.95f,0,1.12f,1.63f,.92f,0xe8d8be,"Offices");Box(g,0,1.8f,0,1.2f,.1f,1,0xc69774);for(float y=.45f;y<1.7f;y+=.35f)for(float x=-.35f;x<.5f;x+=.35f){Box(g,x,y,.472f,.2f,.2f,.025f,0x8aa5a1);Box(g,.572f,y,x,.025f,.2f,.19f,0x9aafa7);}}
            if(b.Kind=="factory"){Box(g,0,.49f,0,1.6f,.7f,1.1f,0x9caeba,"Factory");for(int i=0;i<3;i++){var roof=Box(g,-.53f+i*.53f,.94f,0,.54f,.12f,1.18f,0x728a99);roof.transform.localRotation=Quaternion.Euler(0,0,13);Box(g,-.53f+i*.53f,.55f,.567f,.25f,.2f,.025f,0xd5e0d7);}Primitive(g,"Chimney",new Vector3(-.58f,.96f,-.5f),new Vector3(.26f,.85f,.26f),0x9eaa9f,PrimitiveType.Cylinder);}
            if(b.Kind=="warehouse"){Box(g,0,.55f,0,1.6f,.85f,1.05f,0xb9c5bb,"Warehouse");Box(g,0,1.02f,0,1.72f,.13f,1.16f,0x7b929d,"Roof");for(float x=-.5f;x<.6f;x+=.5f){Box(g,x,.38f,-.54f,.32f,.45f,.025f,0x7b9091);for(float y=.22f;y<.6f;y+=.1f)Box(g,x,y,-.56f,.3f,.018f,.03f,0xa7b5aa);}}
            Box(world,b.Entry.X,.12f,b.Entry.Z,.68f,.04f,.68f,0xf4f0db,"Entrance");Box(world,b.Entry.X,.155f,b.Entry.Z,.12f,.055f,.12f,b.Role==TravelRole.Commute?0xd39a72:0x7895a8,"Role marker");
        }
        public void RefreshRoads()
        {
            if(roads==null)return;Clear(roads);Clear(trees);
            foreach(var p in sim.Roads){int x=p.Key.X,z=p.Key.Z;bool bridge=x==12;float y=bridge?.16f:.125f;int color=p.Value==RoadKind.CommuteOnly?0xc89771:p.Value==RoadKind.FreightOnly?0x7e9eae:bridge?0xd4c5a2:0x96a58f;
                Box(roads,x,y,z,.89f,.06f,.89f,color,"Road "+p.Key+" "+p.Value);
                foreach(var n in sim.Neighbors(p.Key))if(sim.Roads.ContainsKey(n))Box(roads,(x+n.X)/2f,y,(z+n.Z)/2f,n.X==x?.75f:.52f,.06f,n.X==x?.52f:.75f,color);
                if(sim.Roads.ContainsKey(new Cell(x+1,z))||sim.Roads.ContainsKey(new Cell(x-1,z)))Box(roads,x,y+.035f,z,.22f,.007f,.025f,0xe7e9d5);
                if(sim.Roads.ContainsKey(new Cell(x,z+1))||sim.Roads.ContainsKey(new Cell(x,z-1)))Box(roads,x,y+.036f,z,.025f,.007f,.22f,0xe7e9d5);
                if(bridge){bool horizontal=sim.Roads.ContainsKey(new Cell(x-1,z))||sim.Roads.ContainsKey(new Cell(x+1,z));foreach(int side in new[]{-1,1}){Box(roads,x+(horizontal?0:side*.47f),.42f,z+(horizontal?side*.47f:0),horizontal?1.12f:.045f,.055f,horizontal?.045f:1.12f,0x9ca68e);foreach(float step in new[]{-.4f,.4f})Box(roads,x+(horizontal?step:side*.47f),.31f,z+(horizontal?side*.47f:step),.045f,.23f,.045f,0xa6af98);}}
            }
            for(int x=0;x<24;x++)for(int z=0;z<16;z++){if(x==12||(x*37+z*19)%23!=0||sim.Roads.ContainsKey(new Cell(x,z))||RoadSimulation.Blocked(new Cell(x,z)))continue;bool near=false;foreach(var b in RoadSimulation.Buildings)if(Mathf.Abs(b.Position.X-x)<2&&Mathf.Abs(b.Position.Z-z)<2)near=true;if(near)continue;Box(trees,x,.3f,z,.07f,.5f,.07f,0xa08d67,"Trunk");Primitive(trees,"Tree",new Vector3(x,.77f,z),new Vector3(.66f,.9f,.66f),(x+z)%2==0?0x88a374:0x78976c,PrimitiveType.Sphere);}
            RefreshRoutes();
        }
        public void RefreshRoutes(){if(routes==null)return;Clear(routes);for(int role=0;role<2;role++){var path=sim.Routes[role];if(path==null)continue;for(int i=0;i<path.Count-1;i++){var a=path[i];var b=path[i+1];float off=role==0?-.09f:.09f;Box(routes,(a.X+b.X)/2f+(a.X==b.X?off:0),.235f+role*.015f,(a.Z+b.Z)/2f+(a.X==b.X?0:off),a.X==b.X?.075f:1.03f,.014f,a.X==b.X?1.03f:.075f,role==0?0xb97044:0x527e9d,"Planned route");}}routes.gameObject.SetActive(ShowRoutes);}
        GameObject MakeVehicle(Vehicle c)
        {
            var g=Group(c.IsTruck?"Freight truck "+c.Id:"Commuter "+c.Id,traffic);int paint=c.IsTruck?0x7896aa:new[]{0xd59b76,0xe2b989,0xb7ac8e,0xd1a392}[c.Id%4];
            if(c.IsTruck){Box(g,0,.12f,0,.9f,.08f,.29f,0x758384);Box(g,-.13f,.31f,0,.63f,.32f,.31f,paint,"Cargo 6 units");Box(g,.32f,.25f,0,.23f,.22f,.28f,0xd1dbd3,"Cab");Box(g,.34f,.34f,0,.1f,.1f,.29f,0x819a9d,"Windshield");}
            else{Box(g,0,.16f,0,.43f,.12f,.23f,paint,"Car body");Box(g,-.015f,.27f,0,.23f,.12f,.2f,paint);Box(g,.055f,.28f,0,.075f,.085f,.205f,0x91a8a4);Box(g,-.12f,.27f,0,.05f,.075f,.205f,0xa1b4aa);}
            foreach(float x in c.IsTruck?new[]{-.35f,-.2f,.32f}:new[]{-.13f,.13f})foreach(int side in new[]{-1,1}){var wheel=Primitive(g,"Wheel",new Vector3(x,c.IsTruck?.105f:.1f,side*(c.IsTruck?.17f:.12f)),new Vector3(c.IsTruck?.132f:.095f,.026f,c.IsTruck?.132f:.095f),0x58605a,PrimitiveType.Cylinder);wheel.transform.localRotation=Quaternion.Euler(90,0,0);}
            return g.gameObject;
        }
        public void SyncVehicles(){var alive=new HashSet<int>();foreach(var c in sim.Cars){alive.Add(c.Id);GameObject go;if(!vehicles.TryGetValue(c.Id,out go)){go=MakeVehicle(c);vehicles.Add(c.Id,go);}go.transform.localPosition=new Vector3((float)c.X,c.From.X==12||c.To.X==12?.16f:.125f,(float)c.Z);go.transform.localRotation=Quaternion.Euler(0,-Mathf.Atan2(c.To.Z-c.From.Z,c.To.X-c.From.X)*Mathf.Rad2Deg,0);}var remove=new List<int>();foreach(var p in vehicles)if(!alive.Contains(p.Key)){p.Value.SetActive(false);Destroy(p.Value);remove.Add(p.Key);}foreach(var id in remove)vehicles.Remove(id);}
        public void ClearVehicles(){foreach(var g in vehicles.Values){g.SetActive(false);Destroy(g);}vehicles.Clear();}
        public void UpdateCamera(){if(Camera==null)return;var r=GameHud.BoardRect;Camera.pixelRect=new Rect(r.x,Screen.height-r.y-r.height,r.width,r.height);float aspect=r.width/r.height;Camera.orthographicSize=Mathf.Max(20,(24+16*.37f)/aspect)/Zoom/2;float elevation=TopDown?89.95f:46.4f;float angle=Angle*Mathf.Deg2Rad;Camera.transform.position=new Vector3(Mathf.Sin(angle)*42*Mathf.Cos(elevation*Mathf.Deg2Rad),42*Mathf.Sin(elevation*Mathf.Deg2Rad),Mathf.Cos(angle)*42*Mathf.Cos(elevation*Mathf.Deg2Rad));Camera.transform.LookAt(Vector3.zero);lastWidth=Screen.width;lastHeight=Screen.height;}
        void LateUpdate(){if(Screen.width!=lastWidth||Screen.height!=lastHeight)UpdateCamera();}
        public bool Pick(Vector3 mouse,out Cell cell){var plane=new Plane(Vector3.up,new Vector3(0,.13f,0));var ray=Camera.ScreenPointToRay(mouse);float distance;if(plane.Raycast(ray,out distance)){var p=ray.GetPoint(distance);cell=new Cell(Mathf.RoundToInt(p.x+11.5f),Mathf.RoundToInt(p.z+7.5f));return RoadSimulation.Inside(cell);}cell=default(Cell);return false;}
        public void Hover(Cell? cell){cursor.SetActive(cell.HasValue);if(cell.HasValue)cursor.transform.localPosition=new Vector3(cell.Value.X,cell.Value.X==12?.25f:.19f,cell.Value.Z);}
        void OnDestroy(){foreach(var m in materials.Values)if(m!=null)Destroy(m);}
    }
}

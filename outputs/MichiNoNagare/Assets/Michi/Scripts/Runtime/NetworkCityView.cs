using System.Collections.Generic;
using UnityEngine;
using Michi.Core;

namespace Michi.Runtime
{
    public sealed partial class NetworkCityView : MonoBehaviour
    {
        public Camera Camera {get;private set;}
        readonly OrbitRotation rotation=new OrbitRotation(-18);
        public float Angle {get{return (float)rotation.Angle;}set{rotation.Snap(value);}}
        public float Zoom=1;public bool TopDown;
        public void TurnCamera(float degrees){rotation.Turn(degrees);}
        public void StopRotation(){rotation.Snap(rotation.Angle);}
        public void TickRotation(float axis,float dt){if(rotation.Step(axis,dt))UpdateCamera();}
        public int SelectedNode=-1,PendingNode=-1;
        public Vector3 PanOffset {get;private set;}
        public bool GroundPoint(Vector3 screen,out Vector3 point){float d;var ray=Camera.ScreenPointToRay(screen);if(new Plane(Vector3.up,Vector3.up*.65f).Raycast(ray,out d)){point=ray.GetPoint(d);return true;}point=Vector3.zero;return false;}
        public void DragMap(Vector3 anchor,Vector3 screen){Vector3 current;if(!GroundPoint(screen,out current))return;var delta=anchor-current;PanOffset=new Vector3(Mathf.Clamp(PanOffset.x+delta.x,minX-4,maxX+4),0,Mathf.Clamp(PanOffset.z+delta.z,minZ-4,maxZ+4));UpdateCamera();}
        public void RestoreCamera(float angle,float zoom,bool top,float x,float z){Angle=angle;Zoom=zoom;TopDown=top;PanOffset=new Vector3(x,0,z);UpdateCamera();}
        public void ResetCamera(){PanOffset=Vector3.zero;Angle=-18;Zoom=1;TopDown=false;UpdateCamera();}
        float minX,maxX,minZ,maxZ;
        NetworkSimulation sim;Transform sceneRoot,city,linkRoot,packetRoot;Material standard,neon;readonly Dictionary<int,Material> materials=new Dictionary<int,Material>();
        readonly Dictionary<int,LinkVisual> linkViews=new Dictionary<int,LinkVisual>();readonly Dictionary<int,GameObject> packetViews=new Dictionary<int,GameObject>();readonly Dictionary<int,LineRenderer> trails=new Dictionary<int,LineRenderer>();readonly List<LineRenderer> rings=new List<LineRenderer>();readonly Dictionary<int,Material> districtWindows=new Dictionary<int,Material>();
        int lastW,lastH;LineRenderer preview;Material packetMaterial;readonly HashSet<Material> owned=new HashSet<Material>();
        public int OwnedMaterialCount {get{return owned.Count;}}
        class LinkVisual {public GridLink Link;public LineRenderer Power,Data;}
        static readonly Color Amber=new Color(1,.48f,.09f),Cyan=new Color(.05f,.7f,1f),Red=new Color(1,.17f,.12f);
        public static Color RGB(int rgb){return new Color(((rgb>>16)&255)/255f,((rgb>>8)&255)/255f,(rgb&255)/255f);}
        public Vector3 NodePoint(int id){return new Vector3((float)sim.Nodes[id].X,.65f,(float)sim.Nodes[id].Z);}
        Material Mat(int color){Material m;if(!materials.TryGetValue(color,out m)){m=new Material(standard);m.color=RGB(color);m.SetFloat("_Glossiness",.35f);materials.Add(color,m);owned.Add(m);}return m;}
        Material Glow(Color color){var m=new Material(neon);m.SetColor("_Color",color);owned.Add(m);return m;}
        Transform Group(string name,Transform parent=null){var g=new GameObject(name).transform;g.SetParent(parent!=null?parent:sceneRoot,false);return g;}
        GameObject Shape(Transform parent,string name,Vector3 p,Vector3 size,Material mat,PrimitiveType type=PrimitiveType.Cube){var g=GameObject.CreatePrimitive(type);g.name=name;g.transform.SetParent(parent,false);g.transform.localPosition=p;g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=mat;var collider=g.GetComponent<Collider>();collider.enabled=false;Destroy(collider);return g;}
        GameObject Box(Transform parent,string name,float x,float y,float z,float w,float h,float d,Material mat){return Shape(parent,name,new Vector3(x,y,z),new Vector3(w,h,d),mat);}
        LineRenderer Line(Transform parent,string name,Vector3[] points,float width,Color color,bool pulse=false){var g=new GameObject(name);g.transform.SetParent(parent!=null?parent:sceneRoot,false);var line=g.AddComponent<LineRenderer>();line.useWorldSpace=true;line.positionCount=points.Length;line.SetPositions(points);line.widthMultiplier=width;line.numCapVertices=4;line.numCornerVertices=3;line.textureMode=LineTextureMode.Stretch;line.sharedMaterial=Glow(color);line.sharedMaterial.SetFloat("_Pulse",pulse?1:0);return line;}
        Vector3[] Circle(Vector3 center,float r){var pts=new Vector3[49];for(int i=0;i<49;i++){float a=i/48f*Mathf.PI*2;pts[i]=center+new Vector3(Mathf.Cos(a)*r,0,Mathf.Sin(a)*r);}return pts;}
        void Release(Material material){if(material!=null&&owned.Remove(material))Destroy(material);}
        void Clear(Transform root){foreach(var line in root.GetComponentsInChildren<LineRenderer>(true))Release(line.sharedMaterial);for(int i=root.childCount-1;i>=0;i--){var g=root.GetChild(i).gameObject;g.SetActive(false);Destroy(g);}}
        public void Initialize(NetworkSimulation engine)
        {
            sim=engine;sceneRoot=new GameObject("City seed "+engine.Map.Seed).transform;standard=Resources.Load<Material>("DioramaMaterial");neon=Resources.Load<Material>("NeonMaterial");if(standard==null||neon==null)throw new System.Exception("Network materials missing");
            SetupMetalLook();packetMaterial=Glow(Cyan*2.3f);
            Application.targetFrameRate=60;QualitySettings.vSyncCount=1;RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=RGB(0x263f65);RenderSettings.fog=true;RenderSettings.fogColor=RGB(0x020b1a);RenderSettings.fogMode=FogMode.ExponentialSquared;RenderSettings.fogDensity=.013f;
            Camera=new GameObject("Network Camera").AddComponent<Camera>();Camera.transform.SetParent(sceneRoot,false);Camera.tag="MainCamera";Camera.allowHDR=true;Camera.gameObject.AddComponent<CityBloom>();Camera.orthographic=true;Camera.nearClipPlane=.1f;Camera.farClipPlane=1000;Camera.clearFlags=CameraClearFlags.SolidColor;Camera.backgroundColor=RGB(0x020916);
            var sun=new GameObject("Moonlight").AddComponent<Light>();sun.transform.SetParent(sceneRoot,false);sun.type=LightType.Directional;sun.intensity=.65f;sun.color=RGB(0x6388bd);sun.transform.rotation=Quaternion.Euler(38,-45,0);sun.shadows=LightShadows.None;
            city=Group("Nocturne City");linkRoot=Group("Luminous conduits");packetRoot=Group("Data packets");
            minX=-17;maxX=17;minZ=-9;maxZ=15;foreach(var n in sim.Nodes){minX=Mathf.Min(minX,(float)n.X-4);maxX=Mathf.Max(maxX,(float)n.X+5);minZ=Mathf.Min(minZ,(float)n.Z-4);maxZ=Mathf.Max(maxZ,(float)n.Z+4);}
            float mx=(minX+maxX)/2,mz=(minZ+maxZ)/2;
            Box(city,"City foundation",mx,-.35f,mz,maxX-minX,.5f,maxZ-minZ,Mat(0x030b16));Box(city,"Reflective urban plane",mx,-.08f,mz,maxX-minX-.2f,.08f,maxZ-minZ-.2f,Mat(0x08152b));
            for(float x=minX;x<=maxX;x+=2)Line(city,"Faint city grid",new[]{new Vector3(x,.01f,minZ),new Vector3(x,.01f,maxZ)},.015f,new Color(.025f,.09f,.17f));
            for(float z=minZ;z<=maxZ;z+=2)Line(city,"Faint city grid",new[]{new Vector3(minX,.01f,z),new Vector3(maxX,.01f,z)},.015f,new Color(.025f,.09f,.17f));
            BuildTerrain();
            foreach(var n in sim.Nodes)BuildNode(n);
            var random=new CityRandom(sim.Map.Seed);
            for(int i=0;i<52;i++){float x=-13+(float)random.Unit()*28,z=8+(float)random.Unit()*6,h=1.5f+(float)random.Unit()*5;bool near=false;foreach(var n in sim.Nodes)if(Mathf.Abs((float)n.X-x)<3&&Mathf.Abs((float)n.Z-z)<4)near=true;if(near)continue;Tower(x,z,.45f+(float)random.Unit()*.65f,h,.45f+(float)random.Unit()*.6f,Mat(0x0c1b32),Glow(new Color(.28f,.48f,.7f)),"Skyline");}
            // A stepped needle makes the night skyline recognisable from a distance.
            float bx=minX+2,bz=12;for(int i=0;i<9;i++){float size=1.3f-i*.125f;Box(city,"Needle tower",bx,1+i*1.05f,bz,size,1.2f,size,Mat(0x102640));Line(city,"Spire ribbon",new[]{new Vector3(bx+size/2+.01f,.5f+i*1.05f,bz),new Vector3(bx+size/2+.01f,1.5f+i*1.05f,bz)},.025f,new Color(.2f,.55f,.8f));}Line(city,"Spire needle",new[]{new Vector3(bx,10,bz),new Vector3(bx,13.5f,bz)},.025f,new Color(.25f,.7f,1));
            RefreshBatteries();RefreshCommunicationCenters();
            preview=Line(null,"Pending connection",new[]{Vector3.zero,Vector3.zero},.06f,Cyan*.7f);preview.gameObject.SetActive(false);RefreshLinks();UpdateCamera();
        }
        void Tower(float x,float z,float w,float h,float d,Material body,Material windows,string name)
        {
            Box(city,name,x,h/2,z,w,h,d,body);int floors=Mathf.Clamp(Mathf.FloorToInt(h/.35f),3,18);for(int f=1;f<floors;f++){float y=f*h/floors;Box(city,"Window ribbon",x,y,z,w+.04f,.075f,d+.04f,windows);}Box(city,"Roof cap",x,h+.025f,z,w+.07f,.06f,d+.07f,body);
        }
        void BuildNode(GridNode n)
        {
            var p=NodePoint(n.Id);Box(city,n.Code+" plinth",p.x,.12f,p.z,1.55f,.24f,1.55f,n.Kind==NetworkNodeKind.District?Mat(0x142840):Metal(DarkMetal));Shape(city,"Hub socket",new Vector3(p.x,.3f,p.z),new Vector3(.8f,.12f,.8f),n.Kind==NetworkNodeKind.District?Mat(0x183c55):Metal(Gunmetal),PrimitiveType.Cylinder);
            var ring=Line(city,n.Code+" capacity ring",Circle(new Vector3(p.x,.38f,p.z),.72f),.075f,Cyan*.3f);rings.Add(ring);
            if(n.Kind==NetworkNodeKind.Reactor){for(int i=0;i<3;i++){var centre=new Vector3(p.x,.65f+i*.25f,p.z);CopperWinding(centre,.48f-i*.06f);var coil=Line(city,"Reactor coil",Circle(centre+Vector3.up*.045f,.48f-i*.06f),.025f,Amber*(.7f+i*.1f));BuildingGlow(n.Id,BuildingAccentKind.ReactorCoil,coil.sharedMaterial,i);}Shape(city,"Reactor core",new Vector3(p.x,.9f,p.z),new Vector3(.4f,1.1f,.4f),Metal(Gunmetal),PrimitiveType.Cylinder);infrastructureLights[n.Id]=PointLight(p+Vector3.up*1.5f,Amber,3,6);}
            else if(n.Kind==NetworkNodeKind.DataCore){for(int j=0;j<3;j++){Box(city,"Server column",p.x+(j-1)*.4f,1.1f,p.z,.26f,1.6f,.6f,Metal(DarkMetal));for(int i=0;i<6;i++){var lamp=Glow(Cyan*.8f);Box(city,"Server light",p.x+(j-1)*.4f,.5f+i*.22f,p.z-.31f,.21f,.035f,.025f,lamp);BuildingGlow(n.Id,BuildingAccentKind.ServerLamp,lamp,j*6+i);}}infrastructureLights[n.Id]=PointLight(p+Vector3.up*1.5f,Cyan,2,6);}
            else if(n.Kind==NetworkNodeKind.Relay){Shape(city,"Switching hub",new Vector3(p.x,.57f,p.z),new Vector3(.6f,.22f,.6f),Metal(Gunmetal),PrimitiveType.Cylinder);Line(city,"Switch ring",Circle(new Vector3(p.x,.86f,p.z),.38f),.04f,Cyan*.5f);}
            else
            {
                BuildDistrict(n,p);

            }
            InfrastructureTrim(n,p);
        }
        Light PointLight(Vector3 p,Color color,float intensity,float range){var light=new GameObject("Infrastructure light").AddComponent<Light>();light.transform.SetParent(sceneRoot,false);light.transform.position=p;light.type=LightType.Point;light.color=color;light.intensity=intensity;light.range=range;return light;}
        public Vector3 Sample(GridLink link,float t){var a=NodePoint(link.A);var b=NodePoint(link.B);var p=Vector3.Lerp(a,b,t);p.y+=Mathf.Sin(t*Mathf.PI)*(.25f+(float)link.Length*.035f);return p;}
        public void RefreshLinks()
        {
            Clear(linkRoot);linkViews.Clear();foreach(var l in sim.Links.Values){var power=new Vector3[25];var data=new Vector3[25];for(int i=0;i<25;i++){float t=i/24f;var p=Sample(l,t);power[i]=p+Vector3.right*.055f;data[i]=p-Vector3.right*.055f;}
                var hull=Line(linkRoot,"Conduit "+l.Id,power,.11f,new Color(.025f,.045f,.065f));hull.sharedMaterial.SetFloat("_Pulse",0);
                var v=new LinkVisual{Link=l,Power=Line(linkRoot,"Power stream",power,.18f,Amber*.25f,true),Data=Line(linkRoot,"Data lane",data,.085f,Cyan*.25f)};v.Power.gameObject.SetActive(l.Kind!=LinkKind.DataOnly);v.Data.gameObject.SetActive(l.Kind!=LinkKind.PowerOnly);linkViews.Add(l.Id,v);
                if(sim.Map.Generated&&sim.Map.CrossesRiver(sim.Nodes[l.A],sim.Nodes[l.B]))BuildBridge(l);
                for(float t=.2f;t<.9f;t+=.3f){var p=Sample(l,t);Box(linkRoot,"Cable support",p.x,p.y/2,p.z,.055f,p.y,.055f,Mat(0x244057));}}
        }
        public void Sync()
        {
            bool design=sim.Phase==RoundPhase.Design;
            foreach(var v in linkViews.Values){float strength=.15f+(float)(v.Link.PowerLoad/16)*1.6f;v.Power.sharedMaterial.SetColor("_Color",Amber*strength);v.Data.sharedMaterial.SetColor("_Color",Cyan*(design?.6f:.5f));}
            foreach(var n in sim.Nodes){float utilisation=(float)((n.PowerLoad+(sim.Time>0?n.Processed/sim.Time:0))/n.Capacity);Color c=n.Kind==NetworkNodeKind.Reactor?Amber:Cyan;float strength=.25f;if(n.Kind==NetworkNodeKind.Relay){c=utilisation>.9?Red:utilisation>.7?Amber:Cyan;strength=.25f+utilisation*.6f;}if(sim.Continuous&&n.Kind==NetworkNodeKind.DataCore){c=sim.DataSourceConnected(n.Id)?Cyan:Amber;strength=.25f+(float)(n.DataOutputRate/System.Math.Max(.001,n.DataSupply));}if(n.Id==SelectedNode||n.Id==PendingNode)strength=1.5f;rings[n.Id].sharedMaterial.SetColor("_Color",c*strength);if(n.Kind==NetworkNodeKind.District){float power=(float)n.PowerFraction;float bright=design?.08f:.12f+power*.9f;Color hue=Color.Lerp(Amber,new Color(.3f,.65f,1f),(float)n.Reliability*.55f);districtWindows[n.Id].SetColor("_Color",hue*bright*WindowMotion(n));}}
            SyncBuildingMotion();SyncBatteries();SyncCommunicationCenters();
            var live=new HashSet<int>();foreach(var p in sim.Packets){live.Add(p.Id);GameObject go;if(!packetViews.TryGetValue(p.Id,out go)){go=Shape(packetRoot,"Data "+p.Id,Vector3.zero,new Vector3(.18f,.18f,.18f),packetMaterial,PrimitiveType.Sphere);packetViews.Add(p.Id,go);var trail=Line(packetRoot,"Packet tail",new[]{Vector3.zero,Vector3.zero},.035f,Cyan*.7f);trails.Add(p.Id,trail);}Vector3 pos,tail;if(p.Travelling){var l=sim.Links[NetworkSimulation.EdgeId(p.Node,p.Next)];float t=(float)p.Progress;if(l.A!=p.Node)t=1-t;pos=Sample(l,t)+Vector3.up*.03f-Vector3.right*.055f;float old=l.A==p.Node?Mathf.Max(0,t-.06f):Mathf.Min(1,t+.06f);tail=Sample(l,old)+Vector3.up*.03f-Vector3.right*.055f;}else{var centre=NodePoint(p.Node);float angle=p.Id*2.399f+Time.unscaledTime*.7f;float radius=.3f+(p.Id%4)*.06f;pos=centre+new Vector3(Mathf.Cos(angle)*radius,.18f,Mathf.Sin(angle)*radius);tail=pos;}go.transform.position=pos;trails[p.Id].SetPositions(new[]{tail,pos});}
            var remove=new List<int>();foreach(var pair in packetViews)if(!live.Contains(pair.Key)){pair.Value.SetActive(false);Destroy(pair.Value);trails[pair.Key].gameObject.SetActive(false);Release(trails[pair.Key].sharedMaterial);Destroy(trails[pair.Key].gameObject);remove.Add(pair.Key);}foreach(int id in remove){packetViews.Remove(id);trails.Remove(id);}
        }
        public void ClearPackets(){foreach(var p in packetViews.Values){p.SetActive(false);Destroy(p);}foreach(var t in trails.Values){t.gameObject.SetActive(false);Release(t.sharedMaterial);Destroy(t.gameObject);}packetViews.Clear();trails.Clear();}
        public void PreviewConnection(int from,int target,Vector3 mouse,Color color)
        {
            if(from<0){preview.gameObject.SetActive(false);return;}
            if(target>=0){preview.gameObject.SetActive(true);var link=new GridLink(from,target,LinkKind.Shared,sim.Distance(from,target));preview.positionCount=25;for(int i=0;i<25;i++)preview.SetPosition(i,Sample(link,i/24f));}
            else{Vector3 point;if(!GroundPoint(mouse,out point)){preview.gameObject.SetActive(false);return;}preview.gameObject.SetActive(true);preview.positionCount=2;preview.SetPositions(new[]{NodePoint(from),point});}
            preview.sharedMaterial.SetColor("_Color",color*.8f);
        }
        public void UpdateCamera(){if(Camera==null)return;float aspect=(float)Screen.width/Screen.height;Camera.orthographicSize=Mathf.Max(14.2f,23.5f/aspect)/Zoom;var target=new Vector3(1,2.1f,3)+PanOffset;float distance=Mathf.Max(42,Camera.orthographicSize*2);float el=TopDown?89.9f:38,angle=Angle*Mathf.Deg2Rad;Camera.transform.position=target+new Vector3(Mathf.Sin(angle)*distance*Mathf.Cos(el*Mathf.Deg2Rad),distance*Mathf.Sin(el*Mathf.Deg2Rad),-Mathf.Cos(angle)*distance*Mathf.Cos(el*Mathf.Deg2Rad));Camera.transform.LookAt(target);lastW=Screen.width;lastH=Screen.height;}
        public void FocusSector(CitySector sector){PanOffset=new Vector3(((float)sim.Nodes[sector.West].X+(float)sim.Nodes[sector.District].X)/2-1,0,(float)sector.Z-3);Zoom=1;UpdateCamera();}
        public void FrameCity(){TopDown=true;Angle=0;PanOffset=new Vector3((minX+maxX)/2-1,0,(minZ+maxZ)/2-3);float aspect=(float)Screen.width/Screen.height;Zoom=Mathf.Max(14.2f,23.5f/aspect)/Mathf.Max((maxZ-minZ)*.82f,(maxX-minX)*.82f/aspect);UpdateCamera();}
        void LateUpdate(){if(Screen.width!=lastW||Screen.height!=lastH)UpdateCamera();}
        public int PickNode(Vector2 gui){int best=-1;float distance=27*Mathf.Min(Screen.width/1440f,Screen.height/900f);foreach(var n in sim.Nodes){var p=Camera.WorldToScreenPoint(NodePoint(n.Id));float d=Vector2.Distance(gui,new Vector2(p.x,Screen.height-p.y));if(d<distance){distance=d;best=n.Id;}}return best;}
        public void Rebuild(NetworkSimulation engine){DisposeScene();materials.Clear();linkViews.Clear();packetViews.Clear();trails.Clear();rings.Clear();districtWindows.Clear();SelectedNode=PendingNode=-1;PanOffset=Vector3.zero;Angle=-18;Zoom=1;TopDown=false;Initialize(engine);}
        void DisposeScene(){communicationRoot=null;centerAccents.Clear();batteryRoot=null;batteryLamps.Clear();buildingAccents.Clear();infrastructureLights.Clear();ReleaseMetalLook();if(sceneRoot!=null){sceneRoot.gameObject.SetActive(false);Destroy(sceneRoot.gameObject);sceneRoot=null;}foreach(var m in owned)if(m!=null)Destroy(m);owned.Clear();}
        void OnDestroy(){DisposeScene();}
        public int PickLink(Vector2 gui){int best=-1;float shortest=12*Mathf.Min(Screen.width/1440f,Screen.height/900f);foreach(var l in sim.Links.Values)for(int i=0;i<16;i++){var a=Camera.WorldToScreenPoint(Sample(l,i/16f));var b=Camera.WorldToScreenPoint(Sample(l,(i+1)/16f));var p=new Vector2(a.x,Screen.height-a.y);var q=new Vector2(b.x,Screen.height-b.y);var v=q-p;float t=v.sqrMagnitude>.001f?Mathf.Clamp01(Vector2.Dot(gui-p,v)/v.sqrMagnitude):0;float d=Vector2.Distance(gui,p+v*t);if(d<shortest){shortest=d;best=l.Id;}}return best;}
    }
}

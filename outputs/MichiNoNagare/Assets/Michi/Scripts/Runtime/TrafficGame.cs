using UnityEngine;
using Michi.Core;
namespace Michi.Runtime
{
    public sealed class TrafficGame : MonoBehaviour
    {
        public RoadSimulation Simulation {get;private set;}
        public DioramaView View {get;private set;}
        public int Tool,Speed=2;public bool Paused=true;
        public string Toast="";public float ToastUntil;public Vehicle Selected;
        double accumulator;bool drawing;Cell lastCell;
        void Awake(){Simulation=new RoadSimulation();View=gameObject.AddComponent<DioramaView>();View.Initialize(Simulation);gameObject.AddComponent<GameHud>().Initialize(this);}
        public void Notify(string text){Toast=text;ToastUntil=UnityEngine.Time.unscaledTime+2.8f;}
        public void StartTest(){Simulation.StartTest();View.ClearVehicles();Selected=null;Paused=false;accumulator=0;}
        public void Redesign(){Simulation.ReturnToDesign();View.ClearVehicles();View.RefreshRoads();Paused=true;accumulator=0;Selected=null;}
        public void Load(Preset p){if(Simulation.Phase==RoundPhase.Running)return;Simulation.LoadPreset(p);View.ClearVehicles();View.RefreshRoads();Paused=true;Selected=null;}
        public void Undo(){if(Simulation.Undo()){View.RefreshRoads();Notify("ひとつ前の設計に戻しました");}}
        void Apply(Cell p){RoadKind? kind=Tool==3?(RoadKind?)null:Tool==1?RoadKind.CommuteOnly:Tool==2?RoadKind.FreightOnly:RoadKind.General;var result=Simulation.Edit(p,kind);if(result==EditError.DedicatedBudget)Notify("専用タイルは6枚まで。一般道路で上塗りすると回収できます。");if(result==EditError.RoadBudget)Notify("道路は56マスまで。不要な道を回収してください。");if(result==EditError.BridgeBudget)Notify("橋は2本まで。既存の橋を消すと移せます。");}
        void Update()
        {
            if(Simulation.Phase==RoundPhase.Running&&!Paused){accumulator+=UnityEngine.Time.unscaledDeltaTime*Speed;int ticks=0;while(accumulator>=1.0/60&&Simulation.Phase==RoundPhase.Running&&ticks++<16){Simulation.StepRound(1.0/60);accumulator-=1.0/60;}if(Simulation.Phase==RoundPhase.Result){Paused=true;accumulator=0;}}
            View.SyncVehicles();
            if(Input.GetKeyDown(KeyCode.Return)){if(Simulation.Phase==RoundPhase.Design)StartTest();else if(Simulation.Phase==RoundPhase.Result)Redesign();}
            if(Simulation.Phase!=RoundPhase.Running){if(Input.GetKeyDown(KeyCode.F1))Load(Preset.Shared);if(Input.GetKeyDown(KeyCode.F2))Load(Preset.Solution);if(Input.GetKeyDown(KeyCode.F3))Load(Preset.Empty);}
            if(Input.GetKeyDown(KeyCode.Tab))Speed=Speed==1?2:Speed==2?4:1;
            if(Input.GetKeyDown(KeyCode.Space)&&Simulation.Phase==RoundPhase.Running)Paused=!Paused;
            for(int i=0;i<5;i++)if(Input.GetKeyDown(KeyCode.Alpha1+i))Tool=i;
            if(Input.GetKeyDown(KeyCode.Q)){View.Angle-=45;View.UpdateCamera();}if(Input.GetKeyDown(KeyCode.E)){View.Angle+=45;View.UpdateCamera();}
            Vector3 mouse=Input.mousePosition;var gui=new Vector2(mouse.x,Screen.height-mouse.y);bool over=GameHud.WorldInputRect.Contains(gui);Cell p=default(Cell);bool valid=over&&View.Pick(mouse,out p);if(!over)p=default(Cell);
            View.Hover(valid&&Tool!=4?(Cell?)p:null);
            if(over&&Mathf.Abs(Input.mouseScrollDelta.y)>.001f){View.Zoom=Mathf.Clamp(View.Zoom*(Input.mouseScrollDelta.y>0?1.1f:.9f),.7f,2);View.UpdateCamera();}
            if(Input.GetMouseButtonDown(0)&&valid){if(Tool==4){Selected=null;float best=.65f;foreach(var c in Simulation.Cars){float d=Vector2.Distance(new Vector2((float)c.X,(float)c.Z),new Vector2(p.X,p.Z));if(d<best){Selected=c;best=d;}}}else if(Simulation.Phase!=RoundPhase.Design)Notify("設計に戻ってから道路を変更できます。");else{drawing=true;lastCell=p;Simulation.BeginEdit();Apply(p);View.RefreshRoads();}}
            if(drawing&&Input.GetMouseButton(0)&&valid&&p!=lastCell){int x=lastCell.X,z=lastCell.Z;while(x!=p.X||z!=p.Z){if(Mathf.Abs(p.X-x)>=Mathf.Abs(p.Z-z))x+=System.Math.Sign(p.X-x);else z+=System.Math.Sign(p.Z-z);Apply(new Cell(x,z));}lastCell=p;View.RefreshRoads();}
            if(drawing&&Input.GetMouseButtonUp(0)){drawing=false;if(Simulation.EndEdit()){View.ClearVehicles();View.RefreshRoads();Notify("経路の変化を見てから、60秒テストへ。");}}
        }
        void OnApplicationFocus(bool focus){if(!focus&&drawing){drawing=false;Simulation.EndEdit();View.RefreshRoads();}}
    }
}

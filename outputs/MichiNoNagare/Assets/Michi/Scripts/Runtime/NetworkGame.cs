using UnityEngine;
using Michi.Core;
namespace Michi.Runtime
{
    public sealed partial class NetworkGame : MonoBehaviour
    {
        public NetworkSimulation Simulation {get;private set;}public NetworkCityView View {get;private set;}
        public NetworkTutorial Tutorial {get;private set;}
        public readonly ResetConfirmation Reset=new ResetConfirmation();public bool ResetPrompt {get{return Reset.Pending;}}
        public readonly ExpansionConfirmation Expansion=new ExpansionConfirmation();public bool ExpansionPrompt {get{return Expansion.Pending;}}
        public bool GuidePrompt,MapPrompt,SeedFieldFocus;public string SeedText="291026",SeedError="";
        public int Tool,Speed=2,SelectedLink=-1;public bool Paused=true;public string Message="";public float MessageUntil;
        double accumulator;int ignoreConfirmFrame=-1;
        readonly PointerGesture pointer=new PointerGesture();Vector3 dragAnchor;
        void OnApplicationFocus(bool focused){if(!focused){pointer.Cancel();if(View!=null)View.StopRotation();if(!TitleScreen&&!SavePrompt)SaveCity(false);}}
        void Awake(){Simulation=new NetworkSimulation(CityMap.Generate(291026),true);Tutorial=new NetworkTutorial();Paused=true;Tool=4;View=gameObject.AddComponent<NetworkCityView>();View.Initialize(Simulation);Simulation.LoadSolution();View.RefreshLinks();gameObject.AddComponent<NetworkHud>().Initialize(this);LoadSavedCity();}
        public void Notify(string message){Message=message;MessageUntil=Time.unscaledTime+3;}
        public void StartTest(){if(ResetPrompt)return;if(Time.frameCount<=ignoreConfirmFrame)return;if(GuidePrompt||MapPrompt||!Tutorial.AllowsLaunch){Notify("左のガイドに沿って、まず回線をつないでみよう。");return;}Simulation.Start();View.ClearPackets();View.PendingNode=-1;Paused=false;accumulator=0;}
        public void Redesign(){if(Simulation.Continuous){Paused=!Paused;return;}Simulation.ReturnToDesign();View.ClearPackets();View.RefreshLinks();Paused=true;accumulator=0;}
        public void Undo(){if(Tutorial.Active||GuidePrompt){Notify("練習中はガイドで示した操作を試してみよう。");return;}if(Simulation.Undo()){View.RefreshLinks();View.PendingNode=-1;Notify("ひとつ前の回線構成へ戻しました");SaveCity(false);}}
        public void SelectTool(int tool){if(GuidePrompt||!Tutorial.AllowsTool(tool)){Notify("いまは左のガイドで示されたツールを選んでね。");return;}Tool=tool;View.PendingNode=-1;Tutorial.Tick(Simulation,Tool,View.PendingNode,View.SelectedNode);}
        public void NextTutorial(){if(Tutorial.Step==13){RequestCity();return;}Tutorial.Next();if(Tutorial.Step==5)Tool=4;}
        public void EndTutorial(){if(Tutorial.Step==13){NextTutorial();return;}Tutorial.Skip();GuidePrompt=false;View.PendingNode=-1;}
        public void RequestTutorial(){if(Simulation.Phase==RoundPhase.Running&&!Simulation.Continuous){Notify("検証を終えるか、設計へ戻ってからガイドを開いてね。");return;}GuidePrompt=true;}
        public void RestartTutorial(){if(HasSession&&Simulation.Continuous&&!SaveCity(false))return;TitleScreen=false;HasSession=true;TitleGuideConfirm=false;ignoreConfirmFrame=Time.frameCount+1;Simulation=new NetworkSimulation();Tutorial.Begin(Simulation);View.Rebuild(Simulation);GuidePrompt=false;Tool=4;View.PendingNode=View.SelectedNode=-1;SelectedLink=-1;Paused=true;accumulator=0;View.ClearPackets();View.RefreshLinks();View.ResetCamera();pointer.Cancel();}
        public void RequestCity(){if(Simulation.Phase==RoundPhase.Running&&!Simulation.Continuous){Notify("都市の生成は、検証を終えるか設計へ戻ってから行えます。");return;}MapPrompt=true;GuidePrompt=false;SeedFieldFocus=true;SeedText=Simulation.Map.Seed.ToString(System.Globalization.CultureInfo.InvariantCulture);SeedError="";pointer.Cancel();View.PendingNode=-1;}
        public void GenerateCity(){if(Time.frameCount<=ignoreConfirmFrame)return;int seed;if(!int.TryParse(SeedText,System.Globalization.NumberStyles.Integer,System.Globalization.CultureInfo.InvariantCulture,out seed)){SeedError="シード値は -2147483648 ～ 2147483647 の整数です。";return;}var next=new NetworkSimulation(CityMap.Generate(seed),true,NetworkSimulation.CurrentRulesVersion);ignoreConfirmFrame=Time.frameCount+1;Tutorial.Skip();TitleScreen=false;HasSession=true;Simulation=next;Simulation.SetCommunicationPeaks(true);MapPrompt=GuidePrompt=false;View.Rebuild(Simulation);Tool=4;SelectedLink=-1;Paused=true;accumulator=0;pointer.Cancel();Notify("一時停止で開始。通信ピークは90秒後。センター観測で増強できます。");SaveCity(false);}
        public void RequestReset(){if(Tutorial.Active||GuidePrompt){Notify("練習のやり直しは、上の「操作ガイド」から行えます。");return;}if(MapPrompt)return;Reset.Request();pointer.Cancel();View.PendingNode=-1;}
        public void CancelReset(){Reset.Cancel();ignoreConfirmFrame=Time.frameCount+1;}
        public void ConfirmReset(){if(!Reset.Confirm(Simulation))return;ignoreConfirmFrame=Time.frameCount+1;View.PendingNode=View.SelectedNode=-1;SelectedLink=-1;View.Rebuild(Simulation);Tool=4;Paused=true;accumulator=0;pointer.Cancel();Notify("都市を初期化しました。一時停止中です。");SaveCity(false);}
        public void RequestExpansion(){if(!Expansion.Request(Simulation)){Notify("地区の開放は本編の都市で行えます。");return;}pointer.Cancel();View.PendingNode=-1;}
        public void ConfirmExpansion(bool north){ConfirmExpansion(north?ExpansionDirection.North:ExpansionDirection.South);}
        public void ConfirmExpansion(ExpansionDirection direction){if(!Expansion.Confirm(Simulation,direction)){Notify("開放条件と候補地区を確認してください。");return;}ignoreConfirmFrame=Time.frameCount+1;View.Rebuild(Simulation);View.FocusSector(Simulation.Sectors[Simulation.Sectors.Count-1]);SelectedLink=-1;Tool=4;accumulator=0;Notify(NetworkSimulation.DirectionName(direction)+"の"+NetworkSimulation.TerrainName(Simulation.Sectors[Simulation.Sectors.Count-1].Terrain)+"を開放。追加区画へ電力とデータを届けよう。");SaveCity(false);}
        LinkKind Kind {get{return Tool==1?LinkKind.PowerOnly:Tool==2?LinkKind.DataOnly:LinkKind.Shared;}}
        void Edited(NetworkEditError error)
        {
            View.RefreshLinks();if(error==NetworkEditError.None)SaveCity(false);switch(error){case NetworkEditError.TooLong:Notify("距離が長すぎます。11.5以内の中継ハブを経由してください。");break;case NetworkEditError.CableBudget:Notify(Simulation.ModernRules?"建設資材が足りません。共用へ戻すと専用の追加費用を回収できます。":"回線長の上限です。不要な回線を回収してください。");break;case NetworkEditError.DedicatedBudget:Notify("専用回線は送電＋通信で合計"+Simulation.AvailableDedicated+"本まで。共用へ戻すか撤去で回収。");break;case NetworkEditError.Obstacle:Notify("建設不可エリアを横切っています。別の中継施設を経由してください。");break;case NetworkEditError.BridgeBudget:Notify("橋の枠が足りません。川を渡る回線を撤去すると回収できます。");break;case NetworkEditError.None:Notify("回線を更新。電力とデータの経路を確認しよう。");break;}
        }
        void Update()
        {
            ValidateBatteryRemoval();UpdateAutoSave();if(TitleScreen){UpdateTitle();return;}
            bool canRotate=Application.isFocused&&!ResetPrompt&&!MapPrompt&&!GuidePrompt&&!SavePrompt&&!ExpansionPrompt&&!CommunicationPrompt;
            if(canRotate)View.TickRotation((Input.GetKey(KeyCode.E)?1:0)-(Input.GetKey(KeyCode.Q)?1:0),Mathf.Min(Time.unscaledDeltaTime,.1f));else View.StopRotation();
            if(Simulation.Phase==RoundPhase.Running&&!Paused&&!ResetPrompt&&!MapPrompt&&!GuidePrompt&&!SavePrompt&&!ExpansionPrompt&&!CommunicationPrompt){accumulator+=Time.unscaledDeltaTime*Speed;int steps=0;while(accumulator>=1.0/60&&Simulation.Phase==RoundPhase.Running&&steps++<16){Simulation.Step(1.0/60);accumulator-=1.0/60;}if(Simulation.Phase==RoundPhase.Result){Paused=true;accumulator=0;}}
            Tutorial.Tick(Simulation,Tool,View.PendingNode,View.SelectedNode);
            View.Sync();UpdatePlanningPreview();
            if(CommunicationPrompt){if(Input.GetKeyDown(KeyCode.Escape))CloseCommunicationMenu();return;}
            if(SavePrompt){if(Input.GetKeyDown(KeyCode.Escape))CloseSaveMenu();return;}
            if(ExpansionPrompt){if(Input.GetKeyDown(KeyCode.Escape))Expansion.Cancel();else for(int i=0;i<4;i++)if(Input.GetKeyDown(KeyCode.Alpha1+i)){ConfirmExpansion((ExpansionDirection)(i+1));break;}return;}
            if(ResetPrompt){if(Input.GetKeyDown(KeyCode.Escape))CancelReset();return;}
            if(MapPrompt){if(Input.GetKeyDown(KeyCode.Escape))MapPrompt=false;return;}
            if((Input.GetKey(KeyCode.LeftCommand)||Input.GetKey(KeyCode.LeftControl)||Input.GetKey(KeyCode.RightCommand)||Input.GetKey(KeyCode.RightControl))&&Input.GetKeyDown(KeyCode.S)){SaveCity();return;}
            if(Input.GetKeyDown(KeyCode.M)){OpenTitle();return;}
            if(Input.GetKeyDown(KeyCode.X)){RequestExpansion();return;}
            if(Input.GetKeyDown(KeyCode.G)){RequestCity();return;}
            if(Input.GetKeyDown(KeyCode.H))RequestTutorial();
            if(Input.GetKeyDown(KeyCode.Return)&&!GuidePrompt&&Time.frameCount>ignoreConfirmFrame){if(Tutorial.Active&&Tutorial.CanAdvance)NextTutorial();else if(Simulation.Continuous)Paused=!Paused;else if(Simulation.Phase==RoundPhase.Design)StartTest();else if(Simulation.Phase==RoundPhase.Result)Redesign();}
            if(Input.GetKeyDown(KeyCode.Space)&&Simulation.Phase==RoundPhase.Running)Paused=!Paused;
            if(Input.GetKeyDown(KeyCode.Tab))Speed=Speed==1?2:Speed==2?4:1;
            if(Input.GetKeyDown(KeyCode.F1)){RequestReset();return;}
            for(int i=0;i<5;i++)if(Input.GetKeyDown(KeyCode.Alpha1+i))SelectTool(i);
            if(Input.GetKeyDown(KeyCode.Escape)){View.SelectedNode=View.PendingNode=-1;SelectedLink=-1;}
            if((Input.GetKey(KeyCode.LeftCommand)||Input.GetKey(KeyCode.LeftControl))&&Input.GetKeyDown(KeyCode.Z))Undo();
            if(Input.GetKeyDown(KeyCode.V))View.FrameCity();
            if(Time.frameCount<=ignoreConfirmFrame){pointer.Cancel();return;}
            var mouse=new Vector2(Input.mousePosition.x,Screen.height-Input.mousePosition.y);
            bool overUI=GuidePrompt||MapPrompt||ResetPrompt||SavePrompt||ExpansionPrompt||CommunicationPrompt||NetworkHud.IsUI(mouse,Tutorial.ShowsHubDetails||View.SelectedNode>=0||SelectedLink>=0);
            if(Input.GetMouseButtonDown(0)&&!overUI&&View.GroundPoint(Input.mousePosition,out dragAnchor))pointer.Begin(mouse.x,mouse.y,8*Mathf.Min(Screen.width/1440f,Screen.height/900f));
            if(pointer.Active&&Input.GetMouseButton(0)){pointer.Move(mouse.x,mouse.y);if(pointer.Dragging){View.PendingNode=-1;View.DragMap(dragAnchor,Input.mousePosition);}}
            bool click=false;if(Input.GetMouseButtonUp(0))click=pointer.End(mouse.x,mouse.y);
            else if(pointer.Active&&!Input.GetMouseButton(0))pointer.Cancel();
            if(overUI)return;
            if(Mathf.Abs(Input.mouseScrollDelta.y)>.001f){pointer.Cancel();View.Zoom=Mathf.Clamp(View.Zoom*(Input.mouseScrollDelta.y>0?1.09f:.92f),.2f,1.8f);View.UpdateCamera();}
            if(pointer.Dragging)return;
            if(!click)return;int node=View.PickNode(mouse);int link=node<0?View.PickLink(mouse):-1;
            if(Tutorial.Active&&(node>=0?!Tutorial.AllowsNode(node):!Tutorial.AllowsLink(link))){Notify("光る輪の場所を、短くクリックしてね。画面はドラッグで移動できます。");return;}
            if(!Simulation.CanEdit||Tool==4){View.SelectedNode=node;SelectedLink=link;View.PendingNode=-1;return;}
            if(node>=0){View.SelectedNode=node;SelectedLink=-1;if(Tool==3){View.PendingNode=-1;Notify("撤去は回線をクリックしてください");return;}if(View.PendingNode<0){View.PendingNode=node;Notify("接続先のノードをクリック。Escで解除。");}else if(View.PendingNode==node)View.PendingNode=-1;else{Simulation.BeginEdit();var error=Simulation.Connect(View.PendingNode,node,Kind);Simulation.EndEdit();View.PendingNode=-1;Edited(error);}}
            else if(link>=0){View.SelectedNode=-1;SelectedLink=link;View.PendingNode=-1;Simulation.BeginEdit();NetworkEditError error=NetworkEditError.None;if(Tool==3)Simulation.Remove(link);else error=Simulation.Paint(link,Kind);Simulation.EndEdit();Edited(error);}
            else{View.SelectedNode=View.PendingNode=-1;SelectedLink=-1;}
        }
    }
}

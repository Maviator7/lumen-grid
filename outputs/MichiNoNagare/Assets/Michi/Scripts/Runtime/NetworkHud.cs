using UnityEngine;
using Michi.Core;
namespace Michi.Runtime
{
    // Draw and hit-test in the same reference space, including Retina/full-screen windows.
    public sealed partial class NetworkHud : MonoBehaviour
    {
        NetworkGame game; Font font;Texture2D brandLogo; GUIStyle body,tiny,brand,number,headline,button,wrap; bool ready; float scale;bool drawingModal;
        static readonly Color White=NetworkCityView.RGB(0xe1f5ff),Muted=NetworkCityView.RGB(0x8ba6b8),Amber=NetworkCityView.RGB(0xffba68),Cyan=NetworkCityView.RGB(0x58e5ff),Red=NetworkCityView.RGB(0xff7968),Ink=new Color(.014f,.037f,.065f,.96f);
        static float Scale {get{return Mathf.Min(Screen.width/1440f,Screen.height/900f);}}
        public void Initialize(NetworkGame owner){game=owner;}
        void Setup(){brandLogo=Resources.Load<Texture2D>("Branding/LumenGridLogo");font=Resources.Load<Font>("UI/NotoSansCJKjp-Regular");body=new GUIStyle(GUI.skin.label){font=font,fontSize=16,clipping=TextClipping.Clip};body.normal.textColor=White;tiny=new GUIStyle(body){fontSize=13};tiny.normal.textColor=Muted;brand=new GUIStyle(body){fontSize=27};number=new GUIStyle(body){fontSize=32};headline=new GUIStyle(body){fontSize=24};wrap=new GUIStyle(body){wordWrap=true,fontSize=15};button=new GUIStyle(body){alignment=TextAnchor.MiddleCenter,fontSize=16};ready=true;}
        static void Fill(Rect r,Color c){var old=GUI.color;GUI.color=c;GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=old;}
        static void Line(Vector2 a,Vector2 b,Color c,float thickness=1){var matrix=GUI.matrix;GUI.matrix=matrix*Matrix4x4.TRS(new Vector3(a.x,a.y,0),Quaternion.Euler(0,0,Mathf.Atan2(b.y-a.y,b.x-a.x)*Mathf.Rad2Deg),Vector3.one);Fill(new Rect(0,0,(b-a).magnitude,thickness),c);GUI.matrix=matrix;}
        static void Frame(Rect r,Color c,bool selected=false,Color? background=null){Color surface=background??Ink;float k=10;Fill(new Rect(r.x,r.y+k,r.width,r.height-2*k),surface);for(int row=0;row<10;row++){float cut=k-row;Fill(new Rect(r.x+cut,r.y+row,r.width-cut,1),surface);Fill(new Rect(r.x,r.yMax-row-1,r.width-cut,1),surface);}var v=new[]{new Vector2(r.x+k,r.y),new Vector2(r.xMax,r.y),new Vector2(r.xMax,r.yMax-k),new Vector2(r.xMax-k,r.yMax),new Vector2(r.x,r.yMax),new Vector2(r.x,r.y+k)};for(int i=0;i<v.Length;i++)Line(v[i],v[(i+1)%v.Length],new Color(c.r,c.g,c.b,selected?.9f:.3f));if(selected){Fill(new Rect(r.x+14,r.yMax-3,r.width-28,2),c);Fill(new Rect(r.x+14,r.y+1,22,2),c);}}
        void Text(float x,float y,float w,float h,string text,GUIStyle style=null,Color? color=null){var st=style??body;var old=st.normal.textColor;if(color.HasValue)st.normal.textColor=color.Value;float measured=Mathf.Ceil(st.CalcHeight(new GUIContent(text),w))+8;GUI.Label(new Rect(x,y,w,Mathf.Max(h,measured)),text,st);st.normal.textColor=old;}
        bool Button(Rect rect,string text,bool active=false,Color? tint=null,Color? background=null){bool previousEnabled=GUI.enabled;GUI.enabled=previousEnabled&&((!game.MapPrompt&&!game.ResetPrompt&&!game.SavePrompt&&!game.ExpansionPrompt&&!game.CommunicationPrompt)||drawingModal);var c=tint??Cyan;bool hover=rect.Contains(Event.current.mousePosition)&&GUI.enabled;Color baseSurface=background??Ink;Color surface=hover?Color.Lerp(baseSurface,new Color(c.r,c.g,c.b,Ink.a),.08f):baseSurface;Frame(rect,GUI.enabled?c:Muted,active||hover,surface);var old=button.normal.textColor;button.normal.textColor=GUI.enabled?(active?c:White):Muted;bool clicked=GUI.Button(rect,text,button);button.normal.textColor=old;GUI.enabled=previousEnabled;return clicked;}
        const float FooterHeight=192;
        static Rect expansionNoticeBounds;
        static Rect guideBounds=new Rect(26,143,344,326),inspectorBounds;
        public static bool IsUI(Vector2 p,bool inspector){float z=Scale,w=Screen.width/z,h=Screen.height/z;p/=z;return p.y<124||p.y>h-FooterHeight-6||guideBounds.Contains(p)||expansionNoticeBounds.Contains(p)||(inspector&&inspectorBounds.Contains(p));}
        void OnGUI(){expansionNoticeBounds=Rect.zero;if(game==null||game.Simulation==null)return;if(!ready)Setup();scale=Scale;var matrix=GUI.matrix;GUI.matrix=Matrix4x4.TRS(Vector3.zero,Quaternion.identity,new Vector3(scale,scale,1));try{if(game.TitleScreen){DrawTitle();if(game.SavePrompt){drawingModal=true;try{DrawSaveMenu();}finally{drawingModal=false;}}if(game.MapPrompt){drawingModal=true;try{DrawSeedPicker();}finally{drawingModal=false;}}return;}GUI.enabled=!game.MapPrompt&&!game.ResetPrompt&&!game.SavePrompt&&!game.ExpansionPrompt&&!game.CommunicationPrompt;Draw();GUI.enabled=true;if(game.MapPrompt){drawingModal=true;try{DrawSeedPicker();}finally{drawingModal=false;}}if(game.ResetPrompt){drawingModal=true;try{DrawResetPrompt();}finally{drawingModal=false;}}if(game.ExpansionPrompt){drawingModal=true;try{DrawExpansionPicker();}finally{drawingModal=false;}}if(game.SavePrompt){drawingModal=true;try{DrawSaveMenu();}finally{drawingModal=false;}}if(game.CommunicationPrompt){drawingModal=true;try{DrawCommunicationSettings();}finally{drawingModal=false;}}}finally{GUI.matrix=matrix;GUI.enabled=true;}}
        void Draw()
        {
            var s=game.Simulation;float width=Screen.width/scale,height=Screen.height/scale;bool design=s.CanEdit,running=s.Phase==RoundPhase.Running,result=s.Phase==RoundPhase.Result;
            Fill(new Rect(0,0,width,119),new Color(.006f,.019f,.035f,.95f));Line(new Vector2(26,118),new Vector2(width-26,118),new Color(.25f,.7f,.85f,.3f));
            if(brandLogo!=null)GUI.DrawTexture(new Rect(30,11,304,44),brandLogo,ScaleMode.ScaleToFit,true);else Text(30,18,330,40,"LUMEN / GRID",brand);Text(32,55,320,23,game.Simulation.Map.Generated?"SEED "+game.Simulation.Map.Seed+" / v"+CityMap.GeneratorVersion+ (s.ModernRules?" / 地区別供給":" / クラシック"):"操作ガイド / 固定マップ",tiny);Fill(new Rect(32,97,25,3),Cyan);if(Button(new Rect(30,80,133,32),"操作ガイド H"))game.RequestTutorial();if(Button(new Rect(171,80,149,32),"都市を生成 G"))game.RequestCity();
            float tx=width/2-235;Telemetry(tx,"電力供給",(s.Continuous?s.PowerCoverage:design?s.PowerCoverage:s.AveragePower).ToString("P0"),s.Continuous?"現在 / 98%以上":"POWER  /  98%以上",Amber,(float)(s.Continuous?s.PowerCoverage:design?s.PowerCoverage:s.AveragePower));Telemetry(tx+155,"通信成功率",s.Successful+s.Expired>0?(s.Continuous?s.RecentData:s.DataReliability).ToString("P0"):"—",s.Continuous?"直近15秒 / 90%以上":"DATA  /  90%以上",Cyan,(float)(s.Continuous?s.RecentData:s.DataReliability));Telemetry(tx+310,"平均遅延",(s.Continuous?s.RecentDelivered>0:s.Successful>0)?(s.Continuous?s.RecentLatency:s.AverageLatency).ToString("F2")+"s":"—",s.Continuous?"直近15秒 / 2.8s以内":"LATENCY  /  2.8s以内",(s.Continuous?s.RecentLatency:s.AverageLatency)>2.8?Red:White,(s.Continuous?s.RecentDelivered>0:s.Successful>0)?Mathf.Clamp01(1-(float)(s.Continuous?s.RecentLatency:s.AverageLatency)/5):0);
            float bx=width-242;Text(bx,13,214,22,s.Continuous?(game.Paused?"CITY PAUSED":"LIVE / "+s.Time.ToString("0")+"s"):design?"SYSTEM READY":running?(game.Paused?"SIMULATION PAUSED":"LIVE  /  "+(45-s.Time).ToString("00.0")+"s"):s.Passed?"ALL SYSTEMS ONLINE":"ACTION REQUIRED",tiny,running?Cyan:Muted);
            bool nextGuide=game.Tutorial.Active&&game.Tutorial.CanAdvance;
            if(s.Continuous){if(Button(new Rect(bx,43,214,46),game.Paused?"都市を再開  >":"一時停止  II",game.Paused))game.Paused=!game.Paused;}
            else if(running){if(Button(new Rect(bx,43,99,46),game.Paused?"再開":"一時停止",game.Paused))game.Paused=!game.Paused;GUI.enabled=!game.Tutorial.Active;if(Button(new Rect(bx+109,43,105,46),"設計へ"))game.Redesign();GUI.enabled=true;}
            else{GUI.enabled=nextGuide||game.Tutorial.AllowsLaunch;if(Button(new Rect(bx,43,214,46),nextGuide?(game.Tutorial.Step==13?"本編をはじめる >":"ガイドを進める  >"):design?"都市を起動   >":"再設計   >",true,design?Amber:Cyan)){if(nextGuide)game.NextTutorial();else if(design)game.StartTest();else game.Redesign();}GUI.enabled=true;}
            Text(bx,94,220,22,s.Continuous?"ENTER / SPACE  一時停止・再開":game.Tutorial.Active&&!nextGuide&&!game.Tutorial.AllowsLaunch?"左のガイドに沿って操作しよう":"ENTER  /  "+(nextGuide?(game.Tutorial.Step==13?"本編をはじめる":"ガイドを進める"):design?"45秒間の供給を検証":"RETURN TO DESIGN"),tiny);
            if(!game.Tutorial.Active&&!game.GuidePrompt){if(s.Continuous)DrawContinuousBrief();else if(design||result)DrawCityBrief(design);}
            NodeLabels();GuideMarkers();if(game.Tutorial.ShowsHubDetails)Inspector(width-282,157,4);else if(!game.Tutorial.Active)Inspector(width-282,157);DrawTutorial();if(!string.IsNullOrEmpty(game.SaveError)){var errorRect=new Rect(width/2-300,126,600,64);Frame(errorRect,Red,true);Text(errorRect.x+16,errorRect.y+8,568,50,game.SaveError,wrap,Amber);}
            // Resource meters and controls remain anchored to the same safe area at every size.
            float bottom=height-FooterHeight;Fill(new Rect(0,bottom,width,FooterHeight),new Color(.006f,.019f,.035f,.96f));Line(new Vector2(26,bottom),new Vector2(width-26,bottom),new Color(.25f,.7f,.85f,.3f));
            Text(30,bottom+8,292,26,(s.ModernRules?"建設資材  ":"全回線長  ")+s.CableCost.ToString("F1")+" / "+s.CableCostLimit.ToString("0.0"),body);Meter(30,bottom+40,284,(float)(s.CableCost/s.CableCostLimit),Cyan);DrawDedicatedBudget(bottom);
            Text(30,bottom+125,292,24,s.Map.Generated?"川越え  "+s.BridgeCount+" / "+s.AvailableBridges+"本（全種類で共有）":"川越え / 練習では制限なし",tiny,White);
            float start=width/2-385;string[] names={"共用","送電","通信","撤去","観測"};string[] codes={"SHARED","POWER","DATA","REMOVE","INSPECT"};GUI.enabled=design;for(int i=0;i<5;i++){Rect r=new Rect(start+i*112,bottom+16,102,96);Color c=i==1?Amber:i==3?Red:Cyan;GUI.enabled=design&&(!game.Tutorial.Active||game.Tutorial.AllowsTool(i));if(Button(r,"",game.Tool==i||game.Tutorial.TargetTool==i,c))game.SelectTool(i);Icon(new Vector2(r.x+51,r.y+28),i,game.Tool==i?c:Muted);Text(r.x+8,r.y+51,86,25,names[i],button,game.Tool==i?c:White);Text(r.x+9,r.y+75,90,21,(i+1)+" / "+codes[i],tiny);}
            GUI.enabled=design&&!game.Tutorial.Active;if(Button(new Rect(start+574,bottom+16,89,44),"取り消し"))game.Undo();GUI.enabled=true;if(Button(new Rect(start+574,bottom+68,89,44),game.Speed+"× 速度"))game.Speed=game.Speed==1?2:game.Speed==2?4:1;
            GUI.enabled=!game.Tutorial.Active;if(Button(new Rect(start+675,bottom+16,140,44),"都市初期化 F1",false,Amber))game.RequestReset();GUI.enabled=true;
            float cx=width-270;if(Button(new Rect(cx,bottom+16,54,44),"<"))game.View.TurnCamera(-20);if(Button(new Rect(cx+62,bottom+16,54,44),">"))game.View.TurnCamera(20);if(Button(new Rect(cx+124,bottom+16,116,44),game.View.TopDown?"斜め視点":"真上視点",game.View.TopDown)){game.View.TopDown=!game.View.TopDown;game.View.UpdateCamera();}if(Button(new Rect(cx,bottom+68,116,44),"初期位置"))game.View.ResetCamera();if(Button(new Rect(cx+124,bottom+68,116,44),"都市全体"))game.View.FrameCity();
            string[] hints={"共用は電力＋データ。専用枠は使わず、回線長・川越え枠を使います。","送電専用 / 電力を別ルートへ逃がし、通信の処理枠を確保。","通信専用 / データだけを通す回線。","撤去 / 回線をクリックして資材を回収。","観測 / ハブや回線を選び、負荷と通信余力を確認。"};Text(start,bottom+126,790,28,game.Tool==1||game.Tool==2?DedicatedHint():game.Tool==0&&s.ModernRules?"共用：電力＋データ / 資材×1。専用は×1.75。どちらも川越え枠を使います。":hints[game.Tool],tiny,game.Tool==1||game.Tool==2?Cyan:White);if(Button(new Rect(30,height-36,140,29),"タイトルへ M"))game.OpenTitle();GUI.enabled=s.Continuous&&!game.Tutorial.Active;if(Button(new Rect(182,height-36,140,29),"セーブ管理"))game.OpenSaveMenu();GUI.enabled=true;Text(width-310,height-30,286,23,"ドラッグ 移動 / Q・E 長押し回転 / ホイール 拡大",tiny);
            DrawFacilityLegend(start,height-33);
            if(game.EditPreview!=null)DrawPlanningPreview(width,bottom);else if(Time.unscaledTime<game.MessageUntil){Frame(new Rect(width/2-340,bottom-49,680,36),Cyan);Text(width/2-324,bottom-46,650,30,game.Message,body,Cyan);}
        }
        void DedicatedCounts(out int power,out int data){power=data=0;foreach(var link in game.Simulation.Links.Values){if(link.Kind==LinkKind.PowerOnly)power++;else if(link.Kind==LinkKind.DataOnly)data++;}}
        void DrawDedicatedBudget(float bottom)
        {
            var s=game.Simulation;int power,data;DedicatedCounts(out power,out data);int used=power+data,left=s.AvailableDedicated-used;
            Text(30,bottom+48,292,24,"共用  "+(s.Links.Count-used)+"本 / "+(s.ModernRules?"資材×1・本数上限なし":"本数上限なし"),tiny,White);
            Text(30,bottom+73,128,24,"送電専用  "+power+"本",tiny,Amber);
            Text(160,bottom+73,154,24,"＋ 通信専用  "+data+"本",tiny,Cyan);
            Text(30,bottom+99,292,24,"専用枠  "+used+" / "+s.AvailableDedicated+"本 ・ 残り "+left+"本",tiny,left==0?Red:White);
        }
        string DedicatedHint(){if(game.Simulation.ModernRules)return (game.Tool==1?"送電：電力だけ":"通信：データだけ")+" / 資材×1.75。共用へ戻すと差額回収。専用枠の残り "+(game.Simulation.AvailableDedicated-game.Simulation.DedicatedCount)+"本。";int power,data;DedicatedCounts(out power,out data);int left=game.Simulation.AvailableDedicated-power-data;string prefix=game.Tool==1?"送電専用は電力だけ。":"通信専用はデータだけ。";return prefix+(game.Simulation.ModernRules?"資材×1.75 / 共用に戻すと差額回収。":"")+"送電＋通信で専用枠を共有（残り"+left+"本）。"+(left==0?"共用へ戻すか撤去で1枠回収。":"回線長・川越え枠も使います。");}
        void Meter(float x,float y,float width,float value,Color color){for(int j=0;j<20;j++)Fill(new Rect(x+j*width/20,y,width/20-Mathf.Min(3,width/60),4),j/20f<value?color:new Color(.15f,.27f,.34f));}
        void Telemetry(float x,string caption,string value,string goal,Color c,float fraction){Text(x,12,150,23,caption,tiny);Text(x,33,150,45,value,number,c);Meter(x,85,129,fraction,c);Text(x,94,150,21,goal,tiny);}
        void Icon(Vector2 p,int kind,Color c){if(kind==1){Vector2[] v={p+new Vector2(4,-15),p+new Vector2(-8,2),p+new Vector2(2,2),p+new Vector2(-4,15),p+new Vector2(10,-3),p+new Vector2(0,-3)};for(int i=0;i<v.Length-1;i++)Line(v[i],v[i+1],c,2);}else if(kind==2){for(int i=-1;i<=1;i++){Vector2 a=p+new Vector2(i*9-5,-9),b=a+new Vector2(9,9),d=a+new Vector2(0,18);Line(a,b,c,2);Line(b,d,c,2);}}else if(kind==3){Line(p+new Vector2(-10,-10),p+new Vector2(10,10),c,2);Line(p+new Vector2(10,-10),p+new Vector2(-10,10),c,2);}else if(kind==4){for(int i=0;i<24;i++){float a=i*Mathf.PI/12,b=(i+1)*Mathf.PI/12;Line(p+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*10,p+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*10,c);}Line(p+new Vector2(8,8),p+new Vector2(17,17),c,2);}else{Line(p+new Vector2(-17,0),p+new Vector2(17,0),c,2);for(int j=-1;j<=1;j++)Fill(new Rect(p.x+j*15-3,p.y-3,6,6),c);}}
        static readonly string[] Roles={"発電所","データセンター","中継①","中継②","中央ハブ","中継③","中継④","工業区","居住区","医療区"};
        static readonly string[] Purposes={"電力を送り出す","データを送り出す","電力・データを中継","電力・データを中継","電力・データの合流点","電力・データを中継","電力・データを中継","電力とデータが必要","電力とデータが必要","電力とデータが必要"};
        static string BaseRoleName(GridNode n){return n.Id<10?Roles[n.Id]:n.Name;}
        string RoleName(GridNode n){if(game.Simulation.IsCenterBoosted(n.Id))return "増強 / "+BaseRoleName(n);return game.Simulation.BatteryAt(n.Id)!=null?"蓄電所 / "+BaseRoleName(n):BaseRoleName(n);}
        static string Purpose(GridNode n){return n.Id<10?Purposes[n.Id]:n.Kind==NetworkNodeKind.Reactor?"電力を送り出す":n.Kind==NetworkNodeKind.DataCore?"データを送り出す":n.Kind==NetworkNodeKind.Relay?"電力・データを中継":"電力とデータが必要";}
        Vector2 NodeScreen(int id){var p=game.View.Camera.WorldToScreenPoint(game.View.NodePoint(id));return new Vector2(p.x/scale,(Screen.height-p.y)/scale);}
        string DistrictContract(GridNode n)
        {
            if(n.Role==7)return "工業：電力7 MW。通信は7秒以内なので、混雑を避ける長い経路も使えます。";
            if(n.Role==9)return "医療：電力3 MW。通信は生成から2.8秒以内。近いセンターと短い経路を確保しよう。";
            return "住宅：通常3 MW / ピーク5 MW。90秒周期、最後の30秒がピーク。"+(game.Simulation.HousingPeak?"終了まで ":"次のピークまで ")+Mathf.CeilToInt((float)game.Simulation.HousingChangeIn)+"秒。";
        }
        string DistrictIssue(GridNode node)
        {
            if(!game.Simulation.Continuous)return "";
            if(node.Kind==NetworkNodeKind.DataCore&&game.Simulation.IsCenterBoosted(node.Id)&&node.PowerFraction<.98)return "増強電力不足";
            if(node.Kind!=NetworkNodeKind.District)return "";
            bool power=node.PowerFraction<.98,data=!game.Simulation.DistrictDataConnected(node.Id);
            return power&&data?"電力不足 / 通信未接続":power?"電力不足":data?"通信未接続":"";
        }
        void Inspector(float x,float y,int nodeOverride=-1)
        {
            var s=game.Simulation;int id=nodeOverride>=0?nodeOverride:game.View.SelectedNode;
            if(DrawBatteryInspector(x,y,id)||DrawCommunicationInspector(x,y,id))return;
            string[] lines;GUIStyle[] styles;Color[] colors;Color frameColor=Cyan;
            var paragraph=new GUIStyle(tiny){wordWrap=true};var title=new GUIStyle(headline){wordWrap=true};
            if(id>=0){var n=s.Nodes[id];frameColor=FacilityColor(n);
                lines=new[]{n.Code,RoleName(n),
                    n.Kind==NetworkNodeKind.District?"電力需要  "+n.PowerDemand+" MW":n.Kind==NetworkNodeKind.Reactor?"発電容量  "+n.PowerSupply+" MW":n.Kind==NetworkNodeKind.DataCore?"データ出力  24 /s":"共用処理枠  "+n.Capacity,
                    n.Kind==NetworkNodeKind.District?"供給  "+n.PowerFraction.ToString("P0"):"通過電力  "+n.PowerLoad.ToString("F1")+" MW",
                    n.Kind==NetworkNodeKind.District?"期限内データ  "+n.Reliability.ToString("P0"):"通信の余力  "+System.Math.Max(0,n.Capacity-n.PowerLoad).ToString("F1")+" /s",
                    Purpose(n)+"。\n"+(n.Kind==NetworkNodeKind.Reactor?"回線で区画までつながると、\n電気は自動で流れます。":n.Kind==NetworkNodeKind.DataCore?"青い光がデータです。\n5秒以内に区画へ届けましょう。":n.Kind==NetworkNodeKind.Relay?"電力の中継に使った分だけ、\nデータをさばく余力が減ります。":"電力とデータの両方が届くと、\n区画が正常に働きます。"),"ESC  /  CLOSE"};
                if(s.Continuous&&n.Kind==NetworkNodeKind.DataCore)lines=new[]{n.Code,RoleName(n),"出力上限  "+n.DataSupply.ToString("0.0")+" 件/s","送信中  "+n.DataOutputRate.ToString("0.0")+" 件/s","送信累計  "+n.DataDispatched+" 件",(s.DataSourceConnected(id)?"供給可能。送信量は直近15秒。":"未接続。中継と区画へ回線をつなぐと供給に使えます。")+(s.ModernRules?"\n需要が出力を超えると送信待ち。通信期限は医療2.8秒・住宅5秒・工業7秒です。":"\n需要が出力を超えると送信待ちになり、生成から5秒で期限切れになります。"),"ESC / CLOSE"};
                if(s.Continuous&&n.Kind==NetworkNodeKind.District)lines=new[]{n.Code,RoleName(n),"電力  "+n.PowerFraction.ToString("P0")+" / "+n.PowerDemand+" MW","通信需要  "+n.DataDemand.ToString("0.0")+" 件/s","通信期限  "+s.RequestDeadline(id).ToString("0.0")+" 秒","受信 "+n.DataDeliveryRate.ToString("0.0")+" 件/s / 成功率（累計） "+n.Reliability.ToString("P0")+"\n"+PowerSources(s.InspectPower(id))+"\n"+(s.ModernRules?DistrictContract(n):"送信待ちと移動を合わせて、生成から5秒以内に届けましょう。"),"ESC / CLOSE"};
                if(n.Kind==NetworkNodeKind.Relay)lines[5]=RelayPowerDetails(n);
                if(n.Kind==NetworkNodeKind.Reactor)lines=new[]{n.Code,RoleName(n),"発電容量  "+n.PowerSupply.ToString("0.0")+" MW","現在の供給  "+n.PowerLoad.ToString("0.0")+" MW","未使用容量  "+System.Math.Max(0,n.PowerSupply-n.PowerLoad).ToString("0.0")+" MW",ReactorPowerDetails(n),"ESC / CLOSE"};
                styles=new[]{body,title,body,body,body,paragraph,tiny};colors=new[]{frameColor,frameColor,White,Amber,n.Kind==NetworkNodeKind.Reactor?White:Cyan,Muted,Muted};
                if(n.Kind==NetworkNodeKind.Relay||n.Kind==NetworkNodeKind.Reactor){
                    var power=s.InspectPower(id);
                    lines=new[]{lines[0],lines[1],lines[2],lines[3],lines[4],PowerStateTitle(power,n),lines[5],lines[6]};
                    styles=new[]{body,title,body,body,body,body,paragraph,tiny};
                    colors=new[]{frameColor,frameColor,White,Amber,n.Kind==NetworkNodeKind.Reactor?White:Cyan,power.State==PowerUse.InUse||power.State==PowerUse.Standby?Cyan:Amber,Muted,Muted};
                }
            }
            else if(game.SelectedLink>=0&&s.Links.ContainsKey(game.SelectedLink)){var l=s.Links[game.SelectedLink];
                lines=new[]{"CONDUIT  /  "+l.Id,l.Kind==LinkKind.Shared?"共用回線":l.Kind==LinkKind.PowerOnly?"送電専用":"通信専用",(s.ModernRules?"長さ "+l.Length.ToString("F1")+" / 資材 "+s.LinkCost(l.Length,l.Kind).ToString("F1"):"回線長  "+l.Length.ToString("F1")),"通過電力  "+l.PowerLoad.ToString("F1")+" / 16 MW","処理済みデータ  "+l.Sent,l.Kind==LinkKind.DataOnly?"通信専用なので電力は流れません。":"0 MWでも未接続とは限りません。近い発電所の経路から需要分を供給します。","ESC  /  CLOSE"};
                styles=new[]{body,title,body,body,body,paragraph,tiny};colors=new[]{Cyan,White,White,Amber,Cyan,Muted,Muted};
            }
            else {inspectorBounds=Rect.zero;return;}
            const float width=246;float cursor=y;var rows=new Rect[lines.Length];
            for(int i=0;i<lines.Length;i++){float h=Mathf.Ceil(styles[i].CalcHeight(new GUIContent(lines[i]),width))+8;rows[i]=new Rect(x,cursor,width,h);cursor+=h+(i==1?18:i==lines.Length-2?16:8);}
            inspectorBounds=new Rect(x-12,y-10,width+24,cursor-y+18);Frame(inspectorBounds,frameColor);
            for(int i=0;i<lines.Length;i++){var r=rows[i];Text(r.x,r.y,r.width,r.height,lines[i],styles[i],colors[i]);if(i==1)Fill(new Rect(x,r.yMax+7,width,1),new Color(.14f,.33f,.45f,.7f));}
        }
    }
}

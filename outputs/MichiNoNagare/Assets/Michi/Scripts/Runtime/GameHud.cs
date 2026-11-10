using UnityEngine;
using Michi.Core;

namespace Michi.Runtime
{
    public sealed class GameHud : MonoBehaviour
    {
        TrafficGame game;GUIStyle body,small,title,heading,metric,button,label,wrap;Font font;Texture2D round;bool ready;
        public static Rect BoardRect {get{return new Rect(18,207,Mathf.Max(420,Screen.width-332),Mathf.Max(260,Screen.height-327));}}
        public static Rect WorldInputRect {get{var r=BoardRect;return new Rect(r.x,r.y+35,r.width,r.height-95);}}
        public void Initialize(TrafficGame owner){game=owner;}
        void Setup()
        {
            font=Resources.Load<Font>("UI/NotoSansCJKjp-Regular");if(font==null)throw new System.InvalidOperationException("Bundled Japanese font missing.");
            round=new Texture2D(12,12,TextureFormat.RGBA32,false);for(int y=0;y<12;y++)for(int x=0;x<12;x++){float dx=x<4?4-x:x>7?x-7:0;float dy=y<4?4-y:y>7?y-7:0;round.SetPixel(x,y,dx*dx+dy*dy>16?Color.clear:Color.white);}round.Apply();
            body=new GUIStyle(GUI.skin.label){font=font,fontSize=13,richText=true};body.normal.textColor=DioramaView.ColorOf(0x344e40);
            small=new GUIStyle(body){fontSize=10};small.normal.textColor=DioramaView.ColorOf(0x819078);
            title=new GUIStyle(body){fontSize=28,fontStyle=FontStyle.Bold};heading=new GUIStyle(body){fontSize=17};metric=new GUIStyle(body){fontSize=25};
            label=new GUIStyle(body){fontSize=11,alignment=TextAnchor.MiddleCenter};label.normal.background=round;label.border=new RectOffset(4,4,4,4);
            wrap=new GUIStyle(body){fontSize=12,wordWrap=true};wrap.normal.textColor=DioramaView.ColorOf(0x76876c);
            button=new GUIStyle(body){alignment=TextAnchor.MiddleCenter,clipping=TextClipping.Clip,padding=new RectOffset(7,7,5,5),border=new RectOffset(4,4,4,4)};button.normal.background=round;button.hover.background=round;button.active.background=round;button.hover.textColor=body.normal.textColor;button.active.textColor=body.normal.textColor;ready=true;
        }
        static void Paint(Rect r,int rgb,float alpha=1){var old=GUI.color;var c=DioramaView.ColorOf(rgb);c.a=alpha;GUI.color=c;GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=old;}
        bool Button(Rect r,string text,bool selected=false,int selectedColor=0x365d4d,bool enabled=true){var oldB=GUI.backgroundColor;var oldC=GUI.contentColor;bool oldE=GUI.enabled;GUI.enabled=enabled;GUI.backgroundColor=DioramaView.ColorOf(selected?selectedColor:0xfffdf6);GUI.contentColor=selected?Color.white:body.normal.textColor;var normal=button.normal.textColor;button.normal.textColor=selected?Color.white:body.normal.textColor;bool clicked=GUI.Button(r,text,button);button.normal.textColor=normal;GUI.backgroundColor=oldB;GUI.contentColor=oldC;GUI.enabled=oldE;return clicked;}
        void Text(float x,float y,float w,float h,string value,GUIStyle style=null){GUI.Label(new Rect(x,y,w,h),value,style??body);}
        void Line(float x,float y,float w){Paint(new Rect(x,y,w,1),0xdbe1d2);}
        void OnGUI()
        {
            if(game==null||game.Simulation==null||game.View.Camera==null)return;if(!ready)Setup();var s=game.Simulation;var b=BoardRect;float side=Screen.width-292;var result=s.Evaluate();bool design=s.Phase==RoundPhase.Design,running=s.Phase==RoundPhase.Running;
            Text(22,15,450,38,"みちの流れ",title);Text(23,53,500,18,"PUZZLE 01  /  近道を、誰に譲る？",small);Text(Screen.width-270,30,250,20,"UNITY 6  /  FIRST PLAYABLE",small);Line(18,73,Screen.width-36);
            Paint(new Rect(b.x,85,b.width,49),0xe5ecdc);Text(32,91,b.width-330,20,design?"01 設計する":running?"02 交通をテスト中":"03 結果を見て、組み替える");
            Text(32,113,b.width-300,16,design?"専用タイルで通れる車を選び、橋の役割を決めよう。":running?"残り "+Mathf.CeilToInt((float)(60-s.Time))+"秒  /  道路は固定・カメラは操作できます":"同じ交通需要で何度でも比較できます。",small);
            if(design){if(Button(new Rect(b.xMax-148,92,134,34),"▶ 60秒テスト",true))game.StartTest();}
            else{if(Button(new Rect(b.xMax-148,92,134,34),"設計に戻る"))game.Redesign();if(running&&Button(new Rect(b.xMax-233,92,77,34),game.Paused?"▶ 再開":"Ⅱ 停止"))game.Paused=!game.Paused;}
            string[] names={"通勤の到着","届けた貨物","通勤の平均時間","待っている車"};string[] numbers={s.Arrived[0]+" <size=11>台</size>",s.FreightDelivered+" <size=11>個</size>",(s.Arrived[0]>0?s.CommuteAverage.ToString("F1"):"—")+" <size=11>秒</size>",s.Waiting+" <size=11>台</size>"};float width=b.width/4;
            for(int i=0;i<4;i++){float x=b.x+12+i*width;if(i>0)Paint(new Rect(x-12,146,1,43),0xdde3d4);Text(x,144,width-20,18,names[i],small);Text(x,157,width-20,47,numbers[i],metric);}
            Text(b.x+15,b.y+13,260,20,"RIVERSIDE DISTRICT",small);Text(b.xMax-60,b.y+13,55,20,System.TimeSpan.FromSeconds(s.Time).ToString(@"mm\:ss"),small);
            foreach(var building in RoadSimulation.Buildings){var p=game.View.Camera.WorldToScreenPoint(DioramaView.World(building.Position,building.Kind=="office"?2.15f:building.Kind=="factory"?2.1f:1.3f));var gui=new Vector2(p.x,Screen.height-p.y);if(b.Contains(gui)){var old=GUI.backgroundColor;GUI.backgroundColor=DioramaView.ColorOf(0xfcf9ed);GUI.Label(new Rect(gui.x-57,gui.y-21,114,21),building.Name,label);GUI.backgroundColor=old;}}
            if(s.Phase==RoundPhase.Result){Paint(new Rect(b.x+20,b.y+42,b.width-40,71),result.Passed?0xe5efdc:0xf4e8d6,.96f);Text(b.x+34,b.y+49,b.width-70,25,result.Passed?"✓ この街の流れを、ほどけました。":"もう少し、流れを整えよう。",heading);Text(b.x+34,b.y+81,b.width-70,24,result.Passed?"通勤と物流の目標をすべて達成。専用タイル "+s.DedicatedCount+"枚でクリア。":"右の未達成項目を確認し、「設計に戻る」で再挑戦。",body);}
            float camX=b.center.x-170,camY=b.yMax-48;Paint(new Rect(camX-4,camY-4,348,37),0xf6f8ec,.94f);
            if(Button(new Rect(camX,camY,35,28),"↶")){game.View.Angle-=45;game.View.UpdateCamera();}if(Button(new Rect(camX+39,camY,35,28),"↷")){game.View.Angle+=45;game.View.UpdateCamera();}
            if(Button(new Rect(camX+78,camY,35,28),"＋")){game.View.Zoom=Mathf.Min(2,game.View.Zoom*1.17f);game.View.UpdateCamera();}if(Button(new Rect(camX+117,camY,35,28),"−")){game.View.Zoom=Mathf.Max(.7f,game.View.Zoom/1.17f);game.View.UpdateCamera();}
            if(Button(new Rect(camX+158,camY,102,28),game.View.TopDown?"斜めから":"真上から",game.View.TopDown)){game.View.TopDown=!game.View.TopDown;game.View.Angle=game.View.TopDown?0:-36;game.View.UpdateCamera();}
            if(Button(new Rect(camX+266,camY,72,28),"初期視点")){game.View.TopDown=false;game.View.Angle=-36;game.View.Zoom=1;game.View.UpdateCamera();}
            Text(b.x+15,b.yMax-22,b.width-120,18,design?"オレンジ：通勤の経路   /   青：物流の経路":s.Waiting>15?"流れが滞っています":"ふたつの流れを観察しよう",small);
            float ty=b.yMax+12,xTool=b.x;string[] tools={"一般 [1]","通勤専用 [2]","物流専用 [3]","消す [4]","観察 [5]"};float[] widths={77,108,108,77,77};
            for(int i=0;i<5;i++){if(Button(new Rect(xTool,ty,widths[i],33),tools[i],game.Tool==i,i==1?0xb78159:i==2?0x65889d:0x365d4d))game.Tool=i;xTool+=widths[i]+5;}
            if(Button(new Rect(xTool+4,ty,58,33),"↶ 戻す",false,0x365d4d,design))game.Undo();xTool+=67;
            if(Button(new Rect(xTool+4,ty,65,33),"経路",game.View.ShowRoutes)){game.View.ShowRoutes=!game.View.ShowRoutes;game.View.RefreshRoutes();}xTool+=74;
            if(Button(new Rect(xTool+4,ty,45,33),game.Speed+"×"))game.Speed=game.Speed==1?2:game.Speed==2?4:1;
            Text(b.x+2,ty+41,b.width,20,game.Tool==1?"通勤専用：乗用車だけ通行。既存の道路にも塗れます。":game.Tool==2?"物流専用：トラックだけ通行。既存の道路にも塗れます。":"ドラッグで編集・一般で上塗りすると制限を解除  /  Q・E：回転  /  ホイール：ズーム",small);
            Sidebar(side,85,274,s,result,design,running);
            if(UnityEngine.Time.unscaledTime<game.ToastUntil){float w=Mathf.Min(b.width-40,550);Paint(new Rect(b.center.x-w/2,b.yMax-100,w,34),0x345548,.96f);var old=GUI.contentColor;GUI.contentColor=Color.white;var st=new GUIStyle(body){alignment=TextAnchor.MiddleCenter};st.normal.textColor=Color.white;GUI.Label(new Rect(b.center.x-w/2+8,b.yMax-98,w-16,30),game.Toast,st);GUI.contentColor=old;}
        }
        void Sidebar(float x,float y,float w,RoadSimulation s,PuzzleResult result,bool design,bool running)
        {
            Paint(new Rect(x,y,w,135),0xe6ecdc);Text(x+15,y+11,w-30,18,"YOUR CHALLENGE",small);Text(x+15,y+33,w-30,28,"2本あるのに、なぜ詰まる？",heading);Text(x+15,y+68,w-30,61,"どちらの車も近い橋を選び、遠い橋は空いたまま。専用タイルで道の役割を決めよう。",wrap);
            y+=153;Text(x,y,w,20,"道路のストック",body);Text(x,y+30,w,20,"道路                                      "+s.Roads.Count+" / 56");Text(x,y+57,w,20,"橋                                           "+s.BridgeCount+" / 2");Text(x,y+84,w,20,"専用タイル                              "+s.DedicatedCount+" / 6");Line(x,y+113,w);
            y+=128;Text(x,y,w,20,"60秒後の達成目標",body);string[] items={"通勤の到着","貨物の到着","通勤の平均","終了時の待ち車"};string[] vals={s.Arrived[0]+" / 55台",s.FreightDelivered+" / 150個",(s.Arrived[0]>0?s.CommuteAverage.ToString("F1"):"—")+" / ≤12秒",s.Waiting+" / ≤15台"};for(int i=0;i<4;i++){string tint=result.Goals[i]?"#548147":"#88927C";Text(x,y+30+i*27,115,20,items[i]);Text(x+116,y+30+i*27,w-116,20,"<color="+tint+">"+(result.Goals[i]?"✓ ":"")+vals[i]+"</color>");}Line(x,y+145,w);
            y+=160;Text(x,y,w,20,"設計を読み込む",body);if(Button(new Rect(x,y+28,w,29),"01  未整理の街",false,0x365d4d,!running))game.Load(Preset.Shared);if(Button(new Rect(x,y+63,w,29),"02  解法の一例を見る",false,0x365d4d,!running))game.Load(Preset.Solution);if(Button(new Rect(x,y+98,w,29),"03  白紙から設計する",false,0x365d4d,!running))game.Load(Preset.Empty);
            y+=141;if(game.Selected!=null){Text(x,y,w,55,game.Selected.IsTruck?"大型トラック / 貨物6個\n工場 → 物流倉庫":"乗用車 / 通勤\n住宅 → オフィス",wrap);}else Text(x,y,w,44,"UNITY FIRST PLAYABLE\n固定マップ1面。街の成長・復路は後続。",small);
        }
        void OnDestroy(){if(round!=null)Destroy(round);}
    }
}

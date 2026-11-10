using UnityEngine;
using Michi.Core;
namespace Michi.Runtime
{
    public sealed partial class NetworkHud
    {
        void DrawTutorial()
        {
            var t=game.Tutorial;
            if(game.GuidePrompt){guideBounds=new Rect(26,143,344,294);Frame(new Rect(26,143,344,294),Amber);Text(44,157,310,30,"操作ガイドを最初から",headline,Amber);Text(44,204,302,110,"いまの回線配置をリセットして、\n最初の一本をつなぐところから\n練習します。",wrap);if(Button(new Rect(44,319,308,45),"配置をリセットして始める",true,Amber))game.RestartTutorial();if(Button(new Rect(44,376,308,40),"キャンセル"))game.GuidePrompt=false;return;}
            if(!t.Active)return;
            string title="",instruction="",why="";int group=1;
            switch(t.Step){
                case 0:title="一本つないでみよう";instruction="あなたは都市の電力・通信の設計者。\nまずは発電所から電気を届けます。\nいまは一本だけ線が切れているので、\n上の電力供給は 0% です。";why="建物ではなく、丸い接続点をクリック。\n奥の高層ビルは背景です。";break;
                case 1:title="①「送電」を選ぶ";instruction="画面下で光っている\n稲妻アイコンの「送電」をクリック。\nキーボードの［2］でも選べます。";why="送電専用の線は、電気だけを通します。";break;
                case 2:title="② 発電所をクリック";instruction="オレンジのコイルが発電所です。\n光る輪の真ん中を、短くクリック。\nまず「接続元」を決めます。";why="押したまま動かすとマップ移動。\nここでは押して、すぐ離してね。";break;
                case 3:title="③ 中継①をクリック";instruction="次に、発電所の右にある\n光る「中継①」を短くクリック。\n2点の間に送電線ができます。";why="ドラッグで線を引く操作ではありません。\n接続元 → 接続先を、1回ずつクリック。";break;
                case 4:group=2;title="電気がつながった！";instruction="上の電力供給が 100% になりました。\n発電所 → 中継① → 中央ハブから、\n既存の線を通って右の3区画へ\n電気が自動で流れています。";why="電力は区画の需要に合わせて流れます。\n本編では近い発電所の経路を優先。\n接続済みでも通過電力が0のことがあります。";break;
                case 5:group=3;title="中央ハブを見てみよう";instruction="中央で光る丸い施設をクリック。\n電力とデータの両方がここに集まり、\n電力が処理枠を14使っています。\nデータに使える枠は残り2だけ。";why="目的は、電気を届けたまま、\nデータの渋滞も解消すること。";if(t.HubInspected){instruction="右の中央ハブの詳細を見てみよう。\n通過電力 "+t.HubPowerBefore.ToString("0.0")+" / 共用処理枠 "+t.HubCapacityBefore.ToString("0.0")+"。\n通信に使える余力は残り "+(t.HubCapacityBefore-t.HubPowerBefore).ToString("0.0")+" /s。";why="数値を確認したら「次へ」。\n電気を迂回させて余力を増やそう。";}break;
                case 6:group=4;title="電気の迂回路を作る";instruction="もう一度、下の「送電」を選びます。\n中央を通らずに電気を送れる線を\nこれから一本、増やしましょう。";why="専用線は全部で3本まで。\nこの練習では3本ちょうど使います。";break;
                case 7:group=4;title="中継①をクリック";instruction="発電所の右にある「中継①」が\n迂回路の出発点です。\n光る輪の中心をクリック。";why="点線は、これから作る線の見本です。";break;
                case 8:group=4;title="中継③をクリック";instruction="中央ハブの右下にある「中継③」を\nクリックしてつなぎます。\n中央を通らない送電線ができます。";why="まだ中央の線も電気を通すので、\n次に通れる種類を変更します。";break;
                case 9:group=5;title="「通信」を選ぶ";instruction="下の矢印アイコンの「通信」を選択。\nキーボードは［3］です。\n既存の線を、通信専用に変えます。";why="通信専用の線には電気が入らないので、\n電気は先ほどの迂回路を通ります。";break;
                case 10:group=5;title="光る既存の線をクリック";instruction="「中継①」と「中央ハブ」の間で、\n光っている線の途中をクリック。\n施設の丸い部分ではなく、線です。";why="線の種類が変わり、電気とデータの\n通り道が分かれます。";break;
                case 11:group=6;title="都市を動かして確認";instruction="右上の「都市を起動」をクリック。\nまたは Enter を押してください。\n45秒間、電気とデータが\nちゃんと届くか検証します。";why="中央ハブの通過電力："+t.HubPowerBefore.ToString("0.0")+" → "+game.Simulation.Nodes[4].PowerLoad.ToString("0.0")+"。\n通信余力："+(t.HubCapacityBefore-t.HubPowerBefore).ToString("0.0")+" → "+(game.Simulation.Nodes[4].Capacity-game.Simulation.Nodes[4].PowerLoad).ToString("0.0")+" /s。\n電気を届けたまま、通信の余力が増えた！";break;
                case 12:group=6;title="青い光を追ってみよう";instruction="オレンジの線は電力、\n移動する青い光はデータです。\n右の区画まで届くと通信成功。\n結果が出るまで見てみましょう。";why="下の速度ボタン / Tabで倍速。\nSpaceで一時停止・再開できます。";break;
                case 13:group=6;title="できた！ 都市が正常稼働";instruction="電気を迂回させて、中央ハブに\nデータを処理する余力ができました。\n「つなぐ」「線の種類を変える」で\n流れを設計するゲームです。";why="本編は近い発電所の経路を優先。\n別経路から供給中は、接続済みでも通過0。\n観測で実際の供給元を確認できます。\n45秒間の安定で都市を広げられます。";break;
            }
            // Measure Japanese font line metrics and wrapping instead of assuming a fixed line height.
            const float left=44,contentWidth=304;
            var titleStyle=new GUIStyle(headline){wordWrap=true};
            var hintStyle=new GUIStyle(tiny){wordWrap=true};
            float titleY=188,titleHeight=Mathf.Ceil(titleStyle.CalcHeight(new GUIContent(title),contentWidth))+8;
            float bodyY=titleY+titleHeight+12,bodyHeight=Mathf.Ceil(wrap.CalcHeight(new GUIContent(instruction),contentWidth))+8;
            float hintY=bodyY+bodyHeight+14,hintHeight=Mathf.Ceil(hintStyle.CalcHeight(new GUIContent(why),contentWidth))+8;
            float buttonY=hintY+hintHeight+18;
            guideBounds=new Rect(26,143,344,buttonY+60-143);
            Frame(guideBounds,Cyan,true);
            Text(left,155,contentWidth,28,"操作ガイド  /  "+group.ToString("00")+" — 06",tiny,Cyan);
            Text(left,titleY,contentWidth,titleHeight,title,titleStyle,White);
            Text(left,bodyY,contentWidth,bodyHeight,instruction,wrap);
            Text(left,hintY,contentWidth,hintHeight,why,hintStyle,Amber);
            bool next=t.CanAdvance;
            if(next){if(t.Step==13){if(Button(new Rect(left,buttonY,308,40),"本編をはじめる >",true,Cyan))game.NextTutorial();}
                else{if(Button(new Rect(left,buttonY,191,40),t.Step==0?"はじめる >":"次へ >",true,Cyan))game.NextTutorial();if(Button(new Rect(245,buttonY,107,40),"ガイド終了"))game.EndTutorial();}}
            else if(Button(new Rect(left,buttonY,308,40),"ガイドを終了（配置は保持）"))game.EndTutorial();
        }
        void GuideMarkers()
        {
            var t=game.Tutorial;if(!t.Active||game.GuidePrompt)return;
            int target=t.TargetNode;
            if(target>=0&&!(t.Step==5&&t.HubInspected)){var p=NodeScreen(target);Color c=t.Step==5?Cyan:Amber;float radius=26+Mathf.Sin(Time.unscaledTime*4)*4;for(int i=0;i<40;i++){float a=i*Mathf.PI/20,b=(i+1)*Mathf.PI/20;Line(p+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius,p+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*radius,c,2);}float labelX=Mathf.Clamp(p.x+38,380,Screen.width/scale-190),labelY=Mathf.Clamp(p.y-13,140,Screen.height/scale-FooterHeight-38);Frame(new Rect(labelX,labelY,176,32),c,true);Text(labelX+7,labelY+1,165,29,"ここをクリック",body,c);}
            if(t.Step==7||t.Step==8){var a=NodeScreen(2);var b=NodeScreen(5);for(int i=0;i<24;i+=2)Line(Vector2.Lerp(a,b,i/24f),Vector2.Lerp(a,b,(i+1)/24f),Amber,2);}
            if(t.Step==10){var l=game.Simulation.Links[NetworkSimulation.EdgeId(2,4)];Vector2 mid=Vector2.zero;for(int i=0;i<24;i++){var a=game.View.Camera.WorldToScreenPoint(game.View.Sample(l,i/24f));var b=game.View.Camera.WorldToScreenPoint(game.View.Sample(l,(i+1)/24f));Line(new Vector2(a.x/scale,(Screen.height-a.y)/scale),new Vector2(b.x/scale,(Screen.height-b.y)/scale),Cyan,3);if(i==12)mid=new Vector2(a.x/scale,(Screen.height-a.y)/scale);}Frame(new Rect(mid.x-80,mid.y+22,188,33),Cyan,true);Text(mid.x-73,mid.y+23,177,30,"この線をクリック",body,Cyan);}
        }
    }
}

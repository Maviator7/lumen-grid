using UnityEngine;
using Michi.Core;
namespace Michi.Runtime
{
    public sealed partial class NetworkHud
    {
        void DrawCityBrief(bool design)
        {
            var s=game.Simulation;string title=design?"都市の供給網を設計する":s.Passed?"CITY ONLINE":"NETWORK DEGRADED";
            string text=design?(s.Map.Generated?"電力とデータを、3つの区画へ。\n川を渡る回線は橋を1枠使います。\n橙色の建設不可エリアは通れません。\nHで操作ガイド / Gで別の都市へ。":"中央ハブの混雑を解消しよう。\nHで操作ガイド / Gで新しい都市へ。"):
                s.Passed?"電力・通信ともに目標を達成。\nGで別のシードにも挑戦できます。":"混雑したハブを観測しよう。\n電力を逃がすルートを試してみて。";
            var heading=new GUIStyle(body){wordWrap=true};float th=Mathf.Ceil(heading.CalcHeight(new GUIContent(title),304))+8,bodyY=158+th+12,bh=Mathf.Ceil(wrap.CalcHeight(new GUIContent(text),304))+8;
            guideBounds=new Rect(26,143,344,bodyY+bh+17-143);Frame(guideBounds,design||s.Passed?Cyan:Red);Text(44,158,304,th,title,heading,design||s.Passed?Cyan:Red);Text(44,bodyY,304,bh,text,wrap);
        }
        void DrawSeedPicker()
        {
            if(Event.current.type==EventType.KeyDown){if(Event.current.keyCode==KeyCode.Return||Event.current.keyCode==KeyCode.KeypadEnter){game.GenerateCity();Event.current.Use();return;}if(Event.current.keyCode==KeyCode.Escape){game.MapPrompt=false;Event.current.Use();return;}}
            float width=Screen.width/scale,height=Screen.height/scale;Fill(new Rect(0,0,width,height),new Color(0,.012f,.025f,.82f));float x=width/2-272,y=height/2-233;
            Frame(new Rect(x,y,544,466),Cyan,true);var title=new GUIStyle(headline){wordWrap=true};Text(x+26,y+21,492,48,"シードから都市を生成",title,Cyan);
            string info="新しい都市は「地区別供給」ルールです。\n工業は大電力、医療は短期限、住宅は定期ピーク。\n通信ピークON。センター増強は追加電力2MW。\n共用の資材×1／専用×1.75。配置はシードで再現。";
            Text(x+26,y+81,492,106,info,wrap);Text(x+26,y+184,492,28,"SEED  /  整数を入力",tiny);
            Rect field=new Rect(x+26,y+222,365,47);Frame(field,Cyan,true);GUI.SetNextControlName("citySeed");game.SeedText=GUI.TextField(new Rect(field.x+12,field.y+3,field.width-24,field.height-6),game.SeedText,12,new GUIStyle(body){fontSize=22,alignment=TextAnchor.MiddleLeft});if(game.SeedFieldFocus){GUI.FocusControl("citySeed");game.SeedFieldFocus=false;}
            if(Button(new Rect(x+402,y+222,116,47),"ランダム")){game.SeedText=Random.Range(0,int.MaxValue).ToString();game.SeedError="";}
            Text(x+26,y+281,492,57,string.IsNullOrEmpty(game.SeedError)?"生成すると、現在の都市と拡張の進行を置き換えます。\nキャンセルすれば現在の状態を保持します。":game.SeedError,wrap,string.IsNullOrEmpty(game.SeedError)?Muted:Red);
            if(Button(new Rect(x+26,y+362,492,44),"新ルールで都市を生成  >",true,Cyan))game.GenerateCity();if(Button(new Rect(x+26,y+417,492,32),"キャンセル  /  ESC"))game.MapPrompt=false;
        }
    }
}

using UnityEngine;
namespace Michi.Runtime
{
    public sealed partial class NetworkHud
    {
        void DrawResetPrompt()
        {
            // Plain Enter cancels, so repeated shortcut presses cannot erase a layout.
            var ev=Event.current;if(ev.type==EventType.KeyDown){if(ev.keyCode==KeyCode.Escape){game.CancelReset();ev.Use();return;}if(ev.keyCode==KeyCode.Return||ev.keyCode==KeyCode.KeypadEnter){if(ev.command||ev.control)game.ConfirmReset();else game.CancelReset();ev.Use();return;}}
            float width=Screen.width/scale,height=Screen.height/scale;Fill(new Rect(0,0,width,height),new Color(0,.012f,.025f,.82f));const float panelWidth=526,contentWidth=474;
            string title="都市を初期化しますか？",message="同じシードの最初の地区に戻します。\n拡張した地区・編集した回線・稼働記録を\n初期化します。\n一時停止した状態で始まります。";
            var heading=new GUIStyle(headline){wordWrap=true};var hint=new GUIStyle(tiny){wordWrap=true};string keys="Esc / Enter：キャンセル\n⌘ / Ctrl + Enter：都市を初期化";
            float th=Mathf.Ceil(heading.CalcHeight(new GUIContent(title),contentWidth))+8,bh=Mathf.Ceil(wrap.CalcHeight(new GUIContent(message),contentWidth))+8,kh=Mathf.Ceil(hint.CalcHeight(new GUIContent(keys),contentWidth))+8;
            float panelHeight=26+th+18+bh+18+kh+24+48+24,x=(width-panelWidth)/2,y=(height-panelHeight)/2,cursor=y+26;
            Frame(new Rect(x,y,panelWidth,panelHeight),Amber,true);Text(x+26,cursor,contentWidth,th,title,heading,Amber);cursor+=th+18;Text(x+26,cursor,contentWidth,bh,message,wrap);cursor+=bh+18;Text(x+26,cursor,contentWidth,kh,keys,hint);cursor+=kh+24;
            if(Button(new Rect(x+26,cursor,231,48),"キャンセル",true,Cyan))game.CancelReset();if(Button(new Rect(x+269,cursor,231,48),"都市を初期化",false,Amber))game.ConfirmReset();
        }
    }
}

using UnityEngine;
namespace Michi.Runtime
{
    public sealed partial class NetworkHud
    {
        void DrawTitle()
        {
            float w=Screen.width/scale,h=Screen.height/scale;
            // A soft horizontal scrim leaves the skyline visible without boxed menu panels.
            for(int i=0;i<80;i++){float t=i/79f;Fill(new Rect(i*w/80,0,w/80+1,h),new Color(.006f,.018f,.036f,.94f*(1-t*t)+.08f));}
            float x=78,y=h*.22f;
            Text(x,y-47,600,28,"URBAN NETWORK SYSTEM  /  01",tiny,Cyan);
            if(brandLogo!=null)GUI.DrawTexture(new Rect(x,y,660,108),brandLogo,ScaleMode.ScaleToFit,true);
            else Text(x,y,660,100,"LUMEN / GRID",new GUIStyle(brand){fontSize=58});
            Text(x+4,y+129,680,42,"都市の鼓動を、つなげ。",new GUIStyle(headline){fontSize=24},White);
            Text(x+4,y+179,640,64,"電力とデータの流れを設計し、\n夜の都市をどこまでも広げていく。",wrap,Muted);
            float menuY=y+285;
            bool locked=game.MapPrompt||game.TitleGuideConfirm;GUI.enabled=!locked;
            if(game.HasSession){if(TitleChoice(x,menuY,"01","つづける","CONTINUE  /  ENTER",true))game.ContinueCity();menuY+=76;}
            if(TitleChoice(x,menuY,game.HasSession?"02":"01","新しい都市","NEW NETWORK  /  N",!game.HasSession))game.NewCityFromTitle();menuY+=76;
            if(TitleChoice(x,menuY,game.HasSession?"03":"02","操作ガイド","LEARN THE SYSTEM  /  H",false))game.GuideFromTitle();
            GUI.enabled=true;
            Line(new Vector2(x,h-74),new Vector2(w-70,h-74),new Color(Cyan.r,Cyan.g,Cyan.b,.25f));
            Text(x,h-59,800,30,!string.IsNullOrEmpty(game.SaveError)?game.SaveError:game.HasSession?"本編は30秒ごとに自動保存 / 最終保存 "+game.LastSavedAt:"電力を届ける。データを流す。混雑をほどく。",tiny);
            Text(w-260,h-59,200,30,"LUMEN LAB  /  PROTOTYPE",tiny,Cyan);
            if(game.HasSession&&!locked){float rx=w-330;Text(rx,h*.37f,260,28,"CURRENT NETWORK",tiny,Cyan);Text(rx,h*.37f+34,260,44,game.Simulation.Map.Generated?"SEED "+game.Simulation.Map.Seed:"操作ガイド",headline);Text(rx,h*.37f+91,260,70,"稼働時間  "+game.Simulation.Time.ToString("0")+" 秒\nメニュー表示中は時間が止まります。",wrap);}
            if(game.HasSession&&!game.Simulation.Continuous&&game.HasStoredCity&&!locked){if(Button(new Rect(w-360,h*.37f+210,290,50),"保存した都市へ / L")){if(game.LoadSavedCity())game.ContinueCity();}}
            if(game.TitleGuideConfirm){drawingModal=true;try{Fill(new Rect(0,0,w,h),new Color(0,.012f,.025f,.88f));float mx=w/2-280,my=h/2-170;Frame(new Rect(mx,my,560,340),Amber,true);Text(mx+28,my+24,504,44,"操作ガイドへ切り替える？",headline,Amber);Text(mx+28,my+90,504,90,"本編の都市は保存してから切り替えます。\nガイド中は本編のセーブを上書きしません。",wrap);if(Button(new Rect(mx+28,my+207,504,46),"ガイドを始める（本編セーブは保持）",true,Amber))game.RestartTutorial();if(Button(new Rect(mx+28,my+267,504,44),"キャンセル / ESC"))game.TitleGuideConfirm=false;}finally{drawingModal=false;}}
        }
        bool TitleChoice(float x,float y,string code,string label,string english,bool primary)
        {
            Rect r=new Rect(x,y,520,66);bool hover=GUI.enabled&&r.Contains(Event.current.mousePosition);Color c=primary||hover?Cyan:Muted;
            if(hover||primary)Fill(r,new Color(.1f,.65f,.8f,hover?.13f:.055f));
            Line(new Vector2(x,y+65),new Vector2(x+520,y+65),new Color(c.r,c.g,c.b,.28f));
            if(primary||hover)Fill(new Rect(x,y+13,3,37),Cyan);
            Text(x+17,y+21,40,30,code,tiny,c);Text(x+62,y+8,370,38,label,headline,primary||hover?White:Muted);Text(x+65,y+42,400,23,english,tiny,c);Text(x+477,y+15,36,34,"›",headline,c);
            return GUI.Button(r,GUIContent.none,GUIStyle.none);
        }
    }
}

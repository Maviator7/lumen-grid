using UnityEngine;
using Michi.Core;
namespace Michi.Runtime
{
    public sealed partial class NetworkHud
    {
        NetworkSimulation expansionNoticeCity;
        bool wasExpansionReady;
        float expansionNoticeUntil;
        void UpdateExpansionNotice()
        {
            var s=game.Simulation;
            if(expansionNoticeCity!=s){expansionNoticeCity=s;wasExpansionReady=false;}
            if(s.ExpansionReady&&!wasExpansionReady)expansionNoticeUntil=Time.unscaledTime+7;
            wasExpansionReady=s.ExpansionReady;
        }
        void DrawExpansionNotice()
        {
            if(!game.Simulation.ExpansionReady||Time.unscaledTime>=expansionNoticeUntil||game.MapPrompt||game.ResetPrompt||game.ExpansionPrompt)return;
            float x=Screen.width/scale/2-228;
            expansionNoticeBounds=new Rect(x,136,456,114);
            Frame(expansionNoticeBounds,Amber,true);
            Text(x+20,143,416,28,"SUPPLY STABLE / 45秒の安定運転を達成",tiny,Amber);
            Text(x+20,172,416,40,"新しい地区を開放できます",headline,White);
            Text(x+20,218,416,26,"左の「地区を選んで開放」 または Xキー",body,Amber);
        }
        void DrawContinuousBrief()
        {
            var s=game.Simulation;UpdateExpansionNotice();Color status=s.ExpansionReady?Amber:Cyan;guideBounds=new Rect(26,143,344,520);Frame(guideBounds,status,s.ExpansionReady);
            Text(44,158,304,30,s.ExpansionReady?"CITY / 新しい地区を開放可能":"CITY / 成長する供給網",body,status);
            Text(44,199,304,30,s.DistrictCount+" 地区  /  拡張 "+s.ExpansionCount+" 回",headline);
            int noData=s.UnconnectedDataDistrictCount,lowPower=s.UnderpoweredDistrictCount;
            bool needsWiring=noData>0||lowPower>0;
            Text(44,244,304,60,"現在 / 電力不足 "+lowPower+" 地区\n通信未接続 "+noData+" 地区",wrap,needsWiring?Amber:Cyan);
            Text(44,310,304,30,s.ExpansionReady?"開放条件クリア！ / 45秒安定":"安定運転  "+s.StableSeconds.ToString("0")+" / 45 秒",body,Amber);Meter(44,349,304,(float)s.StableSeconds/45,status);
            // A persistent filled CTA remains visible after the one-time announcement ends.
            GUI.enabled=true;
            Color readySurface=Color.Lerp(Ink,Amber,.14f+.025f*Mathf.Sin(Time.unscaledTime*2));
            if(Button(new Rect(44,369,304,44),s.ExpansionReady?"地区を選んで開放  >  X":"開放条件を確認  >  X",s.ExpansionReady,status,s.ExpansionReady?(Color?)readySurface:null))game.RequestExpansion();GUI.enabled=true;
            double available=s.ConnectedDataCapacity;bool shortage=available+.001<s.DataDemandTotal;
            Text(44,431,304,28,"通信需要  "+s.DataDemandTotal.ToString("0.0")+" 件/s",body);
            Text(44,463,304,28,"接続出力  "+available.ToString("0.0")+" 件/s",body,shortage?Red:Cyan);
            Text(44,495,304,26,"全設備 "+s.DataCapacityTotal.ToString("0.0")+" / 未接続 "+(s.DataCapacityTotal-available).ToString("0.0")+" 件/s",tiny);
            Text(44,527,304,28,"送信待ち "+s.WaitingData+" 件 / 移動中 "+s.Packets.Count+" 件",body,s.WaitingData>0?Amber:White);
            string hint=shortage?"通信出力が "+(s.DataDemandTotal-available).ToString("0.0")+" 件/s 不足。追加センターを接続しよう。":"全地区の供給を45秒安定させると拡張可能。";
            if(needsWiring)hint="未供給の区画へ回線をつなごう。\n"+hint;
            else if(shortage)hint+=" 配線が届いていても出力不足で通信は失敗します。";
            if(needsWiring||shortage)hint+=" 通信実績は直近15秒です。";
            if(s.ExpansionReady)hint="次は、隣接する地区を選ぼう。\n回線長・専用枠・川越え枠も増えます。";
            Text(44,570,304,58,hint,new GUIStyle(tiny){wordWrap=true},s.ExpansionReady?Amber:Muted);
            if(s.ModernRules)Text(44,634,304,25,(s.HousingPeak?"住宅ピーク中 / 終了まで ":"住宅ピークまで ")+Mathf.CeilToInt((float)s.HousingChangeIn)+"秒（3→5 MW）",tiny,s.HousingPeak?Amber:Cyan);
            DrawExpansionNotice();

        }
        void DrawExpansionPicker()
        {
            var s=game.Simulation;
            float width=Screen.width/scale,height=Screen.height/scale,x=width/2-342,y=height/2-238;
            Fill(new Rect(0,0,width,height),new Color(0,.012f,.025f,.86f));Frame(new Rect(x,y,684,476),Cyan,true);
            Text(x+26,y+20,632,43,"EXPAND / 都市の次の一歩",headline,Cyan);
            Text(x+26,y+76,632,62,"既存の街と配線を残して、新しい需要を追加します。\n開放後、川を渡る橋と新しい区画への回線をつなぎましょう。",wrap);
            if(!s.ExpansionReady){
                Text(x+26,y+155,632,42,"開放条件はまだ満たされていません",headline,Amber);
                string conditions="安定運転  "+s.StableSeconds.ToString("0")+" / 45 秒"+
                    "\n電力不足 "+s.UnderpoweredDistrictCount+" 地区 / 通信未接続 "+s.UnconnectedDataDistrictCount+" 地区"+
                    "\n通信成功率 "+s.RecentData.ToString("P0")+" / 90%以上  ・  平均遅延 "+s.RecentLatency.ToString("0.00")+"秒 / 2.8秒以内";
                Text(x+26,y+213,632,104,conditions,wrap,White);
                Text(x+26,y+333,632,58,"街へ戻って運転を再開し、全地区の供給を45秒間安定させましょう。\n住宅ピークや配線の変更で条件を外れると、安定時間は0秒に戻ります。",wrap,Amber);
            }else{
            var offers=s.ExpansionOffers;
            for(int i=0;i<2;i++){
                var o=offers[i];float bx=x+26+i*326;Frame(new Rect(bx,y+155,306,234),Cyan);
                Text(bx+16,y+170,274,36,o.Title,headline);
                string conditions=game.Simulation.ModernRules?"電力 "+o.Demand+" MW / 通信 "+NetworkSimulation.DistrictDataRate(o.Role).ToString("0.0")+" 件/s\n期限 "+NetworkSimulation.DistrictDeadline(o.Role).ToString("0.0")+"秒"+(o.Role==8?" / ピーク時5 MW":"")+"\n資材 +"+(o.CableGrant*1.15).ToString("0.0")+" / 専用 +2 / 橋 +1":"電力 +"+o.Demand+" MW / 通信 +"+NetworkSimulation.DistrictDataRate(o.Role).ToString("0.0")+" 件/s\n回線長 +"+o.CableGrant.ToString("0")+" / 専用 +2 / 橋 +1";
                Text(bx+16,y+221,274,96,conditions+"\n"+(o.Utilities?"発電所・データセンターを併設":"既存の供給網を延ばす"),new GUIStyle(tiny){wordWrap=true},White);
                GUI.enabled=o.Available;if(Button(new Rect(bx+16,y+328,274,44),o.Available?(i+1)+"  /  この地区を開放":"この方向は建設できません",true,Cyan))game.ConfirmExpansion(i==0);GUI.enabled=true;
            }
            }
            if(Button(new Rect(x+26,y+412,632,40),"今の街へ戻る  /  ESC"))game.Expansion.Cancel();
        }
    }
}

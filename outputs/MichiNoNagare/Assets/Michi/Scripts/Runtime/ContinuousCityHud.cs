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
            var s = game.Simulation;
            UpdateExpansionNotice();
            bool diagnosing = game.DiagnosticDistrict >= 0;
            bool ready = s.ExpansionReady;
            int lowPower = s.UnderpoweredDistrictCount + s.UnderpoweredCenterCount;
            int noData = s.UnconnectedDataDistrictCount;
            bool preparing = s.Time < 2 || s.RecentDelivered == 0 && s.Expired == 0;
            bool poorQuality = !preparing && (s.RecentData < .90 || s.RecentLatency > 2.8);
            string status = ready ? "新地区を開放できます" : lowPower > 0 ? "電力不足 " + lowPower + " か所" :
                noData > 0 ? "通信未接続 " + noData + " 地区" : poorQuality ? "通信品質を改善する" :
                game.Paused ? "運転を再開して供給を確認" : "供給の安定を確認中";
            Color statusColor = ready || lowPower > 0 || noData > 0 || poorQuality ? Amber : Cyan;
            guideBounds = new Rect(26, 143, 344, diagnosing ? 490 : 312);
            Frame(guideBounds, statusColor, ready);
            Text(44, 154, 304, 26, s.DistrictCount + " 地区  /  拡張 " + s.ExpansionCount + " 回", tiny, Muted);
            Text(44, 185, 304, 35, status, new GUIStyle(body){fontSize=20}, statusColor);
            Text(44, 224, 304, 24, "安定運転 " + s.StableSeconds.ToString("0") + " / 45 秒", tiny);
            Meter(44, 252, 304, (float)s.StableSeconds / 45, statusColor);
            var expansion = new Rect(44, 270, 304, 40);
            if (ready ? PrimaryButton(expansion, "地区を選んで開放  ›", Amber) : QuietButton(expansion, "開放条件  /  X"))
                game.RequestExpansion();
            if (Button(new Rect(44, 321, 304, 35), diagnosing ? "通信の診断を閉じる" : "経路と詰まりを見る", diagnosing)) game.ActivateDiagnostic();
            if (Button(new Rect(44, 371, 145, 36), "ピークを試す")) game.OpenPeakTrial();
            if (QuietButton(new Rect(200, 371, 148, 36), "通信ピーク設定")) game.OpenCommunicationMenu();
            Text(44, 414, 304, 25, NextDemandPeak(), tiny, Muted);
            if (diagnosing) DrawDiagnosticBrief(44, 446, 304);
            DrawExpansionNotice();
        }
        string NextDemandPeak()
        {
            var s = game.Simulation;
            if (s.HousingPeak || s.DataPeakActive || s.ResearchBurstActive)
            {
                string active = s.HousingPeak ? "住宅の電力" : "";
                if (s.DataPeakActive || s.ResearchBurstActive)
                    active += (active.Length > 0 ? "・" : "") + "通信";
                return active + "ピーク中";
            }
            double power = s.ModernRules ? s.HousingChangeIn : double.PositiveInfinity;
            double data = s.CommunicationPeaksEnabled ? s.DataPeakChangeIn : double.PositiveInfinity;
            double research = s.HasResearchDistrict ? s.ResearchBurstChangeIn : double.PositiveInfinity;
            double next = System.Math.Min(power, System.Math.Min(data, research));
            if (double.IsPositiveInfinity(next)) return "通常需要で運転";
            return (next == power ? "住宅電力" : next == data ? "通信" : "研究区の通信") +
                "ピークまで " + Mathf.CeilToInt((float)next) + " 秒";
        }

        void DrawExpansionPicker()
        {
            if(game.Simulation.ExpansionReady){DrawCardinalExpansionPicker();return;}
            var s=game.Simulation;
            float width=Screen.width/scale,height=Screen.height/scale,x=width/2-342,y=height/2-238;
            Fill(new Rect(0,0,width,height),new Color(0,.012f,.025f,.86f));Frame(new Rect(x,y,684,476),Cyan,true);
            Text(x+26,y+20,632,43,"EXPAND / 都市の次の一歩",headline,Cyan);
            Text(x+26,y+76,632,62,"今の街を保ったまま、北・南・東・西へ拡張できます。\n全地区への供給を安定させて、次の地区を選びましょう。",wrap);
            Text(x+26,y+155,632,42,"開放条件はまだ満たされていません",headline,Amber);
            string conditions="安定運転  "+s.StableSeconds.ToString("0")+" / 45 秒"+
                "\n電力不足 "+s.UnderpoweredDistrictCount+" 地区・増強 "+s.UnderpoweredCenterCount+"基 / 通信未接続 "+s.UnconnectedDataDistrictCount+" 地区"+
                "\n通信成功率 "+s.RecentData.ToString("P0")+" / 90%以上  ・  平均遅延 "+s.RecentLatency.ToString("0.00")+"秒 / 2.8秒以内";
            Text(x+26,y+213,632,104,conditions,wrap,White);
            Text(x+26,y+333,632,58,"街へ戻って運転を再開し、全地区の供給を45秒間安定させましょう。\n住宅ピークや配線の変更で条件を外れると、安定時間は0秒に戻ります。",wrap,Amber);
            if(Button(new Rect(x+26,y+412,632,40),"今の街へ戻る  /  ESC"))game.Expansion.Cancel();
        }
    }
}

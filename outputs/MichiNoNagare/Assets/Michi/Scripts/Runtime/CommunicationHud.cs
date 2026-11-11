using UnityEngine;
using Michi.Core;
namespace Michi.Runtime
{
    public sealed partial class NetworkHud
    {
        string CommunicationForecast()
        {
            var s=game.Simulation;return !s.CommunicationPeaksEnabled?"通信ピーク OFF / 設定で有効化できます":(s.DataPeakActive?"通信ピーク中 / 終了まで ":"通信ピークまで ")+Mathf.CeilToInt((float)s.DataPeakChangeIn)+"秒";
        }
        void DrawCommunicationSettings()
        {
            var s=game.Simulation;float width=Screen.width/scale,height=Screen.height/scale,x=width/2-340,y=height/2-250;
            Fill(new Rect(0,0,width,height),new Color(0,.012f,.025f,.86f));Frame(new Rect(x,y,680,500),Cyan,true);
            Text(x+26,y+20,628,42,"TRAFFIC / 通信ピーク設定",headline,Cyan);
            Text(x+26,y+78,628,56,"有効化から90秒後に、30秒間の通信ピークが始まります。\n以後は120秒周期。住宅・工業の需要が増え、医療は一定です。",wrap);
            Text(x+26,y+150,628,54,"住宅 1.5 → 3 / 工業 2 → 3 / 医療 3 件/s\nセンター増強は7 → 11件/s。追加分には送電2 MWが必要です。",wrap,White);
            Text(x+26,y+220,628,31,"現在の需要 "+s.DataDemandTotal.ToString("0.0")+" / ピーク需要 "+s.PeakDataDemandTotal.ToString("0.0")+" 件/s ・ 接続出力 "+s.ConnectedDataCapacity.ToString("0.0")+" 件/s",body,Cyan);
            if(Button(new Rect(x+26,y+272,304,44),"通信ピーク ON",game.CommunicationChoice,Cyan))game.CommunicationChoice=true;
            if(Button(new Rect(x+350,y+272,304,44),"通信ピーク OFF",!game.CommunicationChoice,Muted))game.CommunicationChoice=false;
            Text(x+26,y+334,628,44,game.CommunicationChoice?"都市と配線は維持します。初めての有効化・再有効化は90秒の準備時間。\n観測でセンターを選んで増強。共用か送電回線で電力も届けましょう。":"需要は通常に戻ります。センターの増強と追加電力は維持します。\n標準へ戻す場合はセンター詳細から変更できます。",new GUIStyle(tiny){wordWrap=true},Muted);
            GUI.enabled=game.CommunicationChoice!=s.CommunicationPeaksEnabled;
            if(Button(new Rect(x+26,y+397,628,42),"設定を適用する",true))game.ApplyCommunicationChoice();GUI.enabled=true;
            if(Button(new Rect(x+26,y+450,628,30),"変更せずに戻る / ESC"))game.CloseCommunicationMenu();
        }
        bool DrawCommunicationInspector(float x,float y,int id)
        {
            var s=game.Simulation;if(!s.Continuous||game.Tutorial.Active||id<0||s.Nodes[id].Kind!=NetworkNodeKind.DataCore)return false;
            var n=s.Nodes[id];bool boosted=s.IsCenterBoosted(id);const float w=246;
            inspectorBounds=new Rect(x-12,y-10,w+24,548);Frame(inspectorBounds,Cyan);
            Text(x,y,w,26,n.Code+(boosted?" / BOOST":""),body,Cyan);
            Text(x,y+30,w,50,BaseRoleName(n),new GUIStyle(body){fontSize=20,wordWrap=true},Cyan);Line(new Vector2(x,y+82),new Vector2(x+w,y+82),Cyan*.5f);
            Text(x,y+96,w,29,"実効出力 "+n.DataSupply.ToString("0.0")+" / "+s.NominalDataOutput(id).ToString("0.0")+" 件/s",body,boosted&&n.PowerFraction<.98?Amber:Cyan);
            Text(x,y+131,w,27,"送信中 "+n.DataOutputRate.ToString("0.0")+" 件/s",body,White);
            Text(x,y+165,w,26,s.DataSourceConnected(id)?"通信接続済み / 送信量は直近15秒":"通信未接続 / 中継・区画へ接続しよう",tiny,s.DataSourceConnected(id)?Cyan:Amber);
            Text(x,y+201,w,28,boosted?"増強電力 "+n.PowerReceived.ToString("0.0")+"/2 MW ("+n.PowerFraction.ToString("P0")+")":"標準設備 / 追加電力なし",body,boosted&&n.PowerFraction<.98?Amber:White);
            Text(x,y+240,w,73,boosted?(n.PowerReceived<=.00001?"増強設備への電力が不足。\n発電量と共用・送電回線を確認しよう。":CompactPowerSources(s.InspectPower(id),true)):"基本出力7件/s。増強すると追加4件/s。\n追加分は電力の供給率に応じて動きます。",new GUIStyle(tiny){wordWrap=true},Muted);
            Text(x,y+322,w,48,CommunicationForecast()+"\n街のピーク需要 "+s.PeakDataDemandTotal.ToString("0.0")+" 件/s",new GUIStyle(tiny){wordWrap=true},s.DataPeakActive?Amber:Cyan);
            Text(x,y+378,w,27,"増強枠 "+s.BoostedCenters.Count+" / "+s.AvailableCenterBoosts+" 基・残り "+(s.AvailableCenterBoosts-s.BoostedCenters.Count),body);
            GUI.enabled=boosted||s.CanBoostCenter(id);
            if(Button(new Rect(x,y+417,w,38),boosted?"標準7件/sへ戻す":s.CanBoostCenter(id)?"11件/sへ増強（+2 MW）":"増強枠がありません",boosted))game.ToggleCenterBoost(id);GUI.enabled=true;
            Text(x,y+468,w,47,"増強枠は2回拡張ごと+1。\n電力不足なら追加出力が低下。\n回線Undoは設備変更の対象外。",new GUIStyle(tiny){wordWrap=true},Muted);
            return true;
        }
    }
}

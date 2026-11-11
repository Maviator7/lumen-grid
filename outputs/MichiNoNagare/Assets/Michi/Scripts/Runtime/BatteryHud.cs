using UnityEngine;
using Michi.Core;
namespace Michi.Runtime
{
    public sealed partial class NetworkHud
    {
        static readonly Color StorageGreen=NetworkCityView.RGB(0xa7efb2);
        string BatteryPeakHint()
        {
            var s=game.Simulation;
            return s.ModernRules?(s.HousingPeak?"住宅ピーク中 / 終了まで ":"住宅ピークまで ")+Mathf.CeilToInt((float)s.HousingChangeIn)+"秒\n住宅 3 → 5 MW / 90秒周期":"クラシック都市：住宅ピークなし。\n供給不足のときに放電します。";
        }
        string BatteryStatus(BatteryState b)
        {
            if(game.Paused)return "PAUSED / 充放電も停止中";
            if(b.DischargeRate>.00001)return "DISCHARGE / 不足を補助中";
            if(b.ChargeRate>.00001)return "CHARGING / 余剰で充電中";
            if(b.Energy>=NetworkSimulation.BatteryCapacity-.00001)return "FULL / 満充電・待機中";
            bool source=false;foreach(var n in game.Simulation.Nodes)if(n.Kind==NetworkNodeKind.Reactor&&n.PowerSupply>0&&game.Simulation.Path(n.Id,b.Node,SignalKind.Power)!=null){source=true;break;}
            if(!source)return b.Energy<.00001?"EMPTY / 発電所への経路なし":"STANDBY / 充電経路なし";
            return b.Energy<.00001?"EMPTY / 余剰電力を待機":"STANDBY / 余剰・需要を待機";
        }
        string CompactBatterySources(PowerInspection info)
        {
            string text=info.SourcesPassThroughSelection?"この中継を通る供給元：":"接続先区画の供給元：";
            if(info.Sources.Count==0)return text+"なし";
            for(int i=0;i<info.Sources.Count&&i<2;i++){var source=info.Sources[i];string name=RoleName(game.Simulation.Nodes[source.Source]);if(name.Length>11)name=name.Substring(0,10)+"…";text+="\n"+name+"  "+source.Amount.ToString("0.0")+" MW";}
            if(info.Sources.Count>2)text+="\nほか "+(info.Sources.Count-2)+" 設備";return text;
        }
        bool DrawBatteryInspector(float x,float y,int id)
        {
            var s=game.Simulation;if(!s.Continuous||game.Tutorial.Active||id<0||s.Nodes[id].Kind!=NetworkNodeKind.Relay)return false;
            game.ValidateBatteryRemoval();var n=s.Nodes[id];var b=s.BatteryAt(id);Color tint=b==null?RelayLabel:StorageGreen;
            const float w=246;inspectorBounds=new Rect(x-12,y-10,w+24,528);Frame(inspectorBounds,tint);
            Text(x,y,w,25,b==null?n.Code:"STORAGE / "+n.Code,body,tint);
            Text(x,y+30,w,52,b==null?RoleName(n):"蓄電所 / "+BaseRoleName(n),new GUIStyle(body){fontSize=20,wordWrap=true},tint);
            Line(new Vector2(x,y+82),new Vector2(x+w,y+82),tint*.5f);
            if(b==null){
                var info=s.InspectPower(id);
                Text(x,y+94,w,28,PowerStateTitle(info,n),body,info.State==PowerUse.InUse||info.State==PowerUse.Standby?Cyan:Amber);
                Text(x,y+127,w,26,"送電  "+n.PowerLoad.ToString("0.0")+" / "+n.Capacity.ToString("0")+" MW",body,Amber);
                Text(x,y+157,w,26,"通信余力  "+System.Math.Max(0,n.Capacity-n.PowerLoad).ToString("0.0")+" /s",body,Cyan);
                Text(x,y+191,w,76,CompactBatterySources(info),new GUIStyle(tiny){wordWrap=true},Muted);
                Text(x,y+276,w,28,"蓄電設備 / 空き "+(s.AvailableBatteries-s.Batteries.Count)+" / "+s.AvailableBatteries+" 枠",body,StorageGreen);
                Text(x,y+311,w,66,"余剰で充電し、不足時に放電。\n容量120 MJ / 出力最大4 MW\n中継と配線はそのまま使えます。",new GUIStyle(tiny){wordWrap=true},White);
                GUI.enabled=s.CanInstallBattery(id);
                if(Button(new Rect(x,y+389,w,40),s.Batteries.Count>=s.AvailableBatteries?"設備枠が必要（2拡張ごと+1）":"蓄電所に改装する",false,StorageGreen))game.UpgradeBattery(id);
                GUI.enabled=true;Text(x,y+441,w,60,"設置時の残量は0。充電最大2 MW、\n効率90%。回線Undoは設備の対象外。",new GUIStyle(tiny){wordWrap=true},Muted);
            }else{
                Text(x,y+94,w,32,b.Energy.ToString("0.0")+" / 120 MJ",headline,StorageGreen);Meter(x,y+133,w,(float)(b.Energy/NetworkSimulation.BatteryCapacity),StorageGreen);
                Text(x,y+149,w,29,BatteryStatus(b),tiny,StorageGreen);
                Text(x,y+182,w,27,(game.Paused?"充電予定 ":"充電 ")+b.ChargeRate.ToString("0.0")+" / 2 MW",body,StorageGreen);
                Text(x,y+212,w,27,(game.Paused?"放電予定 ":"放電 ")+b.DischargeRate.ToString("0.0")+" / 4 MW",body,Amber);
                Text(x,y+246,w,44,BatteryPeakHint(),new GUIStyle(tiny){wordWrap=true},s.HousingPeak?Amber:White);
                Text(x,y+299,w,44,"送電 "+n.PowerLoad.ToString("0.0")+" / "+n.Capacity.ToString("0")+" MW\n通信余力 "+System.Math.Max(0,n.Capacity-n.PowerLoad).ToString("0.0")+" /s",tiny,White);
                Text(x,y+350,w,70,CompactBatterySources(s.InspectPower(id)),new GUIStyle(tiny){wordWrap=true},Muted);
                bool confirm=game.BatteryRemovalNode==id;
                Text(x,y+418,w,24,confirm?"残量を破棄し、設備枠を回収します。":"回線Undoは設備の対象外です。",tiny,confirm?Amber:Muted);
                if(Button(new Rect(x,y+447,w,34),confirm?"残量を破棄して撤去を確定":"蓄電設備を撤去…",false,confirm?Red:StorageGreen))game.RequestRemoveBattery(id);
                if(confirm){if(Button(new Rect(x,y+486,w,26),"キャンセル"))game.BatteryRemovalNode=-1;}
                else Text(x,y+488,w,24,"中継機能と配線は維持 / 充電効率90%",tiny,Muted);
            }
            return true;
        }
        void DrawBatterySummary(float x,float y)
        {
            var s=game.Simulation;Text(x,y,304,26,"蓄電所  "+s.Batteries.Count+" / "+s.AvailableBatteries+" 基  ・  "+s.StoredEnergy.ToString("0.0")+" MJ",body,StorageGreen);
            Text(x,y+28,304,44,"観測で中継を選ぶと改装できます。\n設備枠は2回拡張するごとに+1。",tiny,Muted);
        }
    }
}

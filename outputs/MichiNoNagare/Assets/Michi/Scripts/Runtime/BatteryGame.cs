using Michi.Core;
namespace Michi.Runtime
{
    public sealed partial class NetworkGame
    {
        public int BatteryRemovalNode=-1;
        NetworkSimulation batteryRemovalCity;
        bool CanChangeBattery {get{return !TitleScreen&&!Tutorial.Active&&!GuidePrompt&&!MapPrompt&&!ResetPrompt&&!SavePrompt&&!ExpansionPrompt;}}
        public void UpgradeBattery(int node)
        {
            if(!CanChangeBattery||!Simulation.InstallBattery(node))return;
            BatteryRemovalNode=-1;View.PendingNode=-1;View.RefreshBatteries();Notify("蓄電所に改装。空の状態から、余剰電力で自動充電します。");SaveCity(false);
        }
        public void RequestRemoveBattery(int node)
        {
            if(!CanChangeBattery||View.SelectedNode!=node||Simulation.BatteryAt(node)==null)return;
            if(BatteryRemovalNode!=node||batteryRemovalCity!=Simulation){BatteryRemovalNode=node;batteryRemovalCity=Simulation;return;}
            if(Simulation.RemoveBattery(node)){BatteryRemovalNode=-1;View.RefreshBatteries();Notify("蓄電設備を撤去。中継と配線は維持し、設備枠を回収しました。");SaveCity(false);}
        }
        public void ValidateBatteryRemoval(){if(batteryRemovalCity!=Simulation||View.SelectedNode!=BatteryRemovalNode||!CanChangeBattery)BatteryRemovalNode=-1;}
    }
}

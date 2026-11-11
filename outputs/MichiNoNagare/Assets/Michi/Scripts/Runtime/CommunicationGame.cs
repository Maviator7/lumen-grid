using UnityEngine;
namespace Michi.Runtime
{
    public sealed partial class NetworkGame
    {
        public bool CommunicationPrompt,CommunicationChoice;
        bool CanChangeCommunication {get{return !TitleScreen&&!Tutorial.Active&&!GuidePrompt&&!MapPrompt&&!ResetPrompt&&!SavePrompt&&!ExpansionPrompt&&!CommunicationPrompt;}}
        public void OpenCommunicationMenu()
        {
            if(!CanChangeCommunication||!Simulation.Continuous)return;
            CommunicationChoice=Simulation.CommunicationPeaksEnabled;CommunicationPrompt=true;pointer.Cancel();View.PendingNode=-1;View.StopRotation();
        }
        public void CloseCommunicationMenu(){CommunicationPrompt=false;ignoreConfirmFrame=Time.frameCount+1;pointer.Cancel();}
        public void ApplyCommunicationChoice()
        {
            if(!CommunicationPrompt||CommunicationChoice==Simulation.CommunicationPeaksEnabled)return;
            if(!Simulation.SetCommunicationPeaks(CommunicationChoice))return;CloseCommunicationMenu();Notify(CommunicationChoice?"通信ピークを有効化。最初のピークまで90秒、センターと電力を準備しよう。":"通信ピークを無効化。増強設備の追加電力は継続します。");SaveCity(false);
        }
        public void ToggleCenterBoost(int node)
        {
            if(!CanChangeCommunication||View.SelectedNode!=node)return;
            bool next=!Simulation.IsCenterBoosted(node);if(!Simulation.SetCenterBoost(node,next))return;
            View.PendingNode=-1;View.RefreshCommunicationCenters();Notify(next?"センターを増強。追加出力4件/sには電力2MWが必要です。":"標準出力7件/sへ戻し、増強枠を回収しました。");SaveCity(false);
        }
    }
}

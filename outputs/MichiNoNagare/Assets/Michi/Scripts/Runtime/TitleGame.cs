using UnityEngine;
namespace Michi.Runtime
{
    public sealed partial class NetworkGame
    {
        public bool TitleScreen {get;private set;}=true;
        public bool HasSession {get;private set;}
        public bool TitleGuideConfirm;
        public void OpenTitle(){View.StopRotation();SaveCity(false);TitleScreen=true;TitleGuideConfirm=false;MapPrompt=GuidePrompt=false;Expansion.Cancel();Reset.Cancel();pointer.Cancel();View.PendingNode=-1;accumulator=0;}
        public void ContinueCity(){if(!HasSession)return;TitleScreen=false;ignoreConfirmFrame=Time.frameCount+1;pointer.Cancel();}
        public void NewCityFromTitle(){ignoreConfirmFrame=Time.frameCount+1;MapPrompt=true;SeedFieldFocus=true;SeedError="";TitleGuideConfirm=false;}
        public void GuideFromTitle(){if(HasSession)TitleGuideConfirm=true;else RestartTutorial();}
        void UpdateTitle()
        {
            if(SavePrompt){if(Input.GetKeyDown(KeyCode.Escape))CloseSaveMenu();return;}
            // Only the disposable attract city runs. An existing player's city stays frozen.
            if(!HasSession){Simulation.Step(System.Math.Min(Time.unscaledDeltaTime,.05f));View.Angle=-18+Mathf.Sin(Time.unscaledTime*.065f)*8;View.UpdateCamera();}
            View.Sync();
            if(MapPrompt){if(Input.GetKeyDown(KeyCode.Escape))MapPrompt=false;return;}
            if(TitleGuideConfirm){if(Input.GetKeyDown(KeyCode.Escape))TitleGuideConfirm=false;return;}
            if(Input.GetKeyDown(KeyCode.Return)){if(HasSession)ContinueCity();else NewCityFromTitle();}
            if(Input.GetKeyDown(KeyCode.L)&&HasStoredCity&&!Simulation.Continuous){if(LoadSavedCity())ContinueCity();return;}
            if(Input.GetKeyDown(KeyCode.N))NewCityFromTitle();
            if(Input.GetKeyDown(KeyCode.H))GuideFromTitle();
            if(Input.GetKeyDown(KeyCode.Escape)&&HasSession)ContinueCity();
        }
    }
}

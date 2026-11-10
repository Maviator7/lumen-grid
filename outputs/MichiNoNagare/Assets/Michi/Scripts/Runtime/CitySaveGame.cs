using System;
using UnityEngine;
using Michi.Core;
namespace Michi.Runtime
{
    public sealed partial class NetworkGame
    {
        readonly CitySaveStore saveStore=new CitySaveStore();float nextAutoSave;
        public string LastSavedAt="",SaveError="";
        public bool HasStoredCity {get{return saveStore.Exists;}}
        SavedCity CaptureCity(){return new SavedCity{Version=1,SavedAt=DateTime.UtcNow.ToString("O",System.Globalization.CultureInfo.InvariantCulture),Network=Simulation.CaptureSave(),Angle=View.Angle,Zoom=View.Zoom,PanX=View.PanOffset.x,PanZ=View.PanOffset.z,TopDown=View.TopDown,Paused=Paused,Speed=Speed,Tool=Tool};}
        public bool SaveCity(bool feedback=true)
        {
            if(!HasSession||!Simulation.Continuous){if(feedback)Notify("操作ガイドは保存対象外です。本編のセーブは保持します。");return false;}
            nextAutoSave=Time.unscaledTime+10;
            try{
                string now=DateTime.UtcNow.ToString("O",System.Globalization.CultureInfo.InvariantCulture);
                var city=CaptureCity();city.SavedAt=now;
                string error;if(!saveStore.TrySave(city,out error)){SaveError=error;Notify(error);if(SavePrompt)SaveMenuMessage=error;return false;}
                LastSavedAt=DateTime.Parse(now,null,System.Globalization.DateTimeStyles.RoundtripKind).ToLocalTime().ToString("MM/dd HH:mm:ss");SaveError="";nextAutoSave=Time.unscaledTime+10;if(SavePrompt)SaveMenuMessage="保存先への書き込みを確認しました。最終保存 "+LastSavedAt;if(feedback)Notify("都市を保存しました。次回もタイトルの「つづける」から再開できます。");return true;
            }catch(Exception ex){SaveError="保存できませんでした："+ex.Message;Notify(SaveError);return false;}
        }
        public bool LoadSavedCity()
        {
            SavedCity city;string error;if(!saveStore.TryLoad(out city,out error)){SaveError=error;return false;}
            try{
                var loaded=NetworkSimulation.RestoreSave(city.Network);Simulation=loaded;Tutorial.Skip();View.Rebuild(Simulation);View.RestoreCamera(city.Angle,city.Zoom,city.TopDown,city.PanX,city.PanZ);// Loading a city always waits for an explicit resume, even if it was saved running.
                Paused=true;Speed=city.Speed;Tool=city.Tool;HasSession=true;SelectedLink=-1;SeedText=Simulation.Map.Seed.ToString();accumulator=0;pointer.Cancel();
                LastSavedAt=DateTime.Parse(city.SavedAt,null,System.Globalization.DateTimeStyles.RoundtripKind).ToLocalTime().ToString("MM/dd HH:mm:ss");SaveError=error;nextAutoSave=Time.unscaledTime+10;return true;
            }catch(Exception){SaveError="都市を復元できませんでした。セーブは保持しています。";return false;}
        }
        void UpdateAutoSave(){if(!TitleScreen&&!SavePrompt&&HasSession&&Simulation.Continuous&&Time.unscaledTime>=nextAutoSave)SaveCity(false);}
        void OnApplicationPause(bool paused){if(paused&&!TitleScreen&&!SavePrompt)SaveCity(false);}
        void OnApplicationQuit(){if(!TitleScreen&&!SavePrompt)SaveCity(false);}
    }
}

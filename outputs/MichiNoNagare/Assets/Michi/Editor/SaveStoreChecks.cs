using System;
using UnityEngine;
using Michi.Core;
using Michi.Runtime;
namespace Michi.Checks
{
    public static class SaveStoreChecks
    {
        static int checks;
        static void Check(bool ok,string why){checks++;if(!ok)throw new Exception("SAVE STORE: "+why);}
        public static string Run()
        {
            checks=0;string key="lumen-grid.tests."+Guid.NewGuid().ToString("N");var store=new CitySaveStore(key);
            try{
                SavedCity loaded;string error;Check(!store.TryLoad(out loaded,out error)&&error=="","new slot empty");
                var sim=new NetworkSimulation(CityMap.Generate(291026),true);var city=new SavedCity{Version=1,SavedAt=DateTime.UtcNow.ToString("O"),Network=sim.CaptureSave(),Angle=7,Zoom=.75f,PanX=3,PanZ=8,Paused=true,Speed=4,Tool=2};
                Check(store.TrySave(city,out error),"first persistent write: "+error);Check(store.TryLoad(out loaded,out error)&&loaded.Paused&&loaded.Speed==4&&loaded.PanZ==8&&loaded.Network.Seed==291026,"JSON persists settings and city");
                sim.Step(.1);city.Network=sim.CaptureSave();Check(store.TrySave(city,out error),"second write creates backup");
                PlayerPrefs.SetString(key,"broken JSON");PlayerPrefs.Save();Check(store.TryLoad(out loaded,out error)&&loaded.Network.Time==0&&error!="","corrupt primary recovers previous valid city");
                city.Network.Version=999;Check(!store.TrySave(city,out error),"invalid candidate rejected before replacing storage");Check(store.TryLoad(out loaded,out error)&&loaded.Network.Time==0,"failed write leaves backup readable");
                PlayerPrefs.SetString(key+".backup","broken too");Check(!store.TryLoad(out loaded,out error)&&store.Exists&&error!="","both invalid preserved and reported");
                return "PASS: "+checks+" persistent storage checks";
            }finally{PlayerPrefs.DeleteKey(key);PlayerPrefs.DeleteKey(key+".backup");PlayerPrefs.Save();}
        }
    }
}

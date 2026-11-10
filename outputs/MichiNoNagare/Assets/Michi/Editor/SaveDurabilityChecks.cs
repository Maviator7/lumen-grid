using System;
using UnityEngine;
using Michi.Core;
using Michi.Runtime;
namespace Michi.Checks
{
    public static class SaveDurabilityChecks
    {
        static int checks;
        static void Check(bool ok,string why){checks++;if(!ok)throw new Exception("SAVE DURABILITY: "+why);}
        public static string Run()
        {
            checks=0;string key="lumen-grid.durability-tests."+Guid.NewGuid().ToString("N");var store=new CitySaveStore(key);
            try{
                foreach(int seed in new[]{664020296,291026,42}){
                    var sim=new NetworkSimulation(CityMap.Generate(seed),true,1);
                    var city=new SavedCity{Version=1,SavedAt=DateTime.UtcNow.ToString("O"),Network=sim.CaptureSave(),Angle=7,Zoom=.75f,Paused=true,Speed=4,Tool=4};string error;SavedCity loaded;
                    Check(store.TrySave(city,out error),"new city seed="+seed+": "+error);
                    sim.LoadSolution();
                    for(int expansion=0;expansion<3;expansion++){
                        for(int i=0;i<12000&&!sim.ExpansionReady;i++)sim.Step(1.0/60);
                        city.Network=sim.CaptureSave();Check(store.TrySave(city,out error),"stable city seed="+seed+" expansion="+expansion+": "+error);
                        Check(store.TryLoad(out loaded,out error)&&loaded.Network.ExpansionCount==expansion,"expanded JSON restores");
                        Check(sim.Expand(expansion%2==0),"expansion setup");sim.LoadSolution();
                        for(int i=0;i<173;i++)sim.Step(1.0/60);
                        city.Network=sim.CaptureSave();Check(store.TrySave(city,out error),"moving packets seed="+seed+" expansion="+expansion+": "+error);
                    }
                }
                return "PASS: "+checks+" save durability checks";
            }finally{PlayerPrefs.DeleteKey(key);PlayerPrefs.DeleteKey(key+".backup");PlayerPrefs.Save();}
        }
        public static void Diagnose(){Debug.Log(Run());}
    }
}

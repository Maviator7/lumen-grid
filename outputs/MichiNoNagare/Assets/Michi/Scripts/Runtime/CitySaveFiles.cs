using System;
using System.IO;
using UnityEngine;
namespace Michi.Runtime
{
    public sealed partial class NetworkGame
    {
        public string PCBackupStatus="";
        public void ReceivePCBackupStatus(string status){PCBackupStatus=status=="OK"?"PCにも保存済み / "+DateTime.Now.ToString("HH:mm:ss"):status;}
        public bool SavePrompt;public SavedCity ImportCandidate;public string SaveMenuMessage="";
        public void OpenSaveMenu(){SavePrompt=true;ImportCandidate=null;SaveMenuMessage="";pointer.Cancel();View.PendingNode=-1;}
        public void CloseSaveMenu(){SavePrompt=false;ImportCandidate=null;ignoreConfirmFrame=Time.frameCount+1;}
        public void ExportCity()
        {
            if(!HasSession||!Simulation.Continuous){SaveMenuMessage="書き出す本編の都市がありません。";return;}
            try{
                string json=CitySaveStore.Encode(CaptureCity()),name="LumenGrid-"+Simulation.Map.Seed+"-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".json";
#if UNITY_WEBGL && !UNITY_EDITOR
                BrowserSaveBridge.LumenSave_Download(json,name);
                SaveMenuMessage="セーブファイルのダウンロードを開始しました。";
#else
                string path=Path.Combine(Application.persistentDataPath,name);File.WriteAllText(path,json);SaveMenuMessage="書き出しました："+path;
#endif
            }catch(Exception ex){SaveMenuMessage="書き出せませんでした："+ex.Message;}
        }
        public void ChooseSaveFile()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            BrowserSaveBridge.LumenSave_Upload(gameObject.name);
#else
            SaveMenuMessage="ファイルの読み込みはWeb版で利用できます。";
#endif
        }
        public void ReceiveSaveError(string error){SaveMenuMessage=error;}
        public void ReceiveSaveFile(string json)
        {
            try{ImportCandidate=CitySaveStore.Decode(json);SavePrompt=true;SaveMenuMessage="内容を確認し、復元を押してください。現在の都市は置き換わります。";}
            catch(Exception ex){ImportCandidate=null;SaveMenuMessage="読み込めませんでした："+ex.Message;}
        }
        public void ConfirmSaveImport()
        {
            if(ImportCandidate==null)return;string error;
            if(!saveStore.TrySave(ImportCandidate,out error)){SaveMenuMessage=error;return;}
            if(!LoadSavedCity()){SaveMenuMessage=SaveError;return;}
            TitleScreen=false;CloseSaveMenu();Notify("セーブファイルから復元しました。一時停止中です。");
        }
    }
}

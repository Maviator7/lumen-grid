using UnityEngine;
namespace Michi.Runtime
{
    public sealed partial class NetworkHud
    {
        void DrawSaveMenu()
        {
            float w=Screen.width/scale,h=Screen.height/scale,x=w/2-310,y=h/2-250;
            Fill(new Rect(0,0,w,h),new Color(0,.012f,.025f,.9f));Frame(new Rect(x,y,620,500),Cyan,true);
            Text(x+26,y+20,568,42,"SAVE / 保存とバックアップ",headline,Cyan);
            string state=string.IsNullOrEmpty(game.SaveError)?string.IsNullOrEmpty(game.LastSavedAt)?"このブラウザに保存された都市はありません。":"保存済み / "+game.LastSavedAt:game.SaveError;
            Text(x+26,y+78,568,64,state+(string.IsNullOrEmpty(game.PCBackupStatus)?"":"\n"+game.PCBackupStatus),wrap,string.IsNullOrEmpty(game.SaveError)?White:Amber);
            if(game.ImportCandidate!=null){var n=game.ImportCandidate.Network;
                Text(x+26,y+150,568,88,"読み込み候補：SEED "+n.Seed+" / 拡張 "+n.ExpansionCount+" 回\n稼働 "+n.Time.ToString("0")+"秒 / 保存 "+game.ImportCandidate.SavedAt,wrap,White);
                if(Button(new Rect(x+26,y+251,568,44),"このセーブを復元（現在の都市を置き換える）",true,Amber))game.ConfirmSaveImport();
                if(Button(new Rect(x+26,y+307,568,44),"読み込みをキャンセル"))game.ImportCandidate=null;
            }else{
                GUI.enabled=game.HasSession&&game.Simulation.Continuous;
                if(Button(new Rect(x+26,y+150,568,44),"今すぐ都市を保存",true))game.SaveCity();
                if(Button(new Rect(x+26,y+208,568,44),"セーブをファイルに書き出す"))game.ExportCity();GUI.enabled=true;
                if(Button(new Rect(x+26,y+266,568,44),"セーブファイルを読み込む"))game.ChooseSaveFile();
                Text(x+26,y+326,568,48,"配線変更後と10秒ごとに自動保存します。\nファイルを書き出すと、ブラウザを変えても復元できます。",wrap,Muted);
            }
            Text(x+26,y+383,568,56,game.SaveMenuMessage,wrap,Amber);
            if(Button(new Rect(x+26,y+446,568,34),"閉じる / ESC"))game.CloseSaveMenu();
        }
    }
}

using System;
using System.Text;
using System.Security.Cryptography;
using UnityEngine;
using Michi.Core;
namespace Michi.Runtime
{
    [Serializable] public sealed class SavedCity
    {
        public int Version;public string SavedAt;public NetworkSave Network;
        public float Angle,Zoom,PanX,PanZ;public bool TopDown,Paused;public int Speed,Tool;
    }
    [Serializable] sealed class SaveEnvelope {public int Version;public string Data,Checksum;}
    [Serializable] sealed class SavePair {public string Primary,Backup;}
    public sealed class CitySaveStore
    {
        readonly string primary,backup;
        public CitySaveStore(string prefix="lumen-grid.city.v1"){primary=prefix;backup=prefix+".backup";}
        public bool Exists {get{var pair=ReadWeb();return !string.IsNullOrEmpty(pair.Primary)||!string.IsNullOrEmpty(pair.Backup)||PlayerPrefs.HasKey(primary)||PlayerPrefs.HasKey(backup);}}
        SavePair ReadWeb(){
#if UNITY_WEBGL && !UNITY_EDITOR
            string raw=BrowserSaveBridge.LumenSave_Read(primary);if(string.IsNullOrEmpty(raw))return new SavePair();
            try{return JsonUtility.FromJson<SavePair>(raw)??new SavePair{Primary=raw};}catch{return new SavePair{Primary=raw};}
#else
            return new SavePair();
#endif
        }
        string Current(){var pair=ReadWeb();return string.IsNullOrEmpty(pair.Primary)?PlayerPrefs.GetString(primary,""):pair.Primary;}
        string Backup(){var pair=ReadWeb();return string.IsNullOrEmpty(pair.Backup)?PlayerPrefs.GetString(backup,""):pair.Backup;}
        public static string Encode(SavedCity city){string data=JsonUtility.ToJson(city);string json=JsonUtility.ToJson(new SaveEnvelope{Version=1,Data=data,Checksum=Hash(data)});Decode(json);return json;}
        static string Hash(string text){using(var sha=SHA256.Create())return Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(text)));}
        static bool Finite(float n){return !float.IsNaN(n)&&!float.IsInfinity(n);}
        public static SavedCity Decode(string json)
        {
            if(string.IsNullOrEmpty(json)||Encoding.UTF8.GetByteCount(json)>900000)throw new ArgumentException("保存データが空か大きすぎます。");
            var envelope=JsonUtility.FromJson<SaveEnvelope>(json);if(envelope==null||envelope.Version!=1||string.IsNullOrEmpty(envelope.Data)||envelope.Checksum!=Hash(envelope.Data))throw new ArgumentException("保存データの検証に失敗しました。");
            var city=JsonUtility.FromJson<SavedCity>(envelope.Data);DateTime stamp;
            if(city==null||city.Version!=1||!DateTime.TryParse(city.SavedAt,System.Globalization.CultureInfo.InvariantCulture,System.Globalization.DateTimeStyles.RoundtripKind,out stamp)||!Finite(city.Angle)||!Finite(city.Zoom)||city.Zoom<=0||city.Zoom>10||!Finite(city.PanX)||!Finite(city.PanZ)||Math.Abs(city.PanX)>100000||Math.Abs(city.PanZ)>100000||(city.Speed!=1&&city.Speed!=2&&city.Speed!=4)||city.Tool<0||city.Tool>4)throw new ArgumentException("保存データの設定が不正です。");
            NetworkSimulation.RestoreSave(city.Network);return city;
        }
        public bool TryLoad(out SavedCity city,out string error)
        {
            city=null;error="";if(!Exists)return false;
            var pair=ReadWeb();
            var candidates=new[]{pair.Primary,pair.Backup,PlayerPrefs.GetString(primary,""),PlayerPrefs.GetString(backup,"")};
            for(int i=0;i<candidates.Length;i++)try{city=Decode(candidates[i]);if(i==1||i==3)error="前回の保存が読めなかったため、バックアップから復元しました。";return true;}catch(Exception){}
            error="セーブを読み込めませんでした。元データは残しています。";return false;
        }
        public bool TrySave(SavedCity city,out string error)
        {
            error="";try{
                string json=Encode(city);
                string previous=Current();bool valid=false;try{Decode(previous);valid=true;}catch(Exception){}
                string kept=valid?previous:Backup();
                // Web PlayerPrefs has a total 1 MB limit. Keep space for keys and settings too.
                if(Encoding.UTF8.GetByteCount(json)+Encoding.UTF8.GetByteCount(kept)>900000)throw new InvalidOperationException("都市の保存容量が上限に近づいています。");
#if UNITY_WEBGL && !UNITY_EDITOR
                if(BrowserSaveBridge.LumenSave_Write(primary,json,kept)!=1)throw new InvalidOperationException("ブラウザへの書き込み確認に失敗しました。セーブを書き出してください。");
                return true;
#else
                if(valid){PlayerPrefs.SetString(backup,previous);PlayerPrefs.Save();}
                PlayerPrefs.SetString(primary,json);PlayerPrefs.Save();return true;
#endif
            }catch(Exception ex){error="保存できませんでした："+ex.Message;return false;}
        }
    }
}

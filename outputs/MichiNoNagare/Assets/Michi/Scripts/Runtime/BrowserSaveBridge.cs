using System.Runtime.InteropServices;
namespace Michi.Runtime
{
    static class BrowserSaveBridge
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]internal static extern string LumenSave_Read(string key);
        [DllImport("__Internal")]internal static extern int LumenSave_Write(string key,string primary,string backup);
        [DllImport("__Internal")]internal static extern void LumenSave_Download(string json,string name);
        [DllImport("__Internal")]internal static extern void LumenSave_Upload(string target);
#endif
    }
}

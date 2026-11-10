using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
public static class WebBuild
{
    [MenuItem("Michi/Validate and build WebGL")]
    public static void Build()
    {
        if(!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.WebGL,BuildTarget.WebGL))throw new Exception("Web Build Support module is required.");
        ProjectTools.Validate();ProjectTools.CreateScene();
        var target=NamedBuildTarget.WebGL;PlayerSettings.SetScriptingBackend(target,ScriptingImplementation.IL2CPP);PlayerSettings.SetIl2CppCodeGeneration(target,Il2CppCodeGeneration.OptimizeSize);
        PlayerSettings.WebGL.compressionFormat=WebGLCompressionFormat.Gzip;PlayerSettings.WebGL.decompressionFallback=false;PlayerSettings.WebGL.template="PROJECT:LumenGrid";
        PlayerSettings.WebGL.exceptionSupport=WebGLExceptionSupport.FullWithoutStacktrace;PlayerSettings.WebGL.threadsSupport=false;PlayerSettings.WebGL.dataCaching=true;PlayerSettings.WebGL.nameFilesAsHashes=false;
        PlayerSettings.WebGL.initialMemorySize=256;PlayerSettings.WebGL.maximumMemorySize=1024;PlayerSettings.WebGL.memoryGrowthMode=WebGLMemoryGrowthMode.Geometric;
        AssetDatabase.SaveAssets();
        var requested=Environment.GetEnvironmentVariable("MICHI_WEB_BUILD_PATH");string path=Path.GetFullPath(string.IsNullOrEmpty(requested)?"../LumenGrid-Web":requested);Directory.CreateDirectory(path);
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/Michi/Scenes/Nocturne.unity"},locationPathName=path,target=BuildTarget.WebGL,options=BuildOptions.None});
        if(report.summary.result!=BuildResult.Succeeded)throw new Exception("WebGL build failed: "+report.summary.result);
        Debug.Log("MICHI_WEB_BUILD_SUCCEEDED "+path+" / "+report.summary.totalSize+" bytes");
    }
}

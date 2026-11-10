using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using Michi.Runtime;

public static class ProjectTools
{
    [MenuItem("Michi/Validate core")]
    public static void Validate(){string result=CoreChecks.Run()+"\n"+NetworkChecks.Run()+"\n"+PointerChecks.Run()+"\n"+TutorialChecks.Run()+"\n"+SeededCityChecks.Run()+"\n"+ResetChecks.Run()+"\n"+Michi.Checks.ContinuousCityChecks.Run()+"\n"+Michi.Checks.DataBalanceChecks.Run()+"\n"+Michi.Checks.SaveChecks.Run()+"\n"+Michi.Checks.SaveStoreChecks.Run()+"\n"+Michi.Checks.OrbitChecks.Run()+"\n"+Michi.Checks.StrategyChecks.Run()+"\n"+Michi.Checks.PowerInspectionChecks.Run()+"\n"+Michi.Checks.ExpansionDialogChecks.Run()+"\n"+Michi.Checks.SaveDurabilityChecks.Run();Debug.Log(result);Directory.CreateDirectory("Documentation");File.WriteAllText("Documentation/Verification.txt",DateTime.UtcNow.ToString("u")+"\n"+result+"\n");}
    [MenuItem("Michi/Create playable scene")]
    public static void CreateScene()
    {
        Directory.CreateDirectory("Assets/Michi/Resources");
        AssetDatabase.Refresh();
        var font=Resources.Load<Font>("UI/NotoSansCJKjp-Regular");if(font==null)throw new Exception("Bundled Japanese font missing");
        var fontImporter=AssetImporter.GetAtPath("Assets/Michi/Resources/UI/NotoSansCJKjp-Regular.otf") as TrueTypeFontImporter;
        if(fontImporter!=null&&(!fontImporter.includeFontData||fontImporter.fontTextureCase!=FontTextureCase.Dynamic)){fontImporter.includeFontData=true;fontImporter.fontTextureCase=FontTextureCase.Dynamic;fontImporter.SaveAndReimport();}
        var template=AssetDatabase.LoadAssetAtPath<Material>("Assets/Michi/Resources/DioramaMaterial.mat");
        if(template==null){var shader=Shader.Find("Standard");if(shader==null)throw new Exception("Standard shader missing in Editor");template=new Material(shader);AssetDatabase.CreateAsset(template,"Assets/Michi/Resources/DioramaMaterial.mat");}
        if(template.shader.name!="Standard")throw new Exception("Unexpected diorama shader");
        var neonShader=Shader.Find("Michi/Neon");if(neonShader==null)throw new Exception("Neon shader missing");
        if(AssetDatabase.LoadAssetAtPath<Material>("Assets/Michi/Resources/NeonMaterial.mat")==null)AssetDatabase.CreateAsset(new Material(neonShader),"Assets/Michi/Resources/NeonMaterial.mat");
        var bloomShader=Shader.Find("Michi/CityBloom");if(bloomShader==null)throw new Exception("Bloom shader missing");
        if(AssetDatabase.LoadAssetAtPath<Material>("Assets/Michi/Resources/CityBloomMaterial.mat")==null)AssetDatabase.CreateAsset(new Material(bloomShader),"Assets/Michi/Resources/CityBloomMaterial.mat");
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        new GameObject("Lumen Network").AddComponent<NetworkGame>();Directory.CreateDirectory("Assets/Michi/Scenes");EditorSceneManager.SaveScene(scene,"Assets/Michi/Scenes/Nocturne.unity");
        EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/Michi/Scenes/Nocturne.unity",true)};
        PlayerSettings.companyName="Michi Lab";PlayerSettings.productName="LumenGrid";PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Standalone,"com.michilab.lumengrid");
        ConfigureBranding();
        PlayerSettings.defaultScreenWidth=1280;PlayerSettings.defaultScreenHeight=820;PlayerSettings.fullScreenMode=FullScreenMode.Windowed;PlayerSettings.resizableWindow=true;PlayerSettings.runInBackground=true;PlayerSettings.colorSpace=ColorSpace.Linear;
        PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone,ScriptingImplementation.Mono2x);PlayerSettings.macOS.buildNumber="1";
        QualitySettings.SetQualityLevel(Math.Min(2,QualitySettings.names.Length-1));AssetDatabase.SaveAssets();Debug.Log("Created Nocturne scene.");
    }
    static void ConfigureBranding()
    {
        foreach(string file in new[]{"LumenGridLogo","LumenGridIcon"}){
            string path="Assets/Michi/Resources/Branding/"+file+".png";var importer=AssetImporter.GetAtPath(path) as TextureImporter;
            if(importer==null)throw new Exception("Brand texture missing: "+path);
            importer.textureType=TextureImporterType.Default;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.sRGBTexture=true;importer.npotScale=TextureImporterNPOTScale.None;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=2048;importer.SaveAndReimport();
        }
        var icon=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Michi/Resources/Branding/LumenGridIcon.png");
        foreach(var target in new[]{UnityEditor.Build.NamedBuildTarget.Unknown,UnityEditor.Build.NamedBuildTarget.Standalone}){
            var sizes=PlayerSettings.GetIconSizes(target,IconKind.Any);if(sizes.Length==0)continue;
            var icons=new Texture2D[sizes.Length];for(int i=0;i<icons.Length;i++)icons[i]=icon;PlayerSettings.SetIcons(target,icons,IconKind.Any);
        }
    }
    [MenuItem("Michi/Validate and build Mac")]
    public static void BuildMac()
    {
        Validate();CreateScene();var requested=Environment.GetEnvironmentVariable("MICHI_MAC_BUILD_PATH");var path=Path.GetFullPath(string.IsNullOrEmpty(requested)?"../LumenGrid-Mac/LumenGrid.app":requested);Directory.CreateDirectory(Path.GetDirectoryName(path));
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/Michi/Scenes/Nocturne.unity"},locationPathName=path,target=BuildTarget.StandaloneOSX,options=BuildOptions.None});
        if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Mac build failed: "+report.summary.result);Debug.Log("MICHI_BUILD_SUCCEEDED "+path);
    }
}

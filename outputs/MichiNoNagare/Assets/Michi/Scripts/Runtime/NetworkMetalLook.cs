using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Michi.Core;
namespace Michi.Runtime
{
    public sealed partial class NetworkCityView
    {
        const int Gunmetal=0x405368,Silver=0xa2b1bf,Copper=0xa96c48,DarkMetal=0x192838;
        readonly Dictionary<int,Material> metalMaterials=new Dictionary<int,Material>();
        Texture2D brushedMetal;Cubemap metalEnvironment;Texture previousReflection;Mesh windingMesh;
        DefaultReflectionMode previousReflectionMode;float previousReflectionIntensity;
        Material Metal(int color)
        {
            Material material;if(metalMaterials.TryGetValue(color,out material))return material;
            material=new Material(standard);material.name="Brushed metal "+color.ToString("X6");material.color=RGB(color);
            material.SetFloat("_Metallic",color==DarkMetal?.62f:.86f);material.SetFloat("_Glossiness",color==Copper?.72f:color==DarkMetal?.43f:.66f);
            material.mainTexture=brushedMetal;material.mainTextureScale=new Vector2(1,2);
            owned.Add(material);metalMaterials.Add(color,material);return material;
        }
        static Vector3 ReflectionDirection(int face,float u,float v)
        {
            switch(face){case 0:return new Vector3(1,-v,-u).normalized;case 1:return new Vector3(-1,-v,u).normalized;case 2:return new Vector3(u,1,v).normalized;case 3:return new Vector3(u,-1,-v).normalized;case 4:return new Vector3(u,-v,1).normalized;default:return new Vector3(-u,-v,-1).normalized;}
        }
        void SetupMetalLook()
        {
            previousReflection=RenderSettings.customReflectionTexture;previousReflectionMode=RenderSettings.defaultReflectionMode;previousReflectionIntensity=RenderSettings.reflectionIntensity;
            brushedMetal=new Texture2D(128,128,TextureFormat.RGBA32,true,true){name="Fine brushed metal",wrapMode=TextureWrapMode.Repeat,filterMode=FilterMode.Trilinear,anisoLevel=2};
            var grain=new Color[128*128];
            for(int y=0;y<128;y++){uint row=unchecked((uint)(y*1664525+1013904223));float stripe=.94f+(row%97)/1600f;for(int x=0;x<128;x++){float variation=((x*17+y*31)%13)/2600f;grain[y*128+x]=new Color(stripe+variation,stripe+variation,stripe+variation,1);}}
            brushedMetal.SetPixels(grain);brushedMetal.Apply(true,true);
            // One small static environment map supplies stable, view-dependent highlights.
            // Real point lights supply local amber/cyan illumination; no realtime probe capture.
            const int size=64;metalEnvironment=new Cubemap(size,TextureFormat.RGBA32,true){name="Night city metal reflections",filterMode=FilterMode.Trilinear};
            var pixels=new Color[size*size];
            for(int face=0;face<6;face++){
                for(int y=0;y<size;y++)for(int x=0;x<size;x++){
                    var d=ReflectionDirection(face,(x+.5f)/size*2-1,(y+.5f)/size*2-1);
                    float sky=Mathf.Clamp01(d.y*.6f+.4f),cool=Mathf.Exp(-Mathf.Pow((d.x+d.z*.35f-.38f)/.12f,2)),warm=Mathf.Exp(-Mathf.Pow((d.z-d.x*.25f+.58f)/.16f,2));
                    float horizon=Mathf.Clamp01(1-Mathf.Abs(d.y)*.75f);
                    pixels[y*size+x]=Color.Lerp(RGB(0x0b1421),RGB(0x50677d),sky)+new Color(.52f,.67f,.76f)*cool*horizon+new Color(.42f,.22f,.09f)*warm*horizon;
                }
                metalEnvironment.SetPixels(pixels,(CubemapFace)face);
            }
            metalEnvironment.Apply(true,true);RenderSettings.defaultReflectionMode=DefaultReflectionMode.Custom;RenderSettings.customReflectionTexture=metalEnvironment;RenderSettings.reflectionIntensity=.95f;
            var rim=new GameObject("Soft architectural rim").AddComponent<Light>();rim.transform.SetParent(sceneRoot,false);rim.type=LightType.Directional;rim.color=RGB(0xb4d4e5);rim.intensity=.28f;rim.shadows=LightShadows.None;rim.transform.rotation=Quaternion.Euler(22,135,0);
        }
        Mesh WindingMesh()
        {
            if(windingMesh!=null)return windingMesh;
            const int around=40,tube=8;var vertices=new Vector3[(around+1)*(tube+1)];var normals=new Vector3[vertices.Length];var uv=new Vector2[vertices.Length];var triangles=new int[around*tube*6];int index=0;
            for(int i=0;i<=around;i++)for(int j=0;j<=tube;j++){
                float a=i*Mathf.PI*2/around,b=j*Mathf.PI*2/tube;int v=i*(tube+1)+j;
                var radial=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));normals[v]=radial*Mathf.Cos(b)+Vector3.up*Mathf.Sin(b);vertices[v]=radial+normals[v]*.095f;uv[v]=new Vector2(i/(float)around,j/(float)tube);
                if(i<around&&j<tube){int next=v+tube+1;triangles[index++]=v;triangles[index++]=v+1;triangles[index++]=next;triangles[index++]=v+1;triangles[index++]=next+1;triangles[index++]=next;}
            }
            windingMesh=new Mesh{name="Shared copper winding mesh"};windingMesh.vertices=vertices;windingMesh.normals=normals;windingMesh.uv=uv;windingMesh.triangles=triangles;windingMesh.RecalculateBounds();return windingMesh;
        }
        void CopperWinding(Vector3 center,float radius)
        {
            var part=Group("Copper induction winding",city);part.localPosition=center;part.localScale=Vector3.one*radius;part.gameObject.AddComponent<MeshFilter>().sharedMesh=WindingMesh();part.gameObject.AddComponent<MeshRenderer>().sharedMaterial=Metal(Copper);
        }
        void InfrastructureTrim(GridNode node,Vector3 p)
        {
            if(node.Kind==NetworkNodeKind.District)return;
            foreach(float x in new[]{-.53f,.53f})foreach(float z in new[]{-.53f,.53f})Shape(city,"Steel anchor bolt",new Vector3(p.x+x,.27f,p.z+z),new Vector3(.07f,.025f,.07f),Metal(Silver),PrimitiveType.Cylinder);
            if(node.Kind==NetworkNodeKind.Reactor){
                Shape(city,"Reactor steel crown",new Vector3(p.x,1.98f,p.z),new Vector3(.48f,.035f,.48f),Metal(Silver),PrimitiveType.Cylinder);
                Shape(city,"Reactor copper base",new Vector3(p.x,.4f,p.z),new Vector3(.62f,.045f,.62f),Metal(Copper),PrimitiveType.Cylinder);
            }else if(node.Kind==NetworkNodeKind.DataCore){
                for(int j=0;j<3;j++){
                    float x=p.x+(j-1)*.4f;
                    foreach(float side in new[]{-.16f,.16f})Box(city,"Server steel rail",x+side,1.1f,p.z-.33f,.032f,1.72f,.04f,Metal(Silver));
                    Box(city,"Server steel header",x,1.94f,p.z,.35f,.055f,.68f,Metal(Silver));
                }
            }else{
                Shape(city,"Machined hub lid",new Vector3(p.x,.82f,p.z),new Vector3(.66f,.035f,.66f),Metal(Silver),PrimitiveType.Cylinder);
                Shape(city,"Dark hub gasket",new Vector3(p.x,.75f,p.z),new Vector3(.64f,.022f,.64f),Metal(DarkMetal),PrimitiveType.Cylinder);
            }
        }
        void ReleaseMetalLook()
        {
            if(metalEnvironment!=null&&RenderSettings.customReflectionTexture==metalEnvironment){RenderSettings.customReflectionTexture=previousReflection;RenderSettings.defaultReflectionMode=previousReflectionMode;RenderSettings.reflectionIntensity=previousReflectionIntensity;}
            if(metalEnvironment!=null)Destroy(metalEnvironment);if(brushedMetal!=null)Destroy(brushedMetal);if(windingMesh!=null)Destroy(windingMesh);
            metalEnvironment=null;brushedMetal=null;windingMesh=null;metalMaterials.Clear();
        }
    }
}

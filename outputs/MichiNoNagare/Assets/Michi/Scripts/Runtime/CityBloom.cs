using UnityEngine;
namespace Michi.Runtime
{
    [RequireComponent(typeof(Camera))]
    public sealed class CityBloom : MonoBehaviour
    {
        Material material;
        void OnRenderImage(RenderTexture source,RenderTexture destination)
        {
            if(material==null){var template=Resources.Load<Material>("CityBloomMaterial");if(template!=null)material=new Material(template);}
            if(material==null||!material.shader.isSupported){Graphics.Blit(source,destination);return;}
            int w=Mathf.Max(1,source.width/2),h=Mathf.Max(1,source.height/2);var a=RenderTexture.GetTemporary(w,h,0,RenderTextureFormat.DefaultHDR);var b=RenderTexture.GetTemporary(w,h,0,RenderTextureFormat.DefaultHDR);a.filterMode=b.filterMode=FilterMode.Bilinear;
            try{Graphics.Blit(source,a,material,0);for(int i=0;i<2;i++){material.SetVector("_Direction",new Vector4(1+i,0,0,0));Graphics.Blit(a,b,material,1);material.SetVector("_Direction",new Vector4(0,1+i,0,0));Graphics.Blit(b,a,material,1);}material.SetTexture("_BloomTex",a);Graphics.Blit(source,destination,material,2);}finally{RenderTexture.ReleaseTemporary(a);RenderTexture.ReleaseTemporary(b);}
        }
        void OnDisable(){if(material!=null){Destroy(material);material=null;}}
    }
}

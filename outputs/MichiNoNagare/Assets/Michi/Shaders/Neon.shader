Shader "Michi/Neon" {
 Properties { _Color ("Signal", Color) = (0.1,0.8,1,1) _Pulse ("Pulse", Float) = 0 }
 SubShader { Tags { "Queue"="Transparent" "RenderType"="Transparent" } Blend One One ZWrite Off Cull Off
 Pass { CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
 struct v2f { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
 fixed4 _Color; float _Pulse;
 v2f vert(appdata v){v2f o;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color;return o;}
 fixed4 frag(v2f i):SV_Target { float edge=pow(saturate(1-abs(i.uv.y-.5)*2),1.8);float pulse=1-_Pulse*.18+_Pulse*.18*sin(i.uv.x*18-_Time.y*4);return _Color*edge*pulse; }
 ENDCG }
 }
}

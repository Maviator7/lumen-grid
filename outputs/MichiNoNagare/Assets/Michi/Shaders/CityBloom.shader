Shader "Michi/CityBloom" {
 Properties { _MainTex ("Scene", 2D) = "black" {} _BloomTex ("Bloom", 2D) = "black" {} }
 SubShader { Cull Off ZWrite Off ZTest Always
 CGINCLUDE
 #include "UnityCG.cginc"
 sampler2D _MainTex,_BloomTex;float4 _MainTex_TexelSize;float2 _Direction;
 half4 extract(v2f_img i):SV_Target{half3 c=tex2D(_MainTex,i.uv).rgb;half peak=max(c.r,max(c.g,c.b));return half4(c*saturate((peak-.38)/max(peak,.001)),1);}
 half4 blur(v2f_img i):SV_Target{float2 d=_Direction*_MainTex_TexelSize.xy;half3 c=tex2D(_MainTex,i.uv).rgb*.227027;c+=tex2D(_MainTex,i.uv+d*1.384615).rgb*.316216;c+=tex2D(_MainTex,i.uv-d*1.384615).rgb*.316216;c+=tex2D(_MainTex,i.uv+d*3.230769).rgb*.070270;c+=tex2D(_MainTex,i.uv-d*3.230769).rgb*.070270;return half4(c,1);}
 half4 combine(v2f_img i):SV_Target{return half4(tex2D(_MainTex,i.uv).rgb+tex2D(_BloomTex,i.uv).rgb*.85,1);}
 ENDCG
 Pass { CGPROGRAM
 #pragma vertex vert_img
 #pragma fragment extract
 ENDCG }
 Pass { CGPROGRAM
 #pragma vertex vert_img
 #pragma fragment blur
 ENDCG }
 Pass { CGPROGRAM
 #pragma vertex vert_img
 #pragma fragment combine
 ENDCG }
 }
}

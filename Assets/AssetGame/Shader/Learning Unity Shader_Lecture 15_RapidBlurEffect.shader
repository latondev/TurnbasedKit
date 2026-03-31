Shader "Learning Unity Shader/Lecture 15/RapidBlurEffect" {
	Properties {
		_MainTex ("Base (RGB)", 2D) = "white" {}
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		Cull Off
		ZWrite Off
		ZTest Always

		Pass {
			CGPROGRAM
			#pragma target 2.0
			#pragma vertex vert_img
			#pragma fragment frag_h
			#include "UnityCG.cginc"

			sampler2D _MainTex;
			float4 _MainTex_TexelSize;

			fixed4 frag_h(v2f_img i) : SV_Target
			{
				float2 stepUV = float2(_MainTex_TexelSize.x, 0);
				fixed4 col = 0;
				col += tex2D(_MainTex, i.uv - stepUV * 2) * 0.12;
				col += tex2D(_MainTex, i.uv - stepUV) * 0.24;
				col += tex2D(_MainTex, i.uv) * 0.28;
				col += tex2D(_MainTex, i.uv + stepUV) * 0.24;
				col += tex2D(_MainTex, i.uv + stepUV * 2) * 0.12;
				return col;
			}
			ENDCG
		}
		Pass {
			CGPROGRAM
			#pragma target 2.0
			#pragma vertex vert_img
			#pragma fragment frag_v
			#include "UnityCG.cginc"

			sampler2D _MainTex;
			float4 _MainTex_TexelSize;

			fixed4 frag_v(v2f_img i) : SV_Target
			{
				float2 stepUV = float2(0, _MainTex_TexelSize.y);
				fixed4 col = 0;
				col += tex2D(_MainTex, i.uv - stepUV * 2) * 0.12;
				col += tex2D(_MainTex, i.uv - stepUV) * 0.24;
				col += tex2D(_MainTex, i.uv) * 0.28;
				col += tex2D(_MainTex, i.uv + stepUV) * 0.24;
				col += tex2D(_MainTex, i.uv + stepUV * 2) * 0.12;
				return col;
			}
			ENDCG
		}
		Pass {
			CGPROGRAM
			#pragma target 2.0
			#pragma vertex vert_img
			#pragma fragment frag_copy
			#include "UnityCG.cginc"

			sampler2D _MainTex;

			fixed4 frag_copy(v2f_img i) : SV_Target
			{
				return tex2D(_MainTex, i.uv);
			}
			ENDCG
		}
	}
}

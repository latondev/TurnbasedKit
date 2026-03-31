Shader "ASE/Alph_Blend_UV" {
	Properties {
		_TextureSample0 ("Texture Sample 0", 2D) = "white" {}
		_Color ("Color", Color) = (0,0,0,0)
		_Opacity ("Opacity", Range(0, 1)) = 1
		_U_Speed ("U_Speed", Float) = 0
		_V_Speed ("V_Speed", Float) = 0
		_Mask_Tex ("Mask_Tex", 2D) = "white" {}
		_power ("power", Float) = 1
		[HideInInspector] _texcoord ("", 2D) = "white" {}
		[HideInInspector] __dirty ("", Float) = 1
	}
	SubShader {
		Tags { "IsEmissive" = "true" "QUEUE" = "Transparent+0" "RenderType" = "Custom" }
		Pass {
			Name "FORWARD"
			Tags { "IsEmissive" = "true" "LIGHTMODE" = "FORWARDBASE" "QUEUE" = "Transparent+0" "RenderType" = "Custom" }
			Blend SrcAlpha OneMinusSrcAlpha, One One
			ZWrite Off
			Cull Off
			CGPROGRAM
			#pragma target 2.0
			#pragma vertex ASEFallbackVert
			#pragma fragment frag
			#include "ShaderFallbackCommon.cginc"

			sampler2D _TextureSample0;
			sampler2D _Mask_Tex;
			fixed4 _Color;
			float _Opacity;
			float _U_Speed;
			float _V_Speed;
			float _power;

			fixed4 frag(ASEFallbackV2F i) : SV_Target
			{
				float2 uv = ASEScrollUV(i.uv, float2(_U_Speed, _V_Speed));
				fixed4 col = tex2D(_TextureSample0, uv) * _Color * i.color;
				col.rgb *= _power;
				col.a *= _Opacity * tex2D(_Mask_Tex, i.uv).a;
				return col;
			}
			ENDCG
		}
		Pass {
			Name "FORWARD"
			Tags { "IsEmissive" = "true" "LIGHTMODE" = "FORWARDADD" "QUEUE" = "Transparent+0" "RenderType" = "Custom" }
			Blend One One, One One
			ZWrite Off
			Cull Off
			CGPROGRAM
			#pragma target 2.0
			#pragma vertex ASEFallbackVert
			#pragma fragment frag
			#include "ShaderFallbackCommon.cginc"

			sampler2D _TextureSample0;
			sampler2D _Mask_Tex;
			fixed4 _Color;
			float _Opacity;
			float _U_Speed;
			float _V_Speed;
			float _power;

			fixed4 frag(ASEFallbackV2F i) : SV_Target
			{
				float2 uv = ASEScrollUV(i.uv, float2(_U_Speed, _V_Speed));
				fixed4 col = tex2D(_TextureSample0, uv) * _Color * i.color;
				col.rgb *= _power;
				col.a *= _Opacity * tex2D(_Mask_Tex, i.uv).a;
				return col;
			}
			ENDCG
		}
	}
	CustomEditor "ASEMaterialInspector"
}

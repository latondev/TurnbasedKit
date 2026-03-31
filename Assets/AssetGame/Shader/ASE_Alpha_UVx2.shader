Shader "ASE/Alpha_UVx2" {
	Properties {
		_TextureSample0 ("Texture Sample 0", 2D) = "white" {}
		_Color ("Color", Color) = (0,0,0,0)
		_Opacity ("Opacity", Range(0, 1)) = 0
		_U_Speed ("U_Speed", Float) = 0
		_V_Speed ("V_Speed", Float) = 0
		_Tex_2 ("Tex_2", 2D) = "white" {}
		_U2_Speed ("U2_Speed", Float) = 0
		_V2_Speed ("V2_Speed", Float) = 0
		_Mask ("Mask", 2D) = "white" {}
		[HideInInspector] _texcoord ("", 2D) = "white" {}
		[HideInInspector] __dirty ("", Float) = 1
	}
	SubShader {
		Tags { "IsEmissive" = "true" "QUEUE" = "Transparent+0" "RenderType" = "Custom" }
		Pass {
			Name "FORWARD"
			Tags { "IsEmissive" = "true" "LIGHTMODE" = "FORWARDBASE" "QUEUE" = "Transparent+0" "RenderType" = "Custom" }
			Blend SrcAlpha OneMinusSrcAlpha, SrcAlpha OneMinusSrcAlpha
			ZWrite Off
			Cull Off
			CGPROGRAM
			#pragma target 2.0
			#pragma vertex ASEFallbackVert
			#pragma fragment frag
			#include "ShaderFallbackCommon.cginc"

			sampler2D _TextureSample0;
			sampler2D _Tex_2;
			sampler2D _Mask;
			fixed4 _Color;
			float _Opacity;
			float _U_Speed;
			float _V_Speed;
			float _U2_Speed;
			float _V2_Speed;

			fixed4 frag(ASEFallbackV2F i) : SV_Target
			{
				float2 uv1 = ASEScrollUV(i.uv, float2(_U_Speed, _V_Speed));
				float2 uv2 = ASEScrollUV(i.uv, float2(_U2_Speed, _V2_Speed));
				fixed4 baseCol = tex2D(_TextureSample0, uv1) * _Color * i.color;
				fixed4 layerCol = tex2D(_Tex_2, uv2) * _Color * i.color;
				baseCol.rgb += layerCol.rgb * layerCol.a;
				baseCol.a *= _Opacity * tex2D(_Mask, i.uv).a;
				return baseCol;
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
			sampler2D _Tex_2;
			sampler2D _Mask;
			fixed4 _Color;
			float _Opacity;
			float _U_Speed;
			float _V_Speed;
			float _U2_Speed;
			float _V2_Speed;

			fixed4 frag(ASEFallbackV2F i) : SV_Target
			{
				float2 uv1 = ASEScrollUV(i.uv, float2(_U_Speed, _V_Speed));
				float2 uv2 = ASEScrollUV(i.uv, float2(_U2_Speed, _V2_Speed));
				fixed4 baseCol = tex2D(_TextureSample0, uv1) * _Color * i.color;
				fixed4 layerCol = tex2D(_Tex_2, uv2) * _Color * i.color;
				baseCol.rgb += layerCol.rgb * layerCol.a;
				baseCol.a *= _Opacity * tex2D(_Mask, i.uv).a;
				return baseCol;
			}
			ENDCG
		}
	}
	CustomEditor "ASEMaterialInspector"
}

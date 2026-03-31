Shader "ASE/Add_TexUV" {
	Properties {
		_Main_Tex ("Main_Tex", 2D) = "white" {}
		_Main_TexUV_speed ("Main_TexUV_speed", Vector) = (0,0,0,0)
		_Color ("Color", Color) = (0,0,0,0)
		_Opacity ("Opacity", Float) = 1
		_Tex_UV ("Tex_UV", 2D) = "white" {}
		_Tex_UV_power ("Tex_UV_power", Range(0, 1)) = 0
		_Tex_UV_speed ("Tex_UV_speed", Vector) = (0,0,0,0)
		_Mask ("Mask", 2D) = "white" {}
		[HideInInspector] _texcoord ("", 2D) = "white" {}
		[HideInInspector] __dirty ("", Float) = 1
	}
	SubShader {
		Tags { "IsEmissive" = "true" "QUEUE" = "Transparent+0" "RenderType" = "Custom" }
		Pass {
			Name "FORWARD"
			Tags { "IsEmissive" = "true" "LIGHTMODE" = "FORWARDBASE" "QUEUE" = "Transparent+0" "RenderType" = "Custom" }
			Blend One One, One One
			ZWrite Off
			Cull Off
			CGPROGRAM
			#pragma target 2.0
			#pragma vertex ASEFallbackVert
			#pragma fragment frag
			#include "ShaderFallbackCommon.cginc"

			sampler2D _Main_Tex;
			sampler2D _Tex_UV;
			sampler2D _Mask;
			float4 _Main_TexUV_speed;
			float4 _Tex_UV_speed;
			float _Tex_UV_power;
			fixed4 _Color;
			float _Opacity;

			fixed4 frag(ASEFallbackV2F i) : SV_Target
			{
				float2 mainUV = ASEScrollUV(i.uv, _Main_TexUV_speed.xy);
				float2 uvUV = ASEScrollUV(i.uv, _Tex_UV_speed.xy);
				fixed4 col = tex2D(_Main_Tex, mainUV);
				col += tex2D(_Tex_UV, uvUV) * _Tex_UV_power;
				col *= _Color * i.color;
				col.a *= _Opacity * tex2D(_Mask, i.uv).a;
				return ASEFinalizeAdditive(col);
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

			sampler2D _Main_Tex;
			sampler2D _Tex_UV;
			sampler2D _Mask;
			float4 _Main_TexUV_speed;
			float4 _Tex_UV_speed;
			float _Tex_UV_power;
			fixed4 _Color;
			float _Opacity;

			fixed4 frag(ASEFallbackV2F i) : SV_Target
			{
				float2 mainUV = ASEScrollUV(i.uv, _Main_TexUV_speed.xy);
				float2 uvUV = ASEScrollUV(i.uv, _Tex_UV_speed.xy);
				fixed4 col = tex2D(_Main_Tex, mainUV);
				col += tex2D(_Tex_UV, uvUV) * _Tex_UV_power;
				col *= _Color * i.color;
				col.a *= _Opacity * tex2D(_Mask, i.uv).a;
				return ASEFinalizeAdditive(col);
			}
			ENDCG
		}
	}
	CustomEditor "ASEMaterialInspector"
}

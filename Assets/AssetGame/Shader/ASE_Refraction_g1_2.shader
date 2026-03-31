Shader "ASE/Refraction_g1" {
	Properties {
		_SpecColor ("Specular Color", Color) = (1,1,1,1)
		_Distortion_Tex ("Distortion_Tex", 2D) = "white" {}
		_Distortion ("Distortion", Range(-2, 2)) = 1
		_Mask ("Mask", 2D) = "white" {}
		_Speed ("Speed", Range(-1, 1)) = 0.1647059
		[HideInInspector] _texcoord ("", 2D) = "white" {}
		[HideInInspector] __dirty ("", Float) = 1
	}
	SubShader {
		Tags { "IGNOREPROJECTOR" = "true" "IsEmissive" = "true" "QUEUE" = "Transparent+0" "RenderType" = "Transparent" }
		GrabPass {}
		Blend SrcAlpha OneMinusSrcAlpha
		ZWrite Off
		Cull Off
		Pass {
			Name "FORWARD"
			Tags { "LIGHTMODE" = "FORWARDBASE" }
			CGPROGRAM
			#pragma target 2.0
			#pragma vertex ASEFallbackVert
			#pragma fragment frag
			#include "ShaderFallbackCommon.cginc"

			sampler2D _GrabTexture;
			sampler2D _Distortion_Tex;
			sampler2D _Mask;
			fixed4 _SpecColor;
			float _Distortion;
			float _Speed;

			fixed4 frag(ASEFallbackV2F i) : SV_Target
			{
				float2 noiseUV = ASEScrollUV(i.uv, float2(_Speed, _Speed));
				float2 distortion = (tex2D(_Distortion_Tex, noiseUV).rg * 2.0 - 1.0) * _Distortion * 0.08;
				fixed4 col = ASEGrabSampleOffset(_GrabTexture, i.screenPos, distortion);
				col *= _SpecColor;
				col.a *= tex2D(_Mask, i.uv).a;
				return col;
			}
			ENDCG
		}
		Pass {
			Name "FORWARDADD"
			Tags { "LIGHTMODE" = "FORWARDADD" }
			CGPROGRAM
			#pragma target 2.0
			#pragma vertex ASEFallbackVert
			#pragma fragment frag
			#include "ShaderFallbackCommon.cginc"

			sampler2D _GrabTexture;
			sampler2D _Distortion_Tex;
			sampler2D _Mask;
			fixed4 _SpecColor;
			float _Distortion;
			float _Speed;

			fixed4 frag(ASEFallbackV2F i) : SV_Target
			{
				float2 noiseUV = ASEScrollUV(i.uv, float2(_Speed, _Speed));
				float2 distortion = (tex2D(_Distortion_Tex, noiseUV).rg * 2.0 - 1.0) * _Distortion * 0.08;
				fixed4 col = ASEGrabSampleOffset(_GrabTexture, i.screenPos, distortion);
				col *= _SpecColor;
				col.a *= tex2D(_Mask, i.uv).a;
				return col;
			}
			ENDCG
		}
	}
	CustomEditor "ASEMaterialInspector"
}

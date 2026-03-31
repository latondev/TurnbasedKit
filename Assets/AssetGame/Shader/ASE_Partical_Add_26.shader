Shader "ASE/Partical_Add" {
	Properties {
		_TextureSample0 ("Texture Sample 0", 2D) = "white" {}
		_Color ("Color", Color) = (1,1,1,1)
		_Opacity ("Opacity", Range(0, 1)) = 1
		_InvFade ("Soft Particles Factor", Range(0.01, 3)) = 1
		[HideInInspector] _texcoord ("", 2D) = "white" {}
		[HideInInspector] __dirty ("", Float) = 1
	}
	SubShader {
		Tags { "IGNOREPROJECTOR" = "true" "IsEmissive" = "true" "PreviewType" = "Plane" "QUEUE" = "Transparent+0" "RenderType" = "Custom" }
		Blend One One
		ZWrite Off
		Cull Off
		Pass {
			Name "FORWARD"
			CGPROGRAM
			#pragma target 2.0
			#pragma multi_compile_particles
			#pragma vertex ASEParticleVert
			#pragma fragment frag
			#include "ShaderFallbackCommon.cginc"

			sampler2D _TextureSample0;
			fixed4 _Color;
			float _Opacity;
			float _InvFade;

			fixed4 frag(ASEParticleV2F i) : SV_Target
			{
				fixed4 col = tex2D(_TextureSample0, i.uv) * _Color * i.color;
				col.a *= _Opacity * ASEParticleSoftFactor(i, _InvFade);
				return ASEFinalizeAdditive(col);
			}
			ENDCG
		}
		Pass {
			Name "FORWARDADD"
			CGPROGRAM
			#pragma target 2.0
			#pragma multi_compile_particles
			#pragma vertex ASEParticleVert
			#pragma fragment frag
			#include "ShaderFallbackCommon.cginc"

			sampler2D _TextureSample0;
			fixed4 _Color;
			float _Opacity;
			float _InvFade;

			fixed4 frag(ASEParticleV2F i) : SV_Target
			{
				fixed4 col = tex2D(_TextureSample0, i.uv) * _Color * i.color;
				col.a *= _Opacity * ASEParticleSoftFactor(i, _InvFade);
				return ASEFinalizeAdditive(col);
			}
			ENDCG
		}
	}
	CustomEditor "ASEMaterialInspector"
}

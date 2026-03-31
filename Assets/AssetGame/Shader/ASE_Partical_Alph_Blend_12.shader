Shader "ASE/Partical_Alph_Blend" {
	Properties {
		_TextureSample0 ("Texture Sample 0", 2D) = "white" {}
		_Color ("Color", Color) = (0,0,0,0)
		_Opacity ("Opacity", Range(0, 1)) = 1
		_Power ("Power", Float) = 1
		_InvFade ("Soft Particles Factor", Range(0.01, 3)) = 1
		[HideInInspector] _texcoord ("", 2D) = "white" {}
		[HideInInspector] __dirty ("", Float) = 1
	}
	SubShader {
		Tags { "IsEmissive" = "true" "PreviewType" = "Plane" "QUEUE" = "Transparent+0" "RenderType" = "Custom" }
		Blend SrcAlpha OneMinusSrcAlpha
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
			float _Power;
			float _InvFade;

			fixed4 frag(ASEParticleV2F i) : SV_Target
			{
				fixed4 col = tex2D(_TextureSample0, i.uv) * _Color * i.color;
				col.rgb *= _Power;
				col.a *= _Opacity * ASEParticleSoftFactor(i, _InvFade);
				return col;
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
			float _Power;
			float _InvFade;

			fixed4 frag(ASEParticleV2F i) : SV_Target
			{
				fixed4 col = tex2D(_TextureSample0, i.uv) * _Color * i.color;
				col.rgb *= _Power;
				col.a *= _Opacity * ASEParticleSoftFactor(i, _InvFade);
				return col;
			}
			ENDCG
		}
	}
	CustomEditor "ASEMaterialInspector"
}

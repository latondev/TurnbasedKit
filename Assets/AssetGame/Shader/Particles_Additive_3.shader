Shader "Particles/Additive" {
	Properties {
		_TintColor ("Tint Color", Color) = (0.5,0.5,0.5,0.5)
		_MainTex ("Particle Texture", 2D) = "white" {}
		_InvFade ("Soft Particles Factor", Range(0.01, 3)) = 1
	}
	SubShader {
		Tags { "IGNOREPROJECTOR" = "true" "PreviewType" = "Plane" "QUEUE" = "Transparent" "RenderType" = "Transparent" }
		Blend SrcAlpha One
		ZWrite Off
		Cull Off
		Pass {
			CGPROGRAM
			#pragma target 2.0
			#pragma multi_compile_particles
			#pragma vertex ASEParticleVert
			#pragma fragment frag
			#include "ShaderFallbackCommon.cginc"

			sampler2D _MainTex;
			fixed4 _TintColor;
			float _InvFade;

			fixed4 frag(ASEParticleV2F i) : SV_Target
			{
				fixed4 col = tex2D(_MainTex, i.uv) * _TintColor * i.color;
				col.a *= ASEParticleSoftFactor(i, _InvFade);
				return ASEFinalizeAdditive(col);
			}
			ENDCG
		}
	}
}

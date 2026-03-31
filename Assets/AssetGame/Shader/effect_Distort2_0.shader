Shader "effect/Distort2" {
	Properties {
		_NoiseTex ("Noise Texture (RG)", 2D) = "white" {}
		_MainTex ("Alpha (A)", 2D) = "white" {}
		_HeatTime ("Heat Time", Range(0, 1.5)) = 1
		_HeatForce ("Heat Force", Range(0, 0.1)) = 0.1
	}
	SubShader {
		Tags { "QUEUE" = "Transparent+1" "RenderType" = "Transparent" }
		GrabPass {}
		Pass {
			Name "BASE"
			Tags { "LIGHTMODE" = "ALWAYS" }
			Blend SrcAlpha OneMinusSrcAlpha
			ZWrite Off
			Cull Off
			CGPROGRAM
			#pragma target 2.0
			#pragma vertex ASEFallbackVert
			#pragma fragment frag
			#include "ShaderFallbackCommon.cginc"

			sampler2D _GrabTexture;
			sampler2D _NoiseTex;
			sampler2D _MainTex;
			float _HeatTime;
			float _HeatForce;

			fixed4 frag(ASEFallbackV2F i) : SV_Target
			{
				float2 noiseUV = i.uv * 2.0 + _Time.y * _HeatTime;
				float2 distortion = (tex2D(_NoiseTex, noiseUV).rg * 2.0 - 1.0) * _HeatForce;
				fixed4 col = ASEGrabSampleOffset(_GrabTexture, i.screenPos, distortion);
				col.a *= tex2D(_MainTex, i.uv).a;
				return col;
			}
			ENDCG
		}
	}
	SubShader {
		Tags { "QUEUE" = "Transparent+1" "RenderType" = "Transparent" }
		GrabPass {}
		Pass {
			Name "BASE"
			Tags { "QUEUE" = "Transparent+1" "RenderType" = "Transparent" }
			Blend DstColor Zero
			ZWrite Off
			Cull Off
			Fog { Mode Off }
			CGPROGRAM
			#pragma target 2.0
			#pragma vertex ASEFallbackVert
			#pragma fragment frag
			#include "ShaderFallbackCommon.cginc"

			sampler2D _GrabTexture;
			sampler2D _NoiseTex;
			sampler2D _MainTex;
			float _HeatTime;
			float _HeatForce;

			fixed4 frag(ASEFallbackV2F i) : SV_Target
			{
				float2 noiseUV = i.uv * 2.0 + _Time.y * _HeatTime;
				float2 distortion = (tex2D(_NoiseTex, noiseUV).rg * 2.0 - 1.0) * _HeatForce;
				fixed4 col = ASEGrabSampleOffset(_GrabTexture, i.screenPos, distortion);
				col.a *= tex2D(_MainTex, i.uv).a;
				return col;
			}
			ENDCG
		}
	}
}

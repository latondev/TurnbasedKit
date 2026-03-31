Shader "ASE/rongjie_radial_alpha" {
	Properties {
		_MainTex ("MainTex", 2D) = "white" {}
		_NosieTex ("NosieTex", 2D) = "white" {}
		_NoisePowr ("NoisePowr", Float) = 1
		_RongJie ("RongJie", Range(0, 10)) = 1
		_Side ("Side", Float) = 0
		_Side_Color ("Side_Color", Color) = (1,1,1,1)
		_Step_Color ("Step_Color", Color) = (1,1,1,1)
		_Step ("Step", Range(0, 1)) = 0
		[HideInInspector] _texcoord ("", 2D) = "white" {}
		[HideInInspector] __dirty ("", Float) = 1
	}
	SubShader {
		Tags { "IsEmissive" = "true" "QUEUE" = "Transparent+0" "RenderType" = "Custom" }
		Pass {
			Name "FORWARD"
			Tags { "IsEmissive" = "true" "LIGHTMODE" = "FORWARDBASE" "QUEUE" = "Transparent+0" "RenderType" = "Custom" }
			Blend SrcAlpha OneMinusSrcAlpha
			ZWrite Off
			Cull Off
			CGPROGRAM
			#pragma target 2.0
			#pragma vertex ASEFallbackVert
			#pragma fragment frag
			#include "ShaderFallbackCommon.cginc"

			sampler2D _MainTex;
			sampler2D _NosieTex;
			fixed4 _Side_Color;
			fixed4 _Step_Color;
			float _NoisePowr;
			float _RongJie;
			float _Side;
			float _Step;

			fixed4 frag(ASEFallbackV2F i) : SV_Target
			{
				fixed4 col = tex2D(_MainTex, i.uv) * i.color;
				float2 centeredUV = i.uv - 0.5;
				float radial = saturate(1.0 - length(centeredUV) * _RongJie);
				float noise = tex2D(_NosieTex, i.uv).r * _NoisePowr;
				float dissolve = saturate(radial + noise - 0.5);
				float sideEdge = smoothstep(_Side - 0.08, _Side + 0.08, dissolve);
				float stepEdge = smoothstep(_Step - 0.02, _Step + 0.02, dissolve);
				col.rgb = lerp(_Step_Color.rgb, col.rgb, stepEdge);
				col.rgb = lerp(_Side_Color.rgb, col.rgb, sideEdge);
				col.a *= dissolve;
				return col;
			}
			ENDCG
		}
		Pass {
			Name "FORWARDADD"
			Tags { "IsEmissive" = "true" "LIGHTMODE" = "FORWARDADD" "QUEUE" = "Transparent+0" "RenderType" = "Custom" }
			Blend One One
			ZWrite Off
			Cull Off
			CGPROGRAM
			#pragma target 2.0
			#pragma vertex ASEFallbackVert
			#pragma fragment frag
			#include "ShaderFallbackCommon.cginc"

			sampler2D _MainTex;
			sampler2D _NosieTex;
			fixed4 _Side_Color;
			fixed4 _Step_Color;
			float _NoisePowr;
			float _RongJie;
			float _Side;
			float _Step;

			fixed4 frag(ASEFallbackV2F i) : SV_Target
			{
				fixed4 col = tex2D(_MainTex, i.uv) * i.color;
				float2 centeredUV = i.uv - 0.5;
				float radial = saturate(1.0 - length(centeredUV) * _RongJie);
				float noise = tex2D(_NosieTex, i.uv).r * _NoisePowr;
				float dissolve = saturate(radial + noise - 0.5);
				float sideEdge = smoothstep(_Side - 0.08, _Side + 0.08, dissolve);
				float stepEdge = smoothstep(_Step - 0.02, _Step + 0.02, dissolve);
				col.rgb = lerp(_Step_Color.rgb, col.rgb, stepEdge);
				col.rgb = lerp(_Side_Color.rgb, col.rgb, sideEdge);
				col.a *= dissolve;
				return col;
			}
			ENDCG
		}
	}
	CustomEditor "ASEMaterialInspector"
}

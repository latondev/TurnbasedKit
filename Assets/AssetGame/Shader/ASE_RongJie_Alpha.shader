Shader "ASE/RongJie_Alpha" {
	Properties {
		_Main_Color ("Main_Color", Color) = (1,1,1,0)
		_MainTex ("Main Tex", 2D) = "white" {}
		_DissolutionTex ("Dissolution Tex", 2D) = "white" {}
		_Dissolution ("Dissolution", Range(-1, 1)) = 0
		_Side ("Side", Range(0, 1)) = 0.3450517
		_Side_Color ("Side_Color", Color) = (1,1,1,0)
		_Mask_Tex ("Mask_Tex", 2D) = "white" {}
		[HideInInspector] _texcoord ("", 2D) = "white" {}
		[HideInInspector] __dirty ("", Float) = 1
	}
	SubShader {
		Tags { "IGNOREPROJECTOR" = "true" "IsEmissive" = "true" "QUEUE" = "Transparent+0" "RenderType" = "Custom" }
		Pass {
			Name "FORWARD"
			Tags { "IGNOREPROJECTOR" = "true" "IsEmissive" = "true" "LIGHTMODE" = "FORWARDBASE" "QUEUE" = "Transparent+0" "RenderType" = "Custom" }
			Blend SrcAlpha OneMinusSrcAlpha, SrcAlpha OneMinusSrcAlpha
			ZWrite Off
			Cull Off
			CGPROGRAM
			#pragma target 2.0
			#pragma vertex ASEFallbackVert
			#pragma fragment frag
			#include "ShaderFallbackCommon.cginc"

			sampler2D _MainTex;
			sampler2D _DissolutionTex;
			sampler2D _Mask_Tex;
			fixed4 _Main_Color;
			fixed4 _Side_Color;
			float _Dissolution;
			float _Side;

			fixed4 frag(ASEFallbackV2F i) : SV_Target
			{
				fixed4 mainCol = tex2D(_MainTex, i.uv) * _Main_Color * i.color;
				mainCol *= tex2D(_Mask_Tex, i.uv).a;
				float dissolve = tex2D(_DissolutionTex, i.uv).r + _Dissolution;
				float dissolve01 = saturate(dissolve * 0.5 + 0.5);
				float alpha = saturate(1.0 - dissolve01);
				float edge = smoothstep(_Side - 0.08, _Side + 0.08, dissolve01);
				mainCol.rgb = lerp(_Side_Color.rgb, mainCol.rgb, edge);
				mainCol.a *= alpha;
				return mainCol;
			}
			ENDCG
		}
		Pass {
			Name "FORWARDADD"
			CGPROGRAM
			#pragma target 2.0
			#pragma vertex ASEFallbackVert
			#pragma fragment frag
			#include "ShaderFallbackCommon.cginc"

			sampler2D _MainTex;
			sampler2D _DissolutionTex;
			sampler2D _Mask_Tex;
			fixed4 _Main_Color;
			fixed4 _Side_Color;
			float _Dissolution;
			float _Side;

			fixed4 frag(ASEFallbackV2F i) : SV_Target
			{
				fixed4 mainCol = tex2D(_MainTex, i.uv) * _Main_Color * i.color;
				mainCol *= tex2D(_Mask_Tex, i.uv).a;
				float dissolve = tex2D(_DissolutionTex, i.uv).r + _Dissolution;
				float dissolve01 = saturate(dissolve * 0.5 + 0.5);
				float alpha = saturate(1.0 - dissolve01);
				float edge = smoothstep(_Side - 0.08, _Side + 0.08, dissolve01);
				mainCol.rgb = lerp(_Side_Color.rgb, mainCol.rgb, edge);
				mainCol.a *= alpha;
				return mainCol;
			}
			ENDCG
		}
	}
	CustomEditor "ASEMaterialInspector"
}

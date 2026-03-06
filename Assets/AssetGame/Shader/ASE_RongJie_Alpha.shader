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
			GpuProgramID 59873
			// No subprograms found
		}
		Pass {
			Name "FORWARD"
			Tags { "IGNOREPROJECTOR" = "true" "IsEmissive" = "true" "LIGHTMODE" = "FORWARDADD" "QUEUE" = "Transparent+0" "RenderType" = "Custom" }
			Blend One One, One One
			ZWrite Off
			Cull Off
			GpuProgramID 71457
			// No subprograms found
		}
	}
	CustomEditor "ASEMaterialInspector"
}
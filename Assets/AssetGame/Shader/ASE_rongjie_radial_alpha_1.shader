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
			Blend SrcAlpha OneMinusSrcAlpha, SrcAlpha OneMinusSrcAlpha
			ZWrite Off
			Cull Off
			GpuProgramID 61506
			// No subprograms found
		}
		Pass {
			Name "FORWARD"
			Tags { "IsEmissive" = "true" "LIGHTMODE" = "FORWARDADD" "QUEUE" = "Transparent+0" "RenderType" = "Custom" }
			Blend One One, One One
			ZWrite Off
			Cull Off
			GpuProgramID 126013
			// No subprograms found
		}
	}
	CustomEditor "ASEMaterialInspector"
}
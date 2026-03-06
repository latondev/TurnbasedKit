Shader "ASE/Alpha_UVx2" {
	Properties {
		_TextureSample0 ("Texture Sample 0", 2D) = "white" {}
		_Color ("Color", Color) = (0,0,0,0)
		_Opacity ("Opacity", Range(0, 1)) = 0
		_U_Speed ("U_Speed", Float) = 0
		_V_Speed ("V_Speed", Float) = 0
		_Tex_2 ("Tex_2", 2D) = "white" {}
		_U2_Speed ("U2_Speed", Float) = 0
		_V2_Speed ("V2_Speed", Float) = 0
		_Mask ("Mask", 2D) = "white" {}
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
			GpuProgramID 61990
			// No subprograms found
		}
		Pass {
			Name "FORWARD"
			Tags { "IsEmissive" = "true" "LIGHTMODE" = "FORWARDADD" "QUEUE" = "Transparent+0" "RenderType" = "Custom" }
			Blend One One, One One
			ZWrite Off
			Cull Off
			GpuProgramID 77786
			// No subprograms found
		}
	}
	CustomEditor "ASEMaterialInspector"
}
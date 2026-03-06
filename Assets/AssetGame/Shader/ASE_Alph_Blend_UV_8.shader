Shader "ASE/Alph_Blend_UV" {
	Properties {
		_TextureSample0 ("Texture Sample 0", 2D) = "white" {}
		_Color ("Color", Color) = (0,0,0,0)
		_Opacity ("Opacity", Range(0, 1)) = 1
		_U_Speed ("U_Speed", Float) = 0
		_V_Speed ("V_Speed", Float) = 0
		_Mask_Tex ("Mask_Tex", 2D) = "white" {}
		_power ("power", Float) = 1
		[HideInInspector] _texcoord ("", 2D) = "white" {}
		[HideInInspector] __dirty ("", Float) = 1
	}
	SubShader {
		Tags { "IsEmissive" = "true" "QUEUE" = "Transparent+0" "RenderType" = "Custom" }
		Pass {
			Name "FORWARD"
			Tags { "IsEmissive" = "true" "LIGHTMODE" = "FORWARDBASE" "QUEUE" = "Transparent+0" "RenderType" = "Custom" }
			Blend SrcAlpha OneMinusSrcAlpha, One One
			ZWrite Off
			Cull Off
			GpuProgramID 16868
			// No subprograms found
		}
		Pass {
			Name "FORWARD"
			Tags { "IsEmissive" = "true" "LIGHTMODE" = "FORWARDADD" "QUEUE" = "Transparent+0" "RenderType" = "Custom" }
			Blend One One, One One
			ZWrite Off
			Cull Off
			GpuProgramID 106434
			// No subprograms found
		}
	}
	CustomEditor "ASEMaterialInspector"
}
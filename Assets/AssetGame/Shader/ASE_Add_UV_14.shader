Shader "ASE/Add_UV" {
	Properties {
		_TextureSample0 ("Texture Sample 0", 2D) = "white" {}
		_Color ("Color", Color) = (0,0,0,0)
		_Opacity ("Opacity", Range(0, 1)) = 0
		_U_Speed ("U_Speed", Float) = 0
		_V_Speed ("V_Speed", Float) = 0
		_Mask ("Mask", 2D) = "white" {}
		[HideInInspector] _texcoord ("", 2D) = "white" {}
		[HideInInspector] __dirty ("", Float) = 1
	}
	SubShader {
		Tags { "IsEmissive" = "true" "PreviewType" = "Plane" "QUEUE" = "Transparent+0" "RenderType" = "Custom" }
		Pass {
			Name "FORWARD"
			Tags { "IsEmissive" = "true" "LIGHTMODE" = "FORWARDBASE" "PreviewType" = "Plane" "QUEUE" = "Transparent+0" "RenderType" = "Custom" }
			Blend One One, One One
			ZWrite Off
			Cull Off
			GpuProgramID 48608
			// No subprograms found
		}
		Pass {
			Name "FORWARD"
			Tags { "IsEmissive" = "true" "LIGHTMODE" = "FORWARDADD" "PreviewType" = "Plane" "QUEUE" = "Transparent+0" "RenderType" = "Custom" }
			Blend One One, One One
			ZWrite Off
			Cull Off
			GpuProgramID 97638
			// No subprograms found
		}
	}
	CustomEditor "ASEMaterialInspector"
}
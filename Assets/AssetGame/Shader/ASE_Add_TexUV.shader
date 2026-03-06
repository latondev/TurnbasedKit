Shader "ASE/Add_TexUV" {
	Properties {
		_Main_Tex ("Main_Tex", 2D) = "white" {}
		_Main_TexUV_speed ("Main_TexUV_speed", Vector) = (0,0,0,0)
		_Color ("Color", Color) = (0,0,0,0)
		_Opacity ("Opacity", Float) = 1
		_Tex_UV ("Tex_UV", 2D) = "white" {}
		_Tex_UV_power ("Tex_UV_power", Range(0, 1)) = 0
		_Tex_UV_speed ("Tex_UV_speed", Vector) = (0,0,0,0)
		_Mask ("Mask", 2D) = "white" {}
		[HideInInspector] _texcoord ("", 2D) = "white" {}
		[HideInInspector] __dirty ("", Float) = 1
	}
	SubShader {
		Tags { "IsEmissive" = "true" "QUEUE" = "Transparent+0" "RenderType" = "Custom" }
		Pass {
			Name "FORWARD"
			Tags { "IsEmissive" = "true" "LIGHTMODE" = "FORWARDBASE" "QUEUE" = "Transparent+0" "RenderType" = "Custom" }
			Blend One One, One One
			ZWrite Off
			Cull Off
			GpuProgramID 30534
			// No subprograms found
		}
		Pass {
			Name "FORWARD"
			Tags { "IsEmissive" = "true" "LIGHTMODE" = "FORWARDADD" "QUEUE" = "Transparent+0" "RenderType" = "Custom" }
			Blend One One, One One
			ZWrite Off
			Cull Off
			GpuProgramID 72489
			// No subprograms found
		}
	}
	CustomEditor "ASEMaterialInspector"
}
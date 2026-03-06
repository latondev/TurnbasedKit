Shader "ASE/Refraction" {
	Properties {
		_SpecColor ("Specular Color", Color) = (1,1,1,1)
		_Distortion_Tex ("Distortion_Tex", 2D) = "white" {}
		_Distortion ("Distortion", Range(0, 1)) = 1
		_Mask ("Mask", 2D) = "white" {}
		_Speed ("Speed", Range(-1, 1)) = 0.1647059
		[HideInInspector] _texcoord ("", 2D) = "white" {}
		[HideInInspector] __dirty ("", Float) = 1
	}
	SubShader {
		Tags { "IGNOREPROJECTOR" = "true" "IsEmissive" = "true" "QUEUE" = "Transparent+0" "RenderType" = "Transparent" }
		GrabPass {
		}
		Pass {
			Name "FORWARD"
			Tags { "IGNOREPROJECTOR" = "true" "IsEmissive" = "true" "LIGHTMODE" = "FORWARDBASE" "QUEUE" = "Transparent+0" "RenderType" = "Transparent" }
			Blend SrcAlpha OneMinusSrcAlpha, SrcAlpha OneMinusSrcAlpha
			ColorMask RGB
			ZWrite Off
			Cull Off
			GpuProgramID 20021
			// No subprograms found
		}
		Pass {
			Name "FORWARD"
			Tags { "IGNOREPROJECTOR" = "true" "IsEmissive" = "true" "LIGHTMODE" = "FORWARDADD" "QUEUE" = "Transparent+0" "RenderType" = "Transparent" }
			Blend SrcAlpha One, SrcAlpha One
			ColorMask RGB
			ZWrite Off
			Cull Off
			GpuProgramID 82265
			// No subprograms found
		}
	}
	CustomEditor "ASEMaterialInspector"
}
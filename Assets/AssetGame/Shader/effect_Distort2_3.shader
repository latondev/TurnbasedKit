Shader "effect/Distort2" {
	Properties {
		_NoiseTex ("Noise Texture (RG)", 2D) = "white" {}
		_MainTex ("Alpha (A)", 2D) = "white" {}
		_HeatTime ("Heat Time", Range(0, 1.5)) = 1
		_HeatForce ("Heat Force", Range(0, 0.1)) = 0.1
	}
	SubShader {
		Tags { "QUEUE" = "Transparent+1" "RenderType" = "Transparent" }
		GrabPass {
		}
		Pass {
			Name "BASE"
			Tags { "LIGHTMODE" = "ALWAYS" "QUEUE" = "Transparent+1" "RenderType" = "Transparent" }
			Blend SrcAlpha OneMinusSrcAlpha, SrcAlpha OneMinusSrcAlpha
			ZWrite Off
			Cull Off
			GpuProgramID 33443
			// No subprograms found
		}
	}
	SubShader {
		Tags { "QUEUE" = "Transparent+1" "RenderType" = "Transparent" }
		Pass {
			Name "BASE"
			Tags { "QUEUE" = "Transparent+1" "RenderType" = "Transparent" }
			Blend DstColor Zero, DstColor Zero
			ZWrite Off
			Cull Off
			Fog {
				Mode 0
			}
			GpuProgramID 97783
			// No subprograms found
		}
	}
}
Shader "Learning Unity Shader/Lecture 15/RapidBlurEffect" {
	Properties {
		_MainTex ("Base (RGB)", 2D) = "white" {}
	}
	SubShader {
		Pass {
			ZWrite Off
			Cull Off
			GpuProgramID 16500
			// No subprograms found
		}
		Pass {
			ZTest Always
			ZWrite Off
			Cull Off
			GpuProgramID 125543
			// No subprograms found
		}
		Pass {
			ZTest Always
			ZWrite Off
			Cull Off
			GpuProgramID 133630
			// No subprograms found
		}
	}
}
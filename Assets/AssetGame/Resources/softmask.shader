Shader "Hidden/SoftMask" {
	Properties {
	}
	SubShader {
		LOD 100
		Tags { "IGNOREPROJECTOR" = "true" "QUEUE" = "Transparent" "RenderType" = "Transparent" }
		Pass {
			LOD 100
			Tags { "IGNOREPROJECTOR" = "true" "QUEUE" = "Transparent" "RenderType" = "Transparent" }
			Blend SrcAlpha One, SrcAlpha One
			ColorMask 0
			ZWrite Off
			Cull Off
			GpuProgramID 22372
			// No subprograms found
		}
	}
}
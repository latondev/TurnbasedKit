Shader "Custom/BackBlur" {
	Properties {
		[PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
		_Color ("Main Color", Color) = (1,1,1,1)
		_Size ("Size", Range(0, 20)) = 1
	}
	SubShader {
		Tags { "CanUseSpriteAtlas" = "true" "IGNOREPROJECTOR" = "true" "PreviewType" = "Plane" "QUEUE" = "Transparent" "RenderType" = "Transparent" }
		GrabPass {}
		Pass {
			Name "BackBlurHor"
			Tags { "LIGHTMODE" = "ALWAYS" }
			Blend SrcAlpha OneMinusSrcAlpha
			ZWrite Off
			Cull Off
			CGPROGRAM
			#pragma target 2.0
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"
			#include "ShaderFallbackCommon.cginc"

			sampler2D _GrabTexture;
			float4 _GrabTexture_TexelSize;
			fixed4 _Color;
			float _Size;

			struct v2f
			{
				float4 pos : SV_POSITION;
				float4 screenPos : TEXCOORD0;
			};

			v2f vert(appdata_img v)
			{
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				o.screenPos = ComputeScreenPos(o.pos);
				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				float2 stepUV = _GrabTexture_TexelSize.xy * _Size;
				fixed4 col = 0;
				col += ASEGrabSampleOffset(_GrabTexture, i.screenPos, float2(-2, 0) * stepUV) * 0.12;
				col += ASEGrabSampleOffset(_GrabTexture, i.screenPos, float2(-1, 0) * stepUV) * 0.24;
				col += ASEGrabSampleOffset(_GrabTexture, i.screenPos, float2(0, 0) * stepUV) * 0.28;
				col += ASEGrabSampleOffset(_GrabTexture, i.screenPos, float2(1, 0) * stepUV) * 0.24;
				col += ASEGrabSampleOffset(_GrabTexture, i.screenPos, float2(2, 0) * stepUV) * 0.12;
				return col * _Color;
			}
			ENDCG
		}
		GrabPass {}
		Pass {
			Name "BackBlurVer"
			Tags { "LIGHTMODE" = "ALWAYS" }
			Blend SrcAlpha OneMinusSrcAlpha
			ZWrite Off
			Cull Off
			CGPROGRAM
			#pragma target 2.0
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"
			#include "ShaderFallbackCommon.cginc"

			sampler2D _GrabTexture;
			float4 _GrabTexture_TexelSize;
			fixed4 _Color;
			float _Size;

			struct v2f
			{
				float4 pos : SV_POSITION;
				float4 screenPos : TEXCOORD0;
			};

			v2f vert(appdata_img v)
			{
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				o.screenPos = ComputeScreenPos(o.pos);
				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				float2 stepUV = _GrabTexture_TexelSize.xy * _Size;
				fixed4 col = 0;
				col += ASEGrabSampleOffset(_GrabTexture, i.screenPos, float2(0, -2) * stepUV) * 0.12;
				col += ASEGrabSampleOffset(_GrabTexture, i.screenPos, float2(0, -1) * stepUV) * 0.24;
				col += ASEGrabSampleOffset(_GrabTexture, i.screenPos, float2(0, 0) * stepUV) * 0.28;
				col += ASEGrabSampleOffset(_GrabTexture, i.screenPos, float2(0, 1) * stepUV) * 0.24;
				col += ASEGrabSampleOffset(_GrabTexture, i.screenPos, float2(0, 2) * stepUV) * 0.12;
				return col * _Color;
			}
			ENDCG
		}
	}
}

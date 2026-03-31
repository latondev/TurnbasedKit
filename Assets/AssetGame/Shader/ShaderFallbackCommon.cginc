#ifndef ASE_SHADER_FALLBACK_COMMON_INCLUDED
#define ASE_SHADER_FALLBACK_COMMON_INCLUDED

#include "UnityCG.cginc"

struct ASEFallbackV2F
{
	float4 pos : SV_POSITION;
	float2 uv : TEXCOORD0;
	float4 color : COLOR;
	float4 screenPos : TEXCOORD1;
};

struct ASEParticleV2F
{
	float4 pos : SV_POSITION;
	float2 uv : TEXCOORD0;
	float4 color : COLOR;
	#ifdef SOFTPARTICLES_ON
	float4 projPos : TEXCOORD1;
	#endif
};

inline ASEFallbackV2F ASEFallbackVert(appdata_full v)
{
	ASEFallbackV2F o;
	o.pos = UnityObjectToClipPos(v.vertex);
	o.uv = v.texcoord;
	o.color = v.color;
	o.screenPos = ComputeScreenPos(o.pos);
	return o;
}

inline ASEParticleV2F ASEParticleVert(appdata_full v)
{
	ASEParticleV2F o;
	o.pos = UnityObjectToClipPos(v.vertex);
	o.uv = v.texcoord;
	o.color = v.color;
	#ifdef SOFTPARTICLES_ON
	o.projPos = ComputeScreenPos(o.pos);
	COMPUTE_EYEDEPTH(o.projPos.z);
	#endif
	return o;
}

inline float2 ASEScrollUV(float2 uv, float2 speed)
{
	return uv + speed * _Time.y;
}

inline float2 ASERotateUV(float2 uv, float radians)
{
	float s = sin(radians);
	float c = cos(radians);
	uv -= 0.5;
	return float2(uv.x * c - uv.y * s, uv.x * s + uv.y * c) + 0.5;
}

inline fixed4 ASEApplyGray(fixed4 color, half saturation, half brightness)
{
	half luminance = dot(color.rgb, half3(0.299, 0.587, 0.114));
	color.rgb = lerp(half3(luminance, luminance, luminance), color.rgb, saturation);
	color.rgb *= brightness;
	return color;
}

inline fixed4 ASEFinalizeAdditive(fixed4 color)
{
	color.rgb *= color.a;
	return color;
}

inline fixed4 ASEGrabSample(sampler2D grabTex, float4 screenPos)
{
	return tex2Dproj(grabTex, UNITY_PROJ_COORD(screenPos));
}

inline fixed4 ASEGrabSampleOffset(sampler2D grabTex, float4 screenPos, float2 offset)
{
	float4 p = screenPos;
	p.xy += offset * p.w;
	return tex2Dproj(grabTex, UNITY_PROJ_COORD(p));
}

#ifdef SOFTPARTICLES_ON
sampler2D_float _CameraDepthTexture;
#endif

inline float ASEParticleSoftFactor(ASEParticleV2F i, float invFade)
{
	#ifdef SOFTPARTICLES_ON
	float sceneZ = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, UNITY_PROJ_COORD(i.projPos)));
	float partZ = i.projPos.z;
	return saturate(invFade * (sceneZ - partZ));
	#else
	return 1.0;
	#endif
}

#endif

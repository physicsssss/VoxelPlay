#ifndef VOXELPLAY_COMMON
#define VOXELPLAY_COMMON

#include "UnityCG.cginc"
#include "AutoLight.cginc"
#include "Lighting.cginc"
#include "VPCommonOptions.cginc"
#include "VPCommonVertexModifier.cginc"
#include "VPCommonSeeThrough.cginc"

/* cube coords
   
  7+------+6
  /.   3 /|
2+------+ |
 |4.....|.+5
 |/     |/
0+------+1

*/

const static float3 cubeVerts[8] = { 
	-0.5,	-0.5,	-0.5,
	 0.5,	-0.5,	-0.5,
	-0.5,	 0.5,	-0.5,
	 0.5,	 0.5,	-0.5,
	-0.5,	-0.5,	 0.5,
	 0.5,	-0.5,	 0.5,
	 0.5,	 0.5,	 0.5,
	-0.5,	 0.5,	 0.5
};


#if defined(NO_AMBIENT)
    #define _VPAmbientLight 0
#else
    fixed _VPAmbientLight;
#endif

#define AO_FUNCTION ao = 1.05-(1.0-ao)*(1.0-ao)
//#define AO_FUNCTION ao = saturate(ao / 0.9)
//#define AO_FUNCTION

#ifndef NON_ARRAY_TEXTURE
    UNITY_DECLARE_TEX2DARRAY(_MainTex); 
#else
    sampler _MainTex;
#endif

float4 _MainTex_ST;
float4 _MainTex_TexelSize;
fixed4 _OutlineColor;
fixed _OutlineThreshold;
fixed _VPGrassWindSpeed, _VPTreeWindSpeed;

#if defined(USES_TINTING)
	#define VOXELPLAY_TINTCOLOR_DATA fixed4 color : COLOR;
	#define VOXELPLAY_SET_TINTCOLOR(color, i) i.color = color;
	#define VOXELPLAY_OUTPUT_TINTCOLOR(o) o.color = v.color;
	#define VOXELPLAY_APPLY_TINTCOLOR(color, i) color *= i.color;
#else
	#define VOXELPLAY_TINTCOLOR_DATA
	#define VOXELPLAY_SET_TINTCOLOR(color, i)
	#define VOXELPLAY_OUTPUT_TINTCOLOR(o)
	#define VOXELPLAY_APPLY_TINTCOLOR(color, i)
#endif


#if VOXELPLAY_USE_AA
	#if defined(SHADER_API_D3D11) || defined(SHADER_API_XBOXONE) || defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE) || defined(SHADER_API_METAL)
		#define UNITY_SAMPLE_TEX2DARRAY_GRAD(tex,coord,dx,dy) tex.SampleGrad (sampler##tex,coord,dx,dy)
	#elif defined(UNITY_COMPILER_HLSL2GLSL) || defined(SHADER_TARGET_SURFACE_ANALYSIS)
		#define UNITY_SAMPLE_TEX2DARRAY_GRAD(tex,coord,dx,dy) tex2DArray(tex,coord,dx,dy)
	#else
		#define UNITY_SAMPLE_TEX2DARRAY_GRAD(tex,coord,dx,dy) UNITY_SAMPLE_TEX2DARRAY(tex,coord)
	#endif

    #if defined(NON_ARRAY_TEXTURE)
        inline fixed4 ReadSmoothTexel2D(float2 uv) {
            float2 ruv = uv.xy * _MainTex_TexelSize.zw - 0.5;
            float2 f = fwidth(ruv);
            uv.xy = (floor(ruv) + 0.5 + saturate( (frac(ruv) - 0.5 + f ) / f)) / _MainTex_TexelSize.zw; 
            return tex2D(_MainTex, uv);
        }
        #define VOXELPLAY_GET_TEXEL(uv) ReadSmoothTexel2D(uv.xy)
        #define VOXELPLAY_GET_TEXEL_DD(uv) ReadSmoothTexel2D(uv.xy)
    #else
        inline fixed4 ReadSmoothTexel(float3 uv) {
            float2 ruv = uv.xy * _MainTex_TexelSize.zw - 0.5;
            float2 f = fwidth(ruv);
            uv.xy = (floor(ruv) + 0.5 + saturate( (frac(ruv) - 0.5 + f ) / f)) / _MainTex_TexelSize.zw; 
            return UNITY_SAMPLE_TEX2DARRAY(_MainTex, uv);
        }
        inline fixed4 ReadSmoothTexelWithDerivatives(float3 uv) {
            float2 ruv = frac(uv.xy) * _MainTex_TexelSize.zw - 0.5;
            float2 f = fwidth(ruv);
            float2 nuv = (floor(ruv) + 0.5 + saturate( (frac(ruv) - 0.5 + f ) / f)) / _MainTex_TexelSize.zw;    
            return UNITY_SAMPLE_TEX2DARRAY_GRAD(_MainTex, float3(nuv, uv.z), ddx(uv.xy), ddy(uv.xy));
        }
	    #define VOXELPLAY_GET_TEXEL(uv) ReadSmoothTexel(uv)
	    #define VOXELPLAY_GET_TEXEL_DD(uv) ReadSmoothTexelWithDerivatives(uv)
    #endif
    #define VOXELPLAY_GET_TEXEL_2D(uv) ReadSmoothTexel2D(uv)


	#if VOXELPLAY_USE_OUTLINE
	inline void ApplyOutline(inout fixed4 color, float2 uv, float4 outline) {
		float2 grd = abs(frac(uv + 0.5) - 0.5);
		grd /= fwidth(uv) * _OutlineThreshold;

		if (uv.x<0.5) {
			grd.x += outline.x;
		} else {
			grd.x += outline.z;
		}
		if (uv.y<0.5) {
			grd.y += outline.w;
		} else {
			grd.y += outline.y;
		}

		float  lin = 1.0 - saturate(min(grd.x, grd.y));
		color.rgb = lerp(color.rgb, _OutlineColor.rgb, lin * _OutlineColor.a);
	}

	inline void ApplyOutlineSimple(inout fixed4 color, float2 uv) {
		float2 grd = abs(frac(uv + 0.5) - 0.5);
		grd /= fwidth(uv) * _OutlineThreshold;
		float  lin = 1.0 - saturate(min(grd.x, grd.y));
		color.rgb = lerp(color.rgb, _OutlineColor.rgb, lin * _OutlineColor.a);
	}
	#endif


#else // no AA pixels

    #if defined(NON_ARRAY_TEXTURE)
        #define VOXELPLAY_GET_TEXEL(uv) tex2D(_MainTex, uv.xy)
        #define VOXELPLAY_GET_TEXEL_DD(uv) tex2D(_MainTex, uv.xy)
    #else
    	#define VOXELPLAY_GET_TEXEL(uv) UNITY_SAMPLE_TEX2DARRAY(_MainTex, uv.xyz)
    	#define VOXELPLAY_GET_TEXEL_DD(uv) UNITY_SAMPLE_TEX2DARRAY(_MainTex, uv.xyz)
    #endif
    #define VOXELPLAY_GET_TEXEL_2D(uv) tex2D(_MainTex, uv)

	#if VOXELPLAY_USE_OUTLINE
	inline void ApplyOutline(inout fixed4 color, float2 uv, float4 outline) {
		float2 grd = abs(uv - 0.5);

		if (uv.x<0.5) {
			grd.x -= outline.x;
		} else {
			grd.x -= outline.z;
		}
		if (uv.y<0.5) {
			grd.y -= outline.w;
		} else {
			grd.y -= outline.y;
		}

		float  lin = max(grd.x, grd.y) > _OutlineThreshold;
		color.rgb = lerp(color.rgb, _OutlineColor.rgb, lin * _OutlineColor.a);
	}

	inline void ApplyOutlineSimple(inout fixed4 color, float2 uv) {
		float2 grd = abs(uv - 0.5);
		float  lin = max(grd.x, grd.y) > _OutlineThreshold;
		color.rgb = lerp(color.rgb, _OutlineColor.rgb, lin * _OutlineColor.a);
	}
	#endif

#endif


#define VOXELPLAY_NEEDS_TANGENT_SPACE VOXELPLAY_USE_PARALLAX || VOXELPLAY_USE_NORMAL
#if VOXELPLAY_NEEDS_TANGENT_SPACE
	float3x3 objectToTangent;
	#define VOXELPLAY_SET_TANGENT_SPACE(tang,norm) objectToTangent = float3x3( tang, cross(tang, norm), norm );
#else
	#define VOXELPLAY_SET_TANGENT_SPACE(tang,norm)
#endif


#if VOXELPLAY_USE_NORMAL

	inline fixed GetPerVoxelNdotL(float3 normal) {
		return saturate(1.0 + _WorldSpaceLightPos0.y * 2.0);
	}

	#define VOXELPLAY_BUMPMAP_DATA(idx1) float3 tlightDir : TEXCOORD##idx1;
	#define VOXELPLAY_OUTPUT_NORMAL_DATA(uv, i) i.tlightDir = mul(objectToTangent, _WorldSpaceLightPos0.xyz);

	fixed GetPerPixelNdotL(float3 tlightDir, float3 uv) {
		float3 nrm  = UNITY_SAMPLE_TEX2DARRAY_LOD(_MainTex, uv, 0).xyz;
		nrm = (nrm * 2.0) - 1.0;
		nrm.y*=-1.0;
		// diffuse wrap
		return saturate(dot(nrm, tlightDir) * 0.5 + 0.5);
	}

	#define VOXELPLAY_APPLY_NORMAL(i) i.light.x *= GetPerPixelNdotL(i.tlightDir, float3(i.uv.xy, i.uv.z+1));

#else
	inline fixed GetPerVoxelNdotL(float3 normal) {
        #if defined(SUN_SCATTERING)
            const float NdotL = 0.7.xxx;
        #else
//		    float NdotL = saturate(dot(_WorldSpaceLightPos0.xyz, normal) * 0.5 + 0.5); // commented out to avoid shadow acne
		    float NdotL = saturate(dot(_WorldSpaceLightPos0.xyz, normal));
        #endif
		return NdotL * saturate(1.0 + _WorldSpaceLightPos0.y * 2.0);
	}

	#define VOXELPLAY_BUMPMAP_DATA(idx1)
	#define VOXELPLAY_OUTPUT_NORMAL_DATA(uv, i)
	#define VOXELPLAY_APPLY_NORMAL(i)
#endif


CBUFFER_START(VoxelPlayLightBuffers)
float4 _VPPointLightPosition[32];
float4 _VPPointLightColor[32];
int _VPPointLightCount;
CBUFFER_END

float3 ShadePointLights(float3 worldPos, float3 normal, float specularAtten) {
	float3 color = 0;

    #if defined(USES_BRIGHT_POINT_LIGHTS)
	for (int k=0;k<32;k++) {
		if (k<_VPPointLightCount) {
			float3 toLight = _VPPointLightPosition[k].xyz - worldPos;
			float dist = dot(toLight, toLight);
			toLight /= dist + 0.0001;
			float lightAtten = dist / _VPPointLightPosition[k].w;
			float NdL = saturate((dot(normal, toLight) - 1.0) * _VPPointLightColor[k].a + 1.0);
			color += _VPPointLightColor[k].rgb * (NdL / (1.0 + lightAtten));
		}
	}
    #endif

	#if defined(USE_SPECULAR)
		float3 viewDir = normalize(_WorldSpaceCameraPos.xyz - worldPos);
		float3 h = normalize (_WorldSpaceLightPos0.xyz + viewDir);
		h *= (sign(_WorldSpaceLightPos0.y) + 1.0) * 0.5; // avoid specular under the horizon
	    float nh = max (0, dot (normal, h));
	    float spec = pow (nh, 32.0);
	    color.rgb += (specularAtten * spec) * _LightColor0.rgb;
	#endif

	return color;
}


float3 ShadePointLights(float3 worldPos, float3 normal) {
	return ShadePointLights(worldPos, normal, 0.0);
}



float3 ShadePointLightsWithoutNormal(float3 worldPos) {
    fixed3 color = 0;
    #if defined(USES_BRIGHT_POINT_LIGHTS)
    for (int k=0;k<32;k++) {
        if (k<_VPPointLightCount) {
            float3 toLight = _VPPointLightPosition[k].xyz - worldPos;
            float dist = dot(toLight, toLight);
            toLight /= dist + 0.0001;
            float atten = dist / _VPPointLightPosition[k].w;
            float NdL = max(abs(toLight.x), abs(toLight.z));
            color += _VPPointLightColor[k].rgb * (NdL / (1.0 + atten));
        }
    }
    #endif
    return color;
}


half3 _VPSkyTint;
float _VPFogAmount;
half _VPExposure;
float3 _VPFogData;


fixed3 getSkyColor(float3 ray) {
	float3 delta  = _WorldSpaceLightPos0.xyz - ray;
	float dist    = dot(delta, delta);
	float y = abs(ray.y);

	// sky base color
	half3 skyColor = _VPSkyTint;

	// fog
	half fog = saturate(_VPFogAmount - y) / (1.0001 - _VPFogAmount);
	skyColor = lerp(skyColor, 1.0.xxx, fog);

	// sky tint
	float hy = abs(_WorldSpaceLightPos0.y) + y;
	half t = saturate( (0.4 - hy) * 2.2) / (1.0 + dist * 0.8);
	skyColor.r = lerp(skyColor.r, 1.0, t);
	skyColor.b = lerp(skyColor.b, 0.0, t);

	// daylight + obscure opposite side of sky
	fixed dayLightDir = 1.0 + _WorldSpaceLightPos0.y * 2.0;
	half daylight = saturate(dayLightDir - dist * 0.03);
	skyColor *= daylight;

	// exposure
	skyColor *= _VPExposure * _LightColor0.rgb;

	// gamma
	#if defined(UNITY_COLORSPACE_GAMMA) && !defined(SHADER_API_MOBILE)
	    skyColor = sqrt(skyColor);
	#endif

	return skyColor;
}

#if VOXELPLAY_GLOBAL_USE_FOG
    #define VOXELPLAY_FOG_DATA(idx1) fixed4 skyColor: TEXCOORD##idx1;
    #define VOXELPLAY_APPLY_FOG(color, i) color.rgb = lerp(color.rgb, i.skyColor.rgb, i.skyColor.a);

    /*
    #if defined(SUN_SCATTERING)
        #define COMPUTE_SUN_SCATTERING(nviewDir) float scattering = max(0, dot(nviewDir, _WorldSpaceLightPos0.xyz)); light += pow(scattering, 128) * 0.22;
    #else
        #define COMPUTE_SUN_SCATTERING(nviewDir)
    #endif
    */

    #define VOXELPLAY_INITIALIZE_LIGHT_AND_FOG_NORMAL(uv, worldPos, normal) float3 viewDir = worldPos - _WorldSpaceCameraPos; float3 nviewDir = normalize(viewDir); o.skyColor = fixed4(getSkyColor(nviewDir), saturate( (dot(viewDir, viewDir) - _VPFogData.x) / _VPFogData.y)); o.light = fixed2(GetPerVoxelNdotL(normal), uv.w/(4096.0*15.0)); uv.w = ((int)uv.w & 0x1FF) / 15.0;
    #define VOXELPLAY_INITIALIZE_LIGHT_AND_FOG_NORMAL_NO_GI(worldPos, normal) float3 viewDir = worldPos - _WorldSpaceCameraPos; float3 nviewDir = normalize(viewDir); o.skyColor = fixed4(getSkyColor(nviewDir), saturate( (dot(viewDir, viewDir) - _VPFogData.x) / _VPFogData.y)); o.light = fixed2(GetPerVoxelNdotL(normal), 0);
    #define VOXELPLAY_INITIALIZE_LIGHT_AND_FOG(uv, worldPos) float3 viewDir = worldPos - _WorldSpaceCameraPos; float3 nviewDir = normalize(viewDir); float3 normal = -nviewDir; o.skyColor = fixed4(getSkyColor(nviewDir), saturate((dot(viewDir, viewDir) - _VPFogData.x) / _VPFogData.y)); o.light = fixed2(GetPerVoxelNdotL(normal), uv.w/(4096.0*15.0)); uv.w = ((int)uv.w & 0x1FF) / 15.0;

#else // fallbacks when fog is disabled

    #define VOXELPLAY_FOG_DATA(idx1)
    #define VOXELPLAY_APPLY_FOG(color, i)
    #define VOXELPLAY_INITIALIZE_LIGHT_AND_FOG_NORMAL(uv, worldPos, normal) o.light = fixed2(GetPerVoxelNdotL(normal), uv.w/(4096.0*15.0)); uv.w = ((int)uv.w & 0x1FF) / 15.0;
    #define VOXELPLAY_INITIALIZE_LIGHT_AND_FOG_NORMAL_NO_GI(worldPos, normal) o.light = fixed2(GetPerVoxelNdotL(normal), 0);
    #define VOXELPLAY_INITIALIZE_LIGHT_AND_FOG(uv, worldPos) float3 viewDir = _WorldSpaceCameraPos - worldPos; float3 normal = normalize(viewDir); o.light = fixed2(GetPerVoxelNdotL(normal), uv.w/(4096.0*15.0)); uv.w = ((int)uv.w & 0x1FF) / 15.0;

#endif // VOXELPLAY_GLOBAL_USE_FOG

#define TORCH_LIGHT_CONTRIBUTION i.light.yyy

#if VOXELPLAY_PIXEL_LIGHTS
	#define VOXELPLAY_LIGHT_DATA(idx1,idx2) fixed2 light: TEXCOORD##idx1; float3 wpos: TEXCOORD##idx2;
	#define VOXELPLAY_NORMAL_DATA float3 norm: NORMAL;
	#if VOXELPLAY_USE_AO || defined(USE_SPECULAR)
		#define VOXELPLAY_SET_VERTEX_LIGHT(i, worldPos, normal) i.wpos = worldPos; i.norm = normal;
		#define VOXELPLAY_SET_VERTEX_LIGHT_WITHOUT_NORMAL(i, worldPos) i.wpos = worldPos;
		#define VOXELPLAY_SET_FACE_LIGHT(i, worldPos, normal)
	#else
		#define VOXELPLAY_SET_VERTEX_LIGHT(i, worldPos, normal)
		#define VOXELPLAY_SET_VERTEX_LIGHT_WITHOUT_NORMAL(i, worldPos)
		#define VOXELPLAY_SET_FACE_LIGHT(i, worldPos, normal) i.wpos = worldPos; i.norm = normal;
	#endif
	#define VOXELPLAY_SET_LIGHT(i, worldPos, normal) i.wpos = worldPos; i.norm = normal;
	#define VOXELPLAY_SET_LIGHT_WITHOUT_NORMAL(i, worldPos) i.wpos = worldPos;
	#define VOXELPLAY_VERTEX_LIGHT_COLOR(specularAtten) ShadePointLights(i.wpos, i.norm, specularAtten)
#else
	#define VOXELPLAY_LIGHT_DATA(idx1,idx2) fixed2 light: TEXCOORD##idx1; fixed3 vertexLightColor: TEXCOORD##idx2;
	#define VOXELPLAY_NORMAL_DATA
	#if VOXELPLAY_USE_AO
		#define VOXELPLAY_SET_VERTEX_LIGHT(i, worldPos, normal) i.vertexLightColor = ShadePointLights(worldPos, normal);
		#define VOXELPLAY_SET_VERTEX_LIGHT_WITHOUT_NORMAL(i, worldPos) i.vertexLightColor = ShadePointLights(worldPos, normal);
		#define VOXELPLAY_SET_FACE_LIGHT(i, worldPos, normal)
	#else
		#define VOXELPLAY_SET_VERTEX_LIGHT(i, worldPos, normal)
		#define VOXELPLAY_SET_VERTEX_LIGHT_WITHOUT_NORMAL(i, worldPos)
		#define VOXELPLAY_SET_FACE_LIGHT(i, worldPos, normal) i.vertexLightColor = ShadePointLights(worldPos, normal);
	#endif
	#define VOXELPLAY_SET_LIGHT(i, worldPos, normal) i.vertexLightColor = ShadePointLights(worldPos, normal);
	#define VOXELPLAY_SET_LIGHT_WITHOUT_NORMAL(i, worldPos) i.vertexLightColor = ShadePointLightsWithoutNormal(worldPos);
	#define VOXELPLAY_VERTEX_LIGHT_COLOR(atten) i.vertexLightColor
#endif

// note: _VPAmbientLight could be left outside of saturate() function. In that case AO will be affected (diminished due to atten * ao calc, see VOXELPLAY_APPLY_LIGHTING_AO_AND_GI function below) so we leave it inside.
#if defined(NO_SELF_SHADOWS)
    #define VOXELPLAY_LIGHT_ATTENUATION(i) max(0, (1.0 + _WorldSpaceLightPos0.y * _VPDaylightShadowAtten) * i.light.x + _VPAmbientLight)
    #define UNITY_SHADOW_ATTEN(i) 1.0
#else
    #if defined(USE_SOFT_SHADOWS)
        #define VOXELPLAY_SHADOW_ATTENUATION(i) min(1, SHADOW_ATTENUATION(i) + 0.25 + max(0, LinearEyeDepth( i.pos.z ) * _LightShadowData.z + _LightShadowData.w ) )
    #else
        #define VOXELPLAY_SHADOW_ATTENUATION(i) min(1, SHADOW_ATTENUATION(i) + max(0, LinearEyeDepth( i.pos.z ) * _LightShadowData.z + _LightShadowData.w ) )
    #endif
    #define VOXELPLAY_LIGHT_ATTENUATION(i) saturate( (VOXELPLAY_SHADOW_ATTENUATION(i) * i.light.x + _WorldSpaceLightPos0.y * _VPDaylightShadowAtten) + _VPAmbientLight)
    #define UNITY_SHADOW_ATTEN(i) SHADOW_ATTENUATION(i)
#endif

#if defined(SUBTLE_SELF_SHADOWS)
    #define _VPDaylightShadowAtten 0.65
#else
    fixed _VPDaylightShadowAtten;
#endif

#define VOXELPLAY_APPLY_LIGHTING(color,i) fixed atten = VOXELPLAY_LIGHT_ATTENUATION(i); color.rgb *= min(atten * _LightColor0.rgb + TORCH_LIGHT_CONTRIBUTION, 1.2) + VOXELPLAY_VERTEX_LIGHT_COLOR(atten * UNITY_SHADOW_ATTEN(i) );
#define VOXELPLAY_APPLY_LIGHTING_AO_AND_GI(color,i) fixed atten = VOXELPLAY_LIGHT_ATTENUATION(i); float ao = i.uv.w; AO_FUNCTION; color.rgb *= min((atten * ao) * _LightColor0 + TORCH_LIGHT_CONTRIBUTION, 1.2) + VOXELPLAY_VERTEX_LIGHT_COLOR(i.uv.w * UNITY_SHADOW_ATTEN(i) );
#define VOXELPLAY_APPLY_LIGHTING_AND_GI(color,i) fixed atten = VOXELPLAY_LIGHT_ATTENUATION(i); color.rgb *= min((atten * i.uv.w) * _LightColor0.rgb + TORCH_LIGHT_CONTRIBUTION, 1.2) + VOXELPLAY_VERTEX_LIGHT_COLOR(i.uv.w * UNITY_SHADOW_ATTEN(i) );

#if defined(USE_EMISSION)
    fixed _VPEmissionIntensity;
    #define VOXELPLAY_COMPUTE_EMISSION(color) fixed3 emissionColor = color.rgb * ( _VPEmissionIntensity * (1.0 - color.a) );
    #define VOXELPLAY_ADD_EMISSION(color) color.rgb += emissionColor;
#else
    #define VOXELPLAY_COMPUTE_EMISSION(color)
    #define VOXELPLAY_ADD_EMISSION(color)
#endif // EMISSION


#define VOXELPLAY_OUTPUT_UV(x, o) o.uv = (x); 



#if VOXELPLAY_USE_PARALLAX

	float _VPParallaxStrength;
	int _VPParallaxIterations, _VPParallaxIterationsBinarySearch;

	float GetParallaxHeight (float3 uv, float2 uvOffset) {
		return UNITY_SAMPLE_TEX2DARRAY_LOD(_MainTex, float3(uv.xy + uvOffset, uv.z), 0).a;
	}

	void ApplyParallax(float3 tviewDir, inout float3 uv) {

		tviewDir = normalize(tviewDir);
		float2 uvDir = tviewDir.xy / (tviewDir.z + 0.42);
		float stepSize = 1.0 / _VPParallaxIterations;
		float2 uvInc = uvDir * (stepSize * _VPParallaxStrength);

		float2 uvOffset = 0;

		float stepHeight = 1;

		// get the texture index for displacement map
		uv.z ++;
		float surfaceHeight = UNITY_SAMPLE_TEX2DARRAY_LOD(_MainTex, uv, 0).a;

		float2 prevUVOffset = uvOffset;
		float prevStepHeight = stepHeight;
		float prevSurfaceHeight = surfaceHeight;

		for (int i1 = 1; i1 < _VPParallaxIterations && stepHeight > surfaceHeight; i1++) {
			prevUVOffset = uvOffset;
			prevStepHeight = stepHeight;
			prevSurfaceHeight = surfaceHeight;
			uvOffset -= uvInc;
			stepHeight -= stepSize;
			surfaceHeight = GetParallaxHeight(uv, uvOffset);
		}

		for (int i2 = 0; i2 < _VPParallaxIterationsBinarySearch; i2++) {
			uvInc *= 0.5;
			stepSize *= 0.5;

			if (stepHeight < surfaceHeight) {
				uvOffset += uvInc;
				stepHeight += stepSize;
			} else {
				uvOffset -= uvInc;
				stepHeight -= stepSize;
			}
			surfaceHeight = GetParallaxHeight(uv, uvOffset);
		}

		uv.xy += uvOffset;
		uv.z --;
	}

	#define VOXELPLAY_PARALLAX_DATA(idx1) float3 tviewDir : TEXCOORD##idx1; 
	#define VOXELPLAY_OUTPUT_PARALLAX_DATA(v, uv, i) float3 invViewDir = mul(unity_WorldToObject, float4(_WorldSpaceCameraPos.xyz, 1)).xyz - v.vertex.xyz; i.tviewDir = mul(objectToTangent, invViewDir);
	#define VOXELPLAY_APPLY_PARALLAX(i) ApplyParallax(i.tviewDir, i.uv.xyz);
#else
	#define VOXELPLAY_PARALLAX_DATA(idx1) 
	#define VOXELPLAY_OUTPUT_PARALLAX_DATA(worldPos, uv, i) 
	#define VOXELPLAY_APPLY_PARALLAX(i)
#endif // VOXELPLAY_USE_PARALLAX

#if VOXELPLAY_USE_OUTLINE
	#define VOXELPLAY_OUTLINE_DATA(idx1) float4 outlineData : TEXCOORD##idx1; 
	#define VOXELPLAY_INITIALIZE_OUTLINE(i) i.outlineData = 0;
	#define VOXELPLAY_SET_OUTLINE(x) i.outlineData = x>0;
	#define VOXELPLAY_APPLY_OUTLINE(color, i) ApplyOutline(color, i.uv.xy, i.outlineData);
	#define VOXELPLAY_APPLY_OUTLINE_SIMPLE(color, i) ApplyOutlineSimple(color, i.uv.xy);
#else
	#define VOXELPLAY_OUTLINE_DATA(idx1)
	#define VOXELPLAY_INITIALIZE_OUTLINE(i)
	#define VOXELPLAY_SET_OUTLINE(x)
	#define VOXELPLAY_APPLY_OUTLINE(color, i)
	#define VOXELPLAY_APPLY_OUTLINE_SIMPLE(color, i)
#endif

#endif // VOXELPLAY_COMMON


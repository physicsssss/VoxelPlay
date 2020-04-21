#ifndef VOXELPLAY_REALISTIC_WATER
#define VOXELPLAY_REALISTIC_WATER

#define FOAM_SIZE 0.4

float _WaveScale, _WaveSpeed, _WaveAmplitude, _RefractionDistortion, _Fresnel;
half _SpecularPower, _SpecularIntensity, _NormalStrength;
half4 _WaterColor, _UnderWaterFogColor;
half3 _FoamColor;
half3 _OceanWave;
sampler2D _WaterBackgroundTexture;
sampler2D _BumpMap;
float4 _BumpMap_TexelSize;
sampler2D _ReflectiveColor;
sampler2D _CameraDepthTexture;
sampler2D _FoamTex;
sampler2D _FoamGradient;

inline half3 GetWaterNormal(float2 p) {
    return UnpackNormal(tex2D( _BumpMap, p ));
/* // TODO: RMC
    p = p * _BumpMap_TexelSize.xw + 0.5;

    float2 i = floor(p);
    float2 f = p - i;
    f = f*f*f*(f*(f*6.0-15.0)+10.0);
    p = i + f;

    p = (p - 0.5) / _BumpMap_TexelSize.xy;
    return UnpackNormal(tex2D( _BumpMap, p ));
*/
}


#endif // VOXELPLAY_REALISTIC_WATER


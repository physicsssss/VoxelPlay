Shader "Voxel Play/FX/DamageParticle"
{
    Properties
    {
    	_MainTex ("Particle Texture Top", 2D) = "white" {}
    	_TexSides ("Particle Texture Sides", 2D) = "white" {}
    	_TexBottom ("Particle Texture Bottom", 2D) = "white" {}
    	_VoxelLight ("Voxel Ambient Light", Float) = 1
    	_FlashDelay("Flash Delay", Float) = 0
    	_Color ("Tint Color", Color) = (1,1,1)
    }
    SubShader
    {
        Pass
        {
            Tags {"LightMode"="ForwardBase"}
        
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile _ VERTEXLIGHT_ON
            #include "UnityCG.cginc"
            #include "UnityLightingCommon.cginc"
            #include "VPCommonVertexModifier.cginc"

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed3 diff : COLOR0;
                float2 uv : TEXCOORD0;
                float3 normal: TEXCOORD1;
                #if defined(VERTEXLIGHT_ON)
					fixed3 vertexLightColor: TEXCOORD2;
                #endif
				UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex, _TexSides, _TexBottom;
            float4 _MainTex_ST;
            fixed _VoxelLight, _VPAmbientLight;
            fixed _FlashDelay;
            fixed _AnimSeed;
            fixed3 _Color;

            v2f vert (appdata_base v)
            {
                v2f o;

				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_OUTPUT(v2f, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                v.texcoord.y = lerp(v.texcoord.y, v.vertex.y + 0.5, v.normal.y==0);
                float disp = sin(-_Time.w * _FlashDelay + _AnimSeed);
                v.vertex.xyz *= 1.0 + abs(disp) * 0.1;
                v.vertex.y += 0.5 + disp * 0.25;
                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
				VOXELPLAY_MODIFY_VERTEX(v.vertex, worldPos)

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv.xy = TRANSFORM_TEX(v.texcoord.xy, _MainTex);
                o.normal = v.normal;
                // Daylight
				fixed  daylight    = max(0, _WorldSpaceLightPos0.y * 2.0);
                half3 worldNormal = UnityObjectToWorldNormal(v.normal);
                half nl = 0.25 + max(0.25, dot(worldNormal, _WorldSpaceLightPos0.xyz)) * daylight;
                // factor in the light color
                o.diff = max(saturate(nl), _VPAmbientLight) * _VoxelLight * _LightColor0.rgb;
                #if defined(VERTEXLIGHT_ON)
                o.vertexLightColor = Shade4PointLights(unity_4LightPosX0, unity_4LightPosY0, unity_4LightPosZ0,unity_LightColor[0].rgb, unity_LightColor[1].rgb,unity_LightColor[2].rgb, unity_LightColor[3].rgb,unity_4LightAtten0, worldPos, worldNormal);
                #endif
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col1 = tex2D(_MainTex, i.uv);
                fixed4 col2 = tex2D(_TexSides, i.uv);
                fixed4 col3 = tex2D(_TexBottom, i.uv);
                fixed4 col = lerp(col1, col2, i.normal.y == 0);
                col = lerp(col, col3, i.normal.y<0);
                col = saturate(lerp(col, col * 1.1, abs(sin((-_Time.w + i.uv.y + i.uv.x)) * _FlashDelay)));
                col.rgb *= _Color;
                #if defined(VERTEXLIGHT_ON)
                col.rgb *= i.diff + i.vertexLightColor;
                #else
                col.rgb *= i.diff;
                #endif
                return col;
            }
            ENDCG
        }
    }
}
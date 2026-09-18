Shader "Custom/AreaClip"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}

        [NoScaleOffset] _MetallicGlossMap("Metallic Map", 2D) = "black" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0

        [Normal][NoScaleOffset] _BumpMap("Normal Map", 2D) = "bump" {}
        _BumpScale("NormalScale", Float) = 1.0

        [NoScaleOffset] _OcclusionMap("Occlusion Map", 2D) = "white" {}
        _OcclusionStrength("Occlusion Strength", Range(0,1)) = 1.0

        [HDR] _EmissionColor ("Emission Color", Color) = (0,0,0,1)
        [NoScaleOffset] _EmissionMap("Emission Map", 2D) = "white" {}

        _FadeWidth ("Edge Fade Width", Range(0,0.5)) = 0.05

        [Header(Highlights)]
        _HighlightCenter0 ("Highlight Center 0 left hand", Vector) = (0,0,0,0)
        _HighlightRadius0 ("Highlight Radius 0", Range(0,2)) = 0
        _HighlightBrightness0 ("Highlight Brightness 0", Range(0,2)) = 0
        _HighlightColor0 ("Highlight Color 0 overlay", Color) = (0.35,0.45,0.85,1)

        _HighlightCenter1 ("Highlight Center 1 right hand", Vector) = (0,0,0,0)
        _HighlightRadius1 ("Highlight Radius 1", Range(0,2)) = 0
        _HighlightBrightness1 ("Highlight Brightness 1", Range(0,2)) = 0
        _HighlightColor1 ("Highlight Color 1 overlay", Color) = (0.35,0.45,0.85,1)

        [Header(Vertical Fog)]
        _VerticalFog ("Vertical Fog", Vector) = (0,0,0,0)
        _VerticalFogColor ("VerticalFogColor", Color) = (0.35,0.45,0.85,1)

        [Enum(UnityEngine.Rendering.CullMode)]
        _Cull ("Cull", Integer) = 2
    }

    SubShader
    {
        PackageRequirements { "com.unity.render-pipelines.universal" }
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" }
        LOD 200

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "AreaClipUtils.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _Color;
            float4 _EmissionColor;
            float _Glossiness;
            float _Metallic;
            float _BumpScale;
            float _OcclusionStrength;
            float _FadeWidth;
            float4 _MainTex_ST;
            float3 _HighlightCenter0;
            float _HighlightRadius0;
            float _HighlightBrightness0;
            float4 _HighlightColor0;
            float3 _HighlightCenter1;
            float _HighlightRadius1;
            float _HighlightBrightness1;
            float4 _HighlightColor1;
            float3 _VerticalFog;
            float4 _VerticalFogColor;
        CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
                float2 lightmapUV   : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                float4 screenPos : TEXCOORD3;
                float4 tangentWS : TEXCOORD4;
                DECLARE_LIGHTMAP_OR_SH(lightmapUV, vertexSH, 5);
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.positionCS = TransformWorldToHClip(OUT.positionWS);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.tangentWS = float4(TransformObjectToWorldDir(IN.tangentOS.xyz), IN.tangentOS.w);
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.screenPos = ComputeScreenPos(OUT.positionCS);
                OUTPUT_LIGHTMAP_UV(IN.lightmapUV, unity_LightmapST, OUT.lightmapUV);
                OUTPUT_SH(OUT.normalWS, OUT.vertexSH);
                return OUT;
            }
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            Cull Off

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile_local _ _NORMALMAP
            #pragma multi_compile_local _ _METALLICSPECGLOSSMAP

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            TEXTURE2D(_MetallicGlossMap); SAMPLER(sampler_MetallicGlossMap);
            TEXTURE2D(_BumpMap); SAMPLER(sampler_BumpMap);
            TEXTURE2D(_OcclusionMap); SAMPLER(sampler_OcclusionMap);
            TEXTURE2D(_EmissionMap); SAMPLER(sampler_EmissionMap);

            half4 frag(Varyings IN, half facing : VFACE) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                ApplyAreaClip(IN.positionWS, IN.screenPos, _ScreenParams.xy, false);
                if (facing < 0) return half4(0,0,0,1);

                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                float3 albedo = texColor.rgb * _Color.rgb;
                float metallic = _Metallic;
                float smoothness = _Glossiness;
                #if defined(_METALLICSPECGLOSSMAP)
                    half4 mg = SAMPLE_TEXTURE2D(_MetallicGlossMap, sampler_MetallicGlossMap, IN.uv);
                    metallic = mg.r;
                    smoothness = mg.a * _Glossiness;
                #endif
                float3 normalWS = IN.normalWS;
                half4 occTex = SAMPLE_TEXTURE2D(_OcclusionMap, sampler_OcclusionMap, IN.uv);
                float occlusion = lerp(1.0, occTex.g, _OcclusionStrength);
                half4 emTex = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, IN.uv);
                float3 emission = emTex.rgb * _EmissionColor.rgb;

                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = albedo;
                surfaceData.metallic = metallic;
                surfaceData.smoothness = smoothness;
                surfaceData.alpha = texColor.a * _Color.a;
                surfaceData.occlusion = occlusion;
                surfaceData.emission = emission;

                InputData inputData = (InputData)0;
                inputData.positionWS = IN.positionWS;
                inputData.normalWS = normalWS;
                inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(IN.positionWS);
                inputData.bakedGI = SAMPLE_GI(IN.lightmapUV, IN.vertexSH, IN.normalWS);
                inputData.normalizedScreenSpaceUV = IN.screenPos.xy / max(IN.screenPos.w, 0.0001);
                inputData.shadowMask = half4(1,1,1,1);
                #if defined(MAIN_LIGHT_CALCULATE_SHADOWS)
                    inputData.shadowCoord = TransformWorldToShadowCoord(IN.positionWS);
                #else
                    inputData.shadowCoord = float4(0,0,0,0);
                #endif

                half4 color = UniversalFragmentBlinnPhong(inputData, surfaceData);
                float fog = saturate((IN.positionWS.y - _VerticalFog.x)/_VerticalFog.y);
                return lerp(color, _VerticalFogColor, fog * _VerticalFog.z);
            }
            ENDHLSL
        }
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 200
        Cull Off

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows addshadow finalcolor:finalColor
        #pragma target 3.0

        sampler2D _MainTex;
        sampler2D _MetallicGlossMap;
        sampler2D _OcclusionMap;
        sampler2D _EmissionMap;

        fixed4 _Color;
        fixed4 _EmissionColor;
        fixed _Glossiness;
        fixed _Metallic;
        fixed _OcclusionStrength;

        float3 _VerticalFog;
        fixed4 _VerticalFogColor;

        #include "AreaClipUtils.hlsl"

        struct Input
        {
            float2 uv_MainTex;
            float3 worldPos;
            float facing : VFACE;
        };

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            ApplyAreaClip(IN.worldPos, false);

            if (IN.facing < 0)
            {
                o.Albedo = 0;
                o.Emission = 0;
                o.Metallic = 0;
                o.Smoothness = 0;
                o.Occlusion = 1;
                o.Alpha = 1;
                o.Normal = float3(0,0,1);
                return;
            }

            fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = c.rgb;
            o.Alpha = c.a * _Color.a;

            fixed4 mg = tex2D(_MetallicGlossMap, IN.uv_MainTex);
            o.Metallic = mg.r * _Metallic;
            o.Smoothness = mg.a * _Glossiness;

            fixed4 occ = tex2D(_OcclusionMap, IN.uv_MainTex);
            o.Occlusion = lerp(1.0, occ.g, _OcclusionStrength);

            fixed4 em = tex2D(_EmissionMap, IN.uv_MainTex);
            o.Emission = em.rgb * _EmissionColor.rgb;
        }

        void finalColor (Input IN, SurfaceOutputStandard o, inout fixed4 color)
        {
            if (IN.facing < 0)
            {
                color = fixed4(0,0,0,1);
                return;
            }
            float fog = saturate((IN.worldPos.y - _VerticalFog.x)/_VerticalFog.y) * _VerticalFog.z;
            #ifdef UNITY_PASS_FORWARDADD
                // Fog doubling fix: fade add-light instead of adding fog color per light
                color.rgb *= (1.0 - saturate(fog));
            #else
                color.rgb = lerp(color.rgb, _VerticalFogColor.rgb, fog);
            #endif
        }
        ENDCG
    }

    FallBack "Diffuse"
}

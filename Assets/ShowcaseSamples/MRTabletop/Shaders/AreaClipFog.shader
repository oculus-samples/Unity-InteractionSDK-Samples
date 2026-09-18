Shader "Custom/AreaClipFog"
{
    Properties
    {
        _MaskID("_MaskID", Float) = 255

        [Header(Vertical Fog)]
        _VerticalFog ("Vertical Fog", Vector) = (0,0,0,0)
        _VerticalFogColor ("VerticalFogColor", Color) = (0.35,0.45,0.85,1)
    }

    SubShader
    {
        PackageRequirements { "com.unity.render-pipelines.universal" }
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" }
        LOD 200

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        CBUFFER_START(UnityPerMaterial)
            float3 _VerticalFog;
            float4 _VerticalFogColor;
        CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
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
                return OUT;
            }
        ENDHLSL

        Pass
        {
            Cull Back
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha, OneMinusDstAlpha One

            Stencil
            {
                Ref [_MaskID]
                Comp Equal
                Pass Keep
            }

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                float fog = saturate((IN.positionWS.y - _VerticalFog.x)/_VerticalFog.y);
                fog = pow(fog, 0.45);
                half4 color = _VerticalFogColor;
                color.a = fog * _VerticalFog.z;
                return color;
            }
            ENDHLSL
        }
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 200

        Pass
        {
            Cull Back
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha, OneMinusDstAlpha One

            Stencil
            {
                Ref [_MaskID]
                Comp Equal
                Pass Keep
            }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            float3 _VerticalFog;
            fixed4 _VerticalFogColor;

            struct Attributes { float4 vertex : POSITION; };
            struct Varyings { float4 pos : SV_POSITION; float3 worldPos : TEXCOORD0; };
            Varyings vert (Attributes v)
            {
                Varyings o;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.pos = UnityObjectToClipPos(v.vertex);
                return o;
            }
            fixed4 frag (Varyings i) : SV_Target
            {
                float fog = saturate((i.worldPos.y - _VerticalFog.x)/_VerticalFog.y);
                fog = pow(fog, 0.45);
                fixed4 color = _VerticalFogColor;
                color.a = fog * _VerticalFog.z;
                return color;
            }
            ENDCG
        }
    }

    FallBack Off
}

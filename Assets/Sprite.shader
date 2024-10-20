Shader "Universal Render Pipeline/2D/Sprite-Unlit-Default"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _NoiseScale ("Noise Scale", Vector) = (0.1, 0.1, 0, 0)
        _NoiseSnap ("Noise Snap", Float) = 0.1
 
		_FlashColor ("Flash Color", Color) = (1,1,1,1)
		_FlashAmount ("Flash Amount", Range (0,1)) = 0

        // Legacy properties. They're here so that materials using this shader can gracefully fallback to the legacy sprite shader.
        [HideInInspector] PixelSnap ("Pixel snap", Float) = 0
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        [HideInInspector] _AlphaTex ("External Alpha", 2D) = "white" {}
        [HideInInspector] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
    }
 
    SubShader
    {
        Tags {"Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" }
 
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off
 
        Pass
        {
            Tags { "LightMode" = "Universal2D" }
 
            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #if defined(DEBUG_DISPLAY)
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/InputData2D.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/SurfaceData2D.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Debug/Debugging2D.hlsl"
            #endif
 
            #pragma vertex UnlitVertex
            #pragma fragment UnlitFragment
 
            #pragma multi_compile _ DEBUG_DISPLAY
 
            struct Attributes
            {
                float3 positionOS   : POSITION;
                float4 color        : COLOR;
                float2 uv           : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
 
            struct Varyings
            {
                float4  positionCS  : SV_POSITION;
                half4   color       : COLOR;
                float2  uv          : TEXCOORD0;
                #if defined(DEBUG_DISPLAY)
                float3  positionWS  : TEXCOORD2;
                #endif
                UNITY_VERTEX_OUTPUT_STEREO
            };
 
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            half4 _MainTex_ST;
            float4 _Color;

            float4 _NoiseScale;
            float _NoiseSnap;
            
            float4 _FlashColor;
            float _FlashAmount;

            float fract(float x) { return x - floor(x); }
            float rand(float2 co) { return fract(sin(dot(co.xy ,float2(12.9898,78.233))) * 43758.5453); }
            inline float snap (float x, float snap) { return snap * round(x / snap); }

            Varyings UnlitVertex(Attributes v)
            {
                float time = float3(snap(_Time.y, _NoiseSnap), 0, 0);
                float2 noiseCo = (v.positionOS.xyz + time).xy;
                float3 noise = float3(
                    (rand(noiseCo) - 0.5) * 2 * _NoiseScale.x, 
                    (rand(noiseCo + float2(1, 1)) - 0.5) * 2 * _NoiseScale.y, 
                    (rand(noiseCo + float2(2, 2)) - 0.5) * 2
                );

                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
 
                o.positionCS = TransformObjectToHClip(v.positionOS + noise);
                #if defined(DEBUG_DISPLAY)
                o.positionWS = TransformObjectToWorld(v.positionOS);
                #endif
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                return o;
            }

            half4 UnlitFragment(Varyings i) : SV_Target
            {
                float4 mainTex = i.color * _Color * SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                mainTex.rgb = lerp(mainTex.rgb, _FlashColor.rgb, _FlashAmount);
                mainTex.rgb *= mainTex.a;

                #if defined(DEBUG_DISPLAY)
                SurfaceData2D surfaceData;
                InputData2D inputData;
                half4 debugColor = 0;
 
                InitializeSurfaceData(mainTex.rgb, mainTex.a, surfaceData);
                InitializeInputData(i.uv, inputData);
                SETUP_DEBUG_DATA_2D(inputData, i.positionWS);
 
                if(CanDebugOverrideOutputColor(surfaceData, inputData, debugColor))
                {
                    return debugColor;
                }
                #endif
 
                return mainTex;
            }
            ENDHLSL
        }
 
        Pass
        {
            Tags { "LightMode" = "UniversalForward" "Queue"="Transparent" "RenderType"="Transparent"}
 
            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #if defined(DEBUG_DISPLAY)
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/InputData2D.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/SurfaceData2D.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Debug/Debugging2D.hlsl"
            #endif
 
            #pragma vertex UnlitVertex
            #pragma fragment UnlitFragment
 
            #pragma multi_compile_fragment _ DEBUG_DISPLAY
 
            struct Attributes
            {
                float3 positionOS   : POSITION;
                float4 color        : COLOR;
                float2 uv           : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
 
            struct Varyings
            {
                float4  positionCS      : SV_POSITION;
                float4  color           : COLOR;
                float2  uv              : TEXCOORD0;
                #if defined(DEBUG_DISPLAY)
                float3  positionWS      : TEXCOORD2;
                #endif
                UNITY_VERTEX_OUTPUT_STEREO
            };
 
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_ST;
 
            Varyings UnlitVertex(Attributes attributes)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(attributes);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
 
                o.positionCS = TransformObjectToHClip(attributes.positionOS);
                #if defined(DEBUG_DISPLAY)
                o.positionWS = TransformObjectToWorld(attributes.positionOS);
                #endif
                o.uv = TRANSFORM_TEX(attributes.uv, _MainTex);
                o.color = attributes.color;
                return o;
            }
 
            float4 UnlitFragment(Varyings i) : SV_Target
            {
                float4 mainTex = i.color * SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
 
                #if defined(DEBUG_DISPLAY)
                SurfaceData2D surfaceData;
                InputData2D inputData;
                half4 debugColor = 0;
 
                InitializeSurfaceData(mainTex.rgb, mainTex.a, surfaceData);
                InitializeInputData(i.uv, inputData);
                SETUP_DEBUG_DATA_2D(inputData, i.positionWS);
 
                if(CanDebugOverrideOutputColor(surfaceData, inputData, debugColor))
                {
                    return debugColor;
                }
                #endif
 
                return mainTex;
            }
            ENDHLSL
        }
    }
 
    Fallback "Sprites/Default"
}

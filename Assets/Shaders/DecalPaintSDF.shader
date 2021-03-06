Shader "ConformalDecals/Paint/DiffuseSDF"
{
    Properties
    {
        [Header(Texture Maps)]
        _Decal("Decal Texture", 2D) = "gray" {}
        _BumpMap("Bump Map", 2D) = "bump" {}
        
        _EdgeWearStrength("Edge Wear Strength", Range(0,500)) = 100
        _EdgeWearOffset("Edge Wear Offset", Range(0,1)) = 0.1
    
        _Cutoff ("Alpha cutoff", Range(0,1)) = 0.5
        _Smoothness ("SDF smoothness", Range(0,1)) = 0.15
        _SmoothnessMipScale ("Smoothness fadeout", Range(0,1)) = 0.1
        _DecalOpacity("Opacity", Range(0,1) ) = 1
        _Background("Background Color", Color) = (0.9,0.9,0.9,0.7)
        
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", int) = 2
        [Toggle(DECAL_PREVIEW)] _Preview ("Preview", int) = 0

        [Header(Effects)]
        [PerRendererData]_Opacity("_Opacity", Range(0,1) ) = 1
        [PerRendererData]_Color("_Color", Color) = (1,1,1,1)
        [PerRendererData]_RimFalloff("_RimFalloff", Range(0.01,5) ) = 0.1
        [PerRendererData]_RimColor("_RimColor", Color) = (0,0,0,0)
        [PerRendererData]_UnderwaterFogFactor ("Underwater Fog Factor", Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "Queue" = "Geometry+100" "IgnoreProjector" = "true"}
        Cull [_Cull]
        Ztest LEqual  
        
        Pass
        {
            Name "FORWARD"
               Tags { "LightMode" = "ForwardBase" }
             Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert_forward
            #pragma fragment frag_forward

            #pragma multi_compile_fwdbase nolightmap nodirlightmap nodynlightmap
            #pragma multi_compile __ DECAL_PREVIEW
            
            sampler2D _Decal;
            
            float4 _Decal_ST;
            float4 _Decal_TexelSize;
            
            float _Smoothness;
            float _SmoothnessMipScale;
            
            float _EdgeWearStrength;
            float _EdgeWearOffset;

            float _RimFalloff;
            float4 _RimColor;
            
            #define DECAL_BASE_NORMAL
            
            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #include "AutoLight.cginc"
            #include "LightingKSP.cginc"
            #include "DecalsCommon.cginc"

            void surf (DecalSurfaceInput IN, inout SurfaceOutput o)
            {
                float4 color = tex2D(_Decal, IN.uv_decal);
                float3 normal = IN.normal;
 
                float smoothScale = (1 - saturate(1-(CalcMipLevel(IN.uv_decal * _Decal_TexelSize.zw) * _SmoothnessMipScale))) / 2;
                color.a = smoothstep(_Cutoff - smoothScale, saturate(_Smoothness + smoothScale + _Cutoff), color.a);

                decalClipAlpha(color.a);

                half rim = 1.0 - saturate(dot (normalize(IN.viewDir), normal));
                float3 emission = (_RimColor.rgb * pow(rim, _RimFalloff)) * _RimColor.a;
                
                float wearFactor = 1 - normal.z;
                float wearFactorAlpha = saturate(_EdgeWearStrength * wearFactor);

                color.a *= saturate(1 + _EdgeWearOffset - saturate(_EdgeWearStrength * wearFactor));
                
                o.Albedo = UnderwaterFog(IN.worldPosition, color).rgb;
                o.Alpha = color.a * _DecalOpacity;
                o.Emission = emission;
            }

            ENDCG
        } 
        
        Pass
        {
            Name "FORWARD"
               Tags { "LightMode" = "ForwardAdd" }
             Blend One One

            CGPROGRAM
            #pragma vertex vert_forward
            #pragma fragment frag_forward

            #pragma multi_compile_fwdadd nolightmap nodirlightmap nodynlightmap
            #pragma multi_compile __ DECAL_PREVIEW
            
            sampler2D _Decal;
            
            float4 _Decal_ST;
            float4 _Decal_TexelSize;
            
            float _Smoothness;
            float _SmoothnessMipScale;
            
            float _EdgeWearStrength;
            float _EdgeWearOffset;
            
            float _RimFalloff;
            float4 _RimColor;
            
            #define DECAL_BASE_NORMAL
            
            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #include "AutoLight.cginc"
            #include "LightingKSP.cginc"
            #include "DecalsCommon.cginc"
            
            void surf (DecalSurfaceInput IN, inout SurfaceOutput o)
            {
                float4 color = tex2D(_Decal, IN.uv_decal);
                float3 normal = IN.normal;
 
                float smoothScale = (1 - saturate(1-(CalcMipLevel(IN.uv_decal * _Decal_TexelSize.zw) * _SmoothnessMipScale))) / 2;
                color.a = smoothstep(_Cutoff - smoothScale, saturate(_Smoothness + smoothScale + _Cutoff), color.a);

                decalClipAlpha(color.a);

                half rim = 1.0 - saturate(dot (normalize(IN.viewDir), normal));
                float3 emission = (_RimColor.rgb * pow(rim, _RimFalloff)) * _RimColor.a;
                
                float wearFactor = 1 - normal.z;
                float wearFactorAlpha = saturate(_EdgeWearStrength * wearFactor);

                color.a *= saturate(1 + _EdgeWearOffset - saturate(_EdgeWearStrength * wearFactor));
                
                o.Albedo = UnderwaterFog(IN.worldPosition, color).rgb;
                o.Alpha = color.a * _DecalOpacity;
                o.Emission = emission;
            }

            ENDCG
        } 
        
        // shadow casting support
        UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"
    }
}    
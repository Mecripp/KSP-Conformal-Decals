Shader "ConformalDecals/Paint/Specular"
{
    Properties
    {
        [Header(Texture Maps)]
		_Decal("Decal Texture", 2D) = "gray" {}
		_BumpMap("Bump Map", 2D) = "bump" {}
		_SpecMap("Specular Map", 2D) = "black" {}
		
		_EdgeWearStrength("Edge Wear Strength", Range(0,500)) = 100
		_EdgeWearOffset("Edge Wear Offset", Range(0,1)) = 0.1
	
	    _Cutoff ("Alpha cutoff", Range(0,1)) = 0.5
		_DecalOpacity("Opacity", Range(0,1) ) = 1
		_Background("Background Color", Color) = (0.9,0.9,0.9,0.7)
		
        [Header(Specularity)]
        _SpecColor ("_SpecColor", Color) = (0.25, 0.25, 0.25, 1)
        _Shininess ("Shininess", Range (0.03, 10)) = 0.3
        
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
        Tags { "Queue" = "Geometry+100" }
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
            sampler2D _SpecMap;
            
            float4 _Decal_ST;
            float4 _SpecMap_ST;
            
            float _EdgeWearStrength;
            float _EdgeWearOffset;
            
            half _Shininess;

            float _RimFalloff;
            float4 _RimColor;
            
            #define DECAL_BASE_NORMAL
            #define DECAL_SPECULAR
            
            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #include "AutoLight.cginc"
            #include "LightingKSP.cginc"
            #include "DecalsCommon.cginc"

            void surf (DecalSurfaceInput IN, inout SurfaceOutput o)
            {
                float4 color = tex2D(_Decal, IN.uv_decal);
                float3 specular = tex2D(_SpecMap, IN.uv_spec);
                float3 normal = IN.normal;
 
                #ifdef DECAL_PROJECT
                    // clip alpha
                    clip(color.a - _Cutoff + 0.01);
                #endif //DECAL_PROJECT

                half rim = 1.0 - saturate(dot (normalize(IN.viewDir), normal));
                float3 emission = (_RimColor.rgb * pow(rim, _RimFalloff)) * _RimColor.a;
                
                float wearFactor = 1 - normal.z;
                float wearFactorAlpha = saturate(_EdgeWearStrength * wearFactor);

                color.a *= saturate(1 + _EdgeWearOffset - saturate(_EdgeWearStrength * wearFactor));
                color.a *= _DecalOpacity;
                
                o.Albedo = UnderwaterFog(IN.worldPosition, color).rgb;
                o.Alpha = color.a;
                o.Emission = emission;
                o.Specular = _Shininess;
                o.Gloss = specular.r * color.a;
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
            sampler2D _SpecMap;
            
            float4 _Decal_ST;
            float4 _SpecMap_ST;
            
            float _EdgeWearStrength;
            float _EdgeWearOffset;
            
            half _Shininess;

            float _RimFalloff;
            float4 _RimColor;
            
            #define DECAL_BASE_NORMAL
            #define DECAL_SPECULAR
            
            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #include "AutoLight.cginc"
            #include "LightingKSP.cginc"
            #include "DecalsCommon.cginc"

            void surf (DecalSurfaceInput IN, inout SurfaceOutput o)
            {
                float4 color = tex2D(_Decal, IN.uv_decal);
                float3 specular = tex2D(_SpecMap, IN.uv_spec);
                float3 normal = IN.normal;
                
                #ifdef DECAL_PROJECT
                    // clip alpha
                    clip(color.a - _Cutoff + 0.01);
                #endif //DECAL_PROJECT

                half rim = 1.0 - saturate(dot (normalize(IN.viewDir), normal));
                float3 emission = (_RimColor.rgb * pow(rim, _RimFalloff)) * _RimColor.a;
                
                float wearFactor = 1 - normal.z;
                float wearFactorAlpha = saturate(_EdgeWearStrength * wearFactor);

                color.a *= saturate(1 + _EdgeWearOffset - saturate(_EdgeWearStrength * wearFactor));
                
                o.Albedo = UnderwaterFog(IN.worldPosition, color).rgb;
                o.Alpha = color.a * _DecalOpacity;
                o.Emission = emission;
                o.Specular = _Shininess;
                o.Gloss = specular.r;
            }

            ENDCG
        } 
        
        // shadow casting support
        UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"
    }
}	
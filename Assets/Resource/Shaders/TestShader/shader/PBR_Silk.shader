//	--------------------------------------------------------------------
//	Copyright(C) 2006~2019 NetEase. All rights reserved.
//	This file is a part of the project MA99.
//	Project MA99
//	Contact : lupeng02@corp.netease.com
//	Author: lupeng
//  First Edit: 2019/09/02
//	--------------------------------------------------------------------
//  for the charactor or scene model
//	Physically based shading model.

Shader "MA99/PBR_Silk" {
	Properties {
		_Color ("Color", Color) = (1,1,1,1)
		[NoScaleOffset]_MainTex ("Albedo (RGB)", 2D) = "white" {}
		[NoScaleOffset]_MixTex ("MixMap (R[Metallic]G(Roughness)B)", 2D) = "gray" {}
		[NoScaleOffset]_BumpMap ("Bumpmap(B[AO])", 2D) = "bump" {}
    [Toggle(USE_DETAILNORMAL)] _USE_DETAILNORMAL("Use DetailNormal?",Int) = 0
    // [Toggle(USE_UV2)] _USE_UV2("Use UV2 for DetailNormal?", Int) = 0
    [Enum(1U,0,2U,1)] _UVSec ("UV Set for DetailNormal", Float) = 0
    [NoScaleOffset]_DetailBumpMap ("DetailBumpMap", 2D) = "bump" {}
    _DetailBumpMapUVscale ("DetailBumpMapUVscale", vector) = (1,1,1,1)
    _DetailBumpMapInt ("DetailBumpMapInt", Float) = 0.5
    _Anisotropic1 ("AnisotropicDetail01", Range(0.01,1)) = 0.8
    _Anisotropic2 ("AnisotropicDetail02", Range(0.01,1)) = 0.8
    // [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull Mode", Float) = 2
    _EmissionColor("EmissionColor", Color) = (0,0,0,0)
    _EmissionColorInt("EmissionColorInt", Float) = 4.0
		_Roughness ("Roughness", Range(0,1)) = 1.0
    
    _Cutoff ("Alpha cutoff", Range(0.01,1)) = 0.5
		[Gamma] _Metallic ("Metallic", Range(0,1)) = 1.0
     _OutlineWidth ("OutlineWidth", Range(0,1)) = 1.0
     _OutlineBias("OutlineBias", Range(0,1)) = 0
     _OutlineColor("_OutlineColor", Color) = (0,0,0,1)
     _OutlineColorDark("OutlineColorDark", Range(0,1)) = 0.2
    // _Factor("OutLineFactor", Range(0,1)) = 1.0

    // Blending state
    [HideInInspector] _Mode ("__mode", Float) = 0.0
    [HideInInspector] _SrcBlend ("__src", Float) = 1.0
    [HideInInspector] _DstBlend ("__dst", Float) = 0.0
    [HideInInspector] _ZWrite ("__zw", Float) = 1.0
	}
  //High Quality
	SubShader {
		Tags { "RenderType"="Opaque"  "PerformanceChecks"="False"}
		LOD 390
    
     Pass {
       Name "OUTLINE"
       Tags { "LightMode" = "ForwardBase" }
       Cull Front
       Blend [_SrcBlend] [_DstBlend], One One
       ZWrite [_ZWrite]
       CGPROGRAM
       #pragma vertex vert
       #pragma fragment frag
       #pragma target 3.0
       #pragma shader_feature _ _ALPHATEST_ON
             #pragma shader_feature USE_DETAILNORMAL
       #include "PBR_Input.cginc"

       struct appdata_t {
           float4 vertex : POSITION;
           float2 texcoord : TEXCOORD0;
           float3 normal : NORMAL;
           UNITY_VERTEX_INPUT_INSTANCE_ID
       };

       struct v2f {
           UNITY_POSITION(vertex);
           // float4 vertex : SV_POSITION;
           float4 texcoord : TEXCOORD0;
           float3 worldPos : TEXCOORD1;
           #if defined(_SHADER_LOD01) || defined(_SHADER_LOD02)
             float2 heightFogFactor : TEXCOORD2;
           #endif
       };

       sampler2D _MainTex;
       fixed4 _Color,_OutlineColor;
       fixed _OutlineWidth,_OutlineBias,_OutlineColorDark,_Cutoff;

       v2f vert (appdata_t v)
       {
           v2f o;
           UNITY_SETUP_INSTANCE_ID(v);
           UNITY_TRANSFER_INSTANCE_ID(v,o);
           float3 dir = normalize(v.vertex.xyz);
           float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
           float distanceViewDir =length((UnityWorldSpaceViewDir(worldPos)));

           dir = v.normal;
           dir *=  min(distanceViewDir,1.0) ;
           v.vertex.xyz += dir * (_OutlineWidth * 0.005 ) ;
           o.vertex = UnityObjectToClipPos(v.vertex);
           o.texcoord.xyzw= v.texcoord.xyxy;
           o.worldPos = worldPos;

           #if defined(_SHADER_LOD01) || defined(_SHADER_LOD02)
             float3 worldPosToCam = o.worldPos.xyz - _WorldSpaceCameraPos.xyz;       
             o.heightFogFactor = GetExponentialHeightFogFactor(worldPosToCam);
           #endif
           return o;
       }

       fixed4 frag (v2f i) : SV_Target
       {
           fixed4 texture_col = tex2D(_MainTex, i.texcoord) * _Color;
          fixed4 col = lerp(texture_col, _OutlineColor, _OutlineBias);
           col.rgb *= _OutlineColorDark;
           #ifdef _ALPHATEST_ON
             clip(col.a - _Cutoff);
           #endif
             //fog parameters
           float3 worldPosToCam = i.worldPos - _WorldSpaceCameraPos.xyz;
           #if defined(_SHADER_LOD01) || defined(_SHADER_LOD02)
             float2 heightFogFactor = i.heightFogFactor;
           #else
             float2 heightFogFactor = GetExponentialHeightFogFactor(worldPosToCam);
           #endif

           APPLY_HEIGHT_FOG(worldPosToCam,heightFogFactor,col);
           return col;
       }
       ENDCG
     }

    Pass {
      Name "FORWARD"
      Tags { "LightMode" = "ForwardBase" }
      Blend [_SrcBlend] [_DstBlend], One One
      ZWrite [_ZWrite]
      // Cull [_Cull]

      CGPROGRAM
      #pragma vertex VertShading
      #pragma fragment FragShading
      #pragma target 3.0
      #pragma shader_feature _NORMALMAP
      #pragma shader_feature _ _ALPHATEST_ON _ALPHABLEND_ON
      #pragma shader_feature _MIXTEX
      // #pragma shader_feature USE_UV2
      // #pragma shader_feature USE_DETAILNORMAL
      #pragma shader_feature _ _DEBUG_BASECOLOR _DEBUG_ROUGHNESS _DEBUG_METALLIC _DEBUG_AO _DEBUG_SHADOW _DEBUG_EMISSION _DEBUG_SPECULAR _DEBUG_INDIRECTDIFFUSE _DEBUG_NORMAL
      #pragma multi_compile_instancing
      #pragma multi_compile_fwdbase

      #define UNITY_PASS_FORWARDBASE
      #define PBR_SILK
      #define _SHADER_LOD01
      #include "PBR_Input.cginc"
     
      struct VertToFrag {
        UNITY_POSITION(pos);
        float4 texcoord : TEXCOORD0; // uv
        float3 worldPos : TEXCOORD1;
        float4 worldNormal : TEXCOORD2; //w:fogfactor
        float3 worldTangent : TEXCOORD3;
        float3 worldBinormal : TEXCOORD4;
        float4 SHorLMapUV : TEXCOORD5;
        float4 viewDir : TEXCOORD6; //w: DirInscatterFogFactor
        UNITY_SHADOW_COORDS(7)
        UNITY_VERTEX_INPUT_INSTANCE_ID
      };

      sampler2D _MainTex;
      #ifdef _NORMALMAP
        sampler2D _BumpMap;
      #endif
      #ifdef _MIXTEX
        sampler2D _MixTex;
      #endif
      fixed _Roughness,_Cutoff,_Metallic,_EmissionColorInt,_Anisotropic1,_Anisotropic2;
      fixed4 _Color;
      fixed4 _EmissionColor;
      fixed _UVSec;

      #ifdef USE_DETAILNORMAL
        float4 _DetailBumpMapUVscale;
        fixed _DetailBumpMapInt;
        sampler2D _DetailBumpMap;
      #endif

      
      void VertDIYOutput(in VertInput v, inout VertToFrag o)
      {  
        o.pos = UnityObjectToClipPos(v.vertex);
        o.texcoord.xy = v.texcoord.xy;
        o.texcoord.zw = (_UVSec == 0) ? v.texcoord.xy : v.texcoord1.xy;
        
        #ifdef USE_DETAILNORMAL
          o.texcoord.zw *= _DetailBumpMapUVscale.xy;
        #endif
 
        o.worldPos.xyz = mul(unity_ObjectToWorld, v.vertex).xyz;
        o.worldNormal.xyz = UnityObjectToWorldNormal(v.normal);
        o.worldTangent = UnityObjectToWorldDir(v.tangent.xyz);
        fixed tangentSign = v.tangent.w * unity_WorldTransformParams.w;
        o.worldBinormal = cross(o.worldNormal.xyz, o.worldTangent) * tangentSign;

        float3 worldPosToCam = o.worldPos.xyz - _WorldSpaceCameraPos.xyz;
        o.viewDir.xyz = normalize(-worldPosToCam);

        #if defined(_SHADER_LOD01) || defined(_SHADER_LOD02)       
          float2 heightFogFactor = GetExponentialHeightFogFactor(worldPosToCam);
          heightFogFactor = saturate(heightFogFactor);
          o.viewDir.w = heightFogFactor.x;
          o.worldNormal.w = heightFogFactor.y;
        #else
          o.viewDir.w = 1.0;
          o.worldNormal.w = 1.0; 
        #endif
        //输出SH /lightmapUV
        o.SHorLMapUV = GetVertexSHorLMapUV(v,o.worldPos,o.worldNormal.xyz);
      }

      void FragGetParameters (in VertToFrag IN, inout SurfaceOutputPBR o) 
      {
        InitSurfaceOutputPBR (o);
        //Albedo&Alpha       
        fixed4 c = tex2D (_MainTex, IN.texcoord.xy) * _Color;
        o.Albedo = c.rgb;
        o.Alpha = c.a;

        //Metallic&Smoothness&Emission
        #ifdef _MIXTEX
          fixed4 pbr = tex2D (_MixTex, IN.texcoord.xy);
          o.Metallic = pbr.r;
          o.Roughness = pbr.g * _Roughness;
          o.Emission = c.rgb * pbr.b * _EmissionColor.rgb * _EmissionColorInt ;
          fixed maskDetail01 = saturate(pbr.a * 2.2);
          o.Anisotropic = _Anisotropic1 * maskDetail01;
        #else 
          o.Metallic = _Metallic;
          o.Roughness = _Roughness;
          o.Emission = c.rgb * _EmissionColor.rgb * _EmissionColorInt ;
          o.Anisotropic = _Anisotropic1;
        #endif
        //Normal
        fixed3 temNormap = 0.0;
        #ifdef _NORMALMAP
          fixed4 norTex = tex2D (_BumpMap, IN.texcoord.xy);
          temNormap = norTex.rgb;
        #endif
        //detailNormal
        #if defined(USE_DETAILNORMAL) && defined(_MIXTEX)
          fixed maskDetail02 = saturate((pbr.a * 2.0 -1.2)*2);
          fixed4 detailNorTex = tex2D(_DetailBumpMap, IN.texcoord.zw) * maskDetail01;
          detailNorTex.xy = lerp(detailNorTex.zw,detailNorTex.xy,maskDetail02);
          o.Anisotropic = lerp(_Anisotropic2,_Anisotropic1,maskDetail02);
          o.Anisotropic *= maskDetail01;
          temNormap = (temNormap.rgb + detailNorTex.rgb * _DetailBumpMapInt)/(1 + _DetailBumpMapInt * maskDetail01);
        #endif

        #ifdef _NORMALMAP
          float3 tangentNormal = temNormap.rgb * 2.0 - 1.0;
          float3 worldN = tangentNormal.x * IN.worldTangent + tangentNormal.y * IN.worldBinormal + IN.worldNormal.xyz;
        #else
          float3 worldN = IN.worldNormal.xyz;
        #endif
        o.Normal = normalize(worldN);        
        //AO
        #ifdef _NORMALMAP
          o.Occlusion = norTex.b;  
        #else
          o.Occlusion = 1.0; 
        #endif

      }
      #include "PBR_VF.cginc"    
      // #endif
      ENDCG
    }

    // Pass to render object as a shadow caster
    Pass {
      Name "ShadowCaster"
      Tags { "LightMode" = "ShadowCaster" }

      CGPROGRAM
      #pragma vertex vert
      #pragma fragment frag
      #pragma target 2.0
      #pragma multi_compile_shadowcaster
      #pragma shader_feature _ALPHATEST_ON
      #pragma shader_feature USE_UV2
      #pragma multi_compile_instancing // allow instanced shadow pass for most of the shaders
      #include "UnityCG.cginc"
      #include "PBR_Input.cginc"
      struct appdata_t
      {
        float4 vertex : POSITION;
        float3 normal : NORMAL;
        float4 texcoord : TEXCOORD0;
        UNITY_VERTEX_INPUT_INSTANCE_ID
      };
      struct v2f {
        V2F_SHADOW_CASTER;
        float4  uv : TEXCOORD1;
      };


      sampler2D _MainTex;
      #if _ALPHATEST_ON
        fixed _Cutoff;
      #endif
      fixed4 _Color;

      v2f vert( appdata_t v )
      {
          v2f o;
          UNITY_SETUP_INSTANCE_ID(v);
          float4 opos = UnityClipSpaceShadowCasterPos(v.vertex, v.normal);
          o.pos = UnityApplyLinearShadowBias(opos);
          o.uv = v.texcoord.xyxy;
          return o;
      }

      float4 frag( v2f i ) : SV_Target
      {
          fixed4 texcol = tex2D( _MainTex, i.uv.xy);          
          #if _ALPHATEST_ON
            clip( texcol.a*_Color.a - _Cutoff);
          #endif
          SHADOW_CASTER_FRAGMENT(i)
      }
      ENDCG

    }

	}
  //Medium Quality  SHADERLOD1
  SubShader {
		Tags { "RenderType"="Opaque"  "PerformanceChecks"="False"}
		LOD 290
    
    Pass {
      Name "FORWARD"
      Tags { "LightMode" = "ForwardBase" }
      Blend [_SrcBlend] [_DstBlend], One One
      ZWrite [_ZWrite]
      // Cull [_Cull]

      CGPROGRAM
      #pragma vertex VertShading
      #pragma fragment FragShading
      #pragma target 3.0
      #pragma shader_feature _NORMALMAP
      #pragma shader_feature _ _ALPHATEST_ON _ALPHABLEND_ON
      #pragma shader_feature _MIXTEX
      // #pragma shader_feature USE_UV2
      //#pragma shader_feature USE_DETAILNORMAL
      #pragma shader_feature _ _DEBUG_BASECOLOR _DEBUG_ROUGHNESS _DEBUG_METALLIC _DEBUG_AO _DEBUG_SHADOW _DEBUG_EMISSION _DEBUG_SPECULAR _DEBUG_INDIRECTDIFFUSE _DEBUG_NORMAL
      #pragma multi_compile_instancing
      #pragma multi_compile_fwdbase

      #define UNITY_PASS_FORWARDBASE
      #define PBR_SILK
      #define _SHADER_LOD01
      #include "PBR_Input.cginc"
     
      struct VertToFrag {
        UNITY_POSITION(pos);
        float4 texcoord : TEXCOORD0; // uv
        float3 worldPos : TEXCOORD1;
        float4 worldNormal : TEXCOORD2; //w:fogfactor
        float3 worldTangent : TEXCOORD3;
        float3 worldBinormal : TEXCOORD4;
        float4 SHorLMapUV : TEXCOORD5;
        float4 viewDir : TEXCOORD6; //w: DirInscatterFogFactor
        UNITY_SHADOW_COORDS(7)
        UNITY_VERTEX_INPUT_INSTANCE_ID
      };

      sampler2D _MainTex;
      #ifdef _NORMALMAP
        sampler2D _BumpMap;
      #endif
      #ifdef _MIXTEX
        sampler2D _MixTex;
      #endif
      fixed _Roughness,_Cutoff,_Metallic,_EmissionColorInt,_Anisotropic1,_Anisotropic2;
      fixed4 _Color;
      fixed4 _EmissionColor;
      fixed _UVSec;

      #ifdef USE_DETAILNORMAL
        float4 _DetailBumpMapUVscale;
        fixed _DetailBumpMapInt;
        sampler2D _DetailBumpMap;
      #endif

      
      void VertDIYOutput(in VertInput v, inout VertToFrag o)
      {  
        o.pos = UnityObjectToClipPos(v.vertex);
        o.texcoord.xy = v.texcoord.xy;
        o.texcoord.zw = (_UVSec == 0) ? v.texcoord.xy : v.texcoord1.xy;
        
        #ifdef USE_DETAILNORMAL
          o.texcoord.zw *= _DetailBumpMapUVscale.xy;
        #endif
 
        o.worldPos.xyz = mul(unity_ObjectToWorld, v.vertex).xyz;
        o.worldNormal.xyz = UnityObjectToWorldNormal(v.normal);
        o.worldTangent = UnityObjectToWorldDir(v.tangent.xyz);
        fixed tangentSign = v.tangent.w * unity_WorldTransformParams.w;
        o.worldBinormal = cross(o.worldNormal.xyz, o.worldTangent) * tangentSign;

        float3 worldPosToCam = o.worldPos.xyz - _WorldSpaceCameraPos.xyz;
        o.viewDir.xyz = normalize(-worldPosToCam);

        #if defined(_SHADER_LOD01) || defined(_SHADER_LOD02)       
          float2 heightFogFactor = GetExponentialHeightFogFactor(worldPosToCam);
          heightFogFactor = saturate(heightFogFactor);
          o.viewDir.w = heightFogFactor.x;
          o.worldNormal.w = heightFogFactor.y;
        #else
          o.viewDir.w = 1.0;
          o.worldNormal.w = 1.0; 
        #endif
        //输出SH /lightmapUV
        o.SHorLMapUV = GetVertexSHorLMapUV(v,o.worldPos,o.worldNormal.xyz);
      }

      void FragGetParameters (in VertToFrag IN, inout SurfaceOutputPBR o) 
      {
        InitSurfaceOutputPBR (o);
        //Albedo&Alpha       
        fixed4 c = tex2D (_MainTex, IN.texcoord.xy) * _Color;
        o.Albedo = c.rgb;
        o.Alpha = c.a;

        //Metallic&Smoothness&Emission
        #ifdef _MIXTEX
          fixed4 pbr = tex2D (_MixTex, IN.texcoord.xy);
          o.Metallic = pbr.r;
          o.Roughness = pbr.g * _Roughness;
          o.Emission = c.rgb * pbr.b * _EmissionColor.rgb * _EmissionColorInt ;
          fixed maskDetail01 = saturate(pbr.a * 2.2);
          o.Anisotropic = _Anisotropic1 * maskDetail01;
        #else 
          o.Metallic = _Metallic;
          o.Roughness = _Roughness;
          o.Emission = c.rgb * _EmissionColor.rgb * _EmissionColorInt ;
          o.Anisotropic = _Anisotropic1;
        #endif
        //Normal
        fixed3 temNormap = 0.0;
        #ifdef _NORMALMAP
          fixed4 norTex = tex2D (_BumpMap, IN.texcoord.xy);
          temNormap = norTex.rgb;
        #endif
        //detailNormal
        #if defined(USE_DETAILNORMAL) && defined(_MIXTEX)
          fixed maskDetail02 = saturate((pbr.a * 2.0 -1.2)*2);
          fixed4 detailNorTex = tex2D(_DetailBumpMap, IN.texcoord.zw) * maskDetail01;
          detailNorTex.xy = lerp(detailNorTex.zw,detailNorTex.xy,maskDetail02);
          o.Anisotropic = lerp(_Anisotropic2,_Anisotropic1,maskDetail02);
          o.Anisotropic *= maskDetail01;
          temNormap = (temNormap.rgb + detailNorTex.rgb * _DetailBumpMapInt)/(1 + _DetailBumpMapInt * maskDetail01);
        #endif

        #ifdef _NORMALMAP
          float3 tangentNormal = temNormap.rgb * 2.0 - 1.0;
          float3 worldN = tangentNormal.x * IN.worldTangent + tangentNormal.y * IN.worldBinormal + IN.worldNormal.xyz;
        #else
          float3 worldN = IN.worldNormal.xyz;
        #endif
        o.Normal = normalize(worldN);        
        //AO
        #ifdef _NORMALMAP
          o.Occlusion = norTex.b;  
        #else
          o.Occlusion = 1.0; 
        #endif

      }
      #include "PBR_VF.cginc"    
      // #endif
      ENDCG
    }

    // Pass to render object as a shadow caster
    Pass {
      Name "ShadowCaster"
      Tags { "LightMode" = "ShadowCaster" }

      CGPROGRAM
      #pragma vertex vert
      #pragma fragment frag
      #pragma target 2.0
      #pragma multi_compile_shadowcaster
      #pragma shader_feature _ALPHATEST_ON
      #pragma shader_feature USE_UV2
      #pragma multi_compile_instancing // allow instanced shadow pass for most of the shaders
      #include "UnityCG.cginc"
      #include "PBR_Input.cginc"
      struct appdata_t
      {
        float4 vertex : POSITION;
        float3 normal : NORMAL;
        float4 texcoord : TEXCOORD0;
        UNITY_VERTEX_INPUT_INSTANCE_ID
      };
      struct v2f {
        V2F_SHADOW_CASTER;
        float4  uv : TEXCOORD1;
      };


      sampler2D _MainTex;
      #if _ALPHATEST_ON
        fixed _Cutoff;
      #endif
      fixed4 _Color;

      v2f vert( appdata_t v )
      {
          v2f o;
          UNITY_SETUP_INSTANCE_ID(v);
          float4 opos = UnityClipSpaceShadowCasterPos(v.vertex, v.normal);
          o.pos = UnityApplyLinearShadowBias(opos);
          o.uv = v.texcoord.xyxy;
          return o;
      }

      float4 frag( v2f i ) : SV_Target
      {
          fixed4 texcol = tex2D( _MainTex, i.uv.xy);          
          #if _ALPHATEST_ON
            clip( texcol.a*_Color.a - _Cutoff);
          #endif
          SHADOW_CASTER_FRAGMENT(i)
      }
      ENDCG

    }

	}
  //Low Quality   SHADERLOD2
  SubShader {
		Tags { "RenderType"="Opaque"  "PerformanceChecks"="False"}
		LOD 190
    
    Pass {
      Name "FORWARD"
      Tags { "LightMode" = "ForwardBase" }
      Blend [_SrcBlend] [_DstBlend], One One
      ZWrite [_ZWrite]
      // Cull [_Cull]

      CGPROGRAM
      #pragma vertex VertShading
      #pragma fragment FragShading
      #pragma target 3.0

      #pragma shader_feature _ _ALPHATEST_ON _ALPHABLEND_ON
      //#pragma shader_feature _MIXTEX
      // #pragma shader_feature USE_UV2
      #pragma shader_feature _NORMALMAP
      #pragma shader_feature _ _DEBUG_BASECOLOR _DEBUG_ROUGHNESS _DEBUG_METALLIC _DEBUG_AO _DEBUG_SHADOW _DEBUG_EMISSION _DEBUG_SPECULAR _DEBUG_INDIRECTDIFFUSE _DEBUG_NORMAL
      #pragma multi_compile_instancing
      #pragma multi_compile_fwdbase

      // #if !defined(INSTANCING_ON)

      #define UNITY_PASS_FORWARDBASE
      #define PBR_SILK
      #define _SHADER_LOD02
      #include "PBR_Input.cginc"
     
      struct VertToFrag {
        UNITY_POSITION(pos);
        float4 texcoord : TEXCOORD0; // uv
        float3 worldPos : TEXCOORD1;
        float4 worldNormal : TEXCOORD2; //w:fogfactor
        float3 worldTangent : TEXCOORD3;
        float3 worldBinormal : TEXCOORD4;
        float4 SHorLMapUV : TEXCOORD5;
        float4 viewDir : TEXCOORD6; //w: DirInscatterFogFactor
        UNITY_SHADOW_COORDS(7)
        UNITY_VERTEX_INPUT_INSTANCE_ID
      };

      sampler2D _MainTex;

      #ifdef _MIXTEX
        sampler2D _MixTex;
      #endif

      #ifdef _NORMALMAP
        sampler2D _BumpMap;
      #endif

      fixed _Roughness,_Cutoff,_Metallic,_EmissionColorInt,_Anisotropic1,_Anisotropic2;
      fixed4 _Color;
      fixed4 _EmissionColor;



      
      void VertDIYOutput(in VertInput v, inout VertToFrag o)
      {  
        o.pos = UnityObjectToClipPos(v.vertex);
        o.texcoord.xyzw = v.texcoord.xyxy;
     
        o.worldPos.xyz = mul(unity_ObjectToWorld, v.vertex).xyz;
        o.worldNormal.xyz = UnityObjectToWorldNormal(v.normal);
        o.worldTangent = UnityObjectToWorldDir(v.tangent.xyz);
        fixed tangentSign = v.tangent.w * unity_WorldTransformParams.w;
        o.worldBinormal = cross(o.worldNormal.xyz, o.worldTangent) * tangentSign;

        float3 worldPosToCam = o.worldPos.xyz - _WorldSpaceCameraPos.xyz;
        o.viewDir.xyz = normalize(-worldPosToCam);

        #if defined(_SHADER_LOD01) || defined(_SHADER_LOD02)       
          float2 heightFogFactor = GetExponentialHeightFogFactor(worldPosToCam);
          heightFogFactor = saturate(heightFogFactor);
          o.viewDir.w = heightFogFactor.x;
          o.worldNormal.w = heightFogFactor.y;
        #else
          o.viewDir.w = 1.0;
          o.worldNormal.w = 1.0; 
        #endif
        //输出SH /lightmapUV
        o.SHorLMapUV = GetVertexSHorLMapUV(v,o.worldPos,o.worldNormal.xyz);
      }

      void FragGetParameters (in VertToFrag IN, inout SurfaceOutputPBR o) 
      {
        InitSurfaceOutputPBR (o);
        //Albedo&Alpha       
        fixed4 c = tex2D (_MainTex, IN.texcoord.xy) * _Color;
        o.Albedo = c.rgb;
        o.Alpha = c.a;

        //Metallic&Smoothness&Emission
        #ifdef _MIXTEX
          fixed4 pbr = tex2D (_MixTex, IN.texcoord.xy);
          o.Metallic = pbr.r;
          o.Roughness = pbr.g * _Roughness;
          o.Emission = c.rgb * pbr.b * _EmissionColor.rgb * _EmissionColorInt ;
          fixed maskDetail01 = saturate(pbr.a * 2.2);
          o.Anisotropic = _Anisotropic1 * maskDetail01;
        #else 
          o.Metallic = _Metallic;
          o.Roughness = _Roughness;
          o.Emission = c.rgb * _EmissionColor.rgb * _EmissionColorInt ;
          o.Anisotropic = _Anisotropic1;
        #endif

        #ifdef _NORMALMAP
          fixed4 norTex = tex2D (_BumpMap, IN.texcoord.xy);
          fixed3 temNormap = norTex.rgb;
          float3 tangentNormal = temNormap.rgb * 2.0 - 1.0;
          float3 worldN = tangentNormal.x * IN.worldTangent + tangentNormal.y * IN.worldBinormal + IN.worldNormal.xyz;
        #else
          float3 worldN = IN.worldNormal.xyz;
        #endif
        o.Normal = normalize(worldN);        
        //AO
        #ifdef _NORMALMAP
          o.Occlusion = norTex.b;  
        #else
          o.Occlusion = 1.0; 
        #endif
      }

      #include "PBR_VF.cginc"  
    
      // #endif
      ENDCG
    }

	}
	// FallBack "Diffuse"
  CustomEditor "PBRSilkShaderGUI"
}

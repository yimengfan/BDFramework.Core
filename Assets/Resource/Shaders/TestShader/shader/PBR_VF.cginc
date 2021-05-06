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

//-----------------------------------PBR_VF.cginc---------------------      
#include "UnityPBSLighting.cginc"

//scene parameters---start
uniform float _GIScale;
uniform float _ReflectionScale;
uniform fixed4 _AmbiantChange;
uniform half _EmissionScale;
uniform fixed _CharSatFactor;

uniform half4 _FillLightPara; //xyz:FillLightColor
uniform half4 _BackLightPara; //xyz:BackLightColor
//scene parameters---end

// vertex shader
VertToFrag VertShading (VertInput v) {
  UNITY_SETUP_INSTANCE_ID(v);
  VertToFrag o;
  UNITY_INITIALIZE_OUTPUT(VertToFrag,o);
  UNITY_TRANSFER_INSTANCE_ID(v,o);
  VertDIYOutput(v,o);
  UNITY_TRANSFER_SHADOW(o,v.texcoord1.xy); // pass shadow coordinates to pixel shader
  return o;
}

//--------------------brdf---------------------------------------
#ifndef PI
  #define PI 3.14159265359f
#endif
#define INV_PI  0.31830988618f

#define MIN_ROUGHNESS 0.08

float sqr(float x)
{ 
  return x*x;
}
// GGX / Trowbridge-Reitz
// [Walter et al. 2007, "Microfacet models for refraction through rough surfaces"]
inline float D_GGX( float Roughness, float NoH )
{
  float m = Roughness * Roughness;
  float m2 = m * m;
  float d = ( NoH * m2 - NoH ) * NoH + 1.0f;	// 2 mad
  return INV_PI * m2 / ( d*d + 1e-7f );					// INV_PI *m2 / ( d*d + 1e-7f );unity中不除PI，为了和旧材质亮度一致
}

// Anisotropic GGX
// [Burley 2012, "Physically-Based Shading at Disney"]
inline float D_GGXaniso( float RoughnessT, float RoughnessB, float NoH, float ToH, float BoH )
{
  float mx = RoughnessT * RoughnessT;
  float my = RoughnessB * RoughnessB;

  float d = ToH*ToH / (mx*mx) + BoH*BoH / (my*my) + NoH*NoH;
  return INV_PI / (  mx*my * d*d );//1 / ( PI * mx*my * d*d ) unity中不除PI，为了和旧材质亮度一致
  // return 1 / (  mx*my * d*d );//1 / ( PI * mx*my * d*d ) unity中不除PI，为了和旧材质亮度一致
}



float GTR2_aniso(float anisotropic,float Roughness,float NoH, float ToH, float BoH)
{
  Roughness = max(MIN_ROUGHNESS, Roughness);
  float aspect = sqrt(1 - anisotropic * 0.9);
  float ax = max(0.001, sqr(Roughness) / aspect);
  float ay = max(0.001, sqr(Roughness) * aspect);
  return INV_PI / (ax*ay * sqr( sqr(ToH/ax) + sqr(BoH/ay) + NoH*NoH ));
}
// Smith GGX G项，各项异性版本  
// Derived G function for GGX 
inline float smithG_GGXaniso(float VoN, float VoX, float VoY, float ax, float ay) 
{  
       return 1.0 / (VoN + sqrt(pow(VoX * ax, 2.0) + pow(VoY * ay, 2.0) + pow(VoN, 2.0)));  
}  
//Ward GSF是加强版的Implicit GSF,适合用于突出当视角与平面角度发生改变后各向异性带的表现
//http://www.resetoter.cn/?p=592
float G_Ward (float NoL, float NoV)
{
	float G = pow( NoL * NoV, 0.5) * 0.25;
	return  G;
}

// [Beckmann 1963, "The scattering of electromagnetic waves from rough surfaces"]
inline float D_Beckmann( float Roughness, float NoH )
{
  float m = Roughness * Roughness;
  float m2 = m * m;
  float NoH2 = NoH * NoH;
  return exp( (NoH2 - 1) / (m2 * NoH2) ) / ( PI * m2 * NoH2 * NoH2 );
}

inline float G_Implicit()
{
  return 0.25;
}

inline float3 F_None( float3 specularColor )
{
  return specularColor;
}

inline half3 GetDirectSpecular(half NoH, half3 specularColor, half Roughness)
{
  half D = D_GGX( max(MIN_ROUGHNESS, Roughness), NoH );
  half G = G_Implicit();
  half3 F = F_None( specularColor);

  return (D * G) * F ;
}

inline half SoftenFac (half factor, half edge_smooth, half middle)
{
  half factor_fix = smoothstep(saturate(middle + 0.001f - edge_smooth), saturate(middle + edge_smooth), factor);
  return factor_fix;
}

inline half3 GetDirectSpecularGGXAniso(half Anisotropy,half Roughness, half NoL,half NoV, half NoH, half ToH, half BoH, half3 specularColor)
{
  half aspect = sqrt(1.0f - Anisotropy * 0.99f);
  half anisoXRoughness = max(0.01f, Roughness / aspect);
  half anisoYRoughness = max(0.01f, Roughness * aspect);
  // half roughness = max(MIN_ROUGHNESS, Roughness);
  // half D = D_WardAnisotropic(RoughnessT,RoughnessB, NoL, NoV, NoH, ToH, BoH);
  // half D = GTR2_aniso(RoughnessT,RoughnessB,NoH, ToH, BoH);
  half D = D_GGXaniso( anisoXRoughness,  anisoYRoughness, NoH, ToH, BoH );
  // half G = G_Ward ( NoL,  NoV) ;
  half G = G_Implicit();
  half3 F = F_None( specularColor);

  return saturate(D *G) * F ;
}

inline half3 GetDirectSpecularAniso(half RoughnessT,half RoughnessB, half NoL,half NoV, half NoH, half ToH, half BoH, half3 specularColor)
{
  // half roughness = max(MIN_ROUGHNESS, Roughness);
  // half D = D_WardAnisotropic(RoughnessT,RoughnessB, NoL, NoV, NoH, ToH, BoH);
  half D = GTR2_aniso(RoughnessT,RoughnessB,NoH, ToH, BoH);
  // D = D_GGXaniso( RoughnessT,  RoughnessB, NoH, ToH, BoH );
  half G = G_Ward ( NoL,  NoV) ;
  // G = G_Implicit();
  half3 F = F_None( specularColor);

  return (D *G) * F ;
}

inline half3 GetDirectSpecularAnisoCommon(half RoughnessT,half RoughnessB, half NoL,half NoV, half NoH, half ToH, half BoH, half3 specularColor, half IsHair)
{
  // half roughness = max(MIN_ROUGHNESS, Roughness);
  // half D = D_WardAnisotropic(RoughnessT,RoughnessB, NoL, NoV, NoH, ToH, BoH);
  half D = GTR2_aniso(RoughnessT,RoughnessB,NoH, ToH, BoH);
       D = lerp(D*0.1, D , (IsHair));
        
  // half D = D_GGXaniso( RoughnessT,  RoughnessB, NoH, ToH, BoH );
  half G = G_Ward ( NoL,  NoV) ;
  // G = G_Implicit();
  half3 F = F_None( specularColor);

  return (D *G) * F ;
}

half3 EnvBRDFApprox( half3 specularColor, half Roughness, half NoV )
{
  // [ Lazarov 2013, "Getting More Physical in Call of Duty: Black Ops II" ]
  // Adaptation to fit our G term.
  const half4 c0 = { -1, -0.0275, -0.572, 0.022 };
  const half4 c1 = { 1, 0.0425, 1.04, -0.04 };
  half4 r = Roughness * c0 + c1;
  half a004 = min( r.x * r.x, exp2( -9.28 * NoV ) ) * r.x + r.y;
  half2 AB = half2( -1.04, 1.04 ) * a004 + r.zw;

  return specularColor * AB.x + AB.y;
}


half3 SamplerEnvironmentMap (UNITY_ARGS_TEXCUBE(tex), half4 hdr, half3 R, half Roughness) //Unity_GlossyEnvironment
{
    Roughness = Roughness*(1.7 - 0.7*Roughness);
    half mip = Roughness * UNITY_SPECCUBE_LOD_STEPS;
    half4 rgbm = UNITY_SAMPLE_TEXCUBE_LOD(tex, R, mip);

    return DecodeHDR(rgbm, hdr);
}
//IndirectSpecular
inline half3 GetIndirectSpecular(half3 worldPos,  half3 R, half Roughness)//UnityGI_IndirectSpecular()
{
  half3 specular;

  #ifdef UNITY_SPECCUBE_BOX_PROJECTION
      // we will tweak reflUVW in glossIn directly (as we pass it to Unity_GlossyEnvironment twice for probe0 and probe1), so keep original to pass into BoxProjectedCubemapDirection
      half3 originalReflUVW = R;
      R = BoxProjectedCubemapDirection (originalReflUVW, worldPos, unity_SpecCube0_ProbePosition, unity_SpecCube0_BoxMin, unity_SpecCube0_BoxMax);
  #endif

  // #ifdef _GLOSSYREFLECTIONS_OFF
  //     specular = unity_IndirectSpecColor.rgb;
  // #else
    half3 env0 = SamplerEnvironmentMap (UNITY_PASS_TEXCUBE(unity_SpecCube0), unity_SpecCube0_HDR, R,Roughness);
    #ifdef UNITY_SPECCUBE_BLENDING
        const float kBlendFactor = 0.99999;
        float blendLerp = unity_SpecCube1_BoxMin.w;
        UNITY_BRANCH
        if (blendLerp < kBlendFactor)
        {
            #ifdef UNITY_SPECCUBE_BOX_PROJECTION
                R = BoxProjectedCubemapDirection (originalReflUVW, worldPos, unity_SpecCube1_ProbePosition, unity_SpecCube1_BoxMin,unity_SpecCube1_BoxMax);
            #endif

            half3 env1 = SamplerEnvironmentMap (UNITY_PASS_TEXCUBE_SAMPLER(unity_SpecCube1,unity_SpecCube0), unity_SpecCube1_HDR,R,Roughness);
            specular = lerp(env1, env0, blendLerp);
        }
        else
        {
            specular = env0;
        }
    #else
        specular = env0;
    #endif
  // #endif

  return specular ;
}     

inline half3 GetIndirectDiffuse( half4 SHorLMapUV, half3 worldPos, half3 normalWorld)//UnityGI UnityGI_Base()
{
  half3 indirectDiffuse = 0;
  #if UNITY_SHOULD_SAMPLE_SH
      indirectDiffuse = ShadeSHPerPixel(normalWorld, SHorLMapUV.rgb, worldPos);
  #endif

  #if defined(LIGHTMAP_ON)
      // Baked lightmaps
      half4 bakedColorTex = UNITY_SAMPLE_TEX2D(unity_Lightmap, SHorLMapUV.xy);
      half3 bakedColor = DecodeLightmap(bakedColorTex);
      indirectDiffuse += bakedColor;
  #endif
  return indirectDiffuse;
}

inline half GetLerpBakeAndRealtimeShadows(half4 SHorLMapUV,half3 worldPos,half atten)
{
  #if defined(HANDLE_SHADOWS_BLENDING_IN_GI)    //defined( SHADOWS_SCREEN ) && defined( LIGHTMAP_ON )    
    half bakedAtten = UnitySampleBakedOcclusion(SHorLMapUV.xy, worldPos);
    float zDist = dot(_WorldSpaceCameraPos - worldPos, UNITY_MATRIX_V[2].xyz);
    float fadeDist = UnityComputeShadowFadeDistance(worldPos, zDist);
    atten = UnityMixRealtimeAndBakedShadows(atten, bakedAtten, UnityComputeShadowFade(fadeDist));
  #endif
  return atten;
}

inline half softenFac( float factor,  float edge_smooth,  float middle)
	{
		half factor_fix_= smoothstep(saturate(middle + 0.001f - edge_smooth), saturate(middle + edge_smooth), factor);
    return factor_fix_;
	};
//--------------------brdf---------------------------------------

//--------------------SnowOver---------------------------------------
uniform float _SnowRate;
#if defined(IS_FOLIAGE)
  #define SNOW_OVER(BaseColor, N, Metallic, Roughness,IN)\
  {\
    half overlayFactor =  saturate(lerp(0.6 , (N.y+0.2) , saturate(IN.vertexColor.b + _SnowAdjust)) * _SnowRate) * SNOW_HEIGHT_MODIFIER ;\
    BaseColor = lerp(BaseColor, half3(1,1,1), overlayFactor);\
    Metallic *= 1 - overlayFactor;\
    Roughness = lerp(Roughness, 1.0, overlayFactor);\
  }
#else
  #define SNOW_OVER(BaseColor, N, Metallic, Roughness,IN)\
  {\
    half overlayFactor = saturate((N.y) * _SnowRate) * SNOW_HEIGHT_MODIFIER;\
    BaseColor = lerp(BaseColor, half3(1,1,1), overlayFactor);\
    Metallic *= 1 - overlayFactor;\
    Roughness = lerp(Roughness, 1.0, overlayFactor);\
  }
#endif
//--------------------SnowOver---------------------------------------
// fragment shader
fixed4 FragShading (VertToFrag IN) : SV_Target {
  fixed4 c = 0;
  UNITY_SETUP_INSTANCE_ID(IN);

  SurfaceOutputPBR o;
  FragGetParameters (IN, o);
  fixed3 BaseColor = o.Albedo;      
  float3 N = o.Normal;      
  half3 Emission = o.Emission;
  half Metallic = o.Metallic;      
  half Roughness = o.Roughness;    
  half AO = o.Occlusion; 

 #if defined(PBR_HAIRNEW)  
  fixed3 Lightmap = o.Lightmap;
  fixed Ascale = o.Ascale ;
  fixed Asmooth = o.Asmooth ;
  
  fixed Anisotropic =smoothstep(Ascale-Asmooth, Ascale+Asmooth , o.Anisotropic);  
  // fixed3 Lightmap = o.Lightmap;
  #endif
  #if defined(PBR_SKIN) || defined (VBR_SKIN)
    half Curvature = o.Curvature;
    half Thickness = o.Thickness;  
  #endif 
  #if defined(PBR_HAIR)  || defined(PBR_SILK)
    half Anisotropic = o.Anisotropic;
  #endif  

  c.a = o.Alpha;
  // #if  defined(PBR_START)
  //   fixed3 Star = o.Star;
  // #endif 

   #if defined(PBR_COMMON)  
  half Curvature = o.Curvature; 
  half Anisotropic = o.Anisotropic; 
  half IsFace = o.IsFace; 
  half IsHair = o.IsHair; 
  half Anisohair = o.Anisohair; 
  #endif 
  

   //NO Snow on the character
  #if !defined(PBR_CHAR) && !defined(PBR_SKIN) && !defined(PBR_HAIR) && !defined (VBR_SKIN) && !defined(PBR_SILK) && !defined(PBR_HAIRNEW) && !defined (PBR_TERRAIN) && !defined (PBR_COMMON) && !defined (PBR_BOSS)
    SNOW_OVER(BaseColor, N, Metallic, Roughness, IN);
  #endif
  // alpha test
  #if defined(_ALPHATEST_ON)
    clip(c.a - _Cutoff);
  #endif

  float3 worldPos = IN.worldPos.xyz;       
  #ifndef USING_DIRECTIONAL_LIGHT
    float3 lightDir = normalize(UnityWorldSpaceLightDir(worldPos));
  #else
    float3 lightDir = _WorldSpaceLightPos0.xyz;
  #endif

  //fog parameters 
  float3 worldPosToCam = worldPos - _WorldSpaceCameraPos.xyz;
  #if defined(_SHADER_LOD01) || defined(_SHADER_LOD02)
    float2 heightFogFactor = float2(IN.viewDir.w,IN.worldNormal.w);
  #else
    float2 heightFogFactor = GetExponentialHeightFogFactor(worldPosToCam);
  #endif

  //pbr parameters
  // float3 V = normalize(UnityWorldSpaceViewDir(worldPos));
  float3 V = IN.viewDir.xyz;
  float3 L = lightDir;
  half3 H = normalize(L + V); 
  half3 R = reflect(-V, N);
  half _NoL = dot(N,L);
  half NoL = saturate(_NoL);
  half HNoL = NoL*0.5+0.5;
  half NoH = (dot(N,H));
  half NoV = abs(dot(N,V));
  half NoV2 = NoV*NoV;
  half Inv_Nov = 1 - NoV;
  // half LoV = (dot(L,V));
  // half LoH = (dot(L,H));

 
  #if defined (PBR_CHAR) || defined(PBR_SKIN) || defined(PBR_HAIR) || defined(VBR_SKIN) || defined(PBR_SILK)
    fixed3 SunColor = lerp(_LightColor0.rgb,1.0,_CharSatFactor);
  #else
    fixed3 SunColor = _LightColor0.rgb;
  #endif
  
  // compute lighting & shadowing factor
  UNITY_LIGHT_ATTENUATION(atten, IN, worldPos);
  half shadow = GetLerpBakeAndRealtimeShadows(IN.SHorLMapUV,worldPos,atten);

  half NoL_shadow = min(NoL,shadow);
  // AO 
  AO = max(AO,NoL_shadow);

  //PBR Color Parameters
  half3 diffuseColor = BaseColor - BaseColor * Metallic;
  half3 specularColor = lerp(half3(0.04, 0.04, 0.04), BaseColor, Metallic.xxx); 
 
  //IndirectDiffuse
  half3 indirectDiffuse = GetIndirectDiffuse(IN.SHorLMapUV,worldPos, N)  * _GIScale  ;
   
   
   #if defined(PBR_HAIRNEW)    
  half  normal_sig = IN.worldNormal.w;
  half  middle = lerp(0.1f, 0.5f, normal_sig);
  half warp_smooth = lerp(0.1, 0.01, NoV2);
  half ibl_NdotL_raw = max(0.0f, dot(indirectDiffuse.xyz, IN.worldNormal.xyz));
  half ibl_NdotL =  softenFac (ibl_NdotL_raw, warp_smooth, middle) ;
  half3 ibl_diffuse_color_ = lerp(0.7*indirectDiffuse, indirectDiffuse, float3(AO.xxx * ibl_NdotL)); // ao处理
        indirectDiffuse = ibl_diffuse_color_;
  #endif 
  indirectDiffuse = lerp(indirectDiffuse, _AmbiantChange.rgb ,_AmbiantChange.w);

  //DirectDiffuse    
  #ifdef PBR_SKIN
    #if !defined(_SHADER_LOD01)  && !defined(_SHADER_LOD02)
      half3 sssColor = SkinScattering_withShadow(_NoL,Curvature,shadow);
      half3 directDiffuse = sssColor ;
      half3 tsColor = GetBTDF( L, N, V, Thickness) ;
      directDiffuse += tsColor;
    #else
      half3 sss = BaseColor * _NoL * 0.5 + 0.5;
      //half tempShadow = lerp(0.5,1,shadow);
      //half3 sssColor = saturate(sss * shadow);
      half3 directDiffuse = sss * 0.9;
      directDiffuse.gb *= 0.9;
    #endif
    
  #elif defined (VBR_SKIN)
    half NdotL_temp = softenDotProductSSS(_NoL, Curvature, shadow);  

    half NdotL_temp_invert = saturate(-NdotL_temp);
    half scatter_mask = saturate(NdotL_temp + NdotL_temp_invert);
    // float maxChan = max(max(diffuseColor.r,diffuseColor.g),diffuseColor.b);
    // maxChan = max(maxChan - 0.16, 0.1);
    // half3 dark_diffuse = diffuseColor - half3(maxChan, maxChan, maxChan);
    // dark_diffuse = saturate(dark_diffuse);
    // dark_diffuse = _SSSColor;
    half3 sss_dirlight = calculateSSSDirContribution(NdotL_temp, NdotL_temp_invert, shadow, _SSSColor, scatter_mask, Curvature);

    half3 directDiffuse = sss_dirlight + NdotL_temp;

    half3 tsColor = GetBTDF( L, N, V, Thickness) ;
    directDiffuse += tsColor;

    half3 sss_ibl = (2.0 - NoV) *  _SSSColor * Curvature ;
    indirectDiffuse *= (0.9 + sss_ibl);

  #elif defined (PBR_CHAR)
    half3 directDiffuse =  NoL_shadow ;
  #else
    half3 directDiffuse =  NoL_shadow ;
  #endif
  directDiffuse *= SunColor;
  
   #if defined(PBR_COMMON) 
  // half3 sssColor = SkinScattering_withShadow(_NoL,Curvature,shadow); 
        half3 sss = directDiffuse * _NoL * 0.5 + 0.5;
        half tempShadow = lerp(0.5,1,shadow);
        half3 sssColor = saturate(sss * tempShadow);
   half3 _directDiffuse = sssColor ;
   float3 color = lerp(sssColor*0.1, sssColor.rgb, (IsFace ));
   _directDiffuse = color ;
 directDiffuse +=_directDiffuse;
 #endif
  //DirectSpecular
  #if defined(PBR_HAIR) || defined(PBR_SILK) || defined(PBR_COMMON)|| defined(PBR_HAIRNEW)
    half3 T = cross(IN.worldTangent,N);
    half3 B = cross(T,N);
    half ToH = dot(T,H);
    half BoH = dot(B,H);
    half3 directSpecular = half3(0,0,0);
   
    #if defined (PBR_COMMON)
      directSpecular = GetDirectSpecularAnisoCommon( IsHair ? Anisohair : Anisotropic, Roughness , NoL, NoV, NoH,  BoH,ToH, specularColor,IsHair) * lerp(0.8 , 1.0 , NoL_shadow);
    #elif defined (PBR_HAIRNEW)
      directSpecular = GetDirectSpecularAniso(Anisotropic, Roughness , NoL, NoV, NoH,  BoH,ToH, specularColor) * lerp(0.8 , 1.0 , NoL_shadow)*_AColor;
    #elif defined (PBR_SILK)//金属高光会爆掉？先测试一下换成ggx
      directSpecular = GetDirectSpecularAniso(Anisotropic, Roughness , NoL, NoV, NoH,  BoH,ToH, specularColor) * lerp(0.8 , 1.0 , NoL_shadow);
      // directSpecular = GetDirectSpecularGGXAniso(Anisotropic, Roughness , NoL, NoV, NoH,  BoH,ToH, specularColor) * lerp(0.8 , 1.0 , NoL_shadow);
    #else
      directSpecular = GetDirectSpecularAniso(Anisotropic, Roughness , NoL, NoV, NoH,  BoH,ToH, specularColor) * lerp(0.8 , 1.0 , NoL_shadow);
    #endif
      
  #else
      half3 directSpecular = GetDirectSpecular(NoH, specularColor, Roughness) * lerp(0.2 , 1.0 , NoL_shadow);
  #endif
  directSpecular *= SunColor;

  // fill lighting
  #if defined(PBR_CHAR) || defined(VBR_SKIN) || defined(PBR_SKIN)
    #if !defined(_SHADER_LOD02)
      directSpecular += GetDirectSpecular(NoV, specularColor, Roughness) * _FillLightPara.rgb ; 
    #endif
    directDiffuse += NoV * _FillLightPara.rgb;
  #elif defined(PBR_HAIR) || defined(PBR_SILK) || defined(PBR_HAIRNEW)|| defined(PBR_COMMON)
    #if !defined(_SHADER_LOD02)
      half ToV = dot(T,V);
      half BoV = dot(B,V);
      directSpecular += GetDirectSpecularAniso(Anisotropic, Roughness, NoV, NoV, NoV, BoV,ToV, specularColor) * _FillLightPara.rgb;
    #endif
    directDiffuse += NoV * _FillLightPara.rgb;
  #endif

  // back lighting
  #if defined (PBR_CHAR) || defined(PBR_SKIN) || defined(PBR_HAIR) || defined(VBR_SKIN) || defined(PBR_SILK)|| defined(PBR_COMMON)|| defined(PBR_HAIRNEW)
    half3 backlightDir = mul(unity_CameraToWorld,half4(normalize(float3(-0.8,0.1,0.4)),0.0)).xyz;
    // backlightDir = mul(transpose(UNITY_MATRIX_V), (half3(-0.8,-0.1,-1.0))).xyz;
    half3 backlight = (dot(N,backlightDir) + 1) * 0.5;
    #if defined(PBR_HAIR)
      backlight = smoothstep(0.7,0.8,backlight) * _BackLightPara.rgb * max(BaseColor.rgb,0.2) * 0.1;
    #else
      backlight = smoothstep(0.7,0.8,backlight) * _BackLightPara.rgb * max(BaseColor.rgb,0.1);
    #endif
    directSpecular += backlight;
  #endif

  //IndirectSpecular
  #if defined(_SHADER_LOD01) || defined(_SHADER_LOD02)
    half3 indirectSpecular = GetIndirectSpecular (worldPos,R,Roughness) * specularColor * _ReflectionScale * lerp(0.8,1.0,NoL_shadow);
  #else
    half3 indirectSpecularBRDF = EnvBRDFApprox(specularColor,Roughness,NoV);
    half3 indirectSpecular = GetIndirectSpecular (worldPos,R,Roughness) * indirectSpecularBRDF * _ReflectionScale * lerp(0.8,1.0,NoL_shadow);
  #endif

  // #if  defined(PBR_START)
  //  c.rgb = ((directDiffuse + indirectDiffuse) * diffuseColor * AO + (directSpecular + indirectSpecular)+(o.Star*NoV)) * AO  + Emission * _EmissionScale;
  //   //c.rgb = o.Star;
 #if defined(PBR_HAIRNEW)
  // c.rgb = ((directDiffuse + indirectDiffuse) * diffuseColor * AO*Lightmap + (directSpecular + indirectSpecular)) * AO ;
  c.rgb = ((directDiffuse + indirectDiffuse * HNoL)* diffuseColor*Lightmap* AO+ (directSpecular + indirectSpecular)*Lightmap*AO)  ;
  // c.rgb = indirectDiffuse;

  #else
  c.rgb = ((directDiffuse + indirectDiffuse) * diffuseColor * AO + (directSpecular + indirectSpecular)) * AO  + Emission * _EmissionScale;
  // c.rgb = indirectDiffuse;
  #endif

   #if defined(PBR_COMMON) || defined(PBR_CHAR)
   half3 hitColor = Inv_Nov * Inv_Nov * o.HitColor * o.HitIntensity;
    c.rgb = c.rgb + hitColor;
   #endif
  
  //Alpha
  #if !defined(_ALPHATEST_ON) && !defined(_ALPHABLEND_ON)
    c.a = 1.0;
  #else
    c.a = saturate(c.a);
  #endif
  //fog
  APPLY_HEIGHT_FOG(worldPosToCam,heightFogFactor,c);
  //Debug
  #if _DEBUG_BASECOLOR
    c.rgb = o.Albedo;
  #endif
  #if _DEBUG_ROUGHNESS
    c.rgb = o.Roughness;
  #endif
  #if _DEBUG_METALLIC
    c.rgb = o.Metallic;
  #endif
  #if _DEBUG_AO
    c.rgb = o.Occlusion;
  #endif
  #if _DEBUG_NORMAL
    c.rgb = (N + 1) * 0.5;
  #endif
  #if _DEBUG_SHADOW
    c.rgb = shadow;
  #endif
  #if _DEBUG_EMISSION
    c.rgb = Emission * _EmissionScale;;
  #endif
  #if _DEBUG_SPECULAR
    c.rgb = directSpecular + indirectSpecular;
  #endif
  #if _DEBUG_INDIRECTDIFFUSE
    c.rgb = indirectDiffuse;
  #endif

  return c;
}
#include "UnityCG.cginc"
#include "Lighting.cginc"    
#include "AutoLight.cginc"
struct VertInput 
{
  float4 vertex : POSITION;
  float4 tangent : TANGENT;
  float3 normal : NORMAL;
  float4 texcoord : TEXCOORD0;
  float4 texcoord1 : TEXCOORD1;
  float4 texcoord2 : TEXCOORD2;
  float4 texcoord3 : TEXCOORD3;
  fixed4 color : COLOR;
  UNITY_VERTEX_INPUT_INSTANCE_ID
};
 //获取顶点SH环境光或者lightmapUV
inline half4 GetVertexSHorLMapUV(VertInput v, float3 worldPos, half3 worldNormal)
{
    half4 SHorLMapUV = 0;
    // Static lightmaps
    #ifdef LIGHTMAP_ON
        SHorLMapUV.xy = v.texcoord1.xy * unity_LightmapST.xy + unity_LightmapST.zw;
        SHorLMapUV.zw = 0;
    // Sample light probe for Dynamic objects only (no static or dynamic lightmaps)
    #elif UNITY_SHOULD_SAMPLE_SH 
        #ifdef VERTEXLIGHT_ON
            // Approximated illumination from non-important point lights
            SHorLMapUV.rgb = Shade4PointLights (
                unity_4LightPosX0, unity_4LightPosY0, unity_4LightPosZ0,
                unity_LightColor[0].rgb, unity_LightColor[1].rgb, unity_LightColor[2].rgb, unity_LightColor[3].rgb,
                unity_4LightAtten0, worldPos, worldNormal);
        #endif
        SHorLMapUV.rgb = ShadeSHPerVertex (worldNormal, SHorLMapUV.rgb); //UnityStandardUtils.cginc
    #endif
    return SHorLMapUV;
}

struct SurfaceOutputPBR
{
  fixed3 Albedo;      // base (diffuse or specular) color
  float3 Normal;      // world space normal, if written
  half3 Emission;
  half Metallic;      // 0=non-metal, 1=metal
  half Roughness;    // 1=rough, 0=smooth
  half Occlusion;     // occlusion (default 1)
  fixed Alpha;        // alpha for transparencies
  #if defined(PBR_SKIN) || defined (VBR_SKIN)
    half Curvature ;
    half Thickness;
  #endif
  #if defined(PBR_HAIR) || defined(PBR_SILK)
    half Anisotropic;
  #endif
   #if defined(PBR_COMMON)
      half Anisotropic;
      half Curvature;
      half IsFace;
      half IsHair;
      half Anisohair;
      half HitIntensity;
      fixed3 HitColor;
    #endif

  #if defined(PBR_HAIRNEW) 
    half Medium,Ascale,Asmooth;
    fixed3 Lightmap;
    half Anisotropic;
  #endif


  //temp
  #if defined(PBR_CHAR)
    half HitIntensity;
      fixed3 HitColor;
  #endif
};

void InitSurfaceOutputPBR(inout SurfaceOutputPBR o)
{
  o.Albedo = fixed3(1.0,1.0,1.0);
  o.Normal = float3(0.0,1.0,0.0);
  o.Emission = half3(0.0,0.0,0.0);
  o.Metallic = 0.0;
  o.Roughness = 1.0;
  o.Occlusion = 1.0; 
  o.Alpha = 1.0;
  #if defined(PBR_SKIN) || defined (VBR_SKIN) 
  o.Curvature = 0.0;
  o.Thickness = 0.0;
  #endif
  #if defined(PBR_HAIR) || defined(PBR_SILK)
  o.Anisotropic = 0.8;
  #endif      
  #if defined(PBR_COMMON)
  o.Anisotropic = 0.8;
  o.Curvature = 0.0;
  o.IsFace =0.0;
  o.IsHair =0.0;
  o.Anisohair =0.0;
  o.HitIntensity = 0.0;
  o.HitColor = fixed3(1.0,1.0,1.0);
  #endif  

  #if defined(PBR_HAIRNEW) 
  o.Medium = 1.0;
  o.Lightmap = fixed3(1.0,1.0,1.0);
  o.Anisotropic = 0.8;
  o.Ascale = 1.0;
  o.Asmooth = 1.0;
  #endif

  #if defined(PBR_CHAR)
o.HitIntensity = 0.0;
  o.HitColor = fixed3(1.0,1.0,1.0);
  #endif
}

//--------------------------------Faliage Anim---------------------------------
 float4 SmoothCurve( float4 x ) 
{
return x * x *( 3.0 - 2.0 * x );
}

float4 TriangleWave( float4 x ) 
{
return abs( frac( x + 0.5 ) * 2.0 - 1.0 );
}

float4 SmoothTriangleWave( float4 x ) 
{
return SmoothCurve( TriangleWave( x ) );
}

// Detail bending
inline float4 AnimateVertex(float4 localPos, float3 localNormal, float3 animParams,float4 windPara,float fDetailAmp,float fBranchAmp ,float fDetailFreq)
{
    // Phases (object, vertex, branch)
    float fObjPhase = dot(unity_ObjectToWorld._14_24_34, 1);//unity_ObjectToWorld._14_24_34 ：模型坐标轴心转换到世界空间下
    float fBranchPhase = fObjPhase + animParams.y;

    float fVtxPhase = dot(localPos.xyz, animParams.x + fBranchPhase);

    // x is used for edges; y is used for branches
    float2 vWavesIn = _Time.yy + float2(fVtxPhase, fBranchPhase );

    // 1.975, 0.793, 0.375, 0.193 are good frequencies
    float4 vWaves = (frac( vWavesIn.xxyy * float4(1.975, 0.793, 0.375, 0.193) ) * 2.0 - 1.0) * fDetailFreq;

    vWaves = SmoothTriangleWave( vWaves );
    float2 vWavesSum = vWaves.xz + vWaves.yw;

    // Edge (xz) and branch bending (y)
    float3 bend = animParams.x * fDetailAmp * localNormal.xyz;
    bend.z = ( animParams.z) * fBranchAmp;
    localPos.xyz += ((vWavesSum.xxy * bend) + (windPara.xyz * vWavesSum.y * ( animParams.z))) * windPara.w;

    return localPos;
}

//--------------------------------Faliage Anim---------------------------------

//--------------------ExponentialHeightFog---------------------------------------
uniform float3 _HeightFogColor;
uniform float _FogDensity;
uniform float _FogHeightFalloff;
uniform float _FogHeight;
uniform float _FogStartDist;
uniform float3 _DirInscatterColor;
uniform float _DirInscatterColorPow;
uniform float _DirInscatterStartDist;


// worldPosToCam = worldPos - _WorldSpaceCameraPos.xyz; 
inline float2 GetExponentialHeightFogFactor(float3 worldPosToCam) 
{
  float worldPosToCamLengthSqr = dot(worldPosToCam, worldPosToCam);
  float worldPosToCamLengthInv = rsqrt(worldPosToCamLengthSqr);
  float worldPosToCamLength = worldPosToCamLengthSqr * worldPosToCamLengthInv;
  float3 worldPosToCamNormalized = worldPosToCam * worldPosToCamLengthInv;

  float RayOriginTerms = _FogDensity * exp2(-_FogHeightFalloff * (_WorldSpaceCameraPos.y - _FogHeight));
  // float RayLength = worldPosToCamLength;
  // float RayDirectionZ = worldPosToCam.y;

  float Falloff = max(-127.0f, _FogHeightFalloff * worldPosToCam.y);    
  float LineIntegral = ( 1.0f - exp2(-Falloff) ) / Falloff;
  float LineIntegralTaylor = log(2.0f) - ( 0.5 * log(2.0f)*log(2.0f)) * Falloff;		
  float ExponentialHeightLineIntegralShared = RayOriginTerms * ( abs(Falloff) > 0.01f ? LineIntegral : LineIntegralTaylor );
  float ExponentialHeightLineIntegral = ExponentialHeightLineIntegralShared * max(worldPosToCamLength - _FogStartDist , 0.0f);
  float ExpFogFactor = saturate(exp2(-ExponentialHeightLineIntegral));

  //DirectianlLightInscattering
  float DirLightInscatter =  pow(saturate(dot(worldPosToCamNormalized , _WorldSpaceLightPos0.xyz)),_DirInscatterColorPow);
  #if !defined(_SHADER_LOD02)
    float DirExponentialHeightLineIntegral = ExponentialHeightLineIntegralShared * max(worldPosToCamLength - _DirInscatterStartDist , 0.0f);
    float DirInscatterFogFactor = saturate(exp2(-DirExponentialHeightLineIntegral));
    DirLightInscatter = DirLightInscatter * (1 - DirInscatterFogFactor);
  #endif

  return float2(DirLightInscatter,ExpFogFactor); 
}

inline half4 GetExponentialHeightFog(float3 worldPosToCam , float2 HeightFogFactor)
{
  //HeightFogFactor
  #if !defined(_SHADER_LOD01) && !defined(_SHADER_LOD02)
    HeightFogFactor = GetExponentialHeightFogFactor(worldPosToCam);
  #endif
  half3 finalColorFog = lerp(_HeightFogColor.rgb,_DirInscatterColor.rgb,HeightFogFactor.x);
  return half4(finalColorFog,HeightFogFactor.y); 
}

inline half3 ApplyHeightFog(float3 worldPosToCam, float2 HeightFogFactor , float3 sceneColor)
{
  half4 fogInscatterAndOpacity = GetExponentialHeightFog(worldPosToCam,HeightFogFactor);
  return lerp(fogInscatterAndOpacity.rgb,sceneColor.rgb,fogInscatterAndOpacity.w);
}

inline half4 ApplyHeightFogColor(float3 worldPosToCam ,float4 sceneColor,float4 fogColor)
{
  float HeightFogFactor = GetExponentialHeightFogFactor(worldPosToCam);
  return lerp(fogColor,sceneColor,HeightFogFactor);
}

inline half4 ApplyHeightFogColorVFX(float HeightFogFactor,float4 sceneColor,float4 fogColor)
{
  return lerp(fogColor,sceneColor,HeightFogFactor);
}

#define APPLY_HEIGHT_FOG(worldPosToCam,HeightFogFactor,col) col.rgb = ApplyHeightFog(worldPosToCam,HeightFogFactor,col.rgb)
#define APPLY_HEIGHT_FOG_COLOR(worldPosToCam,col,fogColor) col = ApplyHeightFogColor(worldPosToCam,col,fogColor)
#define APPLY_HEIGHT_FOG_COLOR_VFX(HeightFogFactor,col,fogColor) col = ApplyHeightFogColorVFX(HeightFogFactor,col,fogColor)


//--------------------ExponentialHeightFog---------------------------------------
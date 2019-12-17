// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

Shader "Hidden/Internal-GUIRoundedRectWithColorPerBorder"
{
    Properties {
        _MainTex ("Texture", any) = "white" {}
        _SrcBlend("SrcBlend", Int) = 5 // SrcAlpha
        _DstBlend("DstBlend", Int) = 10 // OneMinusSrcAlpha
    }

    CGINCLUDE
    #pragma vertex vert
    #pragma fragment frag
    #pragma target 2.5

    #include "UnityCG.cginc"

    struct appdata_t {
        float4 vertex : POSITION;
        fixed4 color : COLOR;
        float2 texcoord : TEXCOORD0;
    };

    struct v2f {
        float4 vertex : SV_POSITION;
        fixed4 color : COLOR;
        float2 texcoord : TEXCOORD0;
        float2 clipUV : TEXCOORD1;
        float4 pos : TEXCOORD2;
    };

    struct corner_t {
        int xBorder;
        int yBorder;
        bool isLeft;
        bool isTop;
        float2 pos;
        float2 center;
        float radius;
        bool containsPoint;
        float2 borderWidths;
        float2 borderIntersectionPos;
    };

    sampler2D _MainTex;
    sampler2D _GUIClipTexture;
    uniform bool _ManualTex2SRGB;
    uniform int _SrcBlend;

    uniform float4 _MainTex_ST;
    uniform float4x4 unity_GUIClipTextureMatrix;

    uniform float _CornerRadiuses[4];
    uniform float _BorderWidths[4];
    uniform float _Rect[4];

    uniform float4 _BorderColors[4];

    static const int LEFT = 1;
    static const int TOP = 2;
    static const int RIGHT = 4;
    static const int BOTTOM = 8;

    half GetCornerAlpha(float2 fragPos, corner_t corner, float pixelScale)
    {
        bool hasBorder = corner.borderWidths.x > 0.0f || corner.borderWidths.y > 0.0f;

        float2 v = fragPos - corner.center;
        float pixelCenterDist = length(v);

        float outRad = corner.radius;
        float outerDist = (pixelCenterDist - outRad) * pixelScale;
        half outerDistAlpha = hasBorder ? saturate(0.5f + outerDist) : 0.0f;

        float a = corner.radius - corner.borderWidths.x;
        float b = corner.radius - corner.borderWidths.y;

        v.y *= a/b;
        half rawDist = (length(v) - a) * pixelScale;
        half alpha = saturate(rawDist + 0.5f);
        half innerDistAlpha = hasBorder ? ((a > 0 && b > 0) ? alpha : 1.0f) : 0.0f;

        return (outerDistAlpha == 0.0f) ? innerDistAlpha : (1.0f - outerDistAlpha);
    }

    bool IsPointInside(float2 p, float4 rect)
    {
        return p.x >= rect.x && p.x <= (rect.x+rect.z) && p.y >= rect.y && p.y <= (rect.y+rect.w);
    }

    v2f vert (appdata_t v)
    {
        float3 eyePos = UnityObjectToViewPos(v.vertex);
        v2f o;
        o.vertex = UnityObjectToClipPos(v.vertex);
        o.color = v.color;
        o.texcoord = TRANSFORM_TEX(v.texcoord,_MainTex);
        o.clipUV = mul(unity_GUIClipTextureMatrix, float4(eyePos.xy, 0, 1.0));
        o.pos = v.vertex;
        return o;
    }

    int GetRegion(float2 fragPos, corner_t corner, bool isPointInCenter)
    {
        if (corner.containsPoint && (corner.radius > 0))
        {
            return (corner.borderWidths.x > 0) * corner.xBorder | (corner.borderWidths.y > 0) * corner.yBorder;
        }

        bool inXBorder = corner.borderWidths.x > 0 && (corner.isLeft ? fragPos.x <= corner.borderIntersectionPos.x : fragPos.x >= corner.borderIntersectionPos.x);
        bool inYBorder = corner.borderWidths.y > 0 && (corner.isTop ? fragPos.y <= corner.borderIntersectionPos.y : fragPos.y >= corner.borderIntersectionPos.y);

         return isPointInCenter ? 0 : inXBorder * corner.xBorder | inYBorder * corner.yBorder;
    }

    int GetBorder(float2 fragPos, corner_t corner, bool isPointInCenter, int upZ)
    {
        int reg = GetRegion(fragPos, corner, isPointInCenter);

        if (reg == (corner.xBorder | corner.yBorder))
        {
            float2 v = upZ * (corner.pos - corner.borderIntersectionPos);
            float3 awayPos = float3(corner.pos + normalize(v) * 100, 0); // Use a point 100 away from the intersection

            return (cross(float3(corner.pos, 0) - awayPos, float3(fragPos, 0) - awayPos)[2] >= 0) ? corner.xBorder : corner.yBorder;
        }

        return reg;
    }

    fixed4 frag (v2f i) : SV_Target
    {
        float pixelScale = 1.0f/abs(ddx(i.pos.x));

        half4 col = tex2D(_MainTex, i.texcoord);
        if (_ManualTex2SRGB)
            col.rgb = LinearToGammaSpace(col.rgb);

        float2 p = i.pos.xy;

        float cornerRadius2 = _CornerRadiuses[0] * 2.0f;
        float middleWidth = _Rect[2] - cornerRadius2;
        float middleHeight = _Rect[3] - cornerRadius2;
        bool xIsLeft = (p.x - _Rect[0] - _Rect[2]/2.0f) <= 0.0f;
        bool yIsTop = (p.y - _Rect[1] - _Rect[3]/2.0f) <= 0.0f;
        int radiusIndex = 0;

        if (xIsLeft)
            radiusIndex = yIsTop ? 0 : 3;
        else
            radiusIndex = yIsTop ? 1 : 2;

        corner_t activeCorner;

        activeCorner.pos = float2(_Rect[0], _Rect[1]);
        activeCorner.xBorder = xIsLeft ? LEFT : RIGHT;
        activeCorner.yBorder = yIsTop ? TOP : BOTTOM;
        activeCorner.isLeft = xIsLeft;
        activeCorner.isTop = yIsTop;
        activeCorner.borderWidths = float2(_BorderWidths[0], _BorderWidths[1]);
        activeCorner.radius = _CornerRadiuses[radiusIndex];
        activeCorner.center = float2(_Rect[0] + activeCorner.radius, _Rect[1] + activeCorner.radius);
        activeCorner.borderIntersectionPos = float2(activeCorner.pos.x + _BorderWidths[0], activeCorner.pos.y + _BorderWidths[1]);

        if (!xIsLeft)
        {
            activeCorner.pos.x = _Rect[0] + _Rect[2];
            activeCorner.center.x = activeCorner.pos.x - activeCorner.radius;
            activeCorner.borderWidths.x = _BorderWidths[2];
            activeCorner.borderIntersectionPos.x = activeCorner.pos.x - activeCorner.borderWidths.x;
        }
        if (!yIsTop)
        {
            activeCorner.pos.y = _Rect[1] + _Rect[3];
            activeCorner.center.y = (activeCorner.pos.y - activeCorner.radius);
            activeCorner.borderWidths.y = _BorderWidths[3];
            activeCorner.borderIntersectionPos.y = activeCorner.pos.y - activeCorner.borderWidths.y;
        }

        activeCorner.containsPoint = (xIsLeft ? p.x <= activeCorner.center.x : p.x >= activeCorner.center.x) && (yIsTop ? p.y <= activeCorner.center.y : p.y >= activeCorner.center.y);

        float4 centerRect = float4(_Rect[0] + _BorderWidths[0], _Rect[1] + _BorderWidths[1], _Rect[2] - (_BorderWidths[0] + _BorderWidths[2]), _Rect[3] - (_BorderWidths[1] + _BorderWidths[3]));
        bool isPointInCenter = IsPointInside(p, centerRect);

        int border = GetBorder(p, activeCorner, isPointInCenter, ((radiusIndex == 0) || (radiusIndex == 2))? 1 : -1);

        if (border == 0)
        {
            col *= i.color;
        }
        else
        {
            int borderIndex = (border == TOP) ? 1 : ((border == RIGHT) ? 2 : ((border == BOTTOM) ? 3 : 0));

            col *= _BorderColors[borderIndex];
        }

        float cornerAlpha = activeCorner.containsPoint ? GetCornerAlpha(p, activeCorner, pixelScale) : 1.0f;
        col.a *= cornerAlpha;

        half middleMask = isPointInCenter ? 0.0f : 1.0f;
        float borderAlpha = activeCorner.containsPoint ? 1.0f : middleMask;
        col.a *= borderAlpha;

        float clipAlpha = tex2D(_GUIClipTexture, i.clipUV).a;
        col.a *= clipAlpha;

        // If the source blend is not SrcAlpha (default) we need to multiply the color by the rounded corner
        // alpha factors for clipping, since it will not be done at the blending stage.
        if (_SrcBlend != 5) // 5 SrcAlpha
        {
            col.rgb *= cornerAlpha * borderAlpha * clipAlpha;
        }
        return col;
    }
    ENDCG

    SubShader {
        Blend [_SrcBlend] [_DstBlend], One OneMinusSrcAlpha
        Cull Off
        ZWrite Off
        ZTest Always

        Pass {
            CGPROGRAM
            ENDCG
        }
    }

    SubShader {
        Blend [_SrcBlend] [_DstBlend]
        Cull Off
        ZWrite Off
        ZTest Always

        Pass {
            CGPROGRAM
            ENDCG
        }
    }

FallBack "Hidden/Internal-GUITextureClip"
}

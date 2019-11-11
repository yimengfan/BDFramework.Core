// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

// Simplified VertexLit Blended Particle shader. Differences from regular VertexLit Blended Particle one:
// - no AlphaTest
// - no ColorMask

Shader "Mobile/Particles/VertexLit Blended" {
Properties {
    _EmisColor ("Emissive Color", Color) = (.2,.2,.2,0)
    _MainTex ("Particle Texture", 2D) = "white" {}
}

Category {
    Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" }
    Blend SrcAlpha OneMinusSrcAlpha
    Cull Off ZWrite Off Fog { Color (0,0,0,0) }

    Lighting On
    Material { Emission [_EmisColor] }
    ColorMaterial AmbientAndDiffuse

    SubShader {
        Pass {
            SetTexture [_MainTex] {
                combine texture * primary
            }
        }
    }
}
}

// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

Shader "Legacy Shaders/Lightmapped/Specular" {
    Properties {
        _Color ("Main Color", Color) = (1,1,1,1)
        _SpecColor ("Specular Color", Color) = (0.5, 0.5, 0.5, 1)
        [PowerSlider(5.0)] _Shininess ("Shininess", Range (0.01, 1)) = 0.078125
        _MainTex ("Base (RGB) Gloss (A)", 2D) = "white" {}
        _LightMap ("Lightmap (RGB)", 2D) = "lightmap" { LightmapMode }
    }
    FallBack "Legacy Shaders/Lightmapped/Diffuse"
}

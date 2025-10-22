Shader "Sprites/RoundedCornerOutline"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _OutlineColor ("Outline Color", Color) = (1,1,1,1)
        _OutlineWidth ("Outline Width", Range(0, 0.5)) = 0.05
        _CornerRadius ("Corner Radius", Range(0, 0.5)) = 0.15
        _RoundedCorners ("Rounded Corners (TL, TR, BR, BL)", Vector) = (1,1,1,1)
        
        [MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        [PerRendererData] _AlphaTex ("External Alpha", 2D) = "white" {}
        [PerRendererData] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
    }
    
    SubShader
    {
        Tags
        { 
            "Queue"="Transparent" 
            "IgnoreProjector"="True" 
            "RenderType"="Transparent" 
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }
        
        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile_instancing
            #pragma multi_compile _ PIXELSNAP_ON
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            
            #include "UnityCG.cginc"
            
            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            
            fixed4 _Color;
            fixed4 _OutlineColor;
            fixed4 _RendererColor;
            float _OutlineWidth;
            float _CornerRadius;
            float4 _RoundedCorners;
            
            v2f vert(appdata IN)
            {
                v2f OUT;
                
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.texcoord = IN.texcoord;
                OUT.color = IN.color * _Color * _RendererColor;
                
                #ifdef PIXELSNAP_ON
                OUT.vertex = UnityPixelSnap(OUT.vertex);
                #endif
                
                return OUT;
            }
            
            // Signed distance function for a rounded rectangle
            float sdRoundedBox(float2 p, float2 b, float4 r)
            {
                // Select the correct radius based on quadrant
                r.xy = (p.x > 0.0) ? r.xy : r.zw;
                r.x = (p.y > 0.0) ? r.x : r.y;
                
                float2 q = abs(p) - b + r.x;
                return min(max(q.x, q.y), 0.0) + length(max(q, 0.0)) - r.x;
            }
            
            fixed4 frag(v2f IN) : SV_Target
            {
                // Transform UV to centered coordinates (-0.5 to 0.5)
                float2 uv = IN.texcoord - 0.5;
                
                // Size of the box (half extents)
                float2 boxSize = float2(0.5, 0.5);
                
                // Corner radii: TopLeft, TopRight, BottomRight, BottomLeft
                // Note: UV space has Y inverted, so we map accordingly
                float4 radii = float4(
                    _RoundedCorners.x * _CornerRadius, // Top-left
                    _RoundedCorners.y * _CornerRadius, // Top-right
                    _RoundedCorners.z * _CornerRadius, // Bottom-right
                    _RoundedCorners.w * _CornerRadius  // Bottom-left
                );
                
                // Calculate outer and inner distance
                float distOuter = sdRoundedBox(uv, boxSize, radii);
                float distInner = sdRoundedBox(uv, boxSize - _OutlineWidth, radii);
                
                // Create outline: area between outer and inner box
                float outline = step(distOuter, 0.0) * (1.0 - step(distInner, 0.0));
                
                // Optional: smooth the edges for anti-aliasing
                float edgeSmooth = 0.01;
                outline = smoothstep(-edgeSmooth, edgeSmooth, -distOuter) * 
                         (1.0 - smoothstep(-edgeSmooth, edgeSmooth, -distInner));
                
                // Apply outline color
                fixed4 col = _OutlineColor * IN.color;
                col.a *= outline;
                
                // Premultiply alpha
                col.rgb *= col.a;
                
                return col;
            }
            ENDCG
        }
    }
    
    Fallback "Sprites/Default"
}

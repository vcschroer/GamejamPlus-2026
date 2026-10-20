Shader "Custom/SpriteHullOutline"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _OutlineColor ("Outline Color", Color) = (1,1,1,1)
        _OutlineThickness ("Outline Thickness", Range(1, 10)) = 5
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
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            fixed4 _Color;
            fixed4 _OutlineColor;
            float _OutlineThickness;
            sampler2D _MainTex;
            float4 _MainTex_TexelSize;

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.texcoord = IN.texcoord;
                OUT.color = IN.color * _Color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                fixed4 texColor = tex2D(_MainTex, IN.texcoord) * IN.color;

                if (texColor.a > 0.1)
                {
                    return texColor;
                }

                float alphaFound = 0;
                float thick = _OutlineThickness;

                for (int x = -6; x <= 6; x++)
                {
                    for (int y = -6; y <= 6; y++)
                    {
                        if (x == 0 && y == 0) continue;

                        float2 offset = float2(x, y) * _MainTex_TexelSize.xy * (thick * 0.4);
                        float neighborAlpha = tex2D(_MainTex, IN.texcoord + offset).a;
                        
                        if (neighborAlpha > 0.1)
                        {
                            alphaFound = 1.0;
                            break;
                        }
                    }
                    if (alphaFound > 0.0) break;
                }

                if (alphaFound > 0.0)
                {
                    return fixed4(_OutlineColor.rgb, _OutlineColor.a);
                }

                return texColor;
            }
            ENDCG
        }
    }
}
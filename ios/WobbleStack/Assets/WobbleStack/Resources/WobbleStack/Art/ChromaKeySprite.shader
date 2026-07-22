Shader "WobbleStack/ChromaKeySprite"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _KeyColor ("Key Color", Color) = (1,0,1,1)
        _Threshold ("Threshold", Range(0,1)) = 0.16
        _Softness ("Softness", Range(0.001,0.3)) = 0.08
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

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _KeyColor;
            float _Threshold;
            float _Softness;

            v2f vert(appdata input)
            {
                v2f output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.uv = input.uv;
                output.color = input.color * _Color;
                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                fixed4 source = tex2D(_MainTex, input.uv);
                float keyDistance = distance(source.rgb, _KeyColor.rgb);
                fixed4 sample = source * input.color;
                sample.a *= smoothstep(_Threshold, _Threshold + _Softness, keyDistance);
                return sample;
            }
            ENDCG
        }
    }
}

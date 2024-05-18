Shader "Custom/ARPlane"
{
    Properties
    {
        _Color("_Color", Color) = (1, 1, 1, 1)
        _DotColor("_DotColor", Color) = (1, 1, 1, 1)
        _Ratio("Ratio", float) = 1
        _Size("Size", float) = 0.3
    }
    SubShader
    {
        Blend SrcAlpha OneMinusSrcAlpha
        Pass{
        Tags {"Queue" = "Transparent" "RenderType"="Transparent" }
        LOD 200

        CGPROGRAM
        #pragma vertex vert
        #pragma fragment frag
        #include "UnityCG.cginc"

        struct appdata
        {
            float4 vertex : POSITION;
            float2 uv : TEXCOORD0;
        };

        struct v2f
        {
            float4 position : SV_POSITION;
            float2 uv : TEXCOORD0;
        };

        v2f vert(appdata input)
        {
            v2f output;
            output.position = UnityObjectToClipPos(input.vertex);
            output.uv = input.uv;
            return output;
        }

        float _Ratio;
        float _Size;
        fixed4 _Color;
        fixed4 _DotColor;

        fixed4 frag (v2f input) :  SV_Target
        {
            float2 uv = input.uv * _Ratio;
            float2 st = frac(uv);
            fixed d = distance(st, fixed2(0.5, 0.5));
            fixed4 col = d < _Size ? _DotColor : _Color;
            
            return col;
        }
        ENDCG
        }
    }
}
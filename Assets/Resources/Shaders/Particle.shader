// Soft additive particle: radial falloff computed from UVs (no texture needed), tinted by vertex colour.
Shader "GravityLab/Particle"
{
    Properties
    {
        _Intensity ("Intensity", Float) = 1.5
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        LOD 100

        Pass
        {
            Blend One One
            ZWrite Off
            Cull Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            half _Intensity;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float d = length(i.uv - 0.5) * 2.0;
                half a = saturate(1.0 - d);
                a = a * a;
                return fixed4(i.color.rgb * a * i.color.a * _Intensity, 1.0);
            }
            ENDCG
        }
    }
    FallBack Off
}

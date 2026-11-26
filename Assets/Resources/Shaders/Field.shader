// Energy (dissolve) field: additive shimmering curtain with scrolling scan lines, sparkle noise and bright edges.
// _Power fades the whole effect when the field is switched off.
Shader "GravityLab/Field"
{
    Properties
    {
        _Color ("Color", Color) = (0.35,0.75,1,1)
        _Power ("Power", Range(0,1)) = 1
    }

    SubShader
    {
        Tags { "Queue"="Transparent+10" "RenderType"="Transparent" "IgnoreProjector"="True" }
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

            fixed4 _Color;
            half _Power;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float t = _Time.y;
                float along = i.worldPos.x + i.worldPos.z;
                float scan = frac(i.worldPos.y * 1.6 - t * 0.7 + along * 0.15);
                half lines = smoothstep(0.0, 0.08, scan) * (1.0 - smoothstep(0.12, 0.3, scan));

                float2 cell = floor(float2(along, i.worldPos.y) * 14.0);
                half sparkle = step(0.985, Hash21(cell + floor(t * 9.0)));

                float2 e = abs(i.uv - 0.5) * 2.0;
                half edge = smoothstep(0.86, 1.0, max(e.x, e.y));

                half wave = 0.5 + 0.5 * sin(along * 3.0 + t * 2.5 + i.worldPos.y * 1.3);
                half intensity = 0.07 + lines * 0.32 + sparkle * 0.9 + edge * 0.6 + wave * 0.05;
                return fixed4(_Color.rgb * intensity * _Power, 1.0);
            }
            ENDCG
        }
    }
    FallBack Off
}

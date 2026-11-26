// Lab glass: transparent tint whose opacity and brightness rise at grazing angles (fresnel), diagonal fake-reflection
// streaks in world space and a sharp specular glint from the key light.
Shader "GravityLab/Glass"
{
    Properties
    {
        _Color ("Tint (A = base opacity)", Color) = (0.7,0.88,1,0.1)
        _FresnelColor ("Fresnel Color", Color) = (0.85,0.95,1,1)
        _StreakStrength ("Streak Strength", Range(0,1)) = 0.18
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        LOD 100

        Pass
        {
            Tags { "LightMode"="ForwardBase" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "UnityCG.cginc"
            #include "Lighting.cginc"

            fixed4 _Color;
            fixed4 _FresnelColor;
            half _StreakStrength;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
                UNITY_FOG_COORDS(2)
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                UNITY_TRANSFER_FOG(o, o.pos);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 n = normalize(i.worldNormal);
                float3 v = normalize(_WorldSpaceCameraPos.xyz - i.worldPos);
                float3 l = normalize(_WorldSpaceLightPos0.xyz);
                half ndv = saturate(abs(dot(n, v)));
                half fres = pow(1.0 - ndv, 3.0);

                float s = frac((i.worldPos.x + i.worldPos.z + i.worldPos.y * 0.7) * 0.33);
                half streak = (smoothstep(0.0, 0.03, s) * (1.0 - smoothstep(0.05, 0.11, s))
                            + smoothstep(0.15, 0.17, s) * (1.0 - smoothstep(0.18, 0.2, s)) * 0.6) * _StreakStrength;

                float3 h = normalize(l + v);
                half spec = pow(saturate(dot(n, h)), 90.0) * 0.8;

                fixed3 col = _Color.rgb + _FresnelColor.rgb * fres * 0.5 + streak + spec * _LightColor0.rgb;
                half alpha = saturate(_Color.a + fres * 0.45 + streak * 0.8 + spec);
                fixed4 result = fixed4(col, alpha);
                UNITY_APPLY_FOG(i.fogCoord, result);
                return result;
            }
            ENDCG
        }
    }
    FallBack Off
}

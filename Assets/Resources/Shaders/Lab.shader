// Clean sci-fi lab surface for the built-in pipeline.
// Soft half-lambert lighting with tinted shade, received soft shadows, SH ambient, a procedural world-space panel grid,
// fresnel-faked reflections (gradient environment + ceiling light streak), blinn gloss, and per-object effects driven
// by MaterialPropertyBlocks: held glow, stasis (frozen) tint, and a noise dissolve with a glowing edge.
Shader "GravityLab/Lab"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (A = emission mask)", 2D) = "white" {}
        _UVOffset ("UV Offset", Vector) = (0,0,0,0)
        _UseGrid ("World Grid", Float) = 1
        _GridColor ("Grid Color", Color) = (0.62,0.67,0.75,1)
        _GridSize ("Panel Size", Float) = 2
        _ShadeColor ("Shade Color", Color) = (0.66,0.71,0.82,1)
        _FresnelColor ("Fresnel Color", Color) = (0.78,0.9,1,1)
        _FresnelStrength ("Fresnel Strength", Range(0,1)) = 0.35
        _Gloss ("Gloss", Range(0,1)) = 0.35
        _EmissionColor ("Emission", Color) = (0,0,0,0)
        _TexEmission ("Texture Emission", Color) = (0,0,0,0)
        _Glow ("Glow", Range(0,1)) = 0
        _GlowColor ("Glow Color", Color) = (1,0.55,0.15,1)
        _Frozen ("Frozen", Range(0,1)) = 0
        _Dissolve ("Dissolve", Range(0,1)) = 0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 200

        CGINCLUDE
        float LabHash3(float3 p)
        {
            p = frac(p * 0.3183099 + 0.1);
            p *= 17.0;
            return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
        }

        float LabNoise3(float3 x)
        {
            float3 i = floor(x);
            float3 f = frac(x);
            f = f * f * (3.0 - 2.0 * f);
            float a = lerp(LabHash3(i + float3(0,0,0)), LabHash3(i + float3(1,0,0)), f.x);
            float b = lerp(LabHash3(i + float3(0,1,0)), LabHash3(i + float3(1,1,0)), f.x);
            float c = lerp(LabHash3(i + float3(0,0,1)), LabHash3(i + float3(1,0,1)), f.x);
            float d = lerp(LabHash3(i + float3(0,1,1)), LabHash3(i + float3(1,1,1)), f.x);
            return lerp(lerp(a, b, f.y), lerp(c, d, f.y), f.z);
        }
        ENDCG

        Pass
        {
            Tags { "LightMode"="ForwardBase" }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase
            #pragma multi_compile_fog
            #pragma target 3.0

            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #include "AutoLight.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _UVOffset;
            fixed4 _Color;
            half _UseGrid;
            fixed4 _GridColor;
            float _GridSize;
            fixed4 _ShadeColor;
            fixed4 _FresnelColor;
            half _FresnelStrength;
            half _Gloss;
            fixed4 _EmissionColor;
            fixed4 _TexEmission;
            half _Glow;
            fixed4 _GlowColor;
            half _Frozen;
            half _Dissolve;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
                float3 worldNormal : TEXCOORD2;
                float3 objPos : TEXCOORD3;
                SHADOW_COORDS(4)
                UNITY_FOG_COORDS(5)
                half3 sh : TEXCOORD6;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex) + _UVOffset.xy;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.objPos = v.vertex.xyz;
                o.sh = ShadeSH9(float4(o.worldNormal, 1.0));
                TRANSFER_SHADOW(o);
                UNITY_TRANSFER_FOG(o, o.pos);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Dissolve (only props ever set it above zero).
                float dn = LabNoise3(i.objPos * 7.0);
                clip(dn - _Dissolve);
                float dissolveEdge = (1.0 - smoothstep(0.0, 0.07, dn - _Dissolve)) * step(0.001, _Dissolve);

                float3 n = normalize(i.worldNormal);
                fixed4 tex = tex2D(_MainTex, i.uv);
                fixed3 albedo = _Color.rgb * tex.rgb;

                // World-space panel grid: pick the projection plane from the dominant normal axis.
                float3 an = abs(n);
                float2 g = (an.x > an.y && an.x > an.z) ? i.worldPos.zy : ((an.y > an.z) ? i.worldPos.xz : i.worldPos.xy);
                float2 cell = g / _GridSize;
                float2 fw = max(fwidth(cell), 1e-5);
                float2 f = abs(frac(cell - 0.5) - 0.5);
                float2 seam = 1.0 - smoothstep(fw * 0.5, fw * 1.5 + 0.008, f);
                float2 cell4 = cell * 4.0;
                float2 fw4 = max(fwidth(cell4), 1e-5);
                float2 f4 = abs(frac(cell4 - 0.5) - 0.5);
                float2 fine = 1.0 - smoothstep(fw4 * 0.5, fw4 * 1.5, f4);
                float fineMask = max(fine.x, fine.y) * 0.3 * saturate(1.0 - max(fw4.x, fw4.y) * 3.0);
                float gridMask = saturate(max(seam.x, seam.y) + fineMask) * _UseGrid;
                float panelHash = frac(sin(dot(floor(cell), float2(12.9898, 78.233))) * 43758.5453);
                albedo *= lerp(1.0, 0.965 + 0.035 * panelHash, _UseGrid);
                albedo = lerp(albedo, albedo * _GridColor.rgb, gridMask);

                float3 l = normalize(_WorldSpaceLightPos0.xyz);
                float3 v = normalize(_WorldSpaceCameraPos.xyz - i.worldPos);
                UNITY_LIGHT_ATTENUATION(atten, i, i.worldPos);

                half ndl = dot(n, l) * 0.5 + 0.5;
                half wrap = smoothstep(0.3, 0.9, ndl);
                half lit = min(wrap, lerp(0.15, 1.0, atten));
                fixed3 diffuse = lerp(_ShadeColor.rgb, fixed3(1,1,1), lit);
                fixed3 col = albedo * (diffuse * _LightColor0.rgb * 0.8 + i.sh * 0.75);

                // Faked reflection: vertical gradient environment plus a bright ceiling-light streak.
                half ndv = saturate(dot(n, v));
                float3 r = reflect(-v, n);
                fixed3 env = _FresnelColor.rgb * (0.55 + 0.45 * saturate(r.y * 0.5 + 0.5));
                env += smoothstep(0.82, 0.97, r.y) * 0.35;
                half fres = pow(1.0 - ndv, 4.0);
                col = lerp(col, env, saturate(fres * _FresnelStrength + _Gloss * 0.06));

                float3 h = normalize(l + v);
                half spec = pow(saturate(dot(n, h)), 64.0) * _Gloss * lit;
                col += spec * _LightColor0.rgb * 0.6;

                col += _EmissionColor.rgb + _TexEmission.rgb * tex.a;

                // Stasis tint.
                half rim2 = pow(1.0 - ndv, 2.0);
                col = lerp(col, col * fixed3(0.55, 0.85, 1.1) + fixed3(0.05, 0.16, 0.26), _Frozen);
                col += fixed3(0.3, 0.85, 1.0) * rim2 * _Frozen;

                // Held glow.
                col += _GlowColor.rgb * (rim2 * 1.1 + 0.06) * _Glow;

                col += fixed3(0.45, 0.85, 1.0) * 2.5 * dissolveEdge;

                fixed4 result = fixed4(col, 1.0);
                // soft highlight shoulder: bright surfaces roll off instead of clipping to flat white
                { half3 over = max(result.rgb - 0.78, 0.0); result.rgb = result.rgb - over + over / (1.0 + over * 3.0); }
                UNITY_APPLY_FOG(i.fogCoord, result);
                return result;
            }
            ENDCG
        }

        Pass
        {
            Tags { "LightMode"="ShadowCaster" }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_shadowcaster
            #include "UnityCG.cginc"

            half _Dissolve;

            struct v2f
            {
                V2F_SHADOW_CASTER;
                float3 objPos : TEXCOORD1;
            };

            v2f vert (appdata_base v)
            {
                v2f o;
                TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
                o.objPos = v.vertex.xyz;
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                clip(LabNoise3(i.objPos * 7.0) - _Dissolve);
                SHADOW_CASTER_FRAGMENT(i)
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}

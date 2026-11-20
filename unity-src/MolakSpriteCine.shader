Shader "Molak/SpriteCine"
{
    // 0.4.82 그림 화면의 인물: 제 빛 그대로 일반 알파. 다만 그림 가장자리에 남은 아주 옅은 알파(오리고 남은 네모 자국)는 지운다.
    // _Quiet(0~1): 말하지 않는 동안 — 흐리게(둘레 12점 표본), 어둡게, 빛을 조금 뺀다. 말하면 0 으로 돌아온다.
    Properties { _MainTex ("Sprite Texture", 2D) = "white" {} _Color ("Tint", Color) = (1,1,1,1) _Cut ("Cut", Range(0,0.5)) = 0.12 _Quiet ("Quiet", Range(0,1)) = 0 _BlurPx ("Blur px", Float) = 5 }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
        Cull Off Lighting Off ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            static const float2 k1[6] = { float2(1,0), float2(0.5,0.866), float2(-0.5,0.866), float2(-1,0), float2(-0.5,-0.866), float2(0.5,-0.866) };
            static const float2 k2[6] = { float2(0.866,0.5), float2(0,1), float2(-0.866,0.5), float2(-0.866,-0.5), float2(0,-1), float2(0.866,-0.5) };
            sampler2D _MainTex; float4 _MainTex_TexelSize; fixed4 _Color; float _Cut; float _Quiet; float _BlurPx;
            struct appdata_t { float4 vertex : POSITION; float4 color : COLOR; float2 texcoord : TEXCOORD0; };
            struct v2f { float4 vertex : SV_POSITION; fixed4 color : COLOR; float2 texcoord : TEXCOORD0; };
            v2f vert (appdata_t IN) { v2f OUT; OUT.vertex = UnityObjectToClipPos(IN.vertex); OUT.texcoord = IN.texcoord; OUT.color = IN.color * _Color; return OUT; }
            fixed4 frag (v2f IN) : SV_Target
            {
                fixed4 t = tex2D(_MainTex, IN.texcoord);
                if (_Quiet > 0.001)
                {
                    float2 r = _MainTex_TexelSize.xy * _BlurPx * _Quiet;
                    float4 acc = float4(t.rgb * t.a, t.a) * 2.0; float wsum = 2.0;
                    // 안쪽 고리 6점 + 바깥 고리 6점(엇갈려)
                    for (int i = 0; i < 6; i++)
                    {
                        float4 a = tex2D(_MainTex, IN.texcoord + k1[i] * r * 0.5); a.rgb *= a.a; acc += a * 1.5; wsum += 1.5;
                        float4 b = tex2D(_MainTex, IN.texcoord + k2[i] * r);       b.rgb *= b.a; acc += b;       wsum += 1.0;
                    }
                    float al = acc.a / wsum;
                    float3 col = al > 0.001 ? (acc.rgb / wsum) / al : t.rgb;
                    float lum = dot(col, float3(0.3, 0.59, 0.11));
                    col = lerp(col, lum.xxx, 0.5 * _Quiet) * lerp(1.0, 0.5, _Quiet);   // 0.4.85 언메이커: 흐림 없이 더 어둡게(밝기 약 45%, 빛 절반)
                    t = float4(col, lerp(t.a, al, _Quiet));
                }
                fixed4 o; o.rgb = t.rgb * IN.color.rgb;
                o.a = saturate((t.a - _Cut) / (1.0 - _Cut)) * IN.color.a;
                return o;
            }
            ENDCG
        }
    }
}

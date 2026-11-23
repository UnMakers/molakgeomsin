Shader "Molak/TitleWater"
{
    // 0.5.0 타이틀 바탕: 수평선 아래(물)만 일렁인다(언메이커: 그림은 물이 떨리는데 실제로 떨리게). 하늘·인물은 그대로.
    //  _Horizon: 수평선(그림 위에서부터 0~1) · _Feet: 물에 선 사람의 발끝(그림 좌표, 위에서부터) · _Phase: 흐른 시간(초)
    //  잔물결(수평선 쪽으로 갈수록 촘촘)이 옆으로 흔들리고, 발끝에서 동심원이 천천히 퍼진다. 발 바로 둘레는 건드리지 않는다.
    Properties { _MainTex ("Texture", 2D) = "white" {} _Color ("Tint", Color) = (1,1,1,1) _Horizon ("Horizon", Float) = 0.879 _Feet ("Feet", Vector) = (0.312, 0.879, 0, 0) _Phase ("Phase", Float) = 0 _Amp ("Amp", Float) = 1 _Aspect ("Aspect", Float) = 0.684 }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
        Cull Off Lighting Off ZWrite Off ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex; fixed4 _Color; float _Horizon; float4 _Feet; float _Phase; float _Amp; float _Aspect;
            struct appdata_t { float4 vertex : POSITION; float2 texcoord : TEXCOORD0; };
            struct v2f { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; };
            v2f vert (appdata_t IN) { v2f OUT; OUT.vertex = UnityObjectToClipPos(IN.vertex); OUT.uv = IN.texcoord; return OUT; }
            fixed4 frag (v2f IN) : SV_Target
            {
                float2 uv = IN.uv;
                float y = 1.0 - uv.y;                         // 위에서부터
                float below = y - _Horizon;
                float glint = 0.0;
                if (below > 0.0)
                {
                    float d = below / max(0.001, 1.0 - _Horizon);      // 0 수평선 … 1 화면 아래
                    float edge = smoothstep(0.0, 0.035, below);        // 수평선 이음매
                    // 잔물결: 멀수록 촘촘, 가까울수록 크게
                    float ph = 5.5 / (d + 0.07);
                    float w1 = sin(ph + _Phase * 1.15 + uv.x * 9.0);
                    float w2 = sin(ph * 1.7 - _Phase * 0.8 + uv.x * 23.0);
                    float2 off = float2((w1 * 0.0032 + w2 * 0.0013) * (0.25 + d), w1 * 0.0016 * d);
                    // 발끝에서 퍼지는 동심원(위아래로 납작한 타원)
                    float2 p = float2((uv.x - _Feet.x) * _Aspect, (y - _Feet.y) * 3.2);
                    float r = length(p);
                    float ring = sin(r * 70.0 - _Phase * 1.9) * exp(-r * 4.5) * smoothstep(0.035, 0.11, r);
                    float2 dir = r > 0.0001 ? p / r : float2(0, 0);
                    off += float2(dir.x, -dir.y * 0.35) * ring * 0.0045;
                    uv += off * edge * _Amp;
                    glint = (ring * 0.035 + w2 * 0.012 * d) * edge * _Amp;
                }
                fixed4 c = tex2D(_MainTex, uv);
                c.rgb = saturate(c.rgb + glint);
                return c * _Color;
            }
            ENDCG
        }
    }
}

// 0.4.85 장면 전환 「먹이 번진다」(시안 2).
// _MainTex 의 알파 = 1 - m (가운데가 높다). 두 겹의 앞선:
//   _Cut  — 먹의 앞선. 알파가 이보다 높은 곳은 먹이 덮었다(옛 그림 위).
//   _Cut2 — 새 그림의 앞선(뒤따라온다). 이보다 높은 곳은 새 그림이 드러나고, 그 안쪽 _Soft 만큼 먹이 마르듯 걷힌다.
//   먹 앞선 바로 바깥은 물이 먼저 스며든 것처럼 옛 그림을 조금 어둡게.
Shader "Molak/InkEdge"
{
    Properties
    {
        [PerRendererData] _MainTex ("Mask", 2D) = "white" {}
        _Color ("Ink", Color) = (0.055, 0.047, 0.043, 1)
        _Cut ("Ink front", Float) = 1
        _Cut2 ("Picture front", Float) = 1
        _Soft ("Dry width", Range(0.01,0.5)) = 0.16
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
        Cull Off ZWrite Off Lighting Off
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex; fixed4 _Color; float _Cut, _Cut2, _Soft;
            struct a2v { float4 vertex : POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };
            v2f vert (a2v v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = v.uv; o.color = v.color; return o; }
            fixed4 frag (v2f i) : SV_Target
            {
                float a = tex2D(_MainTex, i.uv).a;
                float inkCov = smoothstep(-0.010, 0.006, a - _Cut);              // 먹이 덮은 곳
                float dry = smoothstep(0.0, _Soft, a - _Cut2); dry = dry * dry;   // 처음엔 천천히 걷혀 먹 테두리가 남는다                    // 새 그림 쪽으로 마르며 걷힌다
                float k = inkCov * (1.0 - dry);
                // 먹의 농담: 번지는 테두리에 먹이 고여 가장 짙고, 안쪽은 아주 조금 묽다
                float dens = lerp(1.0, 0.975, saturate((a - _Cut - 0.03) / 0.3));   // 리니어 색 공간이라 0.9 만 줘도 옛 그림이 훤히 비쳤다
                float wet = (1.0 - inkCov) * smoothstep(-0.06, 0.0, a - _Cut) * 0.5;   // 앞선 바깥: 스며든 물기
                float alpha = max(k * dens, wet);
                return fixed4(_Color.rgb, _Color.a * alpha * i.color.a);
            }
            ENDCG
        }
    }
}

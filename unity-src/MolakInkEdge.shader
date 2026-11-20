// 0.4.85 장면 전환 「먹이 번진다」(시안 2): 앞선 하나. 가운데서 번져 나오는 새 그림의 끝에 짙은 먹, 그 바로 바깥 옛 그림은 물기에 어둑해진다.
// _MainTex 알파 = 1 - m (가운데가 높다). d = 알파 - _Cut. d > 0 이면 새 그림 쪽(옛 그림은 Molak/InkOld 가 걷는다).
//   0 < d < _W   : 먹 → 새 그림 (1 - k²)
//   -_Halo < d < 0 : 옛 그림을 어둡게(시안의 45% 어둡게 — 리니어 색 공간이라 0.7)
Shader "Molak/InkEdge"
{
    Properties
    {
        [PerRendererData] _MainTex ("Mask", 2D) = "white" {}
        _Color ("Ink", Color) = (0.063, 0.051, 0.047, 1)
        _Cut ("Front", Float) = 1
        _W ("Ink width", Float) = 0.035
        _Halo ("Wet width", Float) = 0.05
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
            sampler2D _MainTex; fixed4 _Color; float _Cut, _W, _Halo;
            struct a2v { float4 vertex : POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };
            v2f vert (a2v v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = v.uv; o.color = v.color; return o; }
            fixed4 frag (v2f i) : SV_Target
            {
                float d = tex2D(_MainTex, i.uv).a - _Cut;
                // 시안은 sRGB 에서 섞었다 — 리니어 색 공간에서 같은 결이 나오게 2.2 제곱으로 맞춘다
                float k = saturate(d / _W);
                float ink = 1.0 - pow(k * k, 2.2);                                   // 먹 → 새 그림
                float q = saturate(1.0 + d / _Halo);
                float wet = 1.0 - pow(1.0 - 0.45 * q, 2.2);                          // 옛 그림을 45% 어둡게
                float alpha = d > 0 ? ink : wet;
                return fixed4(_Color.rgb, _Color.a * alpha * i.color.a);
            }
            ENDCG
        }
    }
}

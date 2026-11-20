// 0.4.85 장면 전환 「먹이 번진다」(시안 2): 다음 그림이 번져 나오는 테두리에 짙은 먹을 두른다.
// _MainTex 의 알파 = 1 - m (가운데가 높다). 번지는 끝은 m = 1 - _Cut. 그 둘레 _Width 만큼 먹.
Shader "Molak/InkEdge"
{
    Properties
    {
        [PerRendererData] _MainTex ("Mask", 2D) = "white" {}
        _Color ("Ink", Color) = (0.067, 0.059, 0.055, 1)
        _Cut ("Cut", Range(0,1)) = 1
        _Width ("Width", Range(0.001,0.3)) = 0.06
        _Inner ("Inner", Range(0,1)) = 0.55
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
            sampler2D _MainTex; fixed4 _Color; float _Cut, _Width, _Inner;
            struct a2v { float4 vertex : POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };
            v2f vert (a2v v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = v.uv; o.color = v.color; return o; }
            fixed4 frag (v2f i) : SV_Target
            {
                float a = tex2D(_MainTex, i.uv).a;          // 1 - m
                float d = a - _Cut;                          // >0 이면 이미 새 그림 쪽
                float outer = smoothstep(-_Width * 0.25, 0.0, d);              // 바깥(옛 그림 쪽)으로는 짧게 번짐
                float inner = 1.0 - smoothstep(0.0, _Width, d);               // 안쪽(새 그림 쪽)으로 길게 스러짐
                float k = outer * inner;
                k = k * lerp(1.0, _Inner, saturate(d / _Width));
                return fixed4(_Color.rgb, _Color.a * k * i.color.a);
            }
            ENDCG
        }
    }
}

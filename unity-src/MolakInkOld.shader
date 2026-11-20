// 0.4.85 장면 전환 「먹이 번진다」: 옛 그림을 새 그림 위에 얹고, 새 그림이 번져 나온 자리만 걷어 낸다.
// (SpriteMask 의 정렬 범위로 새 그림을 가리던 방식은 새 그림이 처음부터 다 보여 효과만 따로 놀았다.)
// _Reveal 의 알파 = 1 - m (가운데가 높다). 알파가 _Cut2 보다 높은 곳은 새 그림이 드러났다 → 옛 그림을 지운다.
// _Band = (왼쪽 아래 x, y, 너비, 높이) 월드 좌표의 그림 창. 창 밖은 그리지 않는다.
Shader "Molak/InkOld"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _Reveal ("Reveal", 2D) = "white" {}
        _Cut2 ("Picture front", Float) = 1
        _Band ("Band", Vector) = (0,0,1,1)
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
            sampler2D _MainTex, _Reveal; float _Cut2; float4 _Band;
            struct a2v { float4 vertex : POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float2 buv : TEXCOORD1; fixed4 color : COLOR; };
            v2f vert (a2v v)
            {
                v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = v.uv; o.color = v.color;
                float3 w = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.buv = (w.xy - _Band.xy) / _Band.zw;
                return o;
            }
            fixed4 frag (v2f i) : SV_Target
            {
                if (i.buv.x < 0 || i.buv.x > 1 || i.buv.y < 0 || i.buv.y > 1) discard;
                fixed4 c = tex2D(_MainTex, i.uv) * i.color;
                float a = tex2D(_Reveal, i.buv).a;
                c.a *= 1.0 - smoothstep(_Cut2 - 0.006, _Cut2 + 0.006, a);
                return c;
            }
            ENDCG
        }
    }
}

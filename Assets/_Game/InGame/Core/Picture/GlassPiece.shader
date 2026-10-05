// Kính vệt (wispy): giữ nguyên tông màu mảnh, chỉ sáng/tối/đậm/nhạt theo vệt; mỗi mảnh có độ tương phản riêng (uv2.x)
Shader "Falcon/GlassPiece"
{
    Properties
    {
        _MainTex ("Wispy (R streak, G tone, B fiber)", 2D) = "white" {}
        _Facets ("Facets (R brightness, G ridge, B tint)", 2D) = "gray" {}
        _Flat ("Flat (1 = no glass)", Float) = 0
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            sampler2D _Facets;
            float _Flat;

            struct appdata { float4 vertex : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; float2 uv2 : TEXCOORD1; };
            struct v2f { float4 pos : SV_POSITION; fixed4 color : COLOR; float2 uv : TEXCOORD0; float contrast : TEXCOORD1; float facet : TEXCOORD2; };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.color = v.color;
                o.uv = v.uv;
                o.contrast = v.uv2.x;
                o.facet = v.uv2.y;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 col = i.color.rgb;
                if (_Flat > 0.5) return fixed4(col, i.color.a);

                // 2 lần lấy mẫu khác tỉ lệ/góc rồi trộn: vân lặp theo cỡ cố định nhưng mảnh to không lộ nhịp lặp
                float2 uvB = float2(i.uv.x * 0.62 - i.uv.y * 0.21 + 0.37, i.uv.y * 0.62 + i.uv.x * 0.21 + 0.11);
                float4 ta = tex2D(_MainTex, i.uv);
                float4 tb = tex2D(_MainTex, uvB);
                float3 t = 0.5 * (ta.rgb + tb.rgb);
                // gân mảnh lấy mẫu riêng ở tỉ lệ nhỏ hơn: cỡ gân cố định, mảnh to thì nhiều gân hơn, không phóng theo mảnh
                float2 uvF = float2(i.uv.x * 1.35 + i.uv.y * 0.25 + 0.21, i.uv.y * 1.35 - i.uv.x * 0.25 + 0.63);
                float hair = max(tex2D(_MainTex, uvF).a, ta.a * 0.5);
                t = 0.5 + (t - 0.5) * 1.35; // trộn làm giảm tương phản: kéo lại
                float c = i.contrast * 0.25;
                float lum = dot(col, float3(0.299, 0.587, 0.114));

                col = lerp(float3(lum, lum, lum), col, 1.0 + 0.17 * (1.0 - saturate(lum - 0.35)));     // rực hơn, nhất là màu nhạt
                float whiteMask = smoothstep(0.80, 0.97, lum) * (1.0 - saturate((max(col.r, max(col.g, col.b)) - min(col.r, min(col.g, col.b))) * 6.0));
                float wisp = smoothstep(0.42, 0.80, t.r);                        // vệt sáng hơn, cùng tông
                float chroma = max(col.r, max(col.g, col.b)) - min(col.r, min(col.g, col.b));
                float deep = smoothstep(0.40, 0.08, t.r) * smoothstep(0.45, 0.80, lum) * saturate((chroma - 0.12) * 6.0); // mảng đậm: rất nhẹ, mảnh tối không có đốm đen
                col = col + (1.0 - col) * wisp * 0.30 * c * (0.35 + 0.65 * saturate(chroma * 5.0)) * (0.30 + 0.70 * saturate(lum * 1.6)); // mảnh tối: vệt kín hơn, không bị 'sữa'
                col = pow(saturate(col), 1.0 + deep * 0.45 * c);                // chỗ đậm: màu sâu và rực hơn, không xám đi
                col *= 1.0 + (t.g - 0.5) * 0.14 * c;                              // loang tông rất rộng
                col.r *= 1.0 + (t.g - 0.5) * 0.10 * c;                            // trôi màu nhẹ (vàng ↔ cam)
                col.b *= 1.0 - (t.g - 0.5) * 0.20 * c;
                                              // gân sáng mảnh, sắc
                col = lerp(col, col * float3(0.94, 1.0, 0.97), whiteMask);          // kính trắng: hơi xanh lục như kính trong
                // mặt cắt kiểu kính vò/xà cừ: mức độ theo từng mảnh (facet), sáng tối từng mặt, gờ sáng, ánh pastel (mảnh sáng) hoặc bóng phản chiếu (mảnh tối)
                float w = i.facet;
                if (w > 0.01)
                {
                    float2 uvK = float2(i.uv.x * 0.30 + i.uv.y * 0.07, i.uv.y * 0.30 - i.uv.x * 0.07);
                    float3 f = tex2D(_Facets, uvK + 0.31).rgb;
                    float lumNow = dot(col, float3(0.299, 0.587, 0.114));
                    col *= 1.0 + (f.r - 0.5) * 0.58 * w * (1.0 - 0.45 * smoothstep(0.7, 0.95, lumNow));
                                        float3 tint = 0.5 + 0.5 * cos(6.2832 * (f.b + float3(0.0, 0.33, 0.67)));
                    col = lerp(col, col * (0.90 + 0.20 * tint), w * 0.14 * smoothstep(0.55, 0.85, lumNow) * (1.0 - whiteMask));   // ánh ngũ sắc nhạt trên mảnh sáng
                    float spec = smoothstep(0.62, 0.98, f.r);
                    col += spec * w * 0.16 * (1.0 - smoothstep(0.25, 0.65, lumNow));                           // bóng phản chiếu trên mảnh tối
                }
                return fixed4(saturate(col), i.color.a);
            }
            ENDCG
        }
    }
}

Shader "FadeinShader"
{
    Properties
    {
        _Color ("Color", Color) = (1, 1, 1, 1)
        _MainTex ("Albedo", 2D) = "white" {}
        _Far ("Far", Range(.1,10)) = 1.5
        _Near ("Near", Range(.1, 10)) = 1
    }
    SubShader
    {
        Pass
        {
            Tags
            {
                "Queue" = "Transparent"
                "RenderType" = "Transparent"
            }
            LOD 200

            Blend SrcAlpha OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            fixed4 _Color;
            sampler2D _MainTex;
            float4 _MainTex_ST;
            half _Far;
            half _Near;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
                float4 tangent : TANGENT;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                half depth : TEXCOORD1;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                COMPUTE_EYEDEPTH(o.depth.x);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, i.uv) * _Color;
                float v = 1.0F - i.depth.x + _Near / _Far;
                v = saturate(c.a + v);
                c.a = c.a < v ? c.a : v;

                return c;
            }
            ENDCG
        }
    }
}
Shader "Custom/FogDome"
{
    Properties
    {
        _Color ("Fog Color", Color) = (0.5, 0.6, 0.7, 1)
        _FadeStart ("Fade Start Distance", Float) = 20
        _FadeEnd ("Fade End Distance", Float) = 100
        _VerticalFadeStart ("Vertical Fade Start (Y)", Float) = 0
        _VerticalFadeEnd ("Vertical Fade End (Y)", Float) = 50
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 200

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Front

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
            };

            fixed4 _Color;
            float _FadeStart;
            float _FadeEnd;
            float _VerticalFadeStart;
            float _VerticalFadeEnd;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 camPos = _WorldSpaceCameraPos;

                // Radial distance from camera
                float dist = distance(i.worldPos, camPos);
                float fadeRadial = saturate((dist - _FadeStart) / (_FadeEnd - _FadeStart));

                // Vertical fade based on Y position
                float fadeVertical = saturate((i.worldPos.y - _VerticalFadeStart) / (_VerticalFadeEnd - _VerticalFadeStart));

                // Combine fades (you can use max, min, multiply, etc.)
                float alpha = fadeRadial * fadeVertical;

                return fixed4(_Color.rgb, alpha);
            }
            ENDCG
        }
    }
    FallBack "Transparent/Diffuse"
}

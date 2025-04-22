Shader "Custom/InvisibleOccluder"
{
    SubShader
    {
        // Render before most opaque objects; adjust if needed
        Tags { "Queue"="Geometry-1" "RenderType"="Opaque" }

        Pass
        {
            // Do not write any color to the render target (object is invisible)
            ColorMask 0  
            // Write depth information to the depth buffer
            ZWrite On   
            // Use standard depth testing so fragments are drawn only if they pass the depth test
            ZTest LEqual

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
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Return any color—it's not shown because of ColorMask 0,
                // but the fragment will still output depth.
                return fixed4(0, 0, 0, 0);
            }
            ENDCG
        }
    }
    // Fallback "Diffuse" can be added if needed or removed.
}

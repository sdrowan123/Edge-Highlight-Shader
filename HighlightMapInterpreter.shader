// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Unlit alpha-blended shader.
 // - no lighting
 // - no lightmap support
 // - no per-material color
 
 Shader "Unlit/HighlightMapInterpreter" {
 Properties {
     _MainTex ("Highlight Map", 2D) = "white" {}
     _Color ("Color", Color) = (0, 0, 0, 1)
     _Color2 ("Color2", Color) = (0, 0, 0, 1)
     _Intensity("Intensity", Float) = 1.0
     _LightPos("Light Pos", Vector) = (1, 1, 1)
 }
 
    SubShader {
        Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
        LOD 100
        
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha 
        
        Pass {  
            CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                #pragma multi_compile_fog
                
                #include "UnityCG.cginc"
    
                struct appdata_t {
                    float4 vertex : POSITION;
                    float2 texcoord : TEXCOORD0;
                    float3 normal : NORMAL;
                };
    
                struct v2f {
                    float4 vertex : SV_POSITION;
                    half3 worldNormal: TEXCOORD6;
                    half2 texcoord : TEXCOORD0;
                    float angle : TEXCOORD5;
                    UNITY_FOG_COORDS(1)
                };
    
                sampler2D _MainTex;
                fixed4 _Color;
                fixed4 _Color2;
                float4 _MainTex_ST;
                float _Intensity;
                float3 _LightPos;
                
                v2f vert (appdata_t v)
                {
                    v2f o;
                    float4 vertex = UnityObjectToClipPos(v.vertex);
                    o.vertex = vertex;
                    o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                    UNITY_TRANSFER_FOG(o,o.vertex);

                    float3 wNormal = UnityObjectToWorldNormal(v.normal);
                    float3 wVertex = mul(unity_ObjectToWorld, vertex.xyz);
                    float3 angleVec = _LightPos - wVertex;
                    float3 dotProd = dot(angleVec, wNormal.xyz);
                    float angle = abs(acos(dotProd / (length(angleVec) * length(wNormal.xyz))));
                    if(isnan(angle)){angle = 3.14159;}
                    float normalizedAngle = 1 - (angle / 3.14159);
                    o.angle = normalizedAngle;

                    return o;
                }
                
                fixed4 frag (v2f i) : SV_Target
                {
                    fixed4 col = tex2D(_MainTex, i.texcoord);
                    UNITY_APPLY_FOG(i.fogCoord, col);
                    col.rgb = lerp(_Color, _Color2, 15 * col.a * col.a * col.a * _Intensity * pow(i.angle, 5));
                    col.a = ((col.a * _Intensity) + (col.a * col.a * 4 * _Intensity) + (col.a * col.a * col.a * 8 * _Intensity)) * pow(i.angle, 5);
                    return col;
                }


            ENDCG
        }
    }
}
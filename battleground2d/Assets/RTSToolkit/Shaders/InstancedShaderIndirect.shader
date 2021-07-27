Shader "Instanced/InstancedShaderIndirect" {
    Properties {
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
    }
    SubShader {
// 		Tags { "RenderType" = "Opaque"}
        Pass {

            Tags {"LightMode"="ForwardBase"}

            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase nolightmap nodirlightmap nodynlightmap novertexlight
            #pragma target 4.5

            #include "UnityCG.cginc"
            #include "UnityLightingCommon.cginc"
            #include "AutoLight.cginc"

            sampler2D _MainTex;

        #if SHADER_TARGET >= 45
            StructuredBuffer<float4> positionBuffer;
            StructuredBuffer<float4> rotationBuffer;
        #endif

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv_MainTex : TEXCOORD0;
                float3 ambient : TEXCOORD1;
                float3 diffuse : TEXCOORD2;
                float3 color : TEXCOORD3;
                SHADOW_COORDS(4)
            };
			
			inline float4 QuaternionMul(float4 v, float4 q)
			{
				v = float4(v + 2 * cross(q.xyz, cross(q.xyz, v.xyz) + q.w * v), v.w);
				return v;
			}
			inline float3 QuaternionMul3(float3 v, float4 q)
			{
				v = float3(v + 2 * cross(q.xyz, cross(q.xyz, v.xyz) + q.w * v));
				return v;
			}
//             void rotate2D(inout float2 v, float r)
//             {
//                 float s, c;
//                 sincos(r, s, c);
//                 v = float2(v.x * c - v.y * s, v.x * s + v.y * c);
//             }

            v2f vert (appdata_full v, uint instanceID : SV_InstanceID)
            {
            #if SHADER_TARGET >= 45
                float4 data = positionBuffer[instanceID];
                float4 dataRot = rotationBuffer[instanceID];
            #else
                float4 data = 0;
                float4 dataRot = 0;
            #endif

//                 float rotation = data.w * data.w * _Time.x * 0.5f;
//                 rotate2D(data.xz, rotation);
				float4 localPositionU = v.vertex;
				float4 localPositionF = QuaternionMul(localPositionU, dataRot);
				
//                 float3 localPosition = v.vertex.xyz * data.w;
                float3 localPosition = localPositionF.xyz * data.w;
                float3 worldPosition = data.xyz + localPosition;
//                 float3 worldPosition = data.xyz;
//                 float3 worldNormal = v.normal;
                float3 worldNormal = QuaternionMul3(v.normal, dataRot);



                float3 ndotl = saturate(dot(worldNormal, _WorldSpaceLightPos0.xyz));
                float3 ambient = ShadeSH9(float4(worldNormal, 1.0f));
                float3 diffuse = (ndotl * _LightColor0.rgb);
                float3 color = v.color;

                v2f o;
                o.pos = mul(UNITY_MATRIX_VP, float4(worldPosition, 1.0f));
                o.uv_MainTex = v.texcoord;
                o.ambient = ambient;
                o.diffuse = diffuse;
                o.color = color;
                TRANSFER_SHADOW(o)
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed shadow = SHADOW_ATTENUATION(i);
                fixed4 albedo = tex2D(_MainTex, i.uv_MainTex);
                float3 lighting = i.diffuse * shadow + i.ambient;
                fixed4 output = fixed4(albedo.rgb * i.color * lighting, albedo.w);
                UNITY_APPLY_FOG(i.fogCoord, output);
                return output;
            }

            ENDCG
        }
    }
}
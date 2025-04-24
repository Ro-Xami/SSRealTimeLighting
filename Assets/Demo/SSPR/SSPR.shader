Shader "RoXami/Example/UnlitShaderExample" {
	Properties {
		_BaseColor ("Example Colour", Color) = (0, 0.66, 0.73, 1)
		//_SSPRTexture ("TEst" , 2D) = "white" {}
	}
	SubShader {
		Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }

		HLSLINCLUDE
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 
			CBUFFER_START(UnityPerMaterial)
			float4 _SSPRTexture_ST;
			float4 _BaseColor;
			CBUFFER_END
		ENDHLSL

		Pass {
			Name "Example"
			Tags { "LightMode"="UniversalForward" }
 
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 4.5
 
			struct Attributes {
				float4 positionOS : POSITION;
				float2 uv : TEXCOORD0;
				float4 color : COLOR;
			};
 
			struct Varyings {
				float4 positionCS : SV_POSITION;
				float3 positionWS : TEXCOORD2;
				float2 uv : TEXCOORD0;
				float4 color : COLOR;
				float4 srcPos : TEXCOORD3;
			};
 
			TEXTURE2D(_SSPRTexture);
			SAMPLER(sampler_SSPRTexture);
 
			Varyings vert(Attributes IN) {
				Varyings OUT;
 
				VertexPositionInputs positionInputs = GetVertexPositionInputs(IN.positionOS.xyz);
				OUT.positionCS = positionInputs.positionCS;
				OUT.positionWS = positionInputs.positionWS;
				OUT.srcPos = ComputeScreenPos(OUT.positionCS);
				OUT.uv = IN.uv;
				OUT.color = IN.color;
				return OUT;
			}
 
			half4 frag(Varyings IN) : SV_Target {
				half2 screenUV = IN.srcPos.xy / IN.srcPos.w;
				half4 baseMap = SAMPLE_TEXTURE2D(_SSPRTexture, sampler_SSPRTexture, screenUV);

				return half4(baseMap.rgb , 1);
			}
			ENDHLSL
		}
	}
}
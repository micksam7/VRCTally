// Made with Amplify Shader Editor v1.9.2.2
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Tally Light Shader"
{
	Properties
	{
		_MainTex("MainTex", 2D) = "white" {}
		_MetallicSmoothness("MetallicSmoothness", 2D) = "white" {}
		_Emission("Emission", 2D) = "black" {}
		[Normal]_NormalMap("NormalMap", 2D) = "white" {}
		[Toggle]_HeartbeatDetected("Heartbeat Detected", Float) = 0
		[Toggle]_Error("Error", Float) = 0
		[Toggle]_Program("Program", Float) = 0
		[Toggle]_Preview("Preview", Float) = 0
		[Toggle]_Standby("Standby", Float) = 0
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Opaque"  "Queue" = "Geometry+0" "IsEmissive" = "true"  }
		Cull Back
		CGINCLUDE
		#include "UnityShaderVariables.cginc"
		#include "UnityPBSLighting.cginc"
		#include "Lighting.cginc"
		#pragma target 3.0
		#ifdef UNITY_PASS_SHADOWCASTER
			#undef INTERNAL_DATA
			#undef WorldReflectionVector
			#undef WorldNormalVector
			#define INTERNAL_DATA half3 internalSurfaceTtoW0; half3 internalSurfaceTtoW1; half3 internalSurfaceTtoW2;
			#define WorldReflectionVector(data,normal) reflect (data.worldRefl, half3(dot(data.internalSurfaceTtoW0,normal), dot(data.internalSurfaceTtoW1,normal), dot(data.internalSurfaceTtoW2,normal)))
			#define WorldNormalVector(data,normal) half3(dot(data.internalSurfaceTtoW0,normal), dot(data.internalSurfaceTtoW1,normal), dot(data.internalSurfaceTtoW2,normal))
		#endif
		struct Input
		{
			float2 uv_texcoord;
			float3 worldPos;
			float3 worldNormal;
			INTERNAL_DATA
		};

		uniform sampler2D _NormalMap;
		uniform float4 _NormalMap_ST;
		uniform sampler2D _MainTex;
		uniform float4 _MainTex_ST;
		uniform float _HeartbeatDetected;
		uniform float _Error;
		uniform float _Program;
		uniform float _Preview;
		uniform float _Standby;
		uniform sampler2D _MetallicSmoothness;
		uniform float4 _MetallicSmoothness_ST;
		uniform sampler2D _Emission;
		uniform float4 _Emission_ST;

		void surf( Input i , inout SurfaceOutputStandard o )
		{
			float2 uv_NormalMap = i.uv_texcoord * _NormalMap_ST.xy + _NormalMap_ST.zw;
			float2 temp_output_2_0_g2 = uv_NormalMap;
			float2 break6_g2 = temp_output_2_0_g2;
			float temp_output_25_0_g2 = ( pow( 0.5 , 3.0 ) * 0.1 );
			float2 appendResult8_g2 = (float2(( break6_g2.x + temp_output_25_0_g2 ) , break6_g2.y));
			float4 tex2DNode14_g2 = tex2D( _NormalMap, temp_output_2_0_g2 );
			float temp_output_4_0_g2 = 2.0;
			float3 appendResult13_g2 = (float3(1.0 , 0.0 , ( ( tex2D( _NormalMap, appendResult8_g2 ).g - tex2DNode14_g2.g ) * temp_output_4_0_g2 )));
			float2 appendResult9_g2 = (float2(break6_g2.x , ( break6_g2.y + temp_output_25_0_g2 )));
			float3 appendResult16_g2 = (float3(0.0 , 1.0 , ( ( tex2D( _NormalMap, appendResult9_g2 ).g - tex2DNode14_g2.g ) * temp_output_4_0_g2 )));
			float3 normalizeResult22_g2 = normalize( cross( appendResult13_g2 , appendResult16_g2 ) );
			o.Normal = normalizeResult22_g2;
			float2 uv_MainTex = i.uv_texcoord * _MainTex_ST.xy + _MainTex_ST.zw;
			float3 ase_worldPos = i.worldPos;
			float3 ase_worldViewDir = normalize( UnityWorldSpaceViewDir( ase_worldPos ) );
			float3 ase_worldNormal = WorldNormalVector( i, float3( 0, 0, 1 ) );
			float fresnelNdotV45 = dot( ase_worldNormal, ase_worldViewDir );
			float fresnelNode45 = ( 0.0 + 1.0 * pow( 1.0 - fresnelNdotV45, 1.0 ) );
			float mulTime14 = _Time.y * 5.0;
			float clampResult22 = clamp( sin( mulTime14 ) , 0.0 , 1.0 );
			float Flash17 = clampResult22;
			float4 color19 = IsGammaSpace() ? float4(1,0.559062,0,0) : float4(1,0.2728371,0,0);
			float4 color41 = IsGammaSpace() ? float4(0,0.7945037,1,0) : float4(0,0.5945532,1,0);
			float4 color37 = IsGammaSpace() ? float4(0.006026745,0,1,0) : float4(0.0004664663,0,1,0);
			float4 color33 = IsGammaSpace() ? float4(0,1,0.09638786,0) : float4(0,1,0.009471367,0);
			float4 color29 = IsGammaSpace() ? float4(1,0,0,0) : float4(1,0,0,0);
			float4 color24 = IsGammaSpace() ? float4(1,0,0.7358327,0) : float4(1,0,0.5007226,0);
			float4 temp_output_48_0 = ( saturate( ( 1.0 - fresnelNode45 ) ) * (( _HeartbeatDetected )?( (( _Error )?( ( Flash17 * color24 ) ):( (( _Program )?( color29 ):( (( _Preview )?( color33 ):( (( _Standby )?( color37 ):( ( Flash17 * color41 ) )) )) )) )) ):( ( Flash17 * color19 ) )) );
			float2 uv_MetallicSmoothness = i.uv_texcoord * _MetallicSmoothness_ST.xy + _MetallicSmoothness_ST.zw;
			float4 tex2DNode3 = tex2D( _MetallicSmoothness, uv_MetallicSmoothness );
			float4 lerpResult44 = lerp( tex2D( _MainTex, uv_MainTex ) , temp_output_48_0 , tex2DNode3.g);
			o.Albedo = lerpResult44.rgb;
			float2 uv_Emission = i.uv_texcoord * _Emission_ST.xy + _Emission_ST.zw;
			float4 lerpResult43 = lerp( tex2D( _Emission, uv_Emission ) , temp_output_48_0 , tex2DNode3.g);
			o.Emission = lerpResult43.rgb;
			o.Metallic = tex2DNode3.r;
			o.Smoothness = tex2DNode3.a;
			o.Alpha = 1;
		}

		ENDCG
		CGPROGRAM
		#pragma surface surf Standard keepalpha fullforwardshadows 

		ENDCG
		Pass
		{
			Name "ShadowCaster"
			Tags{ "LightMode" = "ShadowCaster" }
			ZWrite On
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.0
			#pragma multi_compile_shadowcaster
			#pragma multi_compile UNITY_PASS_SHADOWCASTER
			#pragma skip_variants FOG_LINEAR FOG_EXP FOG_EXP2
			#include "HLSLSupport.cginc"
			#if ( SHADER_API_D3D11 || SHADER_API_GLCORE || SHADER_API_GLES || SHADER_API_GLES3 || SHADER_API_METAL || SHADER_API_VULKAN )
				#define CAN_SKIP_VPOS
			#endif
			#include "UnityCG.cginc"
			#include "Lighting.cginc"
			#include "UnityPBSLighting.cginc"
			struct v2f
			{
				V2F_SHADOW_CASTER;
				float2 customPack1 : TEXCOORD1;
				float4 tSpace0 : TEXCOORD2;
				float4 tSpace1 : TEXCOORD3;
				float4 tSpace2 : TEXCOORD4;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};
			v2f vert( appdata_full v )
			{
				v2f o;
				UNITY_SETUP_INSTANCE_ID( v );
				UNITY_INITIALIZE_OUTPUT( v2f, o );
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO( o );
				UNITY_TRANSFER_INSTANCE_ID( v, o );
				Input customInputData;
				float3 worldPos = mul( unity_ObjectToWorld, v.vertex ).xyz;
				half3 worldNormal = UnityObjectToWorldNormal( v.normal );
				half3 worldTangent = UnityObjectToWorldDir( v.tangent.xyz );
				half tangentSign = v.tangent.w * unity_WorldTransformParams.w;
				half3 worldBinormal = cross( worldNormal, worldTangent ) * tangentSign;
				o.tSpace0 = float4( worldTangent.x, worldBinormal.x, worldNormal.x, worldPos.x );
				o.tSpace1 = float4( worldTangent.y, worldBinormal.y, worldNormal.y, worldPos.y );
				o.tSpace2 = float4( worldTangent.z, worldBinormal.z, worldNormal.z, worldPos.z );
				o.customPack1.xy = customInputData.uv_texcoord;
				o.customPack1.xy = v.texcoord;
				TRANSFER_SHADOW_CASTER_NORMALOFFSET( o )
				return o;
			}
			half4 frag( v2f IN
			#if !defined( CAN_SKIP_VPOS )
			, UNITY_VPOS_TYPE vpos : VPOS
			#endif
			) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID( IN );
				Input surfIN;
				UNITY_INITIALIZE_OUTPUT( Input, surfIN );
				surfIN.uv_texcoord = IN.customPack1.xy;
				float3 worldPos = float3( IN.tSpace0.w, IN.tSpace1.w, IN.tSpace2.w );
				half3 worldViewDir = normalize( UnityWorldSpaceViewDir( worldPos ) );
				surfIN.worldPos = worldPos;
				surfIN.worldNormal = float3( IN.tSpace0.z, IN.tSpace1.z, IN.tSpace2.z );
				surfIN.internalSurfaceTtoW0 = IN.tSpace0.xyz;
				surfIN.internalSurfaceTtoW1 = IN.tSpace1.xyz;
				surfIN.internalSurfaceTtoW2 = IN.tSpace2.xyz;
				SurfaceOutputStandard o;
				UNITY_INITIALIZE_OUTPUT( SurfaceOutputStandard, o )
				surf( surfIN, o );
				#if defined( CAN_SKIP_VPOS )
				float2 vpos = IN.pos;
				#endif
				SHADOW_CASTER_FRAGMENT( IN )
			}
			ENDCG
		}
	}
	Fallback "Diffuse"
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=19202
Node;AmplifyShaderEditor.SimpleTimeNode;14;-1136,608;Inherit;False;1;0;FLOAT;5;False;1;FLOAT;0
Node;AmplifyShaderEditor.SinOpNode;16;-912,608;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ClampOpNode;22;-752,608;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;17;-560,608;Inherit;False;Flash;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;40;-3024,832;Inherit;False;17;Flash;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;41;-3056,912;Inherit;False;Constant;_ShaderError;Shader Error;6;0;Create;True;0;0;0;False;0;False;0,0.7945037,1,0;0,0,0,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;42;-2784,864;Inherit;False;2;2;0;FLOAT;0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.ColorNode;37;-2512,976;Inherit;False;Constant;_StandbyColor;Standby Color;6;0;Create;True;0;0;0;False;0;False;0.006026745,0,1,0;0,0,0,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ToggleSwitchNode;39;-2144,880;Inherit;False;Property;_Standby;Standby;10;0;Create;True;0;0;0;False;0;False;0;True;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.ColorNode;33;-1696,1024;Inherit;False;Constant;_PreviewColor;Preview Color;6;0;Create;True;0;0;0;False;0;False;0,1,0.09638786,0;0,0,0,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.GetLocalVarNode;23;-496,1088;Inherit;False;17;Flash;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;24;-528,1168;Inherit;False;Constant;_ErrorColor;Error Color;6;0;Create;True;0;0;0;False;0;False;1,0,0.7358327,0;0,0,0,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ToggleSwitchNode;35;-1376,880;Inherit;False;Property;_Preview;Preview;9;0;Create;True;0;0;0;False;0;False;0;True;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.ColorNode;29;-1136,1008;Inherit;False;Constant;_ProgramColor;Program Color;6;0;Create;True;0;0;0;False;0;False;1,0,0,0;0,0,0,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;25;-288,1120;Inherit;False;2;2;0;FLOAT;0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.ToggleSwitchNode;31;-736,896;Inherit;False;Property;_Program;Program;8;0;Create;True;0;0;0;False;0;False;0;True;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.TexturePropertyNode;6;-1120,160;Inherit;True;Property;_Emission;Emission;2;0;Create;True;0;0;0;False;0;False;None;None;False;black;Auto;Texture2D;-1;0;2;SAMPLER2D;0;SAMPLERSTATE;1
Node;AmplifyShaderEditor.TexturePropertyNode;4;-1120,-32;Inherit;True;Property;_MetallicSmoothness;MetallicSmoothness;1;0;Create;True;0;0;0;False;0;False;bfb44e8dcd2318641b7bff46c11f2c2c;bfb44e8dcd2318641b7bff46c11f2c2c;False;white;Auto;Texture2D;-1;0;2;SAMPLER2D;0;SAMPLERSTATE;1
Node;AmplifyShaderEditor.TexturePropertyNode;1;-1120,-224;Inherit;True;Property;_MainTex;MainTex;0;0;Create;True;0;0;0;False;0;False;None;None;False;white;Auto;Texture2D;-1;0;2;SAMPLER2D;0;SAMPLERSTATE;1
Node;AmplifyShaderEditor.ToggleSwitchNode;26;-16,896;Inherit;False;Property;_Error;Error;7;0;Create;True;0;0;0;False;0;False;0;True;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.TexturePropertyNode;8;-1120,352;Inherit;True;Property;_NormalMap;NormalMap;3;1;[Normal];Create;True;0;0;0;False;0;False;None;None;False;white;Auto;Texture2D;-1;0;2;SAMPLER2D;0;SAMPLERSTATE;1
Node;AmplifyShaderEditor.SamplerNode;3;-752,-32;Inherit;True;Property;_TextureSample1;Texture Sample 0;1;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;5;-752,160;Inherit;True;Property;_TextureSample2;Texture Sample 0;1;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;2;-752,-224;Inherit;True;Property;_TextureSample0;Texture Sample 0;1;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.FunctionNode;10;-720,368;Inherit;False;NormalCreate;4;;2;e12f7ae19d416b942820e3932b56220f;0;4;1;SAMPLER2D;;False;2;FLOAT2;0,0;False;3;FLOAT;0.5;False;4;FLOAT;2;False;1;FLOAT3;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;2197.188,-54.72367;Float;False;True;-1;2;ASEMaterialInspector;0;0;Standard;Tally Light Shader;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;Back;0;False;;0;False;;False;0;False;;0;False;;False;0;Opaque;0.5;True;True;0;False;Opaque;;Geometry;All;12;all;True;True;True;True;0;False;;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;2;15;10;25;False;0.5;True;0;0;False;;0;False;;0;0;False;;0;False;;0;False;;0;False;;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;True;Relative;0;;-1;-1;-1;-1;0;False;0;0;False;;-1;0;False;;0;0;0;False;0.1;False;;0;False;;False;17;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;16;FLOAT4;0,0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
Node;AmplifyShaderEditor.LerpOp;44;1661.1,-104.8523;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;48;1227.994,473.9702;Inherit;False;2;2;0;FLOAT;0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.LerpOp;43;1661.1,151.1478;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.OneMinusNode;46;852.1643,381.9849;Inherit;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;47;1072.344,393.8222;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.FresnelNode;45;549.1642,394.9849;Inherit;False;Standard;WorldNormal;ViewDir;False;False;5;0;FLOAT3;0,0,1;False;4;FLOAT3;0,0,0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;20;121.5907,588;Inherit;False;2;2;0;FLOAT;0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.ColorNode;19;-160,624;Inherit;False;Constant;_HeartbeatMissing;Heartbeat Missing;6;0;Create;True;0;0;0;False;0;False;1,0.559062,0,0;0,0,0,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ToggleSwitchNode;11;376.4698,705.3182;Inherit;False;Property;_HeartbeatDetected;Heartbeat Detected;6;0;Create;True;0;0;0;False;0;False;0;True;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode;21;-143,544;Inherit;False;17;Flash;1;0;OBJECT;;False;1;FLOAT;0
WireConnection;16;0;14;0
WireConnection;22;0;16;0
WireConnection;17;0;22;0
WireConnection;42;0;40;0
WireConnection;42;1;41;0
WireConnection;39;0;42;0
WireConnection;39;1;37;0
WireConnection;35;0;39;0
WireConnection;35;1;33;0
WireConnection;25;0;23;0
WireConnection;25;1;24;0
WireConnection;31;0;35;0
WireConnection;31;1;29;0
WireConnection;26;0;31;0
WireConnection;26;1;25;0
WireConnection;3;0;4;0
WireConnection;5;0;6;0
WireConnection;2;0;1;0
WireConnection;10;1;8;0
WireConnection;0;0;44;0
WireConnection;0;1;10;0
WireConnection;0;2;43;0
WireConnection;0;3;3;1
WireConnection;0;4;3;4
WireConnection;44;0;2;0
WireConnection;44;1;48;0
WireConnection;44;2;3;2
WireConnection;48;0;47;0
WireConnection;48;1;11;0
WireConnection;43;0;5;0
WireConnection;43;1;48;0
WireConnection;43;2;3;2
WireConnection;46;0;45;0
WireConnection;47;0;46;0
WireConnection;20;0;21;0
WireConnection;20;1;19;0
WireConnection;11;0;20;0
WireConnection;11;1;26;0
ASEEND*/
//CHKSM=845E54955884C6C293304E69FA2A11C6FE97F3DB
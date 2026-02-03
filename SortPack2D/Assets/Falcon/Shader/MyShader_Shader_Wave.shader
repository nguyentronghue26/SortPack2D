Shader "MyShader/Shader_Wave" {
	Properties {
		[PerRendererData] _Tex ("Sprite Texture", 2D) = "white" {}
		LiquidUV_WaveX_1 ("LiquidUV_WaveX_1", Range(0, 2)) = 2
		LiquidUV_WaveY_1 ("LiquidUV_WaveY_1", Range(0, 2)) = 2
		LiquidUV_DistanceX_1 ("LiquidUV_DistanceX_1", Range(0, 1)) = 0.3
		LiquidUV_DistanceY_1 ("LiquidUV_DistanceY_1", Range(0, 1)) = 0.3
		LiquidUV_Speed_1 ("LiquidUV_Speed_1", Range(-2, 2)) = 1
		Effect_Fade ("Effect_Fade", Range(0, 1)) = 1
		_SpriteFade ("SpriteFade", Range(0, 1)) = 1
		[Enum(UnityEngine.Rendering.BlendMode)] _SrcFactor ("Src Factor", Float) = 5
		[Enum(UnityEngine.Rendering.BlendMode)] _DstFactor ("Dst Factor", Float) = 10
		[Enum(UnityEngine.Rendering.BlendOp)] _Opp ("Operation", Float) = 0
		[Toggle(_CANVAS_GROUP_COMPATIBLE)] _CanvasGroupCompatible ("CanvasGroup Compatible", Float) = 1
		[Enum(UnityEngine.Rendering.CompareFunction)] [HideInInspector] _StencilComp ("Stencil Comparison", Float) = 8
		[HideInInspector] _Stencil ("Stencil ID", Float) = 0
		[Enum(UnityEngine.Rendering.StencilOp)] [HideInInspector] _StencilOp ("Stencil Operation", Float) = 0
		[HideInInspector] _StencilWriteMask ("Stencil Write Mask", Float) = 255
		[HideInInspector] _StencilReadMask ("Stencil Read Mask", Float) = 255
		[HideInInspector] _ColorMask ("Color Mask", Float) = 15
		[Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
	}
	//DummyShaderTextExporter
	SubShader{
		Tags { "RenderType" = "Opaque" }
		LOD 200

		Pass
		{
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			float4x4 unity_ObjectToWorld;
			float4x4 unity_MatrixVP;

			struct Vertex_Stage_Input
			{
				float4 pos : POSITION;
			};

			struct Vertex_Stage_Output
			{
				float4 pos : SV_POSITION;
			};

			Vertex_Stage_Output vert(Vertex_Stage_Input input)
			{
				Vertex_Stage_Output output;
				output.pos = mul(unity_MatrixVP, mul(unity_ObjectToWorld, input.pos));
				return output;
			}

			float4 frag(Vertex_Stage_Output input) : SV_TARGET
			{
				return float4(1.0, 1.0, 1.0, 1.0); // RGBA
			}

			ENDHLSL
		}
	}
	Fallback "Sprites/Default"
	//CustomEditor "SpineShaderWithOutlineGUI"
}
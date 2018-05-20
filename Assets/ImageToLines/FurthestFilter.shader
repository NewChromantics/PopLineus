Shader "NewChromantics/FurthestFilter"
{

	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		[Toggle]Erode("Erode", Range(0,1) ) = 1
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100
		Cull Off

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
			float4 _MainTex_TexelSize;

			float Erode;
			#define ERODE	( Erode > 0.5f )
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				return o;
			}

		

			//	same as PopMath
			float2 AngleRadianToVector2(float radian)
			{
				float x = sin(radian);
				float y = cos(radian);
				return float2(x,y);
			}

			//	same as PopMath
			float2 AngleDegreeToVector2(float degree)
			{
				return AngleRadianToVector2( radians(degree) );
			}

			#define MAX_DISTANCE	10
			bool GetDistance(int2 px,out float Distance)
			{
				float2 uv = px * _MainTex_TexelSize.xy;
				float3 Rgb = tex2D(_MainTex, uv);
				float Score = Rgb.x;
				bool Valid = Rgb.z < 1;
				Distance = Score * (float)MAX_DISTANCE;
				return Valid;
			}

			fixed4 frag (v2f i) : SV_Target
			{
				float2 uv = i.uv;
				int2 px = uv * _MainTex_TexelSize.zw;

				float4 ResultValid = float4(0,0,0,1);
				float4 ResultInvalid = float4(1,1,1,1);
				//float4 ResultBetterNeighbour = float4(1,0,0,1);
				float4 ResultBetterNeighbour = ResultInvalid;

				//	am I the furthest of my neighbours?
				#define NEIGHOUR_COUNT	8
				int2 Neighbours[NEIGHOUR_COUNT];
				Neighbours[0] = int2(	-1,	-1	);
				Neighbours[1] = int2(	 0,	-1	);
				Neighbours[2] = int2(	 1,	-1	);
				Neighbours[3] = int2(	 1,	 0	);
				Neighbours[4] = int2(	 1,	 1	);
				Neighbours[5] = int2(	 0,	 1	);
				Neighbours[6] = int2(	-1,	 1	);
				Neighbours[7] = int2(	-1,	 0	);

				//	this pixel not valid
				float MyDistance = 0;
				if ( !GetDistance(px,MyDistance) )
					return ResultInvalid;

				if ( !ERODE )
					return ResultValid;

				//	see if there's a better neighbour
				bool HasBetterNeighbour = false;
				for ( int n=0;	n<NEIGHOUR_COUNT;	n++ )
				{
					int2 NeighbourOffset = Neighbours[n];
					int2 NeighbourPos = px+NeighbourOffset;
					float NeighbourDistance=0;

					if ( !GetDistance(NeighbourPos,NeighbourDistance) )
						continue;
					
					if ( NeighbourDistance > MyDistance )
						HasBetterNeighbour = true;
				}
				if ( HasBetterNeighbour )
					return ResultBetterNeighbour;
				
				//	 no better neighbours
				return ResultValid;
			}
			ENDCG
		}
	}
}

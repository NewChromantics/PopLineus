Shader "NewChromantics/EdgeFilter"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		BlackMax("BlackMax", Range(0,1) ) = 0.3
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

			float BlackMax;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				return o;
			}


			bool IsBlack(int2 px)
			{
				float2 uv = px * _MainTex_TexelSize.xy;
				float3 Rgb = tex2D(_MainTex, uv);
				float Lum = max( Rgb.x, max( Rgb.y, Rgb.z ) );
				if ( Lum > BlackMax )
					return false;
				return true;
			}

			int DistanceToWhite(int2 Px,float2 Step)
			{
			#define MAX_DISTANCE	5
			#define UNKNOWN_DISTANCE	-1
				for ( int i=0;	i<MAX_DISTANCE;	i++ )
				{
					Px += Step;
					if ( !IsBlack(Px) )
						return i;
				}
				return UNKNOWN_DISTANCE;
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

			fixed4 frag (v2f i) : SV_Target
			{
				float2 uv = i.uv;
				int2 px = uv * _MainTex_TexelSize.zw;

				//	we want to find the shortest distance to white
				#define DIRECTION_COUNT 4

				int MinDistance = UNKNOWN_DISTANCE;
				for ( int d=0;	d<DIRECTION_COUNT;	d++ )
				{
					float DirectionAngle = lerp( 0, 360, (float)d / (float)DIRECTION_COUNT );
					float2 Dir = AngleDegreeToVector2( DirectionAngle );
					int Distance = DistanceToWhite( px, Dir );
					if ( Distance == UNKNOWN_DISTANCE )
						continue;
					if ( MinDistance == UNKNOWN_DISTANCE )
						MinDistance = Distance;
					else
						MinDistance = min( Distance, MinDistance );
				}

				bool HasResult = true;

				if ( !IsBlack(px) )
					HasResult = false;
				if ( MinDistance == UNKNOWN_DISTANCE )
					HasResult = false;

				if ( !HasResult )
					return float4(0,0,1,1);

				float Score = MinDistance / (float)MAX_DISTANCE;
				//Score = 1-Score;
				return float4( Score, 0, 0, 1 );


				if ( IsBlack(px) )
					return float4(0,0,0,1);
				else
					return float4(1,1,1,1);
			}
			ENDCG
		}
	}
}

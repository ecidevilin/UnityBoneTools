Shader "Hidden/VertexColor"
{
	Properties
	{
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" "Queue"="Geometry"}
		LOD 100

		Pass
		{
			CGPROGRAM
			#pragma target 4.5
			#pragma vertex vert
			#pragma geometry geom
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float4 color : COLOR0;
			};

			struct v2g
			{
				float4 pos : SV_POSITION;
				float4 color : COLOR0;
			};
			struct g2f 
			{
    			float4  pos : SV_POSITION;
    			float4  color : COLOR0;
    			float3 dist : TEXCOORD1;
    			float3 vd : TEXCOORD2;
			};
			

			StructuredBuffer<float4> boneColors;
			v2g vert (float4 vertex : POSITION, uint vid : SV_VertexID)
			{
				v2g o;
				o.pos = UnityObjectToClipPos(vertex);
				o.color = boneColors[vid];
				return o;
			}
			
			[maxvertexcount(12)]
			void geom(triangle v2g IN[3], inout TriangleStream<g2f> triStream)
			{
			
				float2 WIN_SCALE = float2(_ScreenParams.x/2.0, _ScreenParams.y/2.0);
				
				//frag position
				float2 p0 = WIN_SCALE * IN[0].pos.xy / IN[0].pos.w;
				float2 p1 = WIN_SCALE * IN[1].pos.xy / IN[1].pos.w;
				float2 p2 = WIN_SCALE * IN[2].pos.xy / IN[2].pos.w;
				
				//barycentric position
				float2 v0 = p2-p1;
				float2 v1 = p2-p0;
				float2 v2 = p1-p0;
				//triangles area
				float area = abs(v1.x*v2.y - v1.y * v2.x);
			
				g2f OUT;
				OUT.pos = IN[0].pos;
				OUT.color = IN[0].color;
				OUT.dist = float3(area/length(v0),0,0);
				OUT.vd = float3(1,0,0);
				triStream.Append(OUT);

				OUT.pos = IN[1].pos;
				OUT.color = IN[1].color;
				OUT.dist = float3(0,area/length(v1),0);
				OUT.vd = float3(0,1,0);
				triStream.Append(OUT);

				OUT.pos = IN[2].pos;
				OUT.color = IN[2].color;
				OUT.dist = float3(0,0,area/length(v2));
				OUT.vd = float3(0,0,1);
				triStream.Append(OUT);
				
			}
			
			fixed4 frag (g2f IN) : SV_Target
			{
				//distance of frag from triangles center
				float d = min(IN.dist.x, min(IN.dist.y, IN.dist.z));
				float vd = max(IN.vd.x, max(IN.vd.y, IN.vd.z));
				//fade based on dist from center
 				float I = exp2(-4.0*d*d);
 				I += step(0.9, vd);
 				return lerp(fixed4(0,0,0,0), IN.color, I);	
//				return i.color * step(i.color.a, 0.99);
			}
			ENDCG
		}
	}
}

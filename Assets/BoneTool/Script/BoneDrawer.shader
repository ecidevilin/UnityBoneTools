Shader "BoneDrawer"
{
	Properties
	{
	}
	SubShader
	{
		Tags { "RenderType"="Transparent" "Queue"="Transparent" }
		ZTest Always
		Pass
		{
			CGPROGRAM
			#pragma target 4.5
			#pragma vertex vert
			#pragma geometry geom
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			struct v2g
			{
				float4 pos : SV_POSITION;
				uint vid : VertexID;
			};
			struct g2f 
			{
    			float4  pos : SV_POSITION;
			};
			

			StructuredBuffer<float4x4> Bones;
			v2g vert (float4 vertex : POSITION, uint vid : SV_VertexID)
			{
				v2g o;
				o.pos = vertex;
				// o.pos = UnityObjectToClipPos(vertex);
				o.vid = vid / 2;
				return o;
			}
			
#define boneSize 0.01

			[maxvertexcount(24)]
			void geom(line v2g IN[2], inout TriangleStream<g2f> triStream)
			{
				float4x4 matr = Bones[IN[0].vid];
				float4 p0 = IN[0].pos;
				float4 p1 = IN[1].pos;
				float4 pi = p0 * 0.25 + p1 * 0.75;

				//g2f OUT;
				//OUT.pos = p0;
				//triStream.Append(OUT);
				//OUT.pos = p1;
				//triStream.Append(OUT);
				//OUT.pos = pi;
				//triStream.Append(OUT);

				 float4 lt = pi;
				 float4 rt = pi;
				 float4 rb = pi;
				 float4 lb = pi;
				 lt += float4(-1, 1, 1, 0) * boneSize;
				 rt += float4( 1, 1, 1, 0) * boneSize;
				 rb += float4( 1,-1, -1, 0) * boneSize;
				 lb += float4(-1,-1, -1, 0) * boneSize;
				  //lt += (-matr._m00_m10_m20_m30 + matr._m10_m11_m21_m31) * boneSize;
				  //rt += (+matr._m00_m10_m20_m30 + matr._m10_m11_m21_m31) * boneSize;
				  //rb += (+matr._m00_m10_m20_m30 - matr._m10_m11_m21_m31) * boneSize;
				  //lb += (-matr._m00_m10_m20_m30 - matr._m10_m11_m21_m31) * boneSize;
 				//  lt.xy += (-matr._m00_m10 + matr._m10_m11) * boneSize;
				 // rt.xy += (+matr._m00_m10 + matr._m10_m11) * boneSize;
				 // rb.xy += (+matr._m00_m10 - matr._m10_m11) * boneSize;
				 // lb.xy += (-matr._m00_m10 - matr._m10_m11) * boneSize;
			
				 p0 = UnityObjectToClipPos(p0);
				 p1 = UnityObjectToClipPos(p1);
				 lt = UnityObjectToClipPos(lt);
				 rt = UnityObjectToClipPos(rt);
				 rb = UnityObjectToClipPos(rb);
				 lb = UnityObjectToClipPos(lb);

				 g2f OUT;
				 OUT.pos = p0;
				 triStream.Append(OUT);
				 OUT.pos = lt;
				 triStream.Append(OUT);
				 OUT.pos = rt;
				 triStream.Append(OUT);
				 OUT.pos = p0;
				 triStream.Append(OUT);
				 OUT.pos = rt;
				 triStream.Append(OUT);
				 OUT.pos = rb;
				 triStream.Append(OUT);
				 OUT.pos = p0;
				 triStream.Append(OUT);
				 OUT.pos = rb;
				 triStream.Append(OUT);
				 OUT.pos = lb;
				 triStream.Append(OUT);
				 OUT.pos = p0;
				 triStream.Append(OUT);
				 OUT.pos = lb;
				 triStream.Append(OUT);
				 OUT.pos = lt;
				 triStream.Append(OUT);



				 OUT.pos = lt;
				 triStream.Append(OUT);
				 OUT.pos = rt;
				 triStream.Append(OUT);
				 OUT.pos = p1;
				 triStream.Append(OUT);
				 OUT.pos = rt;
				 triStream.Append(OUT);
				 OUT.pos = rb;
				 triStream.Append(OUT);
				 OUT.pos = p1;
				 triStream.Append(OUT);
				 OUT.pos = rb;
				 triStream.Append(OUT);
				 OUT.pos = lb;
				 triStream.Append(OUT);
				 OUT.pos = p1;
				 triStream.Append(OUT);
				 OUT.pos = lb;
				 triStream.Append(OUT);
				 OUT.pos = lt;
				 triStream.Append(OUT);
				 OUT.pos = p1;
				 triStream.Append(OUT);
				
			}
			
			fixed4 frag (g2f IN) : SV_Target
			{
				return fixed4(1,0,0,1);
			}
			ENDCG
		}
	}
}

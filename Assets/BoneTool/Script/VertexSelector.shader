Shader "Hidden/VertexSelector"
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
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			struct v2f
			{
				float4 pos : SV_POSITION;
				float4 color : COLOR0;
			};
			
			float4 SelectedColor;
			uint SelectedVid;

			v2f vert (float4 vertex : POSITION, uint vid : SV_VertexID)
			{
				v2f o;
				o.pos = UnityObjectToClipPos(vertex);
				o.color = lerp(float4(0,0,0,1), SelectedColor, vid == SelectedVid);
				return o;
			}
			
			
			fixed4 frag (v2f IN) : SV_Target
			{
				return IN.color;
			}
			ENDCG
		}
	}
}

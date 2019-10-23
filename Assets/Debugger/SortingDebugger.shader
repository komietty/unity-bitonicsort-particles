Shader "Hidden/SortingDebugger"
{
	Properties { }

	CGINCLUDE
	#include "UnityCG.cginc"
	sampler2D _OutRT;
	int _ClmCount;
	int _RowCount;

	float4 frag(v2f_img i) : SV_Target
	{
		float4 col = 0;
		for (int r = 0; r < _RowCount; r++)
			col = (i.uv.x * _RowCount > r && i.uv.x * _RowCount < r + 1) ? tex2D(_OutRT, float2(i.uv.x * _RowCount - r, r)) : col;

		return float4(
			(i.uv.y < 0.33) ? col.x : 0,
			(i.uv.y > 0.33 && i.uv.y < 0.66) ? col.y : 0,
			(i.uv.y > 0.66) ? col.z : 0,
			1);
	}
	ENDCG

	SubShader
	{
		Cull Off ZWrite Off ZTest Always
		Pass
		{
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag
     		ENDCG
		}
	}
}

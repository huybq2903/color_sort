uniform float _Wipe;
uniform float _Softness;
uniform float _InvertDirection;
uniform float4 _MainTex_TexelSize;
uniform float4 _SourceTex_TexelSize;

float4 FinishWipe(float mask, float maskRange, float4 color, float4 vertexColor, float4 rectMask)
{
	// Invert
	if (_InvertDirection.x > 0.0)
	{
		mask = maskRange - mask;
	}

	half strength = _Wipe;
	half softWidth = _Softness;
	float vv = 1.0;
	if (softWidth > 0.0)
	{
		// Remap strength from 0..1 range to -softWidth..maskRange + softWidth
		float t = lerp(-softWidth, maskRange + softWidth, strength);
		float edgeStart = t - softWidth;
		float edgeEnd = t + softWidth;

		//vv = smoothstep(edgeStart, edgeEnd, mask);
		vv = saturate((mask - edgeStart) / (edgeEnd - edgeStart));
	}
	else
	{
		vv = step(strength, mask / maskRange);
	}

	/*softWidth = _Wipe.y;
	strength = -0.5;

	//maskRange = 2.0;
	//mask /= _ResultTex_TexelSize.z;

	// Remap 
	float edgeEnd = _Wipe.x * maskRange;
	float edgeStart = edgeEnd + softWidth;
	float vv = smoothstep(edgeEnd, edgeStart, mask);
	color *= saturate(vv);*/

	//vv = saturate(vv);

	color *= vv;

	//color = lerp(float4(1, 0, 0, 1), float4(0, 0, 1, 1), vv);

	// 2D rect clipping
	#ifdef UNITY_UI_CLIP_RECT
	color = ApplyClipRect(color, rectMask);
	#endif

	// Alpha clipping
	#ifdef UNITY_UI_ALPHACLIP
	clip (color.a - 0.001);
	#endif

	color.rgb *= vertexColor.a;
	color *= vertexColor;

	return color;
}
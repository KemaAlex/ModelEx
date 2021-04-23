﻿matrix World;
matrix View;
matrix Projection;
float3 CameraPosition;
float3 LightDirection = float3(1, 1, 1);

bool UseTexture = false;
Texture2D Texture;

float4 DiffuseColor = float4(0, 1, 0, 1);
float4 AmbientColor = float4(0.2, 0.2, 0.2, 1);
float4 LightColor = float4(0.9, 0.9, 0.9, 1);
float SpecularPower = 32;
float4 SpecularColor = float4(1, 1, 1, 1);

float RealmBlend = 0.0f;

struct VertexShaderInput
{
	float4 Position0 : POSITION0;
	float4 Position1 : POSITION1;
	float3 Color0 : COLOR0;
	float3 Color1 : COLOR1;
	float2 TexCoord : TEXCOORD;
};

struct VertexShaderOutput
{
	float4 Position : SV_POSITION;
	float3 Color : TEXCOORD0;
	float3 ViewDirection : TEXCOORD1;
	float2 TexCoord : TEXCOORD2;
};

SamplerState stateLinear
{
	Filter = MIN_MAG_MIP_LINEAR;
	AddressU = Wrap;
	AddressV = Wrap;
};

RasterizerState DefaultRasterizerState
{
	FillMode = Solid;
	//CullMode = None;
	CullMode = Back;
	FrontCounterClockwise = true;
	//CullMode = CCW;
	//FrontCounterClockwise = false;
};

RasterizerState Gex3RasterizerState
{
	FillMode = Solid;
	//CullMode = None;
	CullMode = Back;
	FrontCounterClockwise = true;
	//CullMode = CCW;
	//FrontCounterClockwise = false;
};

RasterizerState SR1RasterizerState
{
	FillMode = Solid;
	//CullMode = None;
	CullMode = Back;
	FrontCounterClockwise = true;
	//CullMode = CCW;
	//FrontCounterClockwise = false;
};

RasterizerState SR2RasterizerState
{
	FillMode = Solid;
	//CullMode = None;
	CullMode = Back;
	FrontCounterClockwise = true;
	//CullMode = CCW;
	//FrontCounterClockwise = false;
};

VertexShaderOutput VertexShaderFunction(VertexShaderInput input)
{
	//float4 position = RealmBlend < 0.5 ? input.Position0 : input.Position1;
	//float3 color = RealmBlend < 0.5 ? input.Color0 : input.Color1;

	float4 position = input.Position0 - ((input.Position0 - input.Position1) * RealmBlend);
	/*float4 color = float4(
		input.Color0.b + ((input.Color0.b - input.Color1.b) * RealmBlend),
		input.Color0.g + ((input.Color0.g - input.Color1.g) * RealmBlend),
		input.Color0.r + ((input.Color0.r - input.Color1.r) * RealmBlend),
		input.Color0.a + ((input.Color0.a - input.Color1.a) * RealmBlend));
		);*/
	/*float3 color = float3(
		input.Color0.b + ((input.Color0.b - input.Color1.b) * RealmBlend),
		input.Color0.g + ((input.Color0.g - input.Color1.g) * RealmBlend),
		input.Color0.r + ((input.Color0.r - input.Color1.r) * RealmBlend));*/
	
	float4 color;
	color.r = input.Color0.r - ((input.Color0.r - input.Color1.r) * RealmBlend);
	color.g = input.Color0.g - ((input.Color0.g - input.Color1.g) * RealmBlend);
	color.b = input.Color0.b - ((input.Color0.b - input.Color1.b) * RealmBlend);
	//color.a = input.Color0.a + ((input.Color0.a - input.Color1.a) * RealmBlend);
	color.a = 1.0;

	VertexShaderOutput output;
	float4 worldPosition = mul(position, World);
	matrix viewProjection = mul(View, Projection);

	output.Position = mul(worldPosition, viewProjection);
	output.Color = color;
	output.ViewDirection = worldPosition - CameraPosition;
	output.TexCoord = input.TexCoord;

	return output;
}

VertexShaderOutput Gex3VertexShaderFunction(VertexShaderInput input)
{
	//float4 position = RealmBlend < 0.5 ? input.Position0 : input.Position1;
	//float3 color = RealmBlend < 0.5 ? input.Color0 : input.Color1;

	float4 position = input.Position0 - ((input.Position0 - input.Position1) * RealmBlend);
	/*float4 color = float4(
		input.Color0.b + ((input.Color0.b - input.Color1.b) * RealmBlend),
		input.Color0.g + ((input.Color0.g - input.Color1.g) * RealmBlend),
		input.Color0.r + ((input.Color0.r - input.Color1.r) * RealmBlend),
		input.Color0.a + ((input.Color0.a - input.Color1.a) * RealmBlend));
		);*/
		/*float3 color = float3(
			input.Color0.b + ((input.Color0.b - input.Color1.b) * RealmBlend),
			input.Color0.g + ((input.Color0.g - input.Color1.g) * RealmBlend),
			input.Color0.r + ((input.Color0.r - input.Color1.r) * RealmBlend));*/

	float4 color;
	color.r = input.Color0.r - ((input.Color0.r - input.Color1.r) * RealmBlend);
	color.g = input.Color0.g - ((input.Color0.g - input.Color1.g) * RealmBlend);
	color.b = input.Color0.b - ((input.Color0.b - input.Color1.b) * RealmBlend);
	//color.a = input.Color0.a + ((input.Color0.a - input.Color1.a) * RealmBlend);
	color.a = 1.0;
	color.rbg *= 2.0;

	VertexShaderOutput output;
	float4 worldPosition = mul(position, World);
	matrix viewProjection = mul(View, Projection);

	output.Position = mul(worldPosition, viewProjection);
	output.Color = color;
	output.ViewDirection = worldPosition - CameraPosition;
	output.TexCoord = input.TexCoord;

	return output;
}

float4 PixelShaderFunction(VertexShaderOutput input) : SV_TARGET
{
	// Start with diffuse color
	float4 color = UseTexture == false ? DiffuseColor : Texture.Sample(stateLinear, input.TexCoord);
	if (color.a < 0.5)
	{
		clip(-1);
	}

	// Calculate final color
	float3 output = input.Color * color.rgb;

	return float4(output, color.a);
}

technique10 DefaultRender
{
	pass P0
	{
		SetVertexShader(CompileShader(vs_4_0, VertexShaderFunction()));
		SetGeometryShader(NULL);
		SetPixelShader(CompileShader(ps_4_0, PixelShaderFunction()));
		SetRasterizerState(DefaultRasterizerState);
	}
}

technique10 Gex3Render
{
	pass P0
	{
		SetVertexShader(CompileShader(vs_4_0, Gex3VertexShaderFunction()));
		SetGeometryShader(NULL);
		SetPixelShader(CompileShader(ps_4_0, PixelShaderFunction()));
		SetRasterizerState(Gex3RasterizerState);
	}
}

technique10 SR1Render
{
	pass P0
	{
		SetVertexShader(CompileShader(vs_4_0, VertexShaderFunction()));
		SetGeometryShader(NULL);
		SetPixelShader(CompileShader(ps_4_0, PixelShaderFunction()));
		SetRasterizerState(SR1RasterizerState);
	}
}

technique10 SR2Render
{
	pass P0
	{
		SetVertexShader(CompileShader(vs_4_0, VertexShaderFunction()));
		SetGeometryShader(NULL);
		SetPixelShader(CompileShader(ps_4_0, PixelShaderFunction()));
		SetRasterizerState(SR2RasterizerState);
	}
}
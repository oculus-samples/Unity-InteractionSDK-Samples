float3 _ClipCenter;
float3 _ClipSize;
float4x4 _ClipWorldToLocal;
float _ClipMode;
float _ClipFadeWidth;

void ApplyAreaClip(float3 worldPos, float4 screenPos, float2 screenParamsXY, bool invert)
{
    float distFromEdge = 1.0;

    if (_ClipMode < 0.5)
    {
        return;
    }
    if (_ClipMode < 1.5)
    {
        float3 localPos = mul(_ClipWorldToLocal, float4(worldPos, 1.0)).xyz;
        float3 localDistFromEdge = 0.5 - abs(localPos);
        float3 worldDistFromEdge = localDistFromEdge * abs(_ClipSize);
        distFromEdge = min(min(worldDistFromEdge.x, worldDistFromEdge.y), worldDistFromEdge.z);
    }
    else if (_ClipMode < 2.5)
    {
        float3 offset = worldPos - _ClipCenter;
        float dist = length(offset / _ClipSize);
        float avgScale = (abs(_ClipSize.x) + abs(_ClipSize.y) + abs(_ClipSize.z)) / 3.0;
        distFromEdge = (0.5 - dist) * avgScale;
    }
    else if (_ClipMode < 3.5)
    {
        float3 localPos = mul(_ClipWorldToLocal, float4(worldPos, 1.0)).xyz;
        float distXZ = length(localPos.xz);
        float avgScaleXZ = (abs(_ClipSize.x) + abs(_ClipSize.z)) * 0.5;
        float worldDistFromRadius = (0.5 - distXZ) * avgScaleXZ;
        float worldDistFromY = (0.5 - abs(localPos.y)) * abs(_ClipSize.y);
        distFromEdge = min(worldDistFromRadius, worldDistFromY);
    }

    bool outside = distFromEdge < 0;
    if (invert ? !outside : outside)
        discard;
}

void ApplyAreaClip(float3 worldPos, bool invert)
{
    ApplyAreaClip(worldPos, float4(0,0,0,0), float2(0,0), invert);
}

void ApplyAreaClip(float3 worldPos)
{
    ApplyAreaClip(worldPos, false);
}

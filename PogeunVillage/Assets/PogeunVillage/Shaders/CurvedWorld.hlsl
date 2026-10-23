#ifndef POGEUN_CURVED_WORLD_INCLUDED
#define POGEUN_CURVED_WORLD_INCLUDED

// Set globally by CurvedWorld.cs before each camera renders.
float4 _PV_CurveOrigin;   // xz: top of the "planet" (usually the player), y: ground level
float4 _PV_CurveParams;   // x: planet radius, y: enabled (0/1)

// Wraps the flat world onto a sphere of radius R whose top touches the origin.
// Horizontal distance from the origin becomes arc length, so tall objects tilt
// outward like they would on a tiny planet. Normals are rotated the same way.
void PV_BendWorld(inout float3 positionWS, inout float3 normalWS)
{
    if (_PV_CurveParams.y < 0.5)
        return;

    float radius = max(_PV_CurveParams.x, 1.0);
    float2 offset = positionWS.xz - _PV_CurveOrigin.xz;
    float dist = length(offset);
    if (dist < 1e-4)
        return;

    float2 dir2 = offset / dist;
    float3 dir = float3(dir2.x, 0.0, dir2.y);
    float s, c;
    sincos(dist / radius, s, c);

    float r = radius + (positionWS.y - _PV_CurveOrigin.y);
    positionWS.xz = _PV_CurveOrigin.xz + dir2 * (r * s);
    positionWS.y = _PV_CurveOrigin.y + r * c - radius;

    float along = dot(normalWS, dir);
    float up = normalWS.y;
    float3 side = normalWS - along * dir - float3(0.0, up, 0.0);
    normalWS = side + (along * c + up * s) * dir + float3(0.0, -along * s + up * c, 0.0);
}

#endif

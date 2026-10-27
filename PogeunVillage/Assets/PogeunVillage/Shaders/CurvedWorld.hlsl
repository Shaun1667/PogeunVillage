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

// Set globally by CurvedWorld.cs for game cameras.
float4 _PV_OccluderTarget;  // xyz: point on the player the camera looks at, w: enabled (0/1)
float4 _PV_OccluderParams;  // x: radius, y: margin near the player, z: edge softness, w: strength

// Screen-door fade for anything between the camera and the player, so trees and
// houses never hide the player. Works on the bent position (what is on screen).
// Fragments within `margin` of the player (ground at their feet) are never faded.
void PV_OcclusionClip(float3 positionWS, float4 positionCS)
{
    if (_PV_OccluderTarget.w < 0.5)
        return;

    float3 toTarget = _PV_OccluderTarget.xyz - _WorldSpaceCameraPos;
    float len = length(toTarget);
    if (len < 1e-3)
        return;

    float3 dir = toTarget / len;
    float3 toFrag = positionWS - _WorldSpaceCameraPos;
    float along = dot(toFrag, dir);
    float softness = max(_PV_OccluderParams.z, 1e-3);

    float beforeMargin = saturate((len - _PV_OccluderParams.y - along) / softness);
    float perp = length(toFrag - dir * along);
    float inside = 1.0 - smoothstep(_PV_OccluderParams.x - softness, _PV_OccluderParams.x, perp);
    float fade = beforeMargin * inside * _PV_OccluderParams.w;
    if (fade <= 0.0)
        return;

    // 4x4 Bayer threshold
    const float bayer[16] = { 0, 8, 2, 10, 12, 4, 14, 6, 3, 11, 1, 9, 15, 7, 13, 5 };
    uint2 p = (uint2)positionCS.xy & 3;
    clip((bayer[p.y * 4 + p.x] + 0.5) / 16.0 - fade);
}

#endif

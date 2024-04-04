using System;
using Unity.Mathematics;

public struct NearestPointsOnLineSegmentsRes
{
    public float3 PointOnA;
    public float3 PointOnB;
}
public static class MathHelper
{

    private static readonly float Epsilon = 1e-6f;
    public static NearestPointsOnLineSegmentsRes NearestPointsOnLineSegments(float3 a0, float3 a1, float3 b0, float3 b1)
    {
        float3 r = b0 - a0;
        float3 u = a1 - a0;
        float3 v = b1 - b0;

        float ru = math.dot(r, u);
        float rv = math.dot(r, v);
        float uu = math.dot(u, u);
        float uv = math.dot(u, v);
        float vv = math.dot(v, v);

        float det = uu * vv - uv * uv;
        float s, t;

        if (det < Epsilon * uu * vv)
        {
            s = math.clamp(ru / uu, 0f, 1f);
            t = 0f;
        }
        else
        {
            s = math.clamp((ru * vv - rv * uv) / det, 0f, 1f);
            t = math.clamp((ru * uv - rv * uu) / det, 0f, 1f);
        }

        float S = math.clamp((t * uv + ru) / uu, 0f, 1f);
        float T = math.clamp((s * uv - rv) / vv, 0f, 1f);

        float3 A = a0 + S * u;
        float3 B = b0 + T * v;

        return new NearestPointsOnLineSegmentsRes { PointOnA = A, PointOnB = B };
    }
    public static NearestPointsOnLineSegmentsRes NearestPointsOnLineSegments(float3Pair A, float3Pair B) { return NearestPointsOnLineSegments(A.A, A.B, B.A, B.B); }

}
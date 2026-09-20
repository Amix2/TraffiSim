
using System.Reflection.Metadata.Ecma335;
using Unity.Mathematics;

public struct BezierCurve
{
    public float2 P0, P1;
    public float2 H0, H1;    // handle

    public static BezierCurve Cubic(float2 P0, float2 H0, float2 P1, float2 H1)
    {
        return new BezierCurve{ P0 = P0, H0 = H0, P1 = P1, H1 = H1};
    }

    public static BezierCurve Quadratic(float2 P0, float2 P1, float2 Q)
    {
        float2 H0 = P0 + 2.0f / 3.0f * (Q - P0);
        float2 H1 = P1 + 2.0f / 3.0f * (Q - P1);
        return Cubic(P0, H0 - P0, P1, H1 - P1);
    }
    public static BezierCurve Linear(float2 P0, float2 P1)
    {
        float2 H0 = P0 + 1.0f / 3.0f * (P1 - P0);
        float2 H1 = P0 + 2.0f / 3.0f * (P1 - P0);
        return Cubic(P0, H0 - P0, P1, H1 - P1);
    }

    public readonly float2 Evaluate(float t)
    {
        float2 C0 = P0 + H0;
        float2 C1 = P1 + H1;
        return 
            (1 - t) * (1 - t)* (1 - t) * P0 
            + 3 * t * (1 - t) * (1 - t) * C0 
            + 3 * t * t * (1 - t) * C1 
            + t * t * t * P1;
    }

    public readonly float2 Derivative(float t)
    {
        float2 C0 = P0 + H0;
        float2 C1 = P1 + H1;
        return 3 * (1 - t) * (1 - t) * (C0 - P0)
            + 3 * 2 * t * (1 - t) * (C1 - C0)
            + 3 * t * t * (P1 - C1);
    }
    public readonly float2 Tangent(float t, bool bLeft)
    {
        return math.normalizesafe(Derivative(t).yx * (bLeft ? new float2(-1, 1) : new float2(1, -1)));
    }

}

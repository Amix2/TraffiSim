using Unity.Mathematics;

public struct BezierCurve
{
    public float2 P0, P1;
    public float2 H0, H1;    // handle

    public static BezierCurve Cubic(float2 P0, float2 H0, float2 P1, float2 H1)
    {
        return new BezierCurve { P0 = P0, H0 = H0, P1 = P1, H1 = H1 };
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
        float u = 1 - t;
        return
            u * u * u * P0
            + 3 * t * u * u * C0
            + 3 * t * t * u * C1
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

    public readonly float2 SecondDerivative(float t)
    {
        float2 C0 = P0 + H0;
        float2 C1 = P1 + H1;
        return 6 * (1 - t) * (C1 - 2 * C0 + P0)
            + 6 * t * (P1 - 2 * C1 + C0);
    }

    public readonly float2 Tangent(float t, bool bLeft)
    {
        return math.normalizesafe(Derivative(t).yx * (bLeft ? new float2(-1, 1) : new float2(1, -1)));
    }

    /// <summary>Squared distance from point p to the closest point on the curve.</summary>
    public readonly float DistanceSqToPoint(float2 p)
    {
        return DistanceSqToPoint(p, out _);
    }

    /// <summary>Squared distance from point p to the curve; also returns the curve parameter of the closest point.</summary>
    public readonly float DistanceSqToPoint(float2 p, out float tClosest)
    {
        // 1) Coarse sampling to find the right basin (avoids Newton locking onto a local minimum).
        const int Samples = 16;
        float bestT = 0.0f;
        float bestD = math.distancesq(P0, p);
        for (int i = 1; i <= Samples; i++)
        {
            float s = (float)i / Samples;
            float d = math.distancesq(Evaluate(s), p);
            if (d < bestD) { bestD = d; bestT = s; }
        }

        // 2) Newton refinement on f(t) = (B(t) - p) · B'(t) = 0
        //    f'(t) = B'(t) · B'(t) + (B(t) - p) · B''(t)
        float t = bestT;
        for (int iter = 0; iter < 6; iter++)
        {
            float2 diff = Evaluate(t) - p;
            float2 d1 = Derivative(t);
            float2 d2 = SecondDerivative(t);

            float num = math.dot(diff, d1);
            float den = math.dot(d1, d1) + math.dot(diff, d2);
            if (den <= 1e-8f) break; // flat or heading toward a maximum; keep sampled result

            float next = math.saturate(t - num / den);
            bool converged = math.abs(next - t) < 1e-6f;
            t = next;
            if (converged) break;
        }

        float refined = math.distancesq(Evaluate(t), p);
        if (refined < bestD) { bestD = refined; bestT = t; }

        tClosest = bestT;
        return bestD;
    }
}
using Unity.Collections;
using Unity.Mathematics;

public struct BezierFit
{
    public BezierCurve curve;

    private float2 D0, D1;  // drivers

    public BezierFit(NativeArray<float2> points, NativeArray<float> Ts, float2 d0, float2 d1)
    {
        curve = default;
        curve.P0 = points[0];
        curve.P1 = points[^1];
        D0 = math.normalize(d0);
        D1 = math.normalize(d1);
        if (points.Length < 2)
            return;

        GuessTs(points, Ts);

        SetHandlesFromTs(points, Ts);

        for(int iter=0; iter<100; iter++)
        {
            float err = EvaluateNewTs(points, Ts);
            UnityEngine.Debug.Log(err);
            SetHandlesFromTs(points, Ts);
        }

    }

    private readonly float EvaluateNewTs(NativeArray<float2> points, NativeArray<float> Ts)
    {
        float2 P0 = points[0];
        float2 P1 = points[^1];
        float maxError = 0.0f;
        for (int i = 0; i < points.Length; i++)
        {
            float distSq = curve.DistanceSqToPoint(points[i], out float newT);
            Ts[i] = newT;
            maxError = math.max(maxError, distSq);
        }
        return maxError;
    }

    private void SetHandlesFromTs(NativeArray<float2> points, NativeArray<float> Ts)
    {
        float2 P0 = points[0];
        float2 P1 = points[^1];
        float c = math.dot(D0, D1);
        float a11, a12, a22, s1, s2;
        a11 = a12 = a22 = s1 = s2 = 0;
        for (int i = 0; i < points.Length; i++)
        {
            float t = Ts[i];
            float u = 1 - t;
            float b0 = u * u * u;
            float b1 = 3 * t * u * u;
            float b2 = 3 * t * t * u;
            float b3 = t * t * t;
            float2 K = (b0 + b1) * P0 + (b2 + b3) * P1;
            float2 r = points[i] - K;
            a11 += b1 * b1;
            a22 += b2 * b2;
            a12 += b1 * b2 * c;
            s1 += b1 * math.dot(D0, r);
            s2 += b2 * math.dot(D1, r);
        }
        float det = a11 * a22 - a12 * a12;
        float alpha, beta;
        alpha = beta = math.distance(P0, P1) / 3;
        if (det > 1e-6)
        {   // proper calculation
            alpha = (a22 * s1 - a12 * s2) / det;
            beta = (a11 * s2 - a12 * s1) / det;
        }
        if(alpha <= 0 ||  beta <= 0)
            alpha = beta = math.distance(P0, P1) / 3;
        SetHandles(alpha, beta);
    }

    private static void GuessTs(NativeArray<float2> points, NativeArray<float> Ts)
    {
        float2 P0 = points[0];
        float2 P1 = points[^1];
        for (int i = 0; i < Ts.Length; i++)
        {
            if (Ts[i] >= 0)
                continue;
            float2 P = points[i];
            float x0 = math.length(P - P0);
            float x1 = math.length(P - P1);
            float t = x0 / (x1 + x0);
            Ts[i] = t;
        }
    }

    private void SetHandles(float alpha, float beta)
    {
        curve.H0 = D0 * alpha;
        curve.H1 = D1 * beta;
    }
}
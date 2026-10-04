using Unity.Mathematics;

public struct Arc
{
    public float2 StartP, StartDir;
    public float Curvature;
    public float Length;

    /// <summary>
    /// Create arc from 2 point and direction
    /// </summary>
    /// <param name="P">Start point</param>
    /// <param name="T">Direction at start point P</param>
    /// <param name="Q">Target position</param>
    public Arc(float2 P, float2 T, float2 Q)
    {
        float2 n = new(-T.y, T.x);
        float2 D = Q - P;
        float DD = math.dot(D, D);
        Curvature = DD != 0 ? 2 * math.dot(n, D) / DD : 0;
        StartDir = T;
        StartP = P;
        float ang = math.atan2(math.dot(n, D), math.dot(T, D));
        Length = math.abs(ang) > 1e-6 ? math.length(D) * ang / math.sin(ang) : math.length(D);
    }

    public readonly bool CanOffset(float X)
    {
        return 1 - X * Curvature >= 0.001;
    }

    public Arc Offset(float X)
    {
        Unity.Assertions.Assert.IsTrue(CanOffset(X));
        float2 n = new(-StartDir.y, StartDir.x);
        return new Arc
        {
            StartP = StartP + X * n,
            StartDir = StartDir,
            Curvature = Curvature / (1 - X * Curvature),
            Length = Length * (1 - X * Curvature),
        };
    }

    public readonly PositionDirection Evaluate(float s)
    {
        float2 n = new(-StartDir.y, StartDir.x);
        float a = Curvature * s;
        float sa = math.sin(a), ca = math.cos(a);

        float along, side;
        if (math.abs(a) < 1e-4f)
        {
            along = s * (1 - a * a / 6);
            side = s * a * 0.5f;
        }
        else
        {
            along = sa / Curvature;
            side = (1 - ca) / Curvature;
        }

        return new PositionDirection
        {
            Pos = StartP + StartDir * along + n * side,
            Dir = StartDir * ca + n * sa
        };
    }
}

public struct Biarc
{
    public Arc Arc0, Arc1;

    public readonly float Length => Arc0.Length + Arc1.Length;

    public readonly PositionDirection Evaluate(float s)
    {
        return s <= Arc0.Length ? Arc0.Evaluate(s) : Arc1.Evaluate(s - Arc0.Length);
    }

    public readonly bool CanOffset(float X) => Arc0.CanOffset(X) && Arc1.CanOffset(X);

    public Biarc Offset(float X)
    {
        return new Biarc
        {
            Arc0 = Arc0.Offset(X),
            Arc1 = Arc1.Offset(X),
        };
    }

    public Biarc(float2 P0, float2 T0, float2 P1, float2 T1, float r = 1)
    {
        T0 = math.normalize(T0);
        T1 = math.normalize(T1);
        Arc0 = Arc1 = default;
        float2 v = P1 - P0;
        float c = math.dot(T0, T1);
        float2 u = r * T0 + T1;

        float vv = math.dot(v, v);
        float vu = math.dot(v, u);
        float denom = (vu + math.sqrt(math.square(vu) + 2 * r * (1 - c) * vv));
        Unity.Assertions.Assert.IsFalse(denom <= 0);

        if (denom <= 0)
            return; // failure
        float d2 = vv / denom;
        float d1 = r * d2;

        float2 A = P0 + d1 * T0;
        float2 B = P1 - d2 * T1;

        float2 M = A + (d1 / (d1 + d2)) * (B - A);

        Arc0 = new Arc(P0, T0, M);
        Arc1 = new Arc(M, Arc0.Evaluate(Arc0.Length).Dir, P1);
    }
}
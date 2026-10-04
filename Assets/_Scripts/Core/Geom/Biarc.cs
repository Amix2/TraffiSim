using Unity.Collections;
using Unity.Mathematics;

/// <summary>
/// A line or circular arc: start point, unit start direction, signed curvature
/// (positive turns left, 0 is a straight line) and length along the curve.
/// </summary>
public struct Arc
{
    public float2 StartP, StartDir;
    public float Curvature;
    public float Length;

    /// <summary>Smallest lane radius allowed when offsetting, in world units.</summary>
    public const float DefaultMinRadius = 0.1f;

    /// <summary>
    /// Create the arc that starts at P facing T and passes through Q.
    /// The result is invalid (IsValid == false) if Q equals P, T is zero,
    /// or Q is not ahead of P (the arc would have to turn 180 degrees or more).
    /// </summary>
    /// <param name="P">Start point</param>
    /// <param name="T">Direction at start point P (normalised here)</param>
    /// <param name="Q">Target position</param>
    public Arc(float2 P, float2 T, float2 Q)
    {
        StartP = P;
        StartDir = math.normalizesafe(T);
        Curvature = 0f;
        Length = 0f;

        float2 D = Q - P;
        float DD = math.dot(D, D);
        if (DD < 1e-12f || math.lengthsq(StartDir) < 0.5f)
            return;

        float2 n = new(-StartDir.y, StartDir.x);
        float along = math.dot(StartDir, D);
        if (along <= 0f)
            return; // target not ahead: arc would turn 180 degrees or more

        float side = math.dot(n, D);
        Curvature = 2f * side / DD;

        float halfTurn = math.atan2(side, along); // |halfTurn| < pi/2 here
        float chord = math.sqrt(DD);
        Length = math.abs(halfTurn) > 1e-6f ? chord * halfTurn / math.sin(halfTurn) : chord;
    }

    public readonly bool IsValid =>
        Length > 0f && math.isfinite(Length) && math.isfinite(Curvature) && math.lengthsq(StartDir) > 0.5f;

    /// <summary>Left normal at the start.</summary>
    public readonly float2 Normal => new(-StartDir.y, StartDir.x);

    /// <summary>Total heading change in radians (curvature * length).</summary>
    public readonly float Turn => Curvature * Length;

    public readonly float2 EndPos => Evaluate(Length).Pos;
    public readonly float2 EndDir => Evaluate(Length).Dir;

    /// <summary>Position and unit direction at distance s along the arc (clamped to [0, Length]).</summary>
    public readonly PositionDirection Evaluate(float s)
    {
        s = math.clamp(s, 0f, Length);
        float2 n = Normal;
        float a = Curvature * s;
        float sa = math.sin(a), ca = math.cos(a);

        float along, side;
        if (math.abs(a) < 1e-4f)
        {
            along = s * (1f - a * a / 6f);
            side = s * a * 0.5f;
        }
        else
        {
            along = sa / Curvature;
            side = (1f - ca) / Curvature;
        }

        return new PositionDirection
        {
            Pos = StartP + StartDir * along + n * side,
            Dir = StartDir * ca + n * sa
        };
    }

    // ---------------------------------------------------------------- offsets

    /// <summary>
    /// True if offsetting by X (positive = left) keeps the lane radius at least minRadius.
    /// Lines can always be offset.
    /// </summary>
    public readonly bool CanOffset(float X, float minRadius = DefaultMinRadius)
    {
        float f = 1f - X * Curvature;
        return f > 0f && f >= minRadius * math.abs(Curvature);
    }

    /// <summary>Offset by X (positive = left). Returns false if the lane would be too tight or fold over.</summary>
    public readonly bool TryOffset(float X, out Arc result, float minRadius = DefaultMinRadius)
    {
        result = default;
        if (!IsValid || !CanOffset(X, minRadius))
            return false;

        float f = 1f - X * Curvature;
        result = new Arc
        {
            StartP = StartP + X * Normal,
            StartDir = StartDir,
            Curvature = Curvature / f,
            Length = Length * f,
        };
        return true;
    }

    /// <summary>Offset by X (positive = left). Returns an invalid arc if the offset is not possible.</summary>
    public readonly Arc Offset(float X, float minRadius = DefaultMinRadius)
    {
        bool ok = TryOffset(X, out Arc result, minRadius);
        Unity.Assertions.Assert.IsTrue(ok);
        return result;
    }

    // --------------------------------------------------------------- averages
    // Only meaningful for arcs that are offsets of the same arc (same centre,
    // same start direction, same turn). Start points and lengths average
    // linearly; curvature is recovered as turn / length.

    public static Arc Lerp(in Arc a, in Arc b, float t)
    {
        var acc = new ArcAccumulator();
        acc.Add(a, 1f - t);
        acc.Add(b, t);
        return acc.Result();
    }

    public static Arc Average(in Arc a, in Arc b) => Lerp(a, b, 0.5f);

    public static Arc Average(NativeArray<Arc> arcs)
    {
        var acc = new ArcAccumulator();
        for (int i = 0; i < arcs.Length; i++)
            acc.Add(arcs[i], 1f);
        return acc.Result();
    }

    /// <summary>Weighted average. Weights do not need to sum to 1; they are normalised.</summary>
    public static Arc WeightedAverage(NativeArray<Arc> arcs, NativeArray<float> weights)
    {
        Unity.Assertions.Assert.AreEqual(arcs.Length, weights.Length);
        var acc = new ArcAccumulator();
        int count = math.min(arcs.Length, weights.Length);
        for (int i = 0; i < count; i++)
            acc.Add(arcs[i], weights[i]);
        return acc.Result();
    }
}

/// <summary>
/// Accumulates weighted arcs that are offsets of one arc and produces their average.
/// Allocation-free and Burst-friendly.
/// </summary>
public struct ArcAccumulator
{
    private float2 sumP, sumDir;
    private float sumTurn, sumLength, sumWeight;
    private float firstTurn;
    private float2 firstDir;
    private int count;
    private bool invalid;

    public void Add(in Arc arc, float weight)
    {
        if (!arc.IsValid)
        {
            invalid = true;
            return;
        }

        if (count == 0)
        {
            firstTurn = arc.Turn;
            firstDir = arc.StartDir;
        }
        else
        {
            // Inputs must be offsets of one arc: same turn and same start direction.
            Unity.Assertions.Assert.IsTrue(math.abs(arc.Turn - firstTurn) <= 1e-3f * (1f + math.abs(firstTurn)));
            Unity.Assertions.Assert.IsTrue(math.dot(arc.StartDir, firstDir) > 0.9999f);
        }

        count++;
        sumWeight += weight;
        sumP += weight * arc.StartP;
        sumDir += weight * arc.StartDir;
        sumTurn += weight * arc.Turn;
        sumLength += weight * arc.Length;
    }

    public readonly Arc Result()
    {
        if (invalid || count == 0 || math.abs(sumWeight) < 1e-6f)
            return default;

        float length = sumLength / sumWeight;
        if (!(length > 1e-6f))
            return default;

        return new Arc
        {
            StartP = sumP / sumWeight,
            StartDir = math.normalizesafe(sumDir / sumWeight, firstDir),
            Length = length,
            // Sum of turns / sum of lengths: the total weight cancels, giving
            // average turn / average length.
            Curvature = sumTurn / sumLength,
        };
    }
}

/// <summary>
/// Two arcs joined smoothly, from P0 facing T0 to P1 arriving facing T1.
/// </summary>
public struct Biarc
{
    public Arc Arc0, Arc1;
    public bool IsValid;

    /// <summary>Allowed range for the tangent-length ratio r = d1 / d2.</summary>
    public const float MinRatio = 0.05f;
    public const float MaxRatio = 20f;

    public readonly float Length => Arc0.Length + Arc1.Length;
    public readonly float Turn => Arc0.Turn + Arc1.Turn;
    public readonly float2 Joint => Arc1.StartP;

    /// <param name="P0">Start point</param>
    /// <param name="T0">Direction leaving P0</param>
    /// <param name="P1">End point</param>
    /// <param name="T1">Direction arriving at P1 (direction of travel)</param>
    /// <param name="r">Tangent-length ratio d1 / d2; 1 is the symmetric default</param>
    public Biarc(float2 P0, float2 T0, float2 P1, float2 T1, float r = 1f)
    {
        Arc0 = Arc1 = default;
        IsValid = false;

        T0 = math.normalizesafe(T0);
        T1 = math.normalizesafe(T1);
        if (math.lengthsq(T0) < 0.5f || math.lengthsq(T1) < 0.5f)
            return;

        r = math.clamp(r, MinRatio, MaxRatio);

        float2 v = P1 - P0;
        float vv = math.dot(v, v);
        if (vv < 1e-12f)
            return; // start and end are the same point

        float c = math.dot(T0, T1);
        float2 u = r * T0 + T1;
        float vu = math.dot(v, u);
        float denom = vu + math.sqrt(vu * vu + 2f * r * (1f - c) * vv);
        if (!(denom > 1e-12f))
            return; // no biarc with positive tangent lengths

        float d2 = vv / denom;
        float d1 = r * d2;

        float2 A = P0 + d1 * T0;
        float2 B = P1 - d2 * T1;
        float2 M = A + (d1 / (d1 + d2)) * (B - A);

        Arc arc0 = new Arc(P0, T0, M);
        if (!arc0.IsValid)
            return;

        Arc arc1 = new Arc(M, arc0.EndDir, P1);
        if (!arc1.IsValid || math.dot(arc1.EndDir, T1) < 0.9999f)
            return;

        Arc0 = arc0;
        Arc1 = arc1;
        IsValid = true;
    }

    /// <summary>Position and unit direction at distance s along the biarc (clamped to [0, Length]).</summary>
    public readonly PositionDirection Evaluate(float s)
    {
        s = math.clamp(s, 0f, Length);
        return s <= Arc0.Length ? Arc0.Evaluate(s) : Arc1.Evaluate(s - Arc0.Length);
    }

    // ---------------------------------------------------------------- offsets

    public readonly bool CanOffset(float X, float minRadius = Arc.DefaultMinRadius) =>
        IsValid && Arc0.CanOffset(X, minRadius) && Arc1.CanOffset(X, minRadius);

    /// <summary>Offset both arcs by X (positive = left). The result stays connected and smooth.</summary>
    public readonly bool TryOffset(float X, out Biarc result, float minRadius = Arc.DefaultMinRadius)
    {
        result = default;
        if (!IsValid)
            return false;
        if (!Arc0.TryOffset(X, out Arc a0, minRadius) || !Arc1.TryOffset(X, out Arc a1, minRadius))
            return false;

        result = new Biarc { Arc0 = a0, Arc1 = a1, IsValid = true };
        return true;
    }

    /// <summary>Offset by X (positive = left). Returns an invalid biarc if the offset is not possible.</summary>
    public readonly Biarc Offset(float X, float minRadius = Arc.DefaultMinRadius)
    {
        bool ok = TryOffset(X, out Biarc result, minRadius);
        Unity.Assertions.Assert.IsTrue(ok);
        return result;
    }

    // --------------------------------------------------------------- averages
    // Only for biarcs that are offsets of one biarc. If you have that reference
    // biarc, reference.Offset((X1 + X2) / 2) gives the same result directly.

    public static Biarc Lerp(in Biarc a, in Biarc b, float t)
    {
        if (!a.IsValid || !b.IsValid)
            return default;
        return Make(Arc.Lerp(a.Arc0, b.Arc0, t), Arc.Lerp(a.Arc1, b.Arc1, t));
    }

    public static Biarc Average(in Biarc a, in Biarc b) => Lerp(a, b, 0.5f);

    public static Biarc Average(NativeArray<Biarc> biarcs)
    {
        var acc0 = new ArcAccumulator();
        var acc1 = new ArcAccumulator();
        for (int i = 0; i < biarcs.Length; i++)
        {
            Biarc b = biarcs[i];
            if (!b.IsValid)
                return default;
            acc0.Add(b.Arc0, 1f);
            acc1.Add(b.Arc1, 1f);
        }
        return Make(acc0.Result(), acc1.Result());
    }

    /// <summary>Weighted average. Weights do not need to sum to 1; they are normalised.</summary>
    public static Biarc WeightedAverage(NativeArray<Biarc> biarcs, NativeArray<float> weights)
    {
        Unity.Assertions.Assert.AreEqual(biarcs.Length, weights.Length);
        var acc0 = new ArcAccumulator();
        var acc1 = new ArcAccumulator();
        int count = math.min(biarcs.Length, weights.Length);
        for (int i = 0; i < count; i++)
        {
            Biarc b = biarcs[i];
            if (!b.IsValid)
                return default;
            acc0.Add(b.Arc0, weights[i]);
            acc1.Add(b.Arc1, weights[i]);
        }
        return Make(acc0.Result(), acc1.Result());
    }

    private static Biarc Make(in Arc a0, in Arc a1) =>
        new Biarc { Arc0 = a0, Arc1 = a1, IsValid = a0.IsValid && a1.IsValid };
}
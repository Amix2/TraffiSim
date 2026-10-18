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

    /// <summary>
    /// Number of equal segments so that the largest gap between each straight segment
    /// and the curve is at most tolerance (in metres), for every curve offset up to
    /// maxOffset on either side.
    /// </summary>
    public readonly int SegmentCount(float tolerance, float maxOffset = 0f)
    {
        float turn = math.abs(Turn);
        if (turn < 1e-6f)
            return 1;                                                    // straight: endpoints only

        float R = 1f / math.abs(Curvature) + math.abs(maxOffset);       // outermost edge
        float c = math.clamp(1f - tolerance / R, -1f, 1f);
        float maxStep = math.max(2f * math.acos(c), 1e-4f);              // largest angle per segment
        return math.max(1, (int)math.ceil(turn / maxStep));
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
        if (math.lengthsq(T0) < 0.5f)
            T0 = math.normalizesafe(P1 - P0);
        if (math.lengthsq(T1) < 0.5f)
            T1 = T0;

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

    /// <summary>Position and unit direction at fraction t along the biarc (clamped to [0, 1]).</summary>
    public readonly PositionDirection EvaluateFract(float t)
    {
        float s = Length * t;
        return Evaluate(s);
    }

    /// <summary>
    /// Evaluate this biarc at a fraction measured on a reference biarc it is an
    /// offset of (for example the centre line). Points match up across lanes,
    /// including exactly at the joint.
    /// </summary>
    public readonly PositionDirection EvaluateFractMatched(float f, in Biarc reference)
    {
        float j = reference.Arc0.Length / reference.Length;          // joint fraction on the reference
        if (f <= j)
            return Arc0.Evaluate((j > 0f ? f / j : 0f) * Arc0.Length);
        return Arc1.Evaluate((f - j) / (1f - j) * Arc1.Length);
    }

    /// <summary>
    /// Fills result with fractions (0..1) of the total length to sample at, so every
    /// segment is within tolerance of the curve. Always includes 0, the joint and 1.
    /// </summary>
    public readonly void SampleFractions(NativeList<float> result, float tolerance, float maxOffset = 0f)
    {
        result.Clear();
        float total = Length;
        if (!IsValid || !(total > 0f))
            return;

        result.Add(0f);
        AppendArc(result, Arc0, 0f, total, tolerance, maxOffset);
        AppendArc(result, Arc1, Arc0.Length, total, tolerance, maxOffset);
        result[result.Length - 1] = 1f;
    }

    private static void AppendArc(NativeList<float> result, in Arc arc, float startS, float total,
                          float tolerance, float maxOffset)
    {
        int n = arc.SegmentCount(tolerance, maxOffset);
        for (int i = 1; i < n; i++)
            result.Add((startS + arc.Length * i / n) / total);
        result.Add((startS + arc.Length) / total);   // the joint, or the end
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
}
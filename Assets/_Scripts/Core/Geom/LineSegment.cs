using System;
using System.Runtime.CompilerServices;
using Unity.Mathematics;
using static Unity.Mathematics.math;

public enum SegmentIntersectionType : byte
{
    None = 0,
    Point = 1,   // segments cross / touch at a single point
    Overlap = 2  // segments are collinear and share a sub-segment [Point0, Point1]
}

public struct SegmentIntersection
{
    public SegmentIntersectionType Type;

    /// <summary>Point: the intersection. Overlap: start of the shared part.</summary>
    public float2 Point0;

    /// <summary>Point: same as Point0. Overlap: end of the shared part.</summary>
    public float2 Point1;

    public bool Hit
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Type != SegmentIntersectionType.None;
    }
}

/// <summary>
/// 2D line segment from A to B. Blittable, no managed refs, safe to use in Burst jobs
/// and NativeContainers. Parameter t: 0 = A, 1 = B.
/// </summary>
[Serializable]
public struct LineSegment : IEquatable<LineSegment>
{
    public const float DefaultEpsilon = 1e-6f;

    public float2 A;
    public float2 B;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public LineSegment(float2 a, float2 b)
    { A = a; B = b; }

    // ------------------------------------------------------------------
    // Basic properties
    // ------------------------------------------------------------------

    /// <summary>Unnormalized direction B - A.</summary>
    public float2 Direction { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => B - A; }

    public float2 DirectionNormalized { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => normalizesafe(B - A); }
    public float LengthSq { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => lengthsq(B - A); }
    public float Length { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => length(B - A); }
    public float2 Midpoint { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => (A + B) * 0.5f; }

    /// <summary>Unit normal pointing to the left of A->B (counter-clockwise).</summary>
    public float2 Normal { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => normalizesafe(Perp(B - A)); }

    public float2 Min { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => min(A, B); }
    public float2 Max { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => max(A, B); }
    public bool IsDegenerate { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => LengthSq <= DefaultEpsilon * DefaultEpsilon; }

    public LineSegment Reversed { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => new LineSegment(B, A); }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float2 PointAt(float t) => A + (B - A) * t;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public LineSegment Translated(float2 offset) => new LineSegment(A + offset, B + offset);

    // ------------------------------------------------------------------
    // Static helpers
    // ------------------------------------------------------------------

    /// <summary>2D cross product (z of the 3D cross). > 0 if b is CCW from a.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Cross(float2 a, float2 b) => a.x * b.y - a.y * b.x;

    /// <summary>Perpendicular rotated 90* CCW.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float2 Perp(float2 v) => new float2(-v.y, v.x);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int Orient(float2 a, float2 b, float2 c)
    {
        float v = Cross(b - a, c - a);
        return v > 0f ? 1 : (v < 0f ? -1 : 0);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool InBox(float2 a, float2 b, float2 p)
        => all(p >= min(a, b)) && all(p <= max(a, b));

    // ------------------------------------------------------------------
    // Point queries
    // ------------------------------------------------------------------

    /// <summary> > 0 if p is left of A->B, < 0 if right, 0 if on the infinite line. (Not normalized.)</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float Side(float2 p) => Cross(B - A, p - A);

    /// <summary>Signed perpendicular distance from p to the infinite line through A and B (positive = left).</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float SignedDistanceToLine(float2 p)
    {
        float len = Length;
        return len > DefaultEpsilon ? Cross(B - A, p - A) / len : distance(p, A);
    }

    /// <summary>Parameter of p projected onto the infinite line (not clamped).</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float ProjectT(float2 p)
    {
        float2 d = B - A;
        float lsq = lengthsq(d);
        return lsq > 0f ? dot(p - A, d) / lsq : 0f;
    }

    /// <summary>Parameter of the closest point on the segment, clamped to [0,1].</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float ClosestT(float2 p) => saturate(ProjectT(p));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float2 ClosestPoint(float2 p) => PointAt(ClosestT(p));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float DistanceSq(float2 p) => distancesq(p, ClosestPoint(p));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float Distance(float2 p) => sqrt(DistanceSq(p));

    /// <summary>True if p lies on the segment within epsilon distance.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(float2 p, float epsilon = DefaultEpsilon) => DistanceSq(p) <= epsilon * epsilon;

    /// <summary>True if the segment touches or enters a circle.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IntersectsCircle(float2 center, float radius) => DistanceSq(center) <= radius * radius;

    // ------------------------------------------------------------------
    // Segment vs segment
    // ------------------------------------------------------------------

    /// <summary>Axis-aligned bounding box overlap — cheap early-out before precise tests.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool BoundsOverlap(in LineSegment other)
        => all(Min <= other.Max) && all(other.Min <= Max);

    /// <summary>
    /// Fast exact yes/no test (no divisions). Touching endpoints and collinear overlap count as intersecting.
    /// </summary>
    public bool Intersects(in LineSegment other)
    {
        int o1 = Orient(A, B, other.A);
        int o2 = Orient(A, B, other.B);
        int o3 = Orient(other.A, other.B, A);
        int o4 = Orient(other.A, other.B, B);

        if (o1 != o2 && o3 != o4) return true;

        // Collinear special cases
        if (o1 == 0 && InBox(A, B, other.A)) return true;
        if (o2 == 0 && InBox(A, B, other.B)) return true;
        if (o3 == 0 && InBox(other.A, other.B, A)) return true;
        if (o4 == 0 && InBox(other.A, other.B, B)) return true;
        return false;
    }

    /// <summary>
    /// Full intersection with tolerance. Returns None, a single Point, or a collinear Overlap range.
    /// </summary>
    public SegmentIntersection Intersect(in LineSegment other, float epsilon = DefaultEpsilon)
    {
        var result = default(SegmentIntersection);

        float2 r = B - A;
        float2 s = other.B - other.A;
        float2 q = other.A - A;
        float rr = lengthsq(r);
        float ss = lengthsq(s);
        float eps2 = epsilon * epsilon;

        // Degenerate cases (one or both segments are points)
        if (rr <= eps2)
        {
            if (other.Contains(A, epsilon)) SetPoint(ref result, A);
            return result;
        }
        if (ss <= eps2)
        {
            if (Contains(other.A, epsilon)) SetPoint(ref result, other.A);
            return result;
        }

        float denom = Cross(r, s);
        float qxr = Cross(q, r);

        // Parallel?  |sin(angle)| <= epsilon
        if (denom * denom <= eps2 * rr * ss)
        {
            // Not on the same line -> no intersection
            if (qxr * qxr > eps2 * rr) return result;

            // Collinear: project other onto this segment's parameter space
            float t0 = dot(q, r) / rr;
            float t1 = t0 + dot(s, r) / rr;
            float lo = max(0f, min(t0, t1));
            float hi = min(1f, max(t0, t1));

            float tEps = epsilon / sqrt(rr);
            if (lo > hi + tEps) return result;

            float2 p0 = PointAt(lo);
            float2 p1 = PointAt(max(lo, hi));
            if (distancesq(p0, p1) <= eps2)
            {
                SetPoint(ref result, p0);
            }
            else
            {
                result.Type = SegmentIntersectionType.Overlap;
                result.Point0 = p0;
                result.Point1 = p1;
            }
            return result;
        }

        float t = Cross(q, s) / denom; // on this
        float u = qxr / denom;          // on other

        float tTol = epsilon / sqrt(rr);
        float uTol = epsilon / sqrt(ss);
        if (t >= -tTol && t <= 1f + tTol && u >= -uTol && u <= 1f + uTol)
            SetPoint(ref result, PointAt(saturate(t)));

        return result;
    }

    /// <summary>
    /// Convenience: true if the segments meet. For a collinear overlap, point = start of the shared part.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetIntersection(in LineSegment other, out float2 point, float epsilon = DefaultEpsilon)
    {
        var hit = Intersect(other, epsilon);
        point = hit.Point0;
        return hit.Hit;
    }

    /// <summary>
    /// Intersection of the two INFINITE lines. tThis / tOther are the parameters on each line,
    /// so you can filter them yourself (e.g. tThis >= 0 for a ray). False if parallel.
    /// </summary>
    public bool TryIntersectLines(in LineSegment other, out float2 point, out float tThis, out float tOther)
    {
        float2 r = B - A;
        float2 s = other.B - other.A;
        float denom = Cross(r, s);
        if (abs(denom) <= 1e-12f)
        {
            point = default; tThis = 0f; tOther = 0f;
            return false;
        }
        float2 q = other.A - A;
        tThis = Cross(q, s) / denom;
        tOther = Cross(q, r) / denom;
        point = A + r * tThis;
        return true;
    }

    /// <summary>Closest points between two segments (equal if they intersect).</summary>
    public void ClosestPoints(in LineSegment other, out float2 onThis, out float2 onOther)
    {
        if (TryGetIntersection(other, out float2 hit))
        {
            onThis = onOther = hit;
            return;
        }

        // Non-intersecting segments: the minimum is always at an endpoint of one of them.
        float2 c0 = ClosestPoint(other.A);
        float2 c1 = ClosestPoint(other.B);
        float2 c2 = other.ClosestPoint(A);
        float2 c3 = other.ClosestPoint(B);

        float d0 = distancesq(c0, other.A);
        float d1 = distancesq(c1, other.B);
        float d2 = distancesq(c2, A);
        float d3 = distancesq(c3, B);

        onThis = c0; onOther = other.A; float best = d0;
        if (d1 < best) { best = d1; onThis = c1; onOther = other.B; }
        if (d2 < best) { best = d2; onThis = A; onOther = c2; }
        if (d3 < best) { onThis = B; onOther = c3; }
    }

    public float DistanceSq(in LineSegment other)
    {
        if (Intersects(other)) return 0f;
        return min(min(DistanceSq(other.A), DistanceSq(other.B)),
                   min(other.DistanceSq(A), other.DistanceSq(B)));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float Distance(in LineSegment other) => sqrt(DistanceSq(other));

    /// <summary>True if the infinite lines are parallel (or collinear) within epsilon on sin(angle).</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsParallel(in LineSegment other, float epsilon = DefaultEpsilon)
    {
        float2 r = B - A, s = other.B - other.A;
        float c = Cross(r, s);
        return c * c <= epsilon * epsilon * lengthsq(r) * lengthsq(s);
    }

    // ------------------------------------------------------------------
    // Misc
    // ------------------------------------------------------------------

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void SetPoint(ref SegmentIntersection r, float2 p)
    {
        r.Type = SegmentIntersectionType.Point;
        r.Point0 = p;
        r.Point1 = p;
    }

    public bool Equals(LineSegment other) => all(A == other.A) && all(B == other.B);

    public override bool Equals(object obj) => obj is LineSegment o && Equals(o);

    public override int GetHashCode() => (int)hash(new float4(A, B));

    public static bool operator ==(LineSegment l, LineSegment r) => l.Equals(r);

    public static bool operator !=(LineSegment l, LineSegment r) => !l.Equals(r);

    public override string ToString() => $"LineSegment({A}, {B})";
}
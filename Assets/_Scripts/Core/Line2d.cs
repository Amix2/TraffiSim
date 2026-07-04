using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Mathematics;

/// <summary>
/// Infinite 2D line. Plain unmanaged struct — fully blittable, safe to use as a
/// job field, inside NativeArrays, and in Burst-compiled code.
/// Represented as a point on the line plus a unit direction along it.
/// </summary>
public struct Line2D
{
    /// <summary>A point the line passes through.</summary>
    public float2 Point;

    /// <summary>Unit direction along the line.</summary>
    public float2 Direction;

    /// <summary>Unit normal (perpendicular to the line).</summary>
    public float2 Normal
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new float2(-Direction.y, Direction.x);
    }

    // -----------------------------------------------------------------
    // Constructor 1: from a point on the line and a direction that is
    // PERPENDICULAR to the line (i.e. the line's normal).
    // -----------------------------------------------------------------
    public Line2D(float2 pointOnLine, float2 perpendicular)
    {
        Point = pointOnLine;
        float2 n = math.normalizesafe(perpendicular, new float2(0f, 1f));
        Direction = new float2(n.y, -n.x); // rotate the normal 90° to get the line direction
    }

    // -----------------------------------------------------------------
    // Constructor 2: orthogonal (total) least-squares fit of a point set.
    // Minimizes the sum of squared PERPENDICULAR distances to the line.
    // The line passes through the centroid of the points.
    // -----------------------------------------------------------------
    public Line2D(NativeArray<float2> points)
    {
        int count = points.Length;
        if (count == 0)
        {
            Point = float2.zero;
            Direction = new float2(1f, 0f);
            return;
        }

        float2 centroid = float2.zero;
        for (int i = 0; i < count; i++)
            centroid += points[i];
        centroid /= count;

        float sxx = 0f, sxy = 0f, syy = 0f;
        for (int i = 0; i < count; i++)
        {
            float2 d = points[i] - centroid;
            sxx += d.x * d.x;
            sxy += d.x * d.y;
            syy += d.y * d.y;
        }

        Point = centroid;
        Direction = DirectionFromMoments(sxx, sxy, syy);
    }

    // -----------------------------------------------------------------
    // Constructor 3: best-fit line CONSTRAINED to pass through `pivot`.
    // Only the orientation is optimized; moments are taken about the
    // pivot instead of the centroid.
    // -----------------------------------------------------------------
    public Line2D(NativeArray<float2> points, float2 pivot)
    {
        float sxx = 0f, sxy = 0f, syy = 0f;
        for (int i = 0; i < points.Length; i++)
        {
            float2 d = points[i] - pivot;
            sxx += d.x * d.x;
            sxy += d.x * d.y;
            syy += d.y * d.y;
        }

        Point = pivot;
        Direction = DirectionFromMoments(sxx, sxy, syy);
    }

    /// <summary>
    /// Direction that minimizes the sum of squared perpendicular distances,
    /// given second moments about the anchor point:
    ///     theta = 0.5 * atan2(2 * sxy, sxx - syy)
    /// (This is the principal eigenvector of the 2x2 covariance matrix.)
    /// </summary>
    private static float2 DirectionFromMoments(float sxx, float sxy, float syy)
    {
        float a = sxx - syy;
        float b = 2f * sxy;

        // Degenerate: 0-1 points, all points identical, or perfectly
        // isotropic cloud (no preferred direction) -> fall back to +X.
        if (a * a + b * b < 1e-12f)
            return new float2(1f, 0f);

        float theta = 0.5f * math.atan2(b, a);
        return new float2(math.cos(theta), math.sin(theta));
    }

    // -----------------------------------------------------------------
    // closest point on the line to a given point
    // (orthogonal projection).
    // -----------------------------------------------------------------
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float2 ClosestPoint(float2 p)
    {
        return Point + math.dot(p - Point, Direction) * Direction;
    }

    /// <summary>Signed perpendicular distance; sign tells which side of the line.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float SignedDistance(float2 p)
    {
        return math.dot(p - Point, Normal);
    }

    // -----------------------------------------------------------------
    // intersection of the line with a circle given by center
    // and radius.
    //
    // Returns the number of intersection points:
    //   2 — line crosses the circle; `a` and `b` are the two points,
    //       ordered along Direction (a comes first).
    //   1 — line is tangent (within float precision); a == b.
    //   0 — no intersection; `a` and `b` are both set to the point on
    //       the line closest to the center (often useful as a fallback).
    // -----------------------------------------------------------------
    public int IntersectCircle(float2 center, float radius, out float2 a, out float2 b)
    {
        float2 foot = ClosestPoint(center);      // center projected onto the line
        float distSq = math.distancesq(center, foot);
        float radiusSq = radius * radius;

        if (distSq > radiusSq)
        {
            a = foot;
            b = foot;
            return 0;
        }

        float halfChord = math.sqrt(math.max(0f, radiusSq - distSq));
        a = foot - halfChord * Direction;
        b = foot + halfChord * Direction;
        return halfChord > 0f ? 2 : 1;
    }

    // -----------------------------------------------------------------
    // snap `p` onto the line while preserving its distance to
    // `pivot` — i.e. move `p` along the circle centered at `pivot` with
    // radius |p - pivot| to the point of that circle nearest to `p` that
    // lies on the line.
    //
    // If the circle intersects the line, the intersection closest to `p`
    // is returned (distance to pivot is exactly preserved and the result
    // is exactly on the line).
    //
    // If the circle does NOT reach the line (line entirely outside the
    // circle), no such point exists; the method returns the point of the
    // circle closest to the line, so the pivot-distance constraint is
    // still exactly satisfied.
    // -----------------------------------------------------------------
    public float2 ClosestPointOnPivotCircle(float2 p, float2 pivot)
    {
        float radius = math.distance(p, pivot);
        float2 foot = ClosestPoint(pivot);   // pivot projected onto the line
        float2 toFoot = foot - pivot;
        float distToLine = math.length(toFoot);

        if (distToLine <= radius)
        {
            // Circle intersects the line: two candidates on either side of the foot.
            float halfChord = math.sqrt(math.max(0f, radius * radius - distToLine * distToLine));
            float2 a = foot + halfChord * Direction;
            float2 b = foot - halfChord * Direction;
            return math.distancesq(a, p) <= math.distancesq(b, p) ? a : b;
        }

        // Circle never reaches the line: return the circle point nearest to it.
        // distToLine > radius >= 0 here, so the division is safe.
        return pivot + toFoot * (radius / distToLine);
    }
}
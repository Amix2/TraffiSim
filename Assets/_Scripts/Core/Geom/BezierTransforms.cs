using Unity.Mathematics;

public static class BezierTransforms
{
    /// <summary>
    /// Checks if any lines from test point to its offseted position intersects with other lines
    /// </summary>
    /// <returns>True if BesierCurve can be offseted by offset in direction of its Tangent</returns>
    public static bool CanOffset(BezierCurve curve, float offset, bool left, int testPointsCount)
    {
        LineSegment prevLineSegment = default;
        for (int i = 0; i < testPointsCount; i++)
        {
            float t = 1 / (testPointsCount - 1);
            float2 tangent = curve.Tangent(t, left);
            float2 P = curve.Evaluate(i);
            if (i == 0)
            {
                prevLineSegment = new LineSegment(P, P + tangent * offset);
            }
            else
            {
                LineSegment thisSegment = new LineSegment(P, P + tangent * offset);
                if (prevLineSegment.Intersects(thisSegment))
                    return false;
                prevLineSegment = thisSegment;
            }
        }
        return true;
    }
}
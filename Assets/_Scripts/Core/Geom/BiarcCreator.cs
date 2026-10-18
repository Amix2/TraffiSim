using Unity.Collections;
using Unity.Mathematics;

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

public static class BiarcCreator
{
    // --------------------------------------------------------------- averages
    // Only for biarcs that are offsets of one biarc. If you have that reference
    // biarc, reference.Offset((X1 + X2) / 2) gives the same result directly.

    public static Biarc Lerp(in Biarc a, in Biarc b, float t)
    {
        if (!a.IsValid || !b.IsValid)
            return default;
        return Make(BiarcCreator.Lerp(a.Arc0, b.Arc0, t), BiarcCreator.Lerp(a.Arc1, b.Arc1, t));
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
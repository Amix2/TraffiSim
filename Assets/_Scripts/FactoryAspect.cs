using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEditor.ShaderGraph.Internal;

public readonly partial struct FactoryAspect : IAspect
{
    public readonly Entity Entity;
    public readonly RefRO<FactoryInput> InputC;
    public readonly RefRO<FactoryOutput> OutputC;
}
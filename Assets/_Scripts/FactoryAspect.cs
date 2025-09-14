using Unity.Entities;

public readonly partial struct FactoryAspect : IAspect
{
    public readonly Entity Entity;
    public readonly RefRO<FactoryInput> InputC;
    public readonly RefRO<FactoryOutput> OutputC;
}
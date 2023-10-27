using Unity.Entities;

public struct DocumentComponent : IComponentData
{
    public Entity VehiclePrefab;
}

public readonly partial struct DocumentAspect : IAspect
{
    public readonly Entity Entity;

    private readonly RefRW<DocumentComponent> DocumentComponent;

    public Entity VehiclePrefab => DocumentComponent.ValueRO.VehiclePrefab;
}
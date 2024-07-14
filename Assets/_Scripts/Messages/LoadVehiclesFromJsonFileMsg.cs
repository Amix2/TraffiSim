using Unity.Collections;
using Unity.Entities;

public class LoadVehiclesFromJsonFileMsg : ISingleMessage
{
    private string jsonPath;

    public LoadVehiclesFromJsonFileMsg(string jsonPath)
    {
        this.jsonPath = jsonPath;
    }

    public void Execute(MasterSystem masterSystem)
    {
        var ecb = masterSystem.World.GetOrCreateSystemManaged<BeginSimulationEntityCommandBufferSystem>().CreateCommandBuffer();
        NativeList<ComponentType> types = new(2, Allocator.Temp)
        {
           ComponentType.ReadOnly<LoadVehiclesFromJsonFile>()
        };

        var arch = masterSystem.EntityManager.CreateArchetype(types.AsArray());
        var ent = ecb.CreateEntity(arch);
        ecb.SetComponent(ent, new LoadVehiclesFromJsonFile { fileName = jsonPath });
    }
}
public class LoadVehiclesFromJsonTextMsg : ISingleMessage
{
    private string jsonText;

    public LoadVehiclesFromJsonTextMsg(string jsonText)
    {
        this.jsonText = jsonText;
    }

    public void Execute(MasterSystem masterSystem)
    {
        var ecb = masterSystem.World.GetOrCreateSystemManaged<BeginSimulationEntityCommandBufferSystem>().CreateCommandBuffer();
        NativeList<ComponentType> types = new(2, Allocator.Temp)
        {
           ComponentType.ReadOnly<LoadVehiclesFromTextJson>()
        };

        var arch = masterSystem.EntityManager.CreateArchetype(types.AsArray());
        var ent = ecb.CreateEntity(arch);
        ecb.SetSharedComponentManaged(ent, new LoadVehiclesFromTextJson { jsonText = this.jsonText });
    }
}
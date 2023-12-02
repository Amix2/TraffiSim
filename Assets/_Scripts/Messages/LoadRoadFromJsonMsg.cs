using Unity.Collections;
using Unity.Entities;

public class LoadRoadFromJsonMsg : ISingleMessage
{
    private string jsonPath;

    public LoadRoadFromJsonMsg(string jsonPath)
    {
        this.jsonPath = jsonPath;
    }

    public void Execute(MasterSystem masterSystem)
    {
        var ecb = masterSystem.World.GetOrCreateSystemManaged<BeginSimulationEntityCommandBufferSystem>().CreateCommandBuffer();
        NativeList<ComponentType> types = new(2, Allocator.Temp)
        {
           ComponentType.ReadOnly<rgLoadRoadFromJson>()
        };

        var arch = masterSystem.EntityManager.CreateArchetype(types.AsArray());
        var ent = ecb.CreateEntity(arch);
        ecb.SetComponent(ent, new rgLoadRoadFromJson { fileName = jsonPath });
    }
}
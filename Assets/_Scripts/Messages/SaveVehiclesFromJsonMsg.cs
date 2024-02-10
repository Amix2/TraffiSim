using Unity.Collections;
using Unity.Entities;

public class SaveVehiclesFromJsonMsg : ISingleMessage
{
    private string jsonPath;

    public SaveVehiclesFromJsonMsg(string jsonPath)
    {
        this.jsonPath = jsonPath;
    }

    public void Execute(MasterSystem masterSystem)
    {
        var ecb = masterSystem.World.GetOrCreateSystemManaged<BeginSimulationEntityCommandBufferSystem>().CreateCommandBuffer();
        NativeList<ComponentType> types = new(2, Allocator.Temp)
        {
           ComponentType.ReadOnly<SaveVehiclesFromJson>()
        };

        var arch = masterSystem.EntityManager.CreateArchetype(types.AsArray());
        var ent = ecb.CreateEntity(arch);
        ecb.SetComponent(ent, new SaveVehiclesFromJson { fileName = jsonPath });
    }
}
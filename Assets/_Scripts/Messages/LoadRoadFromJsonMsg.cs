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
        //var ecb = masterSystem.World.GetOrCreateSystemManaged<BeginSimulationEntityCommandBufferSystem>().CreateCommandBuffer();
        //NativeList<ComponentType> types = new(2, Allocator.Temp)
        //{
        //   ComponentType.ReadOnly<rgLoadRoadFromJson>()
        //};

        //var arch = masterSystem.EntityManager.CreateArchetype(types.AsArray());
        //var ent = ecb.CreateEntity(arch);
        //ecb.SetComponent(ent, new rgLoadRoadFromJson { fileName = jsonPath });
    }
}

public class LoadRoadFromJsonTextMsg : ISingleMessage
{
    private string jsonText;

    public LoadRoadFromJsonTextMsg(string jsonText)
    {
        this.jsonText = jsonText;
    }

    public void Execute(MasterSystem masterSystem)
    {
        Entity spawnEntity = masterSystem.EntityManager.CreateEntity(typeof(rgSpawnRoadDataFromJsonText));
        masterSystem.EntityManager.SetComponentData(spawnEntity, new rgSpawnRoadDataFromJsonText { JsonText = jsonText });
    }
}
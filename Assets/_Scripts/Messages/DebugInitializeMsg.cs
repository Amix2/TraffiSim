using Unity.Entities;

class DebugInitializeMsg : ISingleMessage
{
    public void Execute(MasterSystem masterSystem)
    {
        var ecb = masterSystem.CreateBeginSimulationEntityCommandBufferSystem();

    }
}
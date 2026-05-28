using Unity.Entities;

class DebugInitializeMsg : ISingleMessage
{
    public void Execute(MasterSystem masterSystem)
    {
        var ecb = masterSystem.CreateBeginSimulationEntityCommandBufferSystem();
        var Document = masterSystem.GetSingletonComponent<rgDocumentC>();
        Entity roadManagerEnt = Document.RoadManager;

        var RoadSegment = ecb.CreateEntity();

        ecb.AddComponent(RoadSegment, new rgRoadSegmentData
        {

        });
        ecb.AddBuffer<rgRoadSegmentLine>(RoadSegment);
    }
}
using Unity.Entities;
using Unity.Mathematics;


public struct RoadParent : IComponentData
{
    public RoadSegmentEnt SegmentEnt;
    public Entity Parent;
}

public class rgSpawnRoadDataFromJsonText : IComponentData
{
    public string JsonText;
}

public struct RoadVisualizerParent : IComponentData
{
    public Entity ParentEnt;
}

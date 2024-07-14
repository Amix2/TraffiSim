public static class LayerMask
{
    public static uint Terrain => 1u << UnityEngine.LayerMask.NameToLayer("Terrain");
    public static uint RoadNode => 1u << UnityEngine.LayerMask.NameToLayer("RoadNode");
}
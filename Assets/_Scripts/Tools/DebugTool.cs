using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;

public class DebugTool : ToolBase
{
    public override void OnUpdate(MasterSystem masterSystem)
    {
        ConsoleLogUI.Log("onupdate");
    }


}
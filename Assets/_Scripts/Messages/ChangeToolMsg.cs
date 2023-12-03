public enum eToolType : int
{
    Camera, AddNode, Debug
}

public class ChangeToolMsg : ISingleMessage
{
    private eToolType m_nToolType;

    public ChangeToolMsg(eToolType nToolType)
    {
        m_nToolType = nToolType;
    }

    public void Execute(MasterSystem masterSystem)
    {
        switch (m_nToolType)
        {
            case eToolType.Camera: masterSystem.SetActiveTool(null); break;
            case eToolType.AddNode: masterSystem.SetActiveTool(new AddRoadObjectsTool()); break;
            case eToolType.Debug: masterSystem.SetActiveTool(new DebugTool()); break;
        }
    }
}
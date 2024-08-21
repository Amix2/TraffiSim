using Unity.Collections;
using Unity.Entities;

public class SimTimeChangeMessage : ISingleMessage
{
    public enum Type
    {
        Faster, Slower, Pause, Step, Play
    };
    private Type type;

    public SimTimeChangeMessage(Type type)
    {
        this.type = type;
    }

    public void Execute(MasterSystem masterSystem)
    {
        SimConfigComponent simConfig = masterSystem.GetSingleton<SimConfigComponent>();

        switch (type)
        {
            case Type.Faster:
                simConfig.ReplaySpeed *= 1.1f;
                break;
            case Type.Slower:
                simConfig.ReplaySpeed /= 1.1f;
                break;
            case Type.Pause:
                simConfig.StepsCount = 0;
                break;
            case Type.Step:
                simConfig.StepsCount = 1;
                break;
            case Type.Play:
                simConfig.StepsCount = -1;
                break;
            default:
                Unity.Assertions.Assert.IsTrue(false);
                break;
        }

        masterSystem.SetSingleton(simConfig);
    }
}
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;

public abstract class ToolBase : ITool
{
    public abstract void OnUpdate(MasterSystem masterSystem);

    protected RaycastHit RayCast(MasterSystem masterSystem, float3 RayFrom, float3 RayTo, uint CollidesWith = ~0u)
    {
        RaycastInput input = new RaycastInput()
        {
            Start = RayFrom,
            End = RayTo,
            Filter = new CollisionFilter()
            {
                BelongsTo = ~0u,
                CollidesWith = CollidesWith,
                GroupIndex = 0
            }
        };

        bool haveHit = masterSystem.CollisionWorld.CastRay(input, out RaycastHit hit);
        if (!haveHit)
            hit.Entity = Entity.Null;

        return hit;
    }

    protected RaycastHit GetHitUnderMouse(MasterSystem masterSystem, uint CollidesWith = ~0u)
    {
        var ray = UnityEngine.Camera.main.ScreenPointToRay(MousePosition);
        return RayCast(masterSystem, ray.origin, ray.origin + ray.direction * 10000, CollidesWith);
    }

    protected float3 MousePosition => UnityEngine.Input.mousePosition;
    protected float2 MouseScreenPosition => new float2(MousePosition.x, MousePosition.z);
}
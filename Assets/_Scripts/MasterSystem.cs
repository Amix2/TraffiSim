using System.Xml;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using UnityEngine;
using RaycastHit = Unity.Physics.RaycastHit;

[UpdateBefore(typeof(BeginSimulationEntityCommandBufferSystem))]
[UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
public partial class MasterSystem : SystemBase
{
    public MessageQueue MessageQueue;

    void ParseNodeList(XmlNodeList nodeList)
    {
        foreach (XmlNode node in nodeList)
            ParseNode(node);
    }

    void ParseNode(XmlNode node)
    {
        Debug.Log(node.Name);

        ParseNodeList(node.ChildNodes);
    }

    protected override void OnUpdate()
    {
        MessageQueue.Execute(this);

        //new rgRoadMapFromSVG("D:\\temp\\graph2.svg");

        //UnityEngine.Ray mouseRay = Camera.main.ScreenPointToRay(Input.mousePosition);
        //Raycast(mouseRay.origin, mouseRay.origin+ mouseRay.direction*10000);
    }

    public Entity Raycast(float3 RayFrom, float3 RayTo)
    {
        var collisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().CollisionWorld;

        RaycastInput input = new RaycastInput()
        {
            Start = RayFrom,
            End = RayTo,
            Filter = new CollisionFilter()
            {
                BelongsTo = ~0u,
                CollidesWith = ~0u, // all 1s, so all layers, collide with everything
                GroupIndex = 0
            }
        };

        RaycastHit hit = new RaycastHit();
        bool haveHit = collisionWorld.CastRay(input, out hit);
        if (haveHit)
        {
            Debug.Log(hit.Position);

            return hit.Entity;
        }
        Debug.Log("miss");

        return Entity.Null;
    }

    protected override void OnCreate()
    {
        MessageQueue = new MessageQueue();
    }
}
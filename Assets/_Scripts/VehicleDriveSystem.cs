using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;


public partial struct VehicleDriveSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        if (!SystemAPI.HasSingleton<rgDocumentC>())
            return;

        new AssignSubSceneJob { dt = 0.1f }.Schedule();

    }

    [BurstCompile]
    public partial struct AssignSubSceneJob : IJobEntity
    {
        public float dt;
        [BurstCompile]
        private void Execute(ref LocalTransform transform, in Velocity velocity, in DynamicBuffer<PathBuffer> path)
        {
            transform = transform.Translate(new float3(0.01f,0,0));
            //float distLeft = velocity * dt;
            //while (distLeft > 0 || !path.IsEmpty) 
            //{ 

            //}
        }
    }
    [BurstCompile]
    private void OnCreate(ref SystemState state)
    { }

    [BurstCompile]
    private void OnDestroy(ref SystemState state)
    { }
}
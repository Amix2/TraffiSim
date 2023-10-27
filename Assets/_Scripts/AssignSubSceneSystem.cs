using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

//public partial struct AssignSubSceneSystem : ISystem
//{
//    [BurstCompile]
//    public void OnUpdate(ref SystemState state)
//    {
//        var Document = SystemAPI.GetAspect<rgDocumentAspect>(SystemAPI.GetSingletonEntity<rgDocumentAspect>());
//        SceneSection sceneSection = state.EntityManager.GetSharedComponentManaged<SceneSection>(Document.DocumentEntity);
//        SceneTag sceneTag = state.EntityManager.GetSharedComponentManaged<SceneTag>(Document.DocumentEntity);


//        var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();

//    }

//    [BurstCompile]
//    public partial struct AssignSubSceneJob : IJobEntity
//    {
//        public EntityCommandBuffer.ParallelWriter ECB;
//        public SceneSection sceneSection;
//        public SceneTag sceneTag;

//        [BurstCompile]
//        private void Execute(ZombieEatAspect zombie, [EntityInQueryIndex] int sortKey)
//        {
//            if (zombie.IsInEatingRange(float3.zero, BrainRadiusSq))
//            {
//                zombie.Eat(DeltaTime, ECB, sortKey, BrainEntity);
//            }
//            else
//            {
//                ECB.SetComponentEnabled<ZombieEatProperties>(sortKey, zombie.Entity, false);
//                ECB.SetComponentEnabled<ZombieWalkProperties>(sortKey, zombie.Entity, true);
//            }
//        }
//    }

//}


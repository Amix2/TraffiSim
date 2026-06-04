using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

public partial struct AssignSubSceneSystem : ISystem
{
    private EntityQuery _missingSceneSectionQuery;
    private EntityQuery _missingSceneTagQuery;

    public void OnCreate(ref SystemState state)
    {
        // Require the source singleton
        state.RequireForUpdate<DocumentComponent>();

        // Entities missing SceneSection
        _missingSceneSectionQuery = new EntityQueryBuilder(Allocator.Temp)
            .WithNone<SceneSection>()
            .WithAny<LocalTransform, RoadLaneNodeData, RoadLaneData>()
            .Build(ref state);

        // Entities missing SceneTag
        _missingSceneTagQuery = new EntityQueryBuilder(Allocator.Temp)
            .WithNone<SceneTag>()
            .WithAny<LocalTransform, RoadLaneNodeData, RoadLaneData>()
            .Build(ref state);
    }

    public void OnUpdate(ref SystemState state)
    {
        var entityManager = state.EntityManager;

        // Get the entity that holds the reference shared components
        var documentEntity = SystemAPI.GetSingletonEntity<DocumentComponent>();

        // Read shared components from it
        SceneSection sceneSection =
            entityManager.GetSharedComponentManaged<SceneSection>(documentEntity);

        SceneTag sceneTag =
            entityManager.GetSharedComponentManaged<SceneTag>(documentEntity);

        // Assign SceneSection to all entities that don't have it
        if (!_missingSceneSectionQuery.IsEmptyIgnoreFilter)
        {
            entityManager.AddSharedComponentManaged(_missingSceneSectionQuery, sceneSection);
        }

        // Assign SceneTag to all entities that don't have it
        if (!_missingSceneTagQuery.IsEmptyIgnoreFilter)
        {
            entityManager.AddSharedComponentManaged(_missingSceneTagQuery, sceneTag);
        }
    }
}
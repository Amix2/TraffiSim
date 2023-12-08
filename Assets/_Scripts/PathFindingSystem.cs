using System.Collections.Generic;
using System.Linq;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.VisualScripting;

public partial struct PathFindingSystem : ISystem
{
    private rgEdgeAspect.Lookup m_EdgesLookup;
    private rgNodeAspect.Lookup m_NodesLookup;

    [BurstCompile]
    private void OnUpdate(ref SystemState state)
    {
        if (!SystemAPI.HasSingleton<rgDocumentC>())
            return;
        m_EdgesLookup.Update(ref state);
        m_NodesLookup.Update(ref state);

        rgRoadManagerAspect RoadManager = SystemAPI.GetAspect<rgRoadManagerAspect>(SystemAPI.GetSingletonEntity<rgRoadManager>());

        new AStarJob { RoadManager = RoadManager, EdgesLookup = m_EdgesLookup, NodesLookup = m_NodesLookup }.Schedule();
    }

    [BurstCompile]
    public partial struct AStarJob : IJobEntity
    {
        [ReadOnly] public rgRoadManagerAspect RoadManager;
        [ReadOnly] public rgEdgeAspect.Lookup EdgesLookup;
        [ReadOnly] public rgNodeAspect.Lookup NodesLookup;

        private float CalcualteCost(Entity nodeEntity, float3 position)
        { return (NodesLookup[nodeEntity].Position - position).length(); }

        private struct OpenSet
        {
            private NativeList<Entity> set;

            public OpenSet(int initSize)
            {
                set = new NativeList<Entity>(initSize, Allocator.Temp);
            }

            public void Add(Entity node)
            {
                set.Add(node);
            }

            public bool IsEmpty => set.IsEmpty;

            private struct FScoreCompare : IComparer<Entity>
            {
                public NativeHashMap<Entity, float> fScore;

                private float fScoreSafe(Entity ent)
                { return fScore.ContainsKey(ent) ? fScore[ent] : float.MaxValue; }

                public int Compare(Entity x, Entity y) => fScoreSafe(x).CompareTo(fScoreSafe(y));
            }

            public Entity Get(NativeHashMap<Entity, float> fScore)
            {
                if (set.IsEmpty)
                    return Entity.Null;

                set.Sort(new FScoreCompare { fScore = fScore });
                return set[0];
            }

            public void Remove(Entity entity)
            {
                set.RemoveAt(set.IndexOf(entity));
            }

            public bool Contains(Entity entity)
            {
                return set.Contains(entity);
            }
        }

        private NativeList<Entity> ReconstructPath(NativeHashMap<Entity, Entity> cameFrom, Entity currentNode)
        {
            NativeList<Entity> path = new NativeList<Entity>(Allocator.Temp);
            path.Add(currentNode);
            while (cameFrom.TryGetValue(currentNode, out currentNode))
            {
                path.InsertRange(0, 1);
                path[0] = currentNode;
            }
            return path;
        }

        private NativeArray<rgNodeEdges> GetNeighbours(Entity nodeEnt)
        { return NodesLookup[nodeEnt].NighboursEntities; }

        private NativeList<Entity> RunAStar(Entity startEdge, float3 startPosition, Entity destinationEdge, float3 destinationPosition)
        {
            OpenSet openSet = new OpenSet(16);
            NativeHashSet<Entity> closedSet = new NativeHashSet<Entity>(16, Allocator.Temp);
            NativeHashMap<Entity, float> gScore = new NativeHashMap<Entity, float>(16, Allocator.Temp);
            float gScoreSafe(Entity ent) { return gScore.ContainsKey(ent) ? gScore[ent] : float.MaxValue; }
            NativeHashMap<Entity, float> fScore = new NativeHashMap<Entity, float>(16, Allocator.Temp);
            NativeHashMap<Entity, Entity> cameFrom = new NativeHashMap<Entity, Entity>(16, Allocator.Temp);

            rgEdgeAspect startEdgeAspect = EdgesLookup[startEdge];
            Entity startA = startEdgeAspect.NodeA;
            Entity startB = startEdgeAspect.NodeB;

            // For node n, gScore[n] is the cost of the cheapest path from start to n currently known.
            gScore[startA] = CalcualteCost(startA, startPosition);
            gScore[startB] = CalcualteCost(startB, startPosition);

            // For node n, fScore[n] := gScore[n] + h(n). fScore[n] represents our current best guess as to
            // how cheap a path could be from start to finish if it goes through n.
            fScore[startA] = gScore[startA] + CalcualteCost(startA, destinationPosition);
            fScore[startB] = gScore[startB] + CalcualteCost(startB, destinationPosition);

            openSet.Add(startA);
            openSet.Add(startB);

            rgEdgeAspect endEdgeAspect = EdgesLookup[destinationEdge];
            Entity goalA = endEdgeAspect.NodeA;
            Entity goalB = endEdgeAspect.NodeB;

            while (!openSet.IsEmpty)
            {
                Entity current = openSet.Get(fScore);
                if (current == goalA)
                    return ReconstructPath(cameFrom, goalA);
                if (current == goalB)
                    return ReconstructPath(cameFrom, goalB);

                openSet.Remove(current);
                foreach (rgNodeEdges neighbourNode in GetNeighbours(current))
                {
                    // d(current,neighbor) is the weight of the edge from current to neighbor
                    // tentative_gScore is the distance from start to the neighbor through current
                    float edgeLen = EdgesLookup[neighbourNode.EdgeEnt].EdgeLength;
                    float tentative_gScore = gScore[current] + edgeLen;
                    if (tentative_gScore < gScoreSafe(neighbourNode.OtherNodeEnt))
                    {   // This path to neighbor is better than any previous one. Record it!
                        cameFrom[neighbourNode.OtherNodeEnt] = current;
                        gScore[neighbourNode.OtherNodeEnt] = tentative_gScore;
                        fScore[neighbourNode.OtherNodeEnt] = tentative_gScore + CalcualteCost(neighbourNode.OtherNodeEnt, destinationPosition);
                        if (!openSet.Contains(neighbourNode.OtherNodeEnt))
                        {
                            openSet.Add(neighbourNode.OtherNodeEnt);
                        }
                    }
                }
            }

            // we failed
            return new NativeList<Entity>(Allocator.Temp);
        }

        [BurstCompile]
        private void Execute(ref DynamicBuffer<PathBuffer> path, in LocalTransform transform, in DestinationPosition target)
        {
            if (!path.IsEmpty)
                return;

            var closestRoad = RoadManager.GetClosestRoad(transform.Position, EdgesLookup);
            var closestEndRoad = RoadManager.GetClosestRoad(target, EdgesLookup);

            float distToTargetSq = (transform.Position - target).lengthsq();
            float distToClosestSq = (transform.Position - closestRoad.RoadPosition).lengthsq();
            float distToEndSq = (target - closestEndRoad.RoadPosition).lengthsq();

            bool directPath = distToTargetSq < distToClosestSq + distToEndSq;

            if (!directPath)
            {
                path.Add(new PathBuffer { Position = closestRoad.RoadPosition, Target = closestRoad.Edge });
                // some smart path finding
                NativeList<Entity> outPath = RunAStar(closestRoad.Edge, closestRoad.RoadPosition, closestEndRoad.Edge, closestEndRoad.RoadPosition);
                //Assert.IsTrue(outPath[0] == EdgesLookup[closestEndRoad.Edge].NodeA || outPath.First() == EdgesLookup[closestEndRoad.Edge].NodeB);
                //Assert.IsTrue(outPath[outPath.Length -1] == EdgesLookup[closestEndRoad.Edge].NodeA || outPath[outPath.Length - 1] == EdgesLookup[closestEndRoad.Edge].NodeB);

                foreach (Entity pathNode in outPath)
                {
                    path.Add(new PathBuffer { Position = NodesLookup[pathNode].Position, Target = pathNode });
                }

                path.Add(new PathBuffer { Position = closestEndRoad.RoadPosition, Target = closestEndRoad.Edge });
            }
            path.Add(new PathBuffer { Position = target, Target = Entity.Null });
        }
    }

    [BurstCompile]
    public partial struct NavMeshJob : IJobEntity
    {
        [ReadOnly] public rgRoadManagerAspect RoadManager;
        [ReadOnly] public rgEdgeAspect.Lookup EdgesLookup;

        [BurstCompile]
        private void Execute(ref DynamicBuffer<PathBuffer> path, in LocalTransform transform, in DestinationPosition target)
        {
            if (!path.IsEmpty)
                return;

            var closestRoad = RoadManager.GetClosestRoad(transform.Position, EdgesLookup);
            var closestEndRoad = RoadManager.GetClosestRoad(target, EdgesLookup);

            float distToTargetSq = (transform.Position - target).lengthsq();
            float distToClosestSq = (transform.Position - closestRoad.RoadPosition).lengthsq();
            float distToEndSq = (target - closestEndRoad.RoadPosition).lengthsq();

            bool directPath = distToTargetSq < distToClosestSq + distToEndSq;

            if (!directPath)
            {
                path.Add(new PathBuffer { Position = closestRoad.RoadPosition, Target = closestRoad.Edge });
                // some smart path finding
                path.Add(new PathBuffer { Position = closestEndRoad.RoadPosition, Target = closestRoad.Edge });
            }
            path.Add(new PathBuffer { Position = target, Target = Entity.Null });
        }
    }

    [BurstCompile]
    private void OnCreate(ref SystemState state)
    {
        m_EdgesLookup = new rgEdgeAspect.Lookup(ref state);
        m_NodesLookup = new rgNodeAspect.Lookup(ref state);
    }

    [BurstCompile]
    private void OnDestroy(ref SystemState state)
    { }
}
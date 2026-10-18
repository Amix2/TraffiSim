using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;

//[UpdateInGroup(typeof(PresentationSystemGroup))]
public partial class rgRoadLaneMesher : SystemBase
{
    private EntitiesGraphicsSystem EntitiesGraphicsSystem;
    private EntityQuery RoadSegmentsToUpdate;

    protected override void OnCreate()
    {
        EntitiesGraphicsSystem = World.GetExistingSystemManaged<EntitiesGraphicsSystem>();
        RoadSegmentsToUpdate = SystemAPI.QueryBuilder()
            .WithAll<RoadSegmentData, LinkedEntityGroup, RoadSegmentLane>()          // IComponentData
            .Build();
        //RequireForUpdate<RoadLaneUpdateMesh>();
    }

    private void TestBezier(BezierCurve bezierCurve)
    {
        DrawShapesSystem drawer = World.GetExistingSystemManaged<DrawShapesSystem>();
        float width = 2;
        NativeArray<float2> points = new NativeArray<float2>(101, Allocator.Temp);
        NativeArray<float> Ts = new NativeArray<float>(101, Allocator.Temp);
        for (int i = 0; i <= 100; i++)
        {
            float2 p = bezierCurve.Evaluate(i / 100.0f);
            drawer.DrawSphere(Color.red, new float3(p.x, 0.1f, p.y), new float3(0.1f, 0.1f, 0.1f));
            float2 d = bezierCurve.Tangent(i / 100.0f, true);
            drawer.DrawSphere(Color.green, new float3(p.x + width * d.x, 0.1f, p.y + width * d.y), new float3(0.1f, 0.1f, 0.1f));
            points[i] = new float2(p.x + width * d.x, p.y + width * d.y);
            Ts[i] = (float)i / 100.0f;
        }
        BezierFit bezierFit = new BezierFit(points, Ts, bezierCurve.H0, bezierCurve.H1, 0.01f, 100);
        for (int i = 0; i <= 100; i++)
        {
            float2 p = bezierFit.curve.Evaluate(i / 100.0f);
            drawer.DrawSphere(Color.blue, new float3(p.x, 0.2f, p.y), new float3(0.05f, 0.05f, 0.05f));
            float2 d = bezierCurve.Tangent(i / 100.0f, true);
        }
    }

    private void TestBiarc(Biarc biarc)
    {
        DrawShapesSystem drawer = World.GetExistingSystemManaged<DrawShapesSystem>();
        for (int i = 0; i <= 100; i++)
        {
            float s = biarc.Length * i / 100.0f;
            float2 p = biarc.Evaluate(s).Pos;
            drawer.DrawSphere(Color.HSVToRGB(i / 100.0f, 1, 1), new float3(p.x, 0.1f, p.y), new float3(0.1f, 0.1f, 0.1f));
        }

        float X = 1;
        Biarc offBA = biarc.Offset(X);
        for (int i = 0; i <= 100; i++)
        {
            float s = offBA.Length * i / 100.0f;
            float2 p = offBA.Evaluate(s).Pos;
            drawer.DrawSphere(Color.HSVToRGB(i / 100.0f, 1, 0.5f), new float3(p.x, 0.1f, p.y), new float3(0.1f, 0.1f, 0.1f));
        }

        Biarc avg = BiarcCreator.Average(biarc, offBA);
        for (int i = 0; i <= 100; i++)
        {
            float s = avg.Length * i / 100.0f;
            float2 p = avg.Evaluate(s).Pos;
            drawer.DrawSphere(Color.HSVToRGB(i / 100.0f, 0.5f, 1), new float3(p.x, 0.1f, p.y), new float3(0.1f, 0.1f, 0.1f));
        }
    }

    protected override void OnUpdate()
    {
        Dependency.Complete();

        DrawShapesSystem drawer = World.GetExistingSystemManaged<DrawShapesSystem>();

        //TestBiarc(new Biarc(new float2(0, 0), new float2(5, 0), new float2(5, 5), new float2(5, 0)));
        //TestBiarc(new Biarc(new float2(10, 0), new float2(5, 0), new float2(15, 5), new float2(0, 1)));
        //TestBiarc(new Biarc(new float2(20, 0), new float2(0, 0), new float2(25,0), new float2(0, 0)));

        //{
        //    TestBezier(BezierCurve.Cubic(new float2(0, 0), new float2(5, 0), new float2(5, 5), new float2(-5, 0)));

        //}

        //{
        //    BezierCurve bezierCurve = BezierCurve.Quadratic(new float2(10, 0), new float2(15, 5), new float2(15, 0));
        //    TestBezier(bezierCurve);
        //}

        //{
        //    BezierCurve bezierCurve = BezierCurve.Linear(new float2(20, 0), new float2(25, 5));
        //    TestBezier(bezierCurve);

        //}
        var Document = SystemAPI.GetSingleton<rgDocumentC>();

        var entities = RoadSegmentsToUpdate.ToEntityArray(Allocator.Temp);
        foreach (Entity entity in entities)
        {
            {
                RoadSegmentMesherAspect mesherAspect = new RoadSegmentMesherAspect(this, entity);
                // cleanup old visualziers
                var ecb = new EntityCommandBuffer(Allocator.Temp);
                mesherAspect.ClearAllVisualizers(ref ecb, EntityManager);
                ecb.Playback(EntityManager);
                ecb.Dispose();
            }

            {
                var ecb = new EntityCommandBuffer(Allocator.Temp);

                RoadSegmentMesherAspect mesherAspect = new RoadSegmentMesherAspect(this, entity);
                for (int i = 0; i < mesherAspect.ChildLanes.Length; i++)
                {
                    Entity RoadSurface = EntityManager.Instantiate(Document.RoadSurfacePrefab);
                    EntityManager.AddComponent<RuntimeUpdateableMesh>(RoadSurface);
                    SystemAPI.GetBuffer<LinkedEntityGroup>(entity).Add(RoadSurface);

                    mesherAspect = new RoadSegmentMesherAspect(this, entity);

                    RoadLaneAspect roadSegment = new RoadLaneAspect(this, mesherAspect.ChildLanes[i].LaneEnt);
                    RoadPortAspect startPort = new RoadPortAspect(this, roadSegment.StartPortEnt);
                    RoadPortAspect endPort = new RoadPortAspect(this, roadSegment.EndPortEnt);
                    RoadNodeAspect startNode = new RoadNodeAspect(this, startPort.Parent);
                    RoadNodeAspect endNode = new RoadNodeAspect(this, endPort.Parent);
                    RoadVisualizerAspect visualizerAspect = new RoadVisualizerAspect(this, RoadSurface);

                    Biarc biarc = new Biarc(startPort.Position.xz, default, endPort.Position.xz, default);
                    float2 startDir = startNode.Line.SignedDistance(biarc.EvaluateFract(0.1f).Pos) > 0 ? startNode.Line.Normal : -startNode.Line.Normal;
                    float2 endDir = endNode.Line.SignedDistance(biarc.EvaluateFract(0.9f).Pos) > 0 ? -endNode.Line.Normal : endNode.Line.Normal;
                    biarc = new Biarc(startPort.Position.xz, startDir, endPort.Position.xz, endDir);

                    //for (int q = 0; q <= 100; q++)
                    //{
                    //    float s = biarc.Length * q / 100.0f;
                    //    float2 p = biarc.Evaluate(s).Pos;
                    //    drawer.DrawSphere(Color.HSVToRGB(q / 100.0f, 1, 1), new float3(p.x, 0.1f, p.y), new float3(0.1f, 0.1f, 0.1f));
                    //}

                    float margin = 0.1f;

                    visualizerAspect.Parent = entity;

                    Biarc leftEdge = biarc.Offset(roadSegment.LaneWidth / 2 + margin);
                    Biarc rightEdge = biarc.Offset(-roadSegment.LaneWidth / 2 - margin);
                    BuildMeshData data = new()
                    {
                        height = 1.1f,
                        leftEdge = leftEdge,
                        rightEdge = rightEdge,
                        center = biarc,
                        roadWidth = roadSegment.LaneWidth,
                        precision = margin,
                    };

                    Mesh mesh = BuildLaneMesh(data);

                    for (int q = 0; q <= 100; q++)
                    {
                        float s = leftEdge.Length * q / 100.0f;
                        float2 p = leftEdge.Evaluate(s).Pos;
                        drawer.DrawSphere(Color.HSVToRGB(q / 100.0f, 1, 1), new float3(p.x, 1.1f, p.y), new float3(0.1f, 0.1f, 0.1f));
                    }
                    for (int q = 0; q <= 100; q++)
                    {
                        float s = rightEdge.Length * q / 100.0f;
                        float2 p = rightEdge.Evaluate(s).Pos;
                        drawer.DrawSphere(Color.HSVToRGB(q / 100.0f, 1, 1), new float3(p.x, 1.1f, p.y), new float3(0.1f, 0.1f, 0.1f));
                    }

                    visualizerAspect.SetMesh(mesh, EntitiesGraphicsSystem);
                }

                ecb.Playback(EntityManager);
                ecb.Dispose();
            }
        }
    }

    private struct BuildMeshData
    {
        public Biarc center;

        public Biarc leftEdge;
        public Biarc rightEdge;
        public float height;
        public float roadWidth;
        public float precision;
    }

    private Mesh BuildLaneMesh(in BuildMeshData data)
    {
        var mesh = new Mesh();

        var verts = new NativeList<float3>(Allocator.Temp);
        var indices = new NativeList<int>(Allocator.Temp);
        var sampleFractions = new NativeList<float>(Allocator.Temp);
        data.center.SampleFractions(sampleFractions, data.precision, data.roadWidth / 2);

        for (int i = 0; i < sampleFractions.Length - 1; i++)
        {
            float fract = sampleFractions[i];
            float next = sampleFractions[i + 1];
            float2 leftP = data.leftEdge.EvaluateFractMatched(fract, data.center).Pos;
            float2 rightP = data.rightEdge.EvaluateFractMatched(fract, data.center).Pos;
            float2 leftN = data.leftEdge.EvaluateFractMatched(next, data.center).Pos;
            float2 rightN = data.rightEdge.EvaluateFractMatched(next, data.center).Pos;

            verts.Add(new float3(leftP.x, data.height, leftP.y));
            verts.Add(new float3(leftN.x, data.height, leftN.y));
            verts.Add(new float3(rightN.x, data.height, rightN.y));
            verts.Add(new float3(rightP.x, data.height, rightP.y));
            for (int q = 0; q < 4; q++)
                indices.Add(indices.Length);
        }
        //var UVs = new NativeList<float2>(Allocator.Temp);
        //UVs.Add(new float2(0, 0));
        //UVs.Add(new float2(0, 1));
        //UVs.Add(new float2(1, 1));
        //UVs.Add(new float2(1, 0));

        mesh.SetVertices(verts.AsArray());
        //mesh.SetUVs(0, UVs.AsArray());
        mesh.SetIndices(indices.AsArray(), MeshTopology.Quads, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        verts.Dispose();
        indices.Dispose();
        sampleFractions.Dispose();
        //UVs.Dispose();

        return mesh;
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;


[UpdateAfter(typeof(AnimationSystem))]
[UpdateInGroup(typeof(PresentationSystemGroup))]

public class RenderSystem : SystemBase
{
    public static EntitySpawner entitySpawner;
    protected override void OnStartRunning()
    {
        entitySpawner = UnityEngine.GameObject.Find("GameManager").GetComponent<EntitySpawner>().instance;
    }
    private struct RenderData
    {
        public Entity entity;
        public float3 position;
        public Matrix4x4 matrix;
        public Vector4 uv;
    }

    private struct PositionComparer : IComparer<RenderData>
    {
        public int Compare(RenderData a, RenderData b)
        {
            if (a.position.y < b.position.y)
                return 1;
            else
                return -1;
        }
    }

    [BurstCompile]
    private struct CullJob : IJobForEachWithEntity<Translation, AnimationComponent>
    {
        public float xMin;
        public float xMax;
        public float yBottom;
        public float yTop_1, yTop_2, yTop_3, yTop_4, yTop_5, yTop_6, yTop_7, yTop_8, yTop_9, yTop_10;
        public float yTop_11, yTop_12, yTop_13, yTop_14, yTop_15, yTop_16, yTop_17, yTop_18, yTop_19, yTop_20;

        // Use NativeList ParallelWriter for thread-safe adds
        [NativeDisableParallelForRestriction]
        public NativeList<RenderData>.ParallelWriter nativeList_1;
        [NativeDisableParallelForRestriction]
        public NativeList<RenderData>.ParallelWriter nativeList_2;
        [NativeDisableParallelForRestriction]
        public NativeList<RenderData>.ParallelWriter nativeList_3;
        [NativeDisableParallelForRestriction]
        public NativeList<RenderData>.ParallelWriter nativeList_4;
        [NativeDisableParallelForRestriction]
        public NativeList<RenderData>.ParallelWriter nativeList_5;
        [NativeDisableParallelForRestriction]
        public NativeList<RenderData>.ParallelWriter nativeList_6;
        [NativeDisableParallelForRestriction]
        public NativeList<RenderData>.ParallelWriter nativeList_7;
        [NativeDisableParallelForRestriction]
        public NativeList<RenderData>.ParallelWriter nativeList_8;
        [NativeDisableParallelForRestriction]
        public NativeList<RenderData>.ParallelWriter nativeList_9;
        [NativeDisableParallelForRestriction]
        public NativeList<RenderData>.ParallelWriter nativeList_10;
        [NativeDisableParallelForRestriction]
        public NativeList<RenderData>.ParallelWriter nativeList_11;
        [NativeDisableParallelForRestriction]
        public NativeList<RenderData>.ParallelWriter nativeList_12;
        [NativeDisableParallelForRestriction]
        public NativeList<RenderData>.ParallelWriter nativeList_13;
        [NativeDisableParallelForRestriction]
        public NativeList<RenderData>.ParallelWriter nativeList_14;
        [NativeDisableParallelForRestriction]
        public NativeList<RenderData>.ParallelWriter nativeList_15;
        [NativeDisableParallelForRestriction]
        public NativeList<RenderData>.ParallelWriter nativeList_16;
        [NativeDisableParallelForRestriction]
        public NativeList<RenderData>.ParallelWriter nativeList_17;
        [NativeDisableParallelForRestriction]
        public NativeList<RenderData>.ParallelWriter nativeList_18;
        [NativeDisableParallelForRestriction]
        public NativeList<RenderData>.ParallelWriter nativeList_19;
        [NativeDisableParallelForRestriction]
        public NativeList<RenderData>.ParallelWriter nativeList_20;

        public void Execute(Entity entity, int index, [ReadOnly] ref Translation translation, [ReadOnly] ref AnimationComponent animation)
        {
            float x = translation.Value.x;
            float y = translation.Value.y;

            if (x > xMin && x < xMax && y < yTop_1 && y > yBottom)
            {
                var renderData = new RenderData
                {
                    entity = entity,
                    position = translation.Value,
                    matrix = animation.matrix,
                    uv = animation.uv
                };

                if (y < yTop_20) nativeList_20.AddNoResize(renderData);
                else if (y < yTop_19) nativeList_19.AddNoResize(renderData);
                else if (y < yTop_18) nativeList_18.AddNoResize(renderData);
                else if (y < yTop_17) nativeList_17.AddNoResize(renderData);
                else if (y < yTop_16) nativeList_16.AddNoResize(renderData);
                else if (y < yTop_15) nativeList_15.AddNoResize(renderData);
                else if (y < yTop_14) nativeList_14.AddNoResize(renderData);
                else if (y < yTop_13) nativeList_13.AddNoResize(renderData);
                else if (y < yTop_12) nativeList_12.AddNoResize(renderData);
                else if (y < yTop_11) nativeList_11.AddNoResize(renderData);
                else if (y < yTop_10) nativeList_10.AddNoResize(renderData);
                else if (y < yTop_9) nativeList_9.AddNoResize(renderData);
                else if (y < yTop_8) nativeList_8.AddNoResize(renderData);
                else if (y < yTop_7) nativeList_7.AddNoResize(renderData);
                else if (y < yTop_6) nativeList_6.AddNoResize(renderData);
                else if (y < yTop_5) nativeList_5.AddNoResize(renderData);
                else if (y < yTop_4) nativeList_4.AddNoResize(renderData);
                else if (y < yTop_3) nativeList_3.AddNoResize(renderData);
                else if (y < yTop_2) nativeList_2.AddNoResize(renderData);
                else nativeList_1.AddNoResize(renderData);
            }
        }
    }


    [BurstCompile]
    private struct NativeQueueToArrayJob : IJob
    {

        public NativeQueue<RenderData> nativeQueue;
        public NativeArray<RenderData> nativeArray;

        public void Execute()
        {
            int index = 0;
            RenderData entity;
            while (nativeQueue.TryDequeue(out entity))
            {
                nativeArray[index] = entity;
                index++;
            }
        }
    }

    [BurstCompile]
    private struct SortByPositionJob : IJob
    {
        public PositionComparer comparer;
        public NativeList<RenderData> sortList;

        public void Execute()
        {
            sortList.Sort(comparer);
        }
    }


    [BurstCompile]
    private struct FillArraysParallelJob : IJobParallelFor
    {
        [ReadOnly] public NativeList<RenderData> nativeList;
        [NativeDisableContainerSafetyRestriction]
        public NativeArray<Matrix4x4> matrixArray;
        [NativeDisableContainerSafetyRestriction]
        public NativeArray<Vector4> uvArray;
        public int startingIndex;

        public void Execute(int index)
        {
            RenderData renderData = nativeList[index];
            matrixArray[startingIndex + index] = renderData.matrix;
            uvArray[startingIndex + index] = renderData.uv;
        }
    }


    [BurstCompile]
    private struct ClearListJob : IJob
    {
        public NativeList<RenderData> nativeList;

        public void Execute()
        {
            nativeList.Clear();
        }
    }


    private const int DRAW_MESH_INSTANCED_SLICE_COUNT = 1023;
    private Matrix4x4[] matrixInstancedArray;
    private Vector4[] uvInstancedArray;
    private MaterialPropertyBlock materialPropertyBlock;
    private Mesh mesh;
    private Material material;
    private int shaderMainTexUVid;

    private void InitDrawMeshInstancedSlicedData()
    {
        if (matrixInstancedArray != null) return; // Already initialized
        matrixInstancedArray = new Matrix4x4[DRAW_MESH_INSTANCED_SLICE_COUNT];
        uvInstancedArray = new Vector4[DRAW_MESH_INSTANCED_SLICE_COUNT];
        materialPropertyBlock = new MaterialPropertyBlock();
        shaderMainTexUVid = Shader.PropertyToID("_MainTex_UV");
        mesh = entitySpawner.quadMesh;
        material = entitySpawner.walkingSpriteSheetMaterial;

        if (mesh == null)
        {
            Debug.LogError("Mesh is null!");
        }
        if (material == null)
        {
            Debug.LogError("Material is null!");
        }
    }

    private const int POSITION_SLICES = 20;

    //private NativeQueue<RenderData>[] nativeQueueArray;
    NativeList<RenderData>[] nativeListArray;
    private NativeArray<JobHandle> jobHandleArray;
    private NativeArray<RenderData>[] nativeArrayArray;
    private PositionComparer positionComparer;

    protected override void OnCreate()
    {
        base.OnCreate();

        nativeListArray = new NativeList<RenderData>[POSITION_SLICES];

        for (int i = 0; i < POSITION_SLICES; i++)
        {
            nativeListArray[i] = new NativeList<RenderData>(Allocator.Persistent);
        }

        jobHandleArray = new NativeArray<JobHandle>(POSITION_SLICES, Allocator.Persistent);

        nativeArrayArray = new NativeArray<RenderData>[POSITION_SLICES];

        positionComparer = new PositionComparer();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        for (int i = 0; i < POSITION_SLICES; i++)
        {
            //nativeQueueArray[i].Dispose();
            nativeListArray[i].Dispose();
        }

        jobHandleArray.Dispose();
    }

    protected override void OnUpdate()
    {
        Camera camera = Camera.main;
        float cameraWidth = camera.aspect * camera.orthographicSize;
        float3 cameraPosition = camera.transform.position;
        float marginX = cameraWidth / 10f;
        float xMin = cameraPosition.x - cameraWidth - marginX;
        float xMax = cameraPosition.x + cameraWidth + marginX;
        float cameraSliceSize = camera.orthographicSize * 2f / POSITION_SLICES;
        float yBottom = cameraPosition.y - camera.orthographicSize;
        float yTop_1 = cameraPosition.y + camera.orthographicSize;
        float yTop_2 = yTop_1 - cameraSliceSize * 1f;
        float yTop_3 = yTop_1 - cameraSliceSize * 2f;
        float yTop_4 = yTop_1 - cameraSliceSize * 3f;
        float yTop_5 = yTop_1 - cameraSliceSize * 4f;
        float yTop_6 = yTop_1 - cameraSliceSize * 5f;
        float yTop_7 = yTop_1 - cameraSliceSize * 6f;
        float yTop_8 = yTop_1 - cameraSliceSize * 7f;
        float yTop_9 = yTop_1 - cameraSliceSize * 8f;
        float yTop_10 = yTop_1 - cameraSliceSize * 9f;
        float yTop_11 = yTop_1 - cameraSliceSize * 10f;
        float yTop_12 = yTop_1 - cameraSliceSize * 11f;
        float yTop_13 = yTop_1 - cameraSliceSize * 12f;
        float yTop_14 = yTop_1 - cameraSliceSize * 13f;
        float yTop_15 = yTop_1 - cameraSliceSize * 14f;
        float yTop_16 = yTop_1 - cameraSliceSize * 15f;
        float yTop_17 = yTop_1 - cameraSliceSize * 16f;
        float yTop_18 = yTop_1 - cameraSliceSize * 17f;
        float yTop_19 = yTop_1 - cameraSliceSize * 18f;
        float yTop_20 = yTop_1 - cameraSliceSize * 19f;
        float marginY = camera.orthographicSize / 10f;
        yTop_1 += marginY;
        yBottom -= marginY;

        int estimatedEntitiesTotal = GetEntityQuery(typeof(Translation)).CalculateEntityCount();
        int estimatedPerSlice = estimatedEntitiesTotal / POSITION_SLICES + 50;

        for (int i = 0; i < POSITION_SLICES; i++)
        {
            if (nativeListArray[i].Capacity < estimatedPerSlice) nativeListArray[i].Capacity = estimatedPerSlice;
            nativeListArray[i].Clear();
        }

        CullJob cullAndSortNativeListJob = new CullJob
        {
            xMin = xMin,
            xMax = xMax,
            yBottom = yBottom,
            yTop_1 = yTop_1,
            yTop_2 = yTop_2,
            yTop_3 = yTop_3,
            yTop_4 = yTop_4,
            yTop_5 = yTop_5,
            yTop_6 = yTop_6,
            yTop_7 = yTop_7,
            yTop_8 = yTop_8,
            yTop_9 = yTop_9,
            yTop_10 = yTop_10,
            yTop_11 = yTop_11,
            yTop_12 = yTop_12,
            yTop_13 = yTop_13,
            yTop_14 = yTop_14,
            yTop_15 = yTop_15,
            yTop_16 = yTop_16,
            yTop_17 = yTop_17,
            yTop_18 = yTop_18,
            yTop_19 = yTop_19,
            yTop_20 = yTop_20,
            nativeList_1 = nativeListArray[0].AsParallelWriter(),
            nativeList_2 = nativeListArray[1].AsParallelWriter(),
            nativeList_3 = nativeListArray[2].AsParallelWriter(),
            nativeList_4 = nativeListArray[3].AsParallelWriter(),
            nativeList_5 = nativeListArray[4].AsParallelWriter(),
            nativeList_6 = nativeListArray[5].AsParallelWriter(),
            nativeList_7 = nativeListArray[6].AsParallelWriter(),
            nativeList_8 = nativeListArray[7].AsParallelWriter(),
            nativeList_9 = nativeListArray[8].AsParallelWriter(),
            nativeList_10 = nativeListArray[9].AsParallelWriter(),
            nativeList_11 = nativeListArray[10].AsParallelWriter(),
            nativeList_12 = nativeListArray[11].AsParallelWriter(),
            nativeList_13 = nativeListArray[12].AsParallelWriter(),
            nativeList_14 = nativeListArray[13].AsParallelWriter(),
            nativeList_15 = nativeListArray[14].AsParallelWriter(),
            nativeList_16 = nativeListArray[15].AsParallelWriter(),
            nativeList_17 = nativeListArray[16].AsParallelWriter(),
            nativeList_18 = nativeListArray[17].AsParallelWriter(),
            nativeList_19 = nativeListArray[18].AsParallelWriter(),
            nativeList_20 = nativeListArray[19].AsParallelWriter()
        };

        JobHandle cullJobHandle = cullAndSortNativeListJob.Schedule(this, Dependency);
        cullJobHandle.Complete();

        int visibleEntityTotal = 0;
        for (int i = 0; i < POSITION_SLICES; i++)
        {
            visibleEntityTotal += nativeListArray[i].Length;
        }

        for (int i = 0; i < POSITION_SLICES; i++)
        {
            SortByPositionJob sortJob = new SortByPositionJob
            {
                sortList = nativeListArray[i],
                comparer = positionComparer
            };
            jobHandleArray[i] = sortJob.Schedule();
        }

        JobHandle.CompleteAll(jobHandleArray);

        NativeArray<Matrix4x4> matrixArray = new NativeArray<Matrix4x4>(visibleEntityTotal, Allocator.TempJob);
        NativeArray<Vector4> uvArray = new NativeArray<Vector4>(visibleEntityTotal, Allocator.TempJob);

        int startIndex = 0;
        JobHandle lastJobHandle = default;

        for (int i = 0; i < POSITION_SLICES; i++)
        {
            FillArraysParallelJob fillJob = new FillArraysParallelJob
            {
                nativeList= nativeListArray[i],
                matrixArray = matrixArray,
                uvArray = uvArray,
                startingIndex = startIndex
            };
            startIndex += nativeListArray[i].Length;

            jobHandleArray[i] = fillJob.Schedule(nativeListArray[i].Length, 10, lastJobHandle);
            lastJobHandle = jobHandleArray[i];
        }

        JobHandle.CompleteAll(jobHandleArray);

        InitDrawMeshInstancedSlicedData();

        for (int i = 0; i < visibleEntityTotal; i += DRAW_MESH_INSTANCED_SLICE_COUNT)
        {
            int sliceSize = math.min(visibleEntityTotal - i, DRAW_MESH_INSTANCED_SLICE_COUNT);
            if (sliceSize == 0) continue;

            NativeArray<Matrix4x4>.Copy(matrixArray, i, matrixInstancedArray, 0, sliceSize);
            NativeArray<Vector4>.Copy(uvArray, i, uvInstancedArray, 0, sliceSize);
            materialPropertyBlock.SetVectorArray(shaderMainTexUVid, uvInstancedArray);

            Graphics.DrawMeshInstanced(mesh, 0, material, matrixInstancedArray, sliceSize, materialPropertyBlock);
        }

        matrixArray.Dispose();
        uvArray.Dispose();
    }


}

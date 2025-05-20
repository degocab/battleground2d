//using Unity.Collections;
//using Unity.Entities;
//using Unity.Mathematics;
//using Unity.Transforms;
//using UnityEngine;

//public class QuadTreeTest : MonoBehaviour
//{
//    public GameObject entityPrefab; // Converted GameObject prefab with ConvertToEntity
//    private Entity prefabEntity;
//    private EntityManager entityManager;
//    private QuadTree quadTree;

//    void Start()
//    {
//        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
//        prefabEntity = GameObjectConversionUtility.ConvertGameObjectHierarchy(entityPrefab, GameObjectConversionSettings.FromWorld(World.DefaultGameObjectInjectionWorld, null));

//        quadTree.Initialize(new float2(500f, 500f), new float2(500f, 500f));

//        for (int i = 0; i < 10; i++)
//        {
//            float2 pos = new float2(UnityEngine.Random.Range(0, 1000f), UnityEngine.Random.Range(0, 1000f));
//            Entity entity = entityManager.Instantiate(prefabEntity);

//            // Setting position (replace LocalTransform)
//            entityManager.SetComponentData(entity, new Translation
//            {
//                Value = new float3(pos.x, pos.y, 0f)
//            });

//            // Insert into QuadTree
//            quadTree.Insert(entity, pos);
//        }

//        QueryNearby(new float2(500f, 500f), 200f);
//    }
//    private float2 queryCenter;
//    private float queryRadius;

//    void QueryNearby(float2 center, float radius)
//    {
//        queryCenter = center;
//        queryRadius = radius;

//        var results = new NativeList<Entity>(Allocator.Temp);
//        quadTree.Query(center, radius, results);

//        Debug.Log($"Found {results.Length} entities near {center}");

//        foreach (var e in results)
//        {
//            float3 pos = entityManager.GetComponentData<Translation>(e).Value;
//            Debug.Log($"Entity at: {pos}");
//        }

//        results.Dispose();
//    }

//    void OnDestroy()
//    {
//        quadTree.Clear();
//    }

//    //private void OnDrawGizmos()
//    //{
//    //    if (Application.isPlaying)
//    //    {
//    //        Gizmos.color = Color.green;
//    //        DrawNodeBoundaries(quadTree.GetRoot());

//    //        Gizmos.color = Color.red;
//    //    }
//    //}

//    private void DrawNodeBoundaries(QuadTreeNode node)
//    {
//        Vector3 center = new Vector3(node.center.x, node.center.y, 0f);
//        Vector3 size = new Vector3(node.halfExtent.x * 2, node.halfExtent.y * 2, 0f);

//        Gizmos.DrawWireCube(center, size);

//        if (node.northWest.HasValue) DrawNodeBoundaries(node.northWest.Value);
//        if (node.northEast.HasValue) DrawNodeBoundaries(node.northEast.Value);
//        if (node.southWest.HasValue) DrawNodeBoundaries(node.southWest.Value);
//        if (node.southEast.HasValue) DrawNodeBoundaries(node.southEast.Value);
//    }



//}

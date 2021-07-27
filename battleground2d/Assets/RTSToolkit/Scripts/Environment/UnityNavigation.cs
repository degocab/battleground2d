using UnityEngine;
using UnityEngine.AI;

namespace RTSToolkit
{
    public class UnityNavigation : MonoBehaviour
    {
        public static UnityNavigation active;

        public bool useMinY;
        public float minY = 0f;

        public float voxelSize = 1.5f;
        public int tileSize = 256;

        public bool useIndividualTerrains = false;

        [HideInInspector] public NavMeshSurface nms;
        [HideInInspector] public AsyncOperation asyncOperation;

        [HideInInspector] public bool buildAsyncStarted = false;

        void Awake()
        {
            active = this;
        }

        void Start()
        {

        }

        void Update()
        {

        }

        public static UnityNavigation GetActive()
        {
            if (UnityNavigation.active == null)
            {
                UnityNavigation.active = UnityEngine.Object.FindObjectOfType<UnityNavigation>();
            }

            return UnityNavigation.active;
        }


        public void Build()
        {
#if !ASTAR
            if (useIndividualTerrains)
            {
                BuildIndividualTerrains();
            }
            else
            {
                Terrain[] terrains = UnityEngine.Object.FindObjectsOfType<Terrain>();
                if (terrains.Length > 0)
                {
                    Bounds bounds = new Bounds();

                    float minx = float.MaxValue;
                    float maxx = float.MinValue;

                    float miny = float.MaxValue;
                    float maxy = float.MinValue;

                    float minz = float.MaxValue;
                    float maxz = float.MinValue;

                    for (int i = 0; i < terrains.Length; i++)
                    {
                        if (terrains[i].transform.position.x < minx)
                        {
                            minx = terrains[i].transform.position.x;
                        }
                        if ((terrains[i].transform.position.x + terrains[i].terrainData.size.x) > maxx)
                        {
                            maxx = terrains[i].transform.position.x + terrains[i].terrainData.size.x;
                        }

                        if (terrains[i].transform.position.y < miny)
                        {
                            miny = terrains[i].transform.position.y;
                        }
                        if ((terrains[i].transform.position.y + terrains[i].terrainData.size.y) > maxy)
                        {
                            maxy = terrains[i].transform.position.y + terrains[i].terrainData.size.y;
                        }

                        if (terrains[i].transform.position.z < minz)
                        {
                            minz = terrains[i].transform.position.z;
                        }
                        if ((terrains[i].transform.position.z + terrains[i].terrainData.size.z) > maxz)
                        {
                            maxz = terrains[i].transform.position.z + terrains[i].terrainData.size.z;
                        }
                    }

                    if (useMinY)
                    {
                        if (miny < minY)
                        {
                            miny = minY;
                        }
                    }

                    bounds.size = new Vector3(maxx - minx, maxy - miny, maxz - minz);
                    bounds.center = new Vector3(0.5f * (minx + maxx), 0.5f * (miny + maxy), 0.5f * (minz + maxz));

                    GenerateTerrain gt = GenerateTerrain.GetActive();

                    if (gt != null)
                    {
                        nms = gt.gameObject.GetComponent<NavMeshSurface>();
                        bool build = false;

                        if (nms == null)
                        {
                            nms = gt.gameObject.AddComponent<NavMeshSurface>();
                            build = true;
                        }

                        nms.overrideVoxelSize = true;
                        nms.voxelSize = voxelSize;
                        nms.overrideTileSize = true;
                        nms.tileSize = tileSize;

                        nms.collectObjects = CollectObjects.Children;

                        if (build)
                        {
                            nms.BuildNavMesh(bounds);
                        }

                        asyncOperation = nms.UpdateNavMesh(nms.navMeshData, bounds);
#if UNITY_EDITOR
                        if (Application.isPlaying == false)
                        {
                            buildAsyncStarted = true;
                        }
#endif
                    }
                }
            }
#endif
        }

        void BuildIndividualTerrains()
        {
            Terrain[] terrains = UnityEngine.Object.FindObjectsOfType<Terrain>();

            if (terrains.Length > 0)
            {
                for (int i = 0; i < terrains.Length; i++)
                {
                    if (terrains[i].GetComponent<NavMeshSurface>() == null)
                    {
                        float minx = terrains[i].transform.position.x;
                        float maxx = terrains[i].transform.position.x + terrains[i].terrainData.size.x;

                        float miny = terrains[i].transform.position.y;
                        float maxy = terrains[i].transform.position.y + terrains[i].terrainData.size.y;

                        float minz = terrains[i].transform.position.z;
                        float maxz = terrains[i].transform.position.z + terrains[i].terrainData.size.z;

                        Bounds bounds = new Bounds();

                        bounds.size = new Vector3(maxx - minx, maxy - miny, maxz - minz);
                        bounds.center = new Vector3(0.5f * (minx + maxx), 0.5f * (miny + maxy), 0.5f * (minz + maxz)) - terrains[i].transform.position;

                        GameObject terGo = terrains[i].gameObject;

                        NavMeshSurface nms1 = terGo.AddComponent<NavMeshSurface>();
                        nms1.overrideVoxelSize = true;
                        nms1.voxelSize = voxelSize;
                        nms1.overrideTileSize = true;
                        nms1.tileSize = tileSize;

                        nms1.collectObjects = CollectObjects.Children;

                        nms1.BuildNavMesh(bounds);
                        nms1.UpdateNavMesh(nms1.navMeshData, bounds);
                    }
                }
            }
        }

        public void Clean()
        {
            GenerateTerrain gt = GenerateTerrain.GetActive();

            if (gt != null)
            {
                nms = gt.gameObject.GetComponent<NavMeshSurface>();

                if (nms != null)
                {
                    ClearSurface(nms);
                    DestroyImmediate(nms);
                }
            }
        }

        public static bool IsAsyncRunning()
        {
            UnityNavigation un = UnityNavigation.active;

            if (un == null)
            {
                return false;
            }
            else
            {
                if (un.asyncOperation == null)
                {
                    return false;
                }
                else
                {
                    if (un.asyncOperation.isDone)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        void ClearSurface(NavMeshSurface navSurface)
        {
#if UNITY_EDITOR
            var assetToDelete = navSurface.navMeshData;
            navSurface.RemoveData();
            navSurface.navMeshData = null;
            UnityEditor.EditorUtility.SetDirty(navSurface);

            if (assetToDelete)
            {
                if (Application.isPlaying == false)
                {
                    string assetPath = UnityEditor.AssetDatabase.GetAssetPath(assetToDelete);

                    if (assetPath != null && assetPath != string.Empty)
                    {
                        UnityEditor.AssetDatabase.DeleteAsset(assetPath);
                    }

                    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(navSurface.gameObject.scene);
                }
            }
#endif
        }

        public void Update_E()
        {
#if UNITY_EDITOR
            if (Application.isPlaying == false)
            {
                if (buildAsyncStarted)
                {
                    if (asyncOperation != null)
                    {
                        if (asyncOperation.isDone)
                        {
                            buildAsyncStarted = false;
                            SceneScripts.SaveScene();
                        }
                    }
                }
            }
#endif
        }
    }
}

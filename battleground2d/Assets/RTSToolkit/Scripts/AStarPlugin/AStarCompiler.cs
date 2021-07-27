using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Collections;
using System.Collections.Generic;
using System.Threading;
#if ASTAR
using Pathfinding;
using Pathfinding.RVO;
#endif

namespace RTSToolkit
{
    public class AStarCompiler : MonoBehaviour
    {
        public static AStarCompiler active;

#if ASTAR
        Pathfinding.Serialization.SerializeSettings serializationSettings = new Pathfinding.Serialization.SerializeSettings();
        public List<NavmeshTile> tiles = new List<NavmeshTile>();

        RecastGraph graph;
        GridGraph gridGraph;

        [HideInInspector] public GameObject astar;
        [HideInInspector] public GameObject rvoSimulatorGo;

        public int nX = 3;
        public int nZ = 3;
        public int tileSize = 2000;
        [HideInInspector] public int subTileSize;
        public int nSubTiles = 1;
        public int cellSize = 8;

        public float maxSlope = 50f;

        public Vector2i zeroPoint = new Vector2i(-1, -1);

        public bool rasterizeTrees = false;
        public bool rasterizeMeshes = false;

        public AStarGraphMode graphMode = AStarGraphMode.NavMesh;
        public enum AStarGraphMode { NavMesh, Grid };

        [HideInInspector] public bool isOnRuntime = false;

        public void Clean()
        {
            ClearCache();
            DestroyImmediate(astar);
            DestroyImmediate(rvoSimulatorGo);
        }

        public void Compile()
        {
            Clean();

            astar = new GameObject();
            astar.name = "A*";
            astar.transform.SetParent(this.gameObject.transform);

            AstarPath asp = astar.AddComponent<AstarPath>();
            asp.data = new AstarData();
            asp.logPathResults = PathLog.None;

            subTileSize = (tileSize / 2) / nSubTiles;

            if (graphMode == AStarGraphMode.NavMesh)
            {
                SetMainGraph();
                asp.Scan(graph);
            }
            else
            {
                SetMainGridGraph();
                asp.Scan(gridGraph);
            }

            if (isOnRuntime == false)
            {
                SaveCache();
            }

            rvoSimulatorGo = new GameObject();
            rvoSimulatorGo.name = "RVO Simulator";
            rvoSimulatorGo.transform.SetParent(this.gameObject.transform);
            RVOSimulator rvoSimulator = rvoSimulatorGo.AddComponent<RVOSimulator>();

            rvoSimulator.doubleBuffering = true;
            rvoSimulator.desiredSimulationFPS = 10;
            rvoSimulator.workerThreads = ThreadCount.AutomaticLowLoad;

            if (graphMode == AStarGraphMode.NavMesh)
            {
                TileHandlerHelper th = rvoSimulatorGo.AddComponent<TileHandlerHelper>();
                th.updateInterval = 1.0f;
            }

            rvoSimulatorGo.AddComponent<ManualRVOs>();

#if UNITY_EDITOR
            if (isOnRuntime == false)
            {
                UnityEngine.SceneManagement.Scene scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
                UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
            }
#endif
        }

        public static AStarCompiler GetActive()
        {
            if (AStarCompiler.active == null)
            {
                AStarCompiler[] allObjects = UnityEngine.Object.FindObjectsOfType<AStarCompiler>();
                AStarCompiler.active = allObjects[0];
            }

            return AStarCompiler.active;
        }

        void SetMainGraph()
        {
            float dHeight = GenerateTerrain.GetActive().height;
            float heightMidle = 0.5f * GenerateTerrain.GetActive().height;

            if (GenerateTerrain.GetActive().water != null)
            {
                dHeight = GenerateTerrain.GetActive().height - (GenerateTerrain.GetActive().water.transform.position.y - 1f);
                heightMidle = 0.5f * (GenerateTerrain.GetActive().height + (GenerateTerrain.GetActive().water.transform.position.y - 1f));
            }

            graph = SetupGraph(
                new Vector3(
                    0.5f * tileSize * nX + tileSize * zeroPoint.x,
                    heightMidle,
                    0.5f * tileSize * nZ + tileSize * zeroPoint.z
                ),
                new Vector3(
                    1f * tileSize * nX,
                    dHeight,
                    1f * tileSize * nZ
                )
            );

            AstarPath.active.threadCount = ThreadCount.AutomaticLowLoad;
            graph.useTiles = true;
            graph.tileSizeX = tileSize / nSubTiles / cellSize;
            graph.tileSizeZ = tileSize / nSubTiles / cellSize;
            graph.editorTileSize = tileSize / nSubTiles / cellSize;
        }

        void SetMainGridGraph()
        {
            float height_orig = GenerateTerrain.GetActive().GetHeight();
            float dHeight = height_orig;
            float heightMidle = 0.5f * height_orig;
            if (GenerateTerrain.GetActive().water != null)
            {
                dHeight = height_orig - (GenerateTerrain.GetActive().water.transform.position.y - 1f);
                heightMidle = 0.5f * (height_orig + (GenerateTerrain.GetActive().water.transform.position.y - 1f));
            }

            gridGraph = SetupGridGraph(
                new Vector3(
                    0.5f * tileSize * nX + tileSize * zeroPoint.x,
                    heightMidle,
                    0.5f * tileSize * nZ + tileSize * zeroPoint.z
                ),
                new Vector3(
                    1f * tileSize * nX,
                    dHeight,
                    1f * tileSize * nZ
                )
            );

            AstarPath.active.threadCount = ThreadCount.AutomaticLowLoad;
        }

        public void UpdateFromCentralAll(int cx, int cz)
        {
            int inew = cx - nX / 2;
            int jnew = cz - nZ / 2;

            int i = inew - zeroPoint.x;
            int j = jnew - zeroPoint.z;

            zeroPoint.x = zeroPoint.x + i;
            zeroPoint.z = zeroPoint.z + j;

            if (graphMode == AStarGraphMode.NavMesh)
            {
                graph.forcedBoundsCenter = graph.forcedBoundsCenter + new Vector3(tileSize * i, 0f, tileSize * j);
            }
            else
            {
                gridGraph.center = gridGraph.center + new Vector3(tileSize * i, 0f, tileSize * j);
                gridGraph.SetDimensions(gridGraph.width, gridGraph.depth, gridGraph.nodeSize);
            }

            if (graphMode == AStarGraphMode.Grid)
            {
                AstarPath.active.AddWorkItem(
                    () =>
                    {
                        Bounds bounds = new Bounds();
                        if (graphMode == AStarGraphMode.NavMesh)
                        {
                            bounds.center = graph.forcedBoundsCenter;
                            bounds.size = graph.forcedBoundsSize;
                        }
                        else
                        {
                            bounds.center = gridGraph.center;
                            bounds.size = new Vector3(tileSize * nX, 600f, tileSize * nZ);
                        }

                        GraphUpdateObject guo = new GraphUpdateObject(bounds);
                        guo.updatePhysics = true;

                        AstarPath.active.UpdateGraphs(guo);
                        AstarPath.active.FloodFill();
                    }
                );
            }

            if (graphMode == AStarGraphMode.NavMesh)
            {
                Bounds bounds = new Bounds();

                if (graphMode == AStarGraphMode.NavMesh)
                {
                    bounds.center = graph.forcedBoundsCenter;
                    bounds.size = graph.forcedBoundsSize;
                }
                else
                {
                    bounds.center = gridGraph.center;
                    bounds.size = new Vector3(tileSize * nX, 600f, tileSize * nZ);
                }

                GraphUpdateObject guo = new GraphUpdateObject(bounds);
                guo.updatePhysics = true;

                if (isScanGraphsAsyncRunning == false)
                {
                    StartCoroutine(ScanGraphsAsync());
                }
            }
        }

        bool isScanGraphsAsyncRunning = false;
        IEnumerator ScanGraphsAsync()
        {
            isScanGraphsAsyncRunning = true;

            foreach (Progress progress in AstarPath.active.ScanAsync())
            {
                yield return null;
            }

            isScanGraphsAsyncRunning = false;
        }

        public void BuildingUpdate(UnitPars up)
        {
            AstarPath.active.AddWorkItem(
                () =>
                {

                    Bounds bounds = new Bounds();
                    bounds.center = up.transform.position;
                    bounds.size = new Vector3(3f * up.rEnclosed, 60f, 3f * up.rEnclosed);

                    GraphUpdateObject guo = new GraphUpdateObject(bounds);
                    guo.updatePhysics = true;

                    if (rasterizeMeshes == false)
                    {
                        graph.rasterizeMeshes = true;
                    }

                    AstarPath.active.UpdateGraphs(guo);
                    AstarPath.active.FloodFill();
                    graph.rasterizeMeshes = rasterizeMeshes;
                }
            );
        }

        RecastGraph SetupGraph(Vector3 boundsCenter, Vector3 boundsSize)
        {
            AstarPath.active.data.FindGraphTypes();
            AstarPath.active.data.AddGraph("RecastGraph");
            RecastGraph graph1 = AstarPath.active.data.recastGraph;

            graph1.rasterizeMeshes = rasterizeMeshes;
            graph1.rasterizeTrees = rasterizeTrees;

            graph1.cellSize = cellSize;
            graph1.characterRadius = cellSize / 2;

            graph1.maxSlope = maxSlope;
            graph1.walkableHeight = 50f;
            graph1.walkableClimb = 8f;

            graph1.terrainSampleSize = 2;

            graph1.forcedBoundsCenter = boundsCenter;
            graph1.forcedBoundsSize = boundsSize;

            return graph1;
        }

        GridGraph SetupGridGraph(Vector3 boundsCenter, Vector3 boundsSize)
        {
            AstarPath.active.data.FindGraphTypes();
            AstarPath.active.data.AddGraph("GridGraph");
            GridGraph graph1 = AstarPath.active.data.gridGraph;

            graph1.width = (int)(1.0f * nX * tileSize / cellSize);
            graph1.depth = (int)(1.0f * nZ * tileSize / cellSize);
            graph1.nodeSize = cellSize;
            graph1.center = new Vector3(boundsCenter.x, GenerateTerrain.GetActive().water.transform.position.y - 1f, boundsCenter.z);

            graph1.SetDimensions(graph1.width, graph1.depth, graph1.nodeSize);

            graph1.maxClimb = 0;
            graph1.maxSlope = maxSlope;

            graph1.collision.fromHeight = boundsSize.y;

            return graph1;
        }

        void SaveCache()
        {
            var bytes = AstarPath.active.data.SerializeGraphs(serializationSettings);
            AstarPath.active.data.file_cachedStartup = SaveGraphData(bytes);
            AstarPath.active.data.cacheStartup = true;
        }

        void Start()
        {
            isOnRuntime = true;
            if (graphMode == AStarGraphMode.NavMesh)
            {
                if (AstarPath.active == null)
                {
                    Compile();
                }
                else if (AstarPath.active.data == null)
                {
                    Compile();
                }
                else if (AstarPath.active.data.recastGraph == null)
                {
                    Compile();
                }
                if (graph == null)
                {
                    graph = AstarPath.active.data.recastGraph;
                }
            }
            else
            {
                if (AstarPath.active == null)
                {
                    Compile();
                }
                else if (AstarPath.active.data == null)
                {
                    Compile();
                }
                else if (AstarPath.active.data.gridGraph == null)
                {
                    Compile();
                }
                if (gridGraph == null)
                {
                    gridGraph = AstarPath.active.data.gridGraph;
                }
            }

            subTileSize = (tileSize / 2) / nSubTiles;
            serializationSettings.editorSettings = false;

#if UNITY_WEBGL
		    AstarPath.active.threadCount = ThreadCount.None;
#endif
        }

        void Update()
        {

        }

        TextAsset SaveGraphData(byte[] bytes)
        {
#if UNITY_EDITOR
            string fullPath = @Application.dataPath + "/GraphCaches/GraphCache.bytes";
            string path = "Assets/GraphCaches/GraphCache.bytes";
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(fullPath));
#if !UNITY_WEBPLAYER
            System.IO.File.WriteAllBytes(fullPath, bytes);
#endif
            AssetDatabase.Refresh();

            return AssetDatabase.LoadAssetAtPath(path, typeof(TextAsset)) as TextAsset;
#else
		    return null;		
#endif
        }

        void ClearCache()
        {
            if (System.IO.Directory.Exists(@Application.dataPath + "/GraphCaches/"))
            {
#if !UNITY_WEBPLAYER
                System.IO.Directory.Delete(@Application.dataPath + "/GraphCaches", true);
#endif
#if UNITY_EDITOR
                AssetDatabase.Refresh();
#endif
            }
        }

        NavmeshTile DuplicateNavMeshTile(NavmeshTile tile)
        {
            NavmeshTile newTile = new NavmeshTile();
            newTile.x = tile.x;
            newTile.z = tile.z;
            newTile.w = tile.w;
            newTile.d = tile.d;
            newTile.verts = tile.verts;
            newTile.tris = tile.tris;
            return newTile;
        }
#endif
    }

#if ASTAR
    public class AStarGraphCache
    {
        public byte[] data;
        public NavmeshTile[] tiles;

        public Vector2i zeroPoint;
    }

    public class AStarGraphCacheTile
    {
        public int x;
        public int z;
    }
#endif
}

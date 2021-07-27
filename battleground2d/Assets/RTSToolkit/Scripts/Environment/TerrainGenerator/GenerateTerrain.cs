using UnityEngine;
using UnityEngine.AI;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace RTSToolkit
{
    public class GenerateTerrain : MonoBehaviour
    {
        public static GenerateTerrain active;

        public int length = 2000;
        public int height = 200;

        public int heightmapResolution = 257;
        public int alphamapResolution = 257;

        [HideInInspector] public int nTilesRadius = 4;

        [HideInInspector] public float radiusInner = 2000f;
        [HideInInspector] public float radiusOuter = 3000f;

        [HideInInspector] public bool isOnRuntime = false;

        [HideInInspector] public TerrainChunkSettings tcs;

        public Transform camTransf;

        public float treeBilboardStart = 70f;
        public float treeCrossFadeLength = 70f;
        public GameObject water;

        [HideInInspector] public List<Terrain> loadedTerrains = new List<Terrain>();
        [HideInInspector] public List<float[,]> loadedTerrainsHeightmaps = new List<float[,]>();
        [HideInInspector] public List<Vector2i> loadedTileIndices = new List<Vector2i>();
        [HideInInspector] public List<ForestPlacer> forestPlacers = new List<ForestPlacer>();
        public List<ForestPlacerSaver> forestPlacerSavers = new List<ForestPlacerSaver>();

        [HideInInspector] public string updateType;
        [HideInInspector] public int updateTypeIndex = 2;

        RTSMaster rtsm;
        private bool isFirstTime = true;

        public Material terrainMaterial;

        public float tileLengthToFogDistanceMultiplier = 1.5f;

        public bool buildNavigation = true;

        void Awake()
        {
            active = this;
            RefreshLoadedTerrainHeightmaps();
        }

        void Start()
        {
            rtsm = RTSMaster.active;
            isOnRuntime = true;
            InitRuntime();
        }

        public void RefreshLoadedTerrainHeightmaps()
        {
            loadedTerrainsHeightmaps.Clear();
            for (int i = 0; i < loadedTerrains.Count; i++)
            {
                if (loadedTerrains[i] != null)
                {
                    loadedTerrainsHeightmaps.Add(loadedTerrains[i].terrainData.GetHeights(0, 0, heightmapResolution, heightmapResolution));
                }
            }
        }

        public void Generate()
        {
            CleanUp();

            camTransf = Camera.main.gameObject.transform;

            if (Rivers.GetActive() != null)
            {
                if (Rivers.GetActive().gameObject.activeSelf)
                {
                    if (Rivers.GetActive().enabled)
                    {
                        Rivers.GetActive().GenerateDLACheck();
                    }
                }
            }

            Init();

            for (int i = 0; i < tcs.terrainGos.Count; i++)
            {
                GameObject go = tcs.terrainGos[i];
                go.transform.SetParent(this.gameObject.transform);
            }

            InitRuntime();
            GenerateTerrain.active = this;
        }

        public void Update_E()
        {
            if (updateTypeIndex != 3)
            {
                Update();
            }
        }

        public void CleanUp()
        {
            for (int i = 0; i < loadedTerrains.Count; i++)
            {
                if (loadedTerrains[i] != null && loadedTerrains[i].terrainData != null)
                {
                    TreeInstance[] ti = new TreeInstance[0];
                    loadedTerrains[i].terrainData.treeInstances = ti;
                    loadedTerrains[i].terrainData.treePrototypes = null;
                    loadedTerrains[i].terrainData.RefreshPrototypes();
                }
            }

            List<GameObject> terDestr = new List<GameObject>();

            foreach (Transform tr in gameObject.transform)
            {
                Terrain ter1 = tr.gameObject.GetComponent<Terrain>();

                if (ter1 != null)
                {
                    terDestr.Add(tr.gameObject);
                    DestroyImmediate(ter1.terrainData);
                }
            }

            for (int i = 0; i < terDestr.Count; i++)
            {
                DestroyImmediate(terDestr[i]);
            }

            if (ResourcePoint.GetActive() != null)
            {
                if (ResourcePoint.GetActive().gameObject.activeSelf)
                {
                    if (ResourcePoint.GetActive().enabled)
                    {
                        ResourcePoint.GetActive().Clean();
                    }
                }
            }

            if (SoundManager.GetActive() != null)
            {
                if (SoundManager.GetActive().gameObject.activeSelf)
                {
                    if (SoundManager.GetActive().enabled)
                    {
                        SoundManager.GetActive().Clean();
                    }
                }
            }

            if (RockPlacer.GetActive() != null)
            {
                if (RockPlacer.GetActive().gameObject.activeSelf)
                {
                    if (RockPlacer.GetActive().enabled)
                    {
                        RockPlacer.GetActive().Clean();
                    }
                }
            }

            if (TerrainTextures.GetActive() != null)
            {
                if (TerrainTextures.GetActive().gameObject.activeSelf)
                {
                    if (TerrainTextures.GetActive().enabled)
                    {
                        TerrainTextures.GetActive().Clean();
                    }
                }
            }

            if (UnityNavigation.GetActive() != null)
            {
                if (UnityNavigation.GetActive().gameObject.activeSelf)
                {
                    if (UnityNavigation.GetActive().enabled)
                    {
                        UnityNavigation.GetActive().Clean();
                    }
                }
            }

            loadedTileIndices.Clear();
            loadedTerrains.Clear();
            forestPlacers.Clear();

            tcs = null;
            GenerateTerrain.active = null;
        }

        public void SwithResolutionRuntime(int lengthIn, int resIn)
        {
            if (length != lengthIn || heightmapResolution != resIn + 1)
            {
                for (int i = 0; i < loadedTileIndices.Count; i++)
                {
                    Diplomacy.active.RemoveNationsFromTile(loadedTileIndices[i]);
                }

                float fracInner = (1f * length) / radiusInner;
                float fracOuter = (1f * length) / radiusOuter;
                length = lengthIn;
                heightmapResolution = resIn + 1;
                alphamapResolution = resIn + 1;
                radiusInner = length / fracInner;
                radiusOuter = length / fracOuter;

                Animals.active.CleanEverything();
                Generate();

                NationSpawner.active.ResetSpawned();
                RenderSettings.fogEndDistance = tileLengthToFogDistanceMultiplier * length;
            }
        }

        public void InitRuntime()
        {
            tcs.length = length;
            tcs.height = height;
            tcs.cache = new ChunkCache();

            for (int i = -nTilesRadius; i < nTilesRadius; i++)
            {
                for (int j = -nTilesRadius; j < nTilesRadius; j++)
                {
                    Vector2i v2i = new Vector2i(i, j);
                    int id = loadedTileIndices.IndexOf(v2i);

                    if (id > -1)
                    {
                        TerrainChunk chunk = new TerrainChunk(tcs, i, j);
                        chunk.generateTerrain = this;
                        chunk.terrain = loadedTerrains[id];

                        tcs.cache.loadedChunksV.Add(v2i);
                        tcs.cache.loadedChunksT.Add(chunk);
                    }
                }
            }

            if (updateTypeIndex != 3)
            {
                for (int i = 0; i < forestPlacers.Count; i++)
                {
                    forestPlacers[i].Starter(false);
                }
            }

            UpdateNeighbourConnectionsICall();
            RenderSettings.fogEndDistance = tileLengthToFogDistanceMultiplier * length;
        }

        public void Init()
        {
            tcs = new TerrainChunkSettings(heightmapResolution, alphamapResolution, length, height, this);
            tcs.generateTerrain = this;
            tcs.cache = new ChunkCache();
            TerrainChunk chunk = null;

            for (int i = -nTilesRadius; i < nTilesRadius; i++)
            {
                for (int j = -nTilesRadius; j < nTilesRadius; j++)
                {
                    Vector2i v2i = new Vector2i(i, j);

                    chunk = new TerrainChunk(tcs, i, j);
                    chunk.generateTerrain = this;

                    tcs.cache.loadedChunksV.Add(v2i);
                    tcs.cache.loadedChunksT.Add(chunk);
                }
            }

            if (rtsm == null)
            {
                rtsm = RTSMaster.GetActive();
            }
        }

        public void UpdateNeighbourConnectionsICall()
        {
            StartCoroutine(UpdateNeighbourConnectionsI());
        }

        IEnumerator UpdateNeighbourConnectionsI()
        {
            yield return new WaitForEndOfFrame();

            if (updateTypeIndex != 3)
            {
                UpdateNeighbourConnections();
            }
        }

        public void UpdateNeighbourConnections()
        {
            tcs.cache.UpdateAllChunkNeighbors();

            for (int i1 = 0; i1 < loadedTileIndices.Count; i1++)
            {
                Terrain xDown = null;
                Terrain zUp = null;
                Terrain xUp = null;
                Terrain zDown = null;

                int j = loadedTileIndices.IndexOf(new Vector2i(loadedTileIndices[i1].x - 1, loadedTileIndices[i1].z));

                if (j > -1)
                {
                    xDown = loadedTerrains[j];
                }

                j = loadedTileIndices.IndexOf(new Vector2i(loadedTileIndices[i1].x, loadedTileIndices[i1].z + 1));

                if (j > -1)
                {
                    zUp = loadedTerrains[j];
                }

                j = loadedTileIndices.IndexOf(new Vector2i(loadedTileIndices[i1].x + 1, loadedTileIndices[i1].z));

                if (j > -1)
                {
                    xUp = loadedTerrains[j];
                }

                j = loadedTileIndices.IndexOf(new Vector2i(loadedTileIndices[i1].x, loadedTileIndices[i1].z - 1));

                if (j > -1)
                {
                    zDown = loadedTerrains[j];
                }

                loadedTerrains[i1].SetNeighbors(xDown, zUp, xUp, zDown);
                loadedTerrains[i1].Flush();
            }
        }

        void Update()
        {
            if (tcs != null)
            {
                if (updateTypeIndex == 1)
                {
                    tcs.UpdaterDistance(camTransf.position, radiusInner, radiusOuter, isFirstTime);
                }
                else if (updateTypeIndex == 2)
                {
                    tcs.Updater(camTransf.position, nTilesRadius, isFirstTime);
                }

                isFirstTime = false;
            }

            if (updateTypeIndex == 3)
            {
                ScanForTerrains();
            }
        }

        void ScanForTerrains()
        {
            Terrain[] allTerrains = UnityEngine.Object.FindObjectsOfType<Terrain>();

            int i_contains = 0;
            for (int i = 0; i < allTerrains.Length; i++)
            {
                if (loadedTerrains.Contains(allTerrains[i]))
                {
                    i_contains = i_contains + 1;
                }
            }

            if ((allTerrains.Length != loadedTerrains.Count) || (allTerrains.Length != i_contains) || (loadedTerrains.Count != i_contains))
            {
                if (allTerrains.Length > 0)
                {

                    for (int i = 0; i < loadedTerrains.Count; i++)
                    {
                        if (loadedTerrains[i] == null)
                        {
                            Animals.active.RemoveAnimalsFromTile(loadedTileIndices[i]);
                        }
                        else if (allTerrains.Contains(loadedTerrains[i]) == false)
                        {
                            Animals.active.RemoveAnimalsFromTile(loadedTileIndices[i]);
                        }

                        Diplomacy.active.SaveTileNationsToMemory(loadedTileIndices[i]);
                    }

                    loadedTerrains = allTerrains.ToList();
                    loadedTileIndices.Clear();
                    forestPlacers.Clear();

                    length = (int)loadedTerrains[0].terrainData.size.x;
                    height = (int)loadedTerrains[0].terrainData.size.z;

                    heightmapResolution = loadedTerrains[0].terrainData.heightmapResolution;
                    alphamapResolution = loadedTerrains[0].terrainData.alphamapResolution;

                    tcs = new TerrainChunkSettings(heightmapResolution, alphamapResolution, length, height, this);
                    tcs.generateTerrain = this;

                    for (int i = 0; i < loadedTerrains.Count; i++)
                    {
                        loadedTileIndices.Add(
                            tcs.GetChunkPosition(
                                new Vector3(
                                    loadedTerrains[i].transform.position.x + 0.5f * length,
                                    0f,
                                    loadedTerrains[i].transform.position.z + 0.5f * length
                                )
                            )
                        );

                        ForestPlacer forestPlacer = new ForestPlacer();
                        forestPlacer.GetFromExistingTerrain(loadedTerrains[i]);

                        forestPlacers.Add(forestPlacer);

                        if (ResourcePoint.GetActive() != null)
                        {
                            if (ResourcePoint.GetActive().gameObject.activeSelf)
                            {
                                if (ResourcePoint.GetActive().enabled)
                                {
                                    if (ResourcePoint.GetActive().IsOnTerrain(loadedTerrains[i]) == false)
                                    {
                                        ResourcePoint.GetActive().PopulateResourcesOnTerrain(loadedTerrains[i]);
                                    }
                                }
                            }
                        }

                        if (SoundManager.GetActive() != null)
                        {
                            if (SoundManager.GetActive().gameObject.activeSelf)
                            {
                                if (SoundManager.GetActive().enabled)
                                {
                                    if (SoundManager.GetActive().IsOnTerrain(loadedTerrains[i]) == false)
                                    {
                                        SoundManager.GetActive().InitializeOnTerrain(loadedTerrains[i]);
                                    }
                                }
                            }
                        }

                        if (Animals.GetActive() != null)
                        {
                            if (Animals.GetActive().gameObject.activeSelf)
                            {
                                if (Animals.GetActive().enabled)
                                {
                                    if (isOnRuntime)
                                    {
                                        Animals.GetActive().FillTerrainTileWithAnimals(loadedTileIndices[i]);
                                    }
                                }
                            }
                        }
                    }

                    for (int j = 0; j < loadedTerrains.Count; j++)
                    {
                        Diplomacy.active.LoadTileNationsFromMemory(loadedTileIndices[j]);
                    }

                    TriggerAStar();
                }
                else
                {
                    loadedTerrains.Clear();
                    loadedTileIndices.Clear();
                    forestPlacers.Clear();
                }
            }
        }

        public void TriggerAStar()
        {
#if ASTAR
        if (isOnRuntime && buildNavigation)
        {
            Vector2i v2i = tcs.GetChunkPosition(camTransf.position);
            AStarCompiler aStarCompiler = AStarCompiler.GetActive();
            aStarCompiler.UpdateFromCentralAll(v2i.x, v2i.z);
        }
#endif
        }

        public void TriggerNavigationTileBuild()
        {
#if !ASTAR
            if (loadedTerrains.Count > 0 && buildNavigation)
            {
                Bounds bounds = new Bounds();

                float minx = float.MaxValue;
                float maxx = float.MinValue;
                float minz = float.MaxValue;
                float maxz = float.MinValue;

                for (int i = 0; i < loadedTerrains.Count; i++)
                {
                    if (loadedTerrains[i].transform.position.x < minx)
                    {
                        minx = loadedTerrains[i].transform.position.x;
                    }

                    if ((loadedTerrains[i].transform.position.x + length) > maxx)
                    {
                        maxx = loadedTerrains[i].transform.position.x + length;
                    }

                    if (loadedTerrains[i].transform.position.z < minz)
                    {
                        minz = loadedTerrains[i].transform.position.z;
                    }

                    if ((loadedTerrains[i].transform.position.z + length) > maxz)
                    {
                        maxz = loadedTerrains[i].transform.position.z + length;
                    }
                }

                bounds.size = new Vector3(maxx - minx, height, maxz - minz);
                bounds.center = new Vector3(0.5f * (minx + maxx), 0.5f * height, 0.5f * (minz + maxz));

                NavMeshSurface nms = this.gameObject.GetComponent<NavMeshSurface>();
                bool build = false;

                if (nms == null)
                {
                    nms = this.gameObject.AddComponent<NavMeshSurface>();
                    build = true;
                }

                nms.overrideVoxelSize = true;
                nms.voxelSize = 1.5f;

                if (build)
                {
                    nms.BuildNavMesh(bounds);
                }
                if (AreTerrainsFullyLoaded())
                {
                    nms.UpdateNavMesh(nms.navMeshData, bounds);
                }
            }
#endif
        }

        public void TriggerAnimalSpawns()
        {
            if (rtsm != null && Animals.active != null && isOnRuntime)
            {
                for (int i = 0; i < loadedTileIndices.Count; i++)
                {
                    Animals.active.FillTerrainTileWithAnimals(loadedTileIndices[i]);
                }
            }
        }

        public void TriggerWaterShift()
        {
            if (isOnRuntime)
            {
                if (water != null)
                {
                    Vector2i v2i = tcs.GetChunkPosition(camTransf.position);
                    water.transform.position = new Vector3(1f * length * v2i.x + 0.5f * length, water.transform.position.y, 1f * length * v2i.z + 0.5f * length);
                }
            }
        }

        public void TriggerNationLoad()
        {
            if (rtsm != null && Diplomacy.active != null && isOnRuntime)
            {
                for (int i = 0; i < loadedTileIndices.Count; i++)
                {
                    Diplomacy.active.LoadTileNationsFromMemory(loadedTileIndices[i]);
                }
            }
        }

        public bool AreTerrainsFullyLoaded()
        {
            Vector2i v2i = tcs.GetChunkPosition(camTransf.position);
            List<Vector2i> chPositions = tcs.GetChunkPositionsInRadius(v2i, nTilesRadius);

            bool areLoaded = true;

            for (int i = 0; i < chPositions.Count; i++)
            {
                if (!loadedTileIndices.Contains(chPositions[i]))
                {
                    areLoaded = false;
                }
            }

            return areLoaded;
        }

        public Terrain GetTerrainBellow(Vector3 pos)
        {
            Terrain ter = null;
            Vector2i v2i = tcs.GetChunkPosition(pos);

            int i = loadedTileIndices.IndexOf(v2i);
            if (i >= 0)
            {
                ter = loadedTerrains[i];
            }

            return ter;
        }

        public int GetTerrainIndexBellow(Vector3 pos)
        {
            Vector2i v2i = tcs.GetChunkPosition(pos);

            int i = loadedTileIndices.IndexOf(v2i);
            if (i >= 0)
            {
                return i;
            }

            return -1;
        }

        public Terrain GetTerrainFromTile(Vector2i tile)
        {
            for (int i = 0; i < loadedTileIndices.Count; i++)
            {
                if (Vector2i.IsEqual(tile, loadedTileIndices[i]))
                {
                    return loadedTerrains[i];
                }
            }

            return null;
        }

        public Vector2i GetTileFromTerrain(Terrain ter)
        {
            return GetChunkPosition(ter.transform.position);
        }

        public Vector2i GetChunkPosition(Vector3 pos)
        {
            return tcs.GetChunkPosition(pos);
        }

        public Vector3 GetChunkMiddlePoint(Vector3 pos)
        {
            Vector2i v2i = GetChunkPosition(pos);
            return GetChunkMiddlePoint(v2i);
        }

        public Vector3 GetChunkMiddlePoint(Vector2i v2i)
        {
            Vector3 v0 = new Vector3((v2i.x * length + 0.5f * length), 0f, (v2i.z * length + 0.5f * length));
            return v0;
        }

        public bool HasNavigation(Vector3 pos)
        {
            return HasNavigation(GetChunkPosition(pos));
        }

        public bool HasNavigation(Vector2i tile)
        {
            if (rtsm.useAStar)
            {
                return true;
            }

            UnityEngine.AI.NavMeshHit closestHit;
            return UnityEngine.AI.NavMesh.SamplePosition(GetChunkMiddlePoint(tile), out closestHit, length, UnityEngine.AI.NavMesh.AllAreas);
        }

        public bool IsTileVisible(Vector2i tile, Vector3 pos)
        {
            Vector2i posTile = GetChunkPosition(pos);

            if (
                (posTile.x - tile.x < nTilesRadius) &&
                (posTile.x - tile.x > -nTilesRadius) &&
                (posTile.z - tile.z < nTilesRadius) &&
                (posTile.z - tile.z > -nTilesRadius)
            )
            {
                return true;
            }

            return false;
        }

        public static GenerateTerrain GetActive()
        {
            if (GenerateTerrain.active == null)
            {
                GenerateTerrain.active = UnityEngine.Object.FindObjectOfType<GenerateTerrain>();
            }

            return GenerateTerrain.active;
        }

        public float GetHeight()
        {
            if (updateTypeIndex != 3)
            {
                return height;
            }
            else
            {
                Terrain[] allTerrains = UnityEngine.Object.FindObjectsOfType<Terrain>();

                if (allTerrains.Length > 0)
                {
                    return allTerrains[0].terrainData.size.y;
                }
            }

            return height;
        }

#if UNITY_EDITOR
        [InitializeOnLoad]
        class EditorUpdater
        {
            static EditorUpdater()
            {
                EditorApplication.update += Update;
            }

            static void Update()
            {
                GenerateTerrain gt = GenerateTerrain.active;
                if (gt != null)
                {
                    gt.Update_E();
                }

                TerrainTextures tt = TerrainTextures.GetActive();
                if (tt != null)
                {
                    tt.Update_E();
                }

                UnityNavigation un = UnityNavigation.GetActive();
                if (un != null)
                {
                    un.Update_E();
                }
            }
        }
#endif
    }

    // Procedural terrain generation libraries based on
    // http://code-phi.com/infinite-terrain-generation-in-unity-3d/
    // approach

    [System.Serializable]
    public class TerrainChunkSettings
    {
        public int heightmapResolution;
        public int alphamapResolution;

        public int length;
        public int height;

        public ChunkCache cache;
        public List<GameObject> terrainGos = new List<GameObject>();
        [HideInInspector] public GenerateTerrain generateTerrain;

        public void Updater(Vector3 pos, int nTilesRadius, bool isFirstTime)
        {
            if (cache == null)
            {
                cache = new ChunkCache();
            }

            cache.Update();
            UpdateTerrain(pos, nTilesRadius, isFirstTime);
        }

        public void UpdaterDistance(Vector3 pos, float radiusIn, float radiusOut, bool isFirstTime)
        {
            if (cache == null)
            {
                cache = new ChunkCache();
            }

            cache.Update();
            UpdateTerrainDistance(pos, radiusIn, radiusOut, isFirstTime);
        }

        public TerrainChunkSettings(int heightmapResolution1, int alphamapResolution1, int length1, int height1, GenerateTerrain generateTerrain1)
        {
            generateTerrain = generateTerrain1;
            heightmapResolution = heightmapResolution1;
            alphamapResolution = alphamapResolution1;
            length = length1;
            height = height1;
        }

        public List<Vector2i> GetChunkPositionsInRadius(Vector2i chunkPosition, int radius)
        {
            List<Vector2i> result = new List<Vector2i>();

            for (int zCircle = -radius; zCircle <= radius; zCircle++)
            {
                for (int xCircle = -radius; xCircle <= radius; xCircle++)
                {
                    if (xCircle * xCircle + zCircle * zCircle < radius * radius)
                        result.Add(new Vector2i(chunkPosition.x + xCircle, chunkPosition.z + zCircle));
                }
            }

            return result;
        }

        public Vector2i GetChunkPosition(Vector3 worldPosition)
        {
            int x = (int)Mathf.Floor(worldPosition.x / length);
            int z = (int)Mathf.Floor(worldPosition.z / length);

            return new Vector2i(x, z);
        }

        public Vector2 GetWorldPosition(Vector2i ipos)
        {
            float x = ipos.x * length + 0.5f * length;
            float z = ipos.z * length + 0.5f * length;

            return new Vector2(x, z);
        }

        private void GenerateChunk(int x, int z)
        {
            if (cache.ChunkCanBeAdded(x, z))
            {
                if (!generateTerrain.loadedTileIndices.Contains(new Vector2i(x, z)))
                {
                    TerrainChunk chunk = new TerrainChunk(this, x, z);
                    chunk.generateTerrain = generateTerrain;
                    cache.AddNewChunk(chunk);
                }
            }
        }

        private void RemoveChunk(int x, int z)
        {
            if (cache.ChunkCanBeRemoved(x, z))
            {
                Vector2i v2i = new Vector2i(x, z);
                if (generateTerrain.loadedTileIndices.Contains(v2i))
                {
                    cache.RemoveChunk(x, z);
                }
            }
        }

        public void UpdateTerrain(Vector3 worldPosition, int radius, bool isFirstTime)
        {
            Vector2i chunkPosition = GetChunkPosition(worldPosition);
            List<Vector2i> newPositions = GetChunkPositionsInRadius(chunkPosition, radius);

            List<Vector2i> loadedChunks = cache.GetGeneratedChunks();
            List<Vector2i> chunksToRemove = loadedChunks.Except(newPositions).ToList();

            List<Vector2i> positionsToGenerate = newPositions.Except(chunksToRemove).ToList();

            for (int i = 0; i < positionsToGenerate.Count; i++)
            {
                Vector2i position = positionsToGenerate[i];
                GenerateChunk(position.x, position.z);
            }

            for (int i = 0; i < chunksToRemove.Count; i++)
            {
                Vector2i position = chunksToRemove[i];
                if (isFirstTime == false)
                {
                    if (generateTerrain.isOnRuntime)
                    {
                        Diplomacy.active.SaveTileNationsToMemory(position);
                    }
                }

                if (generateTerrain.isOnRuntime)
                {
                    if (Animals.active != null)
                    {
                        Animals.active.RemoveAnimalsFromTile(position);
                    }
                }

                RemoveChunk(position.x, position.z);
            }
        }

        public void UpdateTerrainDistance(Vector3 worldPosition, float radiusIn, float radiusOut, bool isFirstTime)
        {
            Vector2i chunkPosition = GetChunkPosition(worldPosition);

            int iRadiusIn = (int)Mathf.Floor(radiusIn / length) + 1;

            List<Vector2i> newPositions = GetChunkPositionsInRadius(chunkPosition, iRadiusIn);
            List<Vector2i> loadedChunks = cache.GetGeneratedChunks();

            List<Vector2i> chunksToRemove = new List<Vector2i>();

            for (int i = 0; i < loadedChunks.Count; i++)
            {
                Vector2i loaded = loadedChunks[i];
                Vector2 v2 = GetWorldPosition(loaded);
                float maxPos = Mathf.Max(Mathf.Abs(worldPosition.x - v2.x), Mathf.Abs(worldPosition.z - v2.y));

                if (maxPos > radiusOut)
                {
                    chunksToRemove.Add(loaded);
                }
            }

            List<Vector2i> positionsToGenerate = newPositions.Except(chunksToRemove).ToList();
            List<Vector2i> positionsToGenerateChecked = new List<Vector2i>();

            for (int i = 0; i < positionsToGenerate.Count; i++)
            {
                Vector2i gen = positionsToGenerate[i];
                Vector2 v2 = GetWorldPosition(gen);
                float maxPos = Mathf.Max(Mathf.Abs(worldPosition.x - v2.x), Mathf.Abs(worldPosition.z - v2.y));

                if (maxPos < radiusIn)
                {
                    positionsToGenerateChecked.Add(gen);
                }
            }

            for (int i = 0; i < positionsToGenerateChecked.Count; i++)
            {
                Vector2i position = positionsToGenerateChecked[i];
                GenerateChunk(position.x, position.z);
            }

            for (int i = 0; i < chunksToRemove.Count; i++)
            {
                Vector2i position = chunksToRemove[i];

                if (isFirstTime == false)
                {
                    if (generateTerrain.isOnRuntime)
                    {
                        Diplomacy.active.SaveTileNationsToMemory(position);
                    }
                }

                if (generateTerrain.isOnRuntime)
                {
                    if (Animals.active != null)
                    {
                        Animals.active.RemoveAnimalsFromTile(position);
                    }
                }

                RemoveChunk(position.x, position.z);
            }
        }
    }

    [System.Serializable]
    public class TerrainChunk
    {
        public Vector2i position;
        public Terrain terrain;
        public TerrainChunkSettings settings;

        TerrainChunkNeighborhood neighborhood;

        public float[,] heightmap;
        public object heightmapThreadLockObject;

        public GenerateTerrain generateTerrain;

        public bool heightMapComplete = false;
        public bool texturesComplete = false;

        public TerrainChunk(TerrainChunkSettings settings1, int x, int z)
        {
            heightmapThreadLockObject = new object();
            settings = settings1;
            neighborhood = new TerrainChunkNeighborhood();
            position = new Vector2i(x, z);
        }

        public void CreateTerrain()
        {
            TerrainData terrainData = new TerrainData();
            terrainData.name = "ter_" + position.x + "_" + position.z;
            terrainData.heightmapResolution = settings.heightmapResolution;
            terrainData.alphamapResolution = settings.alphamapResolution;

            terrainData.size = new Vector3(settings.length, settings.height, settings.length);

            GameObject newTerrainGameObject = Terrain.CreateTerrainGameObject(terrainData);
            terrain = newTerrainGameObject.GetComponent<Terrain>();
            newTerrainGameObject.name = "ter_" + position.x + "_" + position.z;
            newTerrainGameObject.transform.parent = generateTerrain.transform;

            newTerrainGameObject.transform.position = GetChunkWorldPosition();

            terrainData.SetHeights(0, 0, heightmap);

            if (TerrainTextures.GetActive() != null)
            {
                TerrainTextures.GetActive().GenerateSplatMaps(this, terrain);
            }

            settings.terrainGos.Add(newTerrainGameObject);

            terrain.heightmapPixelError = 1f;
            terrain.basemapDistance = 2000f;

            terrain.drawHeightmap = false;
            terrain.drawTreesAndFoliage = false;

            CreateVegetation();

            if (RockPlacer.GetActive() != null)
            {
                if (RockPlacer.GetActive().gameObject.activeSelf)
                {
                    if (RockPlacer.GetActive().enabled)
                    {
                        RockPlacer.GetActive().Initialize(terrain);
                    }
                }
            }

            if (ResourcePoint.GetActive() != null)
            {
                if (ResourcePoint.GetActive().gameObject.activeSelf)
                {
                    if (ResourcePoint.GetActive().enabled)
                    {
                        ResourcePoint.GetActive().PopulateResourcesOnTerrain(terrain);
                    }
                }
            }

            if (SoundManager.GetActive() != null)
            {
                if (SoundManager.GetActive().gameObject.activeSelf)
                {
                    if (SoundManager.GetActive().enabled)
                    {
                        SoundManager.GetActive().InitializeOnTerrain(terrain);
                    }
                }
            }

            generateTerrain.loadedTerrains.Add(terrain);
            generateTerrain.loadedTileIndices.Add(position);
            generateTerrain.RefreshLoadedTerrainHeightmaps();

            if (generateTerrain.AreTerrainsFullyLoaded())
            {
                generateTerrain.TriggerAStar();

                if (UnityNavigation.GetActive() != null && generateTerrain.buildNavigation)
                {
                    if (UnityNavigation.GetActive().gameObject.activeSelf)
                    {
                        if (UnityNavigation.GetActive().enabled)
                        {
                            UnityNavigation.GetActive().Build();
                        }
                    }
                }

                generateTerrain.TriggerAnimalSpawns();
                generateTerrain.TriggerNationLoad();
                generateTerrain.TriggerWaterShift();

                if ((generateTerrain.isOnRuntime) && (LoadingPleaseWait.active != null) && (LoadingPleaseWait.active.uiElement != null))
                {
                    LoadingPleaseWait.active.uiElement.SetActive(false);
                }
            }
        }

        public void CheckForCompletion()
        {
            if (heightMapComplete)
            {
                if (texturesComplete)
                {
                    terrain.Flush();
                    terrain.materialTemplate = generateTerrain.terrainMaterial;

                    generateTerrain.UpdateNeighbourConnectionsICall();

                    terrain.drawHeightmap = true;
                    terrain.drawTreesAndFoliage = true;
#if UNITY_EDITOR
                    if (generateTerrain.isOnRuntime == false)
                    {
                        UnityEngine.SceneManagement.Scene scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
                        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
                        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
                    }
#endif
                    generateTerrain.UpdateNeighbourConnections();
                }
            }
        }

        public Vector3 GetChunkWorldPosition()
        {
            return new Vector3(position.x * settings.length, 0f, position.z * settings.length);
        }

        private void CreateVegetation()
        {
            ForestPlacer forestPlacer = new ForestPlacer();

            forestPlacer.terrain = terrain;
            forestPlacer.heightmapResolution = settings.heightmapResolution;

            forestPlacer.terrainTile = position;
            forestPlacer.Starter();

            generateTerrain.forestPlacers.Add(forestPlacer);

            for (int i = 0; i < generateTerrain.forestPlacerSavers.Count; i++)
            {
                if (Vector2i.IsEqual(generateTerrain.forestPlacerSavers[i].terrainTile, position))
                {
                    forestPlacer.RemoveTrees(generateTerrain.forestPlacerSavers[i].removedTreeIndices);
                    generateTerrain.forestPlacerSavers.Remove(generateTerrain.forestPlacerSavers[i]);
                }
            }
        }

        public float GetTerrainHeight(Vector3 worldPosition)
        {
            return terrain.SampleHeight(worldPosition);
        }

        public void GenerateHeightmap()
        {

            if (TerrainHeightmap.GetActive() != null)
            {
                TerrainHeightmap.GetActive().GenerateHeightmap(this);
            }
            else
            {
                heightmap = new float[settings.heightmapResolution, settings.heightmapResolution];

                for (int zRes = 0; zRes < settings.heightmapResolution; zRes++)
                {
                    for (int xRes = 0; xRes < settings.heightmapResolution; xRes++)
                    {
                        heightmap[zRes, xRes] = 0f;
                    }
                }

                heightMapComplete = true;
            }
        }

        public bool IsHeightmapReady()
        {
            return heightMapComplete;
        }

        public void Remove()
        {
            heightmap = null;
            settings = null;

            generateTerrain.loadedTerrains.Remove(terrain);
            generateTerrain.loadedTileIndices.Remove(position);
            generateTerrain.RefreshLoadedTerrainHeightmaps();

            for (int i = 0; i < generateTerrain.forestPlacers.Count; i++)
            {
                if (generateTerrain.forestPlacers[i].terrain == terrain)
                {
                    ForestPlacerSaver fps = ForestPlacerSaver.CreateForestPlacerSaver(generateTerrain.forestPlacers[i]);

                    if (fps.removedTreeIndices.Count > 0)
                    {
                        generateTerrain.forestPlacerSavers.Add(ForestPlacerSaver.CreateForestPlacerSaver(generateTerrain.forestPlacers[i]));
                    }

                    generateTerrain.forestPlacers.Remove(generateTerrain.forestPlacers[i]);
                }
            }

            if (ResourcePoint.GetActive() != null)
            {
                if (ResourcePoint.GetActive().gameObject.activeSelf)
                {
                    if (ResourcePoint.GetActive().enabled)
                    {
                        ResourcePoint.GetActive().UnsetFromTerrain(terrain);
                    }
                }
            }

            if (SoundManager.GetActive() != null)
            {
                if (SoundManager.GetActive().gameObject.activeSelf)
                {
                    if (SoundManager.GetActive().enabled)
                    {
                        SoundManager.GetActive().UnsetFromTerrain(terrain);
                    }
                }
            }

            if (RockPlacer.GetActive() != null)
            {
                if (RockPlacer.GetActive().gameObject.activeSelf)
                {
                    if (RockPlacer.GetActive().enabled)
                    {
                        RockPlacer.GetActive().UnsetFromTerrain(terrain);
                    }
                }
            }

            if (neighborhood.xDown != null)
            {
                neighborhood.xDown.RemoveFromNeighborhood(this);
                neighborhood.xDown = null;
            }

            if (neighborhood.xUp != null)
            {
                neighborhood.xUp.RemoveFromNeighborhood(this);
                neighborhood.xUp = null;
            }

            if (neighborhood.zDown != null)
            {
                neighborhood.zDown.RemoveFromNeighborhood(this);
                neighborhood.zDown = null;
            }

            if (neighborhood.zUp != null)
            {
                neighborhood.zUp.RemoveFromNeighborhood(this);
                neighborhood.zUp = null;
            }

            if (terrain != null)
            {
                GameObject.Destroy(terrain.terrainData);
                GameObject.Destroy(terrain.gameObject);
            }
        }

        public void RemoveFromNeighborhood(TerrainChunk chunk)
        {
            if (neighborhood.xDown == chunk)
            {
                neighborhood.xDown = null;
            }
            if (neighborhood.xUp == chunk)
            {
                neighborhood.xUp = null;
            }
            if (neighborhood.zDown == chunk)
            {
                neighborhood.zDown = null;
            }
            if (neighborhood.zUp == chunk)
            {
                neighborhood.zUp = null;
            }
        }

        public void SetNeighbors(TerrainChunk chunk, TerrainNeighbor direction)
        {
            if (chunk != null)
            {
                switch (direction)
                {
                    case TerrainNeighbor.xUp:
                        neighborhood.xUp = chunk;
                        break;

                    case TerrainNeighbor.xDown:
                        neighborhood.xDown = chunk;
                        break;

                    case TerrainNeighbor.zUp:
                        neighborhood.zUp = chunk;
                        break;

                    case TerrainNeighbor.zDown:
                        neighborhood.zDown = chunk;
                        break;
                }
            }
        }

        public void UpdateNeighbors()
        {
            if (terrain != null)
            {
                Terrain xDown = neighborhood.xDown == null ? null : neighborhood.xDown.terrain;
                Terrain xUp = neighborhood.xUp == null ? null : neighborhood.xUp.terrain;
                Terrain zDown = neighborhood.zDown == null ? null : neighborhood.zDown.terrain;
                Terrain zUp = neighborhood.zUp == null ? null : neighborhood.zUp.terrain;

                terrain.SetNeighbors(xDown, zUp, xUp, zDown);
                terrain.Flush();
            }
        }
    }

    public class ChunkCache
    {
        readonly int maxChunkThreads = 3;
        public Dictionary<Vector2i, TerrainChunk> requestedChunks;
        public Dictionary<Vector2i, TerrainChunk> chunksBeingGenerated;
        public List<Vector2i> loadedChunksV;
        public List<TerrainChunk> loadedChunksT;
        public HashSet<Vector2i> chunksToRemove;
        public OnChunkGeneratedDelegate onChunkGenerated;

        public ChunkCache()
        {
            requestedChunks = new Dictionary<Vector2i, TerrainChunk>();
            chunksBeingGenerated = new Dictionary<Vector2i, TerrainChunk>();
            loadedChunksV = new List<Vector2i>();
            loadedChunksT = new List<TerrainChunk>();
            chunksToRemove = new HashSet<Vector2i>();
        }

        public void Update()
        {
            TryToDeleteQueuedChunks();
            GenerateHeightmapForAvailableChunks();
            CreateTerrainForReadyChunks();
        }

        public void AddNewChunk(TerrainChunk chunk)
        {
            requestedChunks.Add(chunk.position, chunk);
            GenerateHeightmapForAvailableChunks();
        }

        public void RemoveChunk(int x, int z)
        {
            chunksToRemove.Add(new Vector2i(x, z));
            TryToDeleteQueuedChunks();
        }

        public bool ChunkCanBeAdded(int x, int z)
        {
            Vector2i key = new Vector2i(x, z);
            return !(
                requestedChunks.ContainsKey(key) ||
                chunksBeingGenerated.ContainsKey(key) ||
                loadedChunksV.Contains(key)
            );
        }

        public bool ChunkCanBeRemoved(int x, int z)
        {
            Vector2i key = new Vector2i(x, z);
            return
                requestedChunks.ContainsKey(key) ||
                chunksBeingGenerated.ContainsKey(key) ||
                loadedChunksV.Contains(key);
        }

        public bool IsChunkGenerated(Vector2i chunkPosition)
        {
            return GetGeneratedChunk(chunkPosition) != null;
        }

        public TerrainChunk GetGeneratedChunk(Vector2i chunkPosition)
        {
            if (loadedChunksV.Contains(chunkPosition))
            {
                int id = loadedChunksV.IndexOf(chunkPosition);
                return loadedChunksT[id];
            }

            return null;
        }

        public List<Vector2i> GetGeneratedChunks()
        {
            return loadedChunksV;
        }

        private void GenerateHeightmapForAvailableChunks()
        {
            var requestedChunksL = requestedChunks.ToList();

            if (requestedChunksL.Count > 0 && chunksBeingGenerated.Count < maxChunkThreads)
            {
                var chunksToAdd = requestedChunksL.Take(maxChunkThreads - chunksBeingGenerated.Count);
                int n = 0;

                foreach (var chunkEntry in chunksToAdd)
                {
                    n++;
                }

                if (n > 0)
                {
                    if (
                        (GenerateTerrain.GetActive().isOnRuntime) &&
                        (LoadingPleaseWait.active != null) &&
                        (LoadingPleaseWait.active.uiElement != null) &&
                        (LoadingPleaseWait.active.uiElement.activeSelf == false)
                    )
                    {
                        LoadingPleaseWait.active.uiElement.SetActive(true);
                    }
                    else
                    {
                        foreach (var chunkEntry in chunksToAdd)
                        {
                            chunksBeingGenerated.Add(chunkEntry.Key, chunkEntry.Value);
                            requestedChunks.Remove(chunkEntry.Key);
                            chunkEntry.Value.GenerateHeightmap();
                        }
                    }
                }
            }
        }

        private void CreateTerrainForReadyChunks()
        {
            var anyTerrainCreated = false;
            var chunks = chunksBeingGenerated.ToList();

            foreach (var chunk in chunks)
            {
                if (chunk.Value.IsHeightmapReady())
                {
                    chunksBeingGenerated.Remove(chunk.Key);
                    loadedChunksV.Add(chunk.Key);
                    loadedChunksT.Add(chunk.Value);

                    chunk.Value.CreateTerrain();
                    anyTerrainCreated = true;

                    if (onChunkGenerated != null)
                    {
                        onChunkGenerated.Invoke(chunksBeingGenerated.Count);
                    }

                    SetChunkNeighborhood(chunk.Value);
                }
            }

            if (anyTerrainCreated)
            {
                UpdateAllChunkNeighbors();
            }
        }

        private void TryToDeleteQueuedChunks()
        {
            var chunksToRemove1 = chunksToRemove.ToList();

            for (int i = 0; i < chunksToRemove1.Count; i++)
            {
                Vector2i chunkPosition = chunksToRemove1[i];

                if (requestedChunks.ContainsKey(chunkPosition))
                {
                    requestedChunks.Remove(chunkPosition);
                    chunksToRemove.Remove(chunkPosition);
                }
                else if (loadedChunksV.Contains(chunkPosition))
                {
                    int id = loadedChunksV.IndexOf(chunkPosition);
                    var chunk = loadedChunksT[id];
                    chunk.Remove();

                    loadedChunksV.RemoveAt(id);
                    loadedChunksT.RemoveAt(id);
                    chunksToRemove.Remove(chunkPosition);
                }
                else if (!chunksBeingGenerated.ContainsKey(chunkPosition))
                {
                    chunksToRemove.Remove(chunkPosition);
                }
            }
        }

        public void SetChunkNeighborhood(TerrainChunk chunk)
        {
            TerrainChunk xUp = null;
            TerrainChunk xDown = null;
            TerrainChunk zUp = null;
            TerrainChunk zDown = null;

            int id = loadedChunksV.IndexOf(new Vector2i(chunk.position.x + 1, chunk.position.z));
            if (id > -1)
            {
                xUp = loadedChunksT[id];
            }

            id = loadedChunksV.IndexOf(new Vector2i(chunk.position.x - 1, chunk.position.z));
            if (id > -1)
            {
                xDown = loadedChunksT[id];
            }

            id = loadedChunksV.IndexOf(new Vector2i(chunk.position.x, chunk.position.z + 1));
            if (id > -1)
            {
                zUp = loadedChunksT[id];
            }

            id = loadedChunksV.IndexOf(new Vector2i(chunk.position.x, chunk.position.z - 1));
            if (id > -1)
            {
                zDown = loadedChunksT[id];
            }

            if (xUp != null)
            {
                chunk.SetNeighbors(xUp, TerrainNeighbor.xUp);
                xUp.SetNeighbors(chunk, TerrainNeighbor.xDown);
            }

            if (xDown != null)
            {
                chunk.SetNeighbors(xDown, TerrainNeighbor.xDown);
                xDown.SetNeighbors(chunk, TerrainNeighbor.xUp);
            }

            if (zUp != null)
            {
                chunk.SetNeighbors(zUp, TerrainNeighbor.zUp);
                zUp.SetNeighbors(chunk, TerrainNeighbor.zDown);
            }

            if (zDown != null)
            {
                chunk.SetNeighbors(zDown, TerrainNeighbor.zDown);
                zDown.SetNeighbors(chunk, TerrainNeighbor.zUp);
            }
        }

        public void UpdateAllChunkNeighbors()
        {
            foreach (var chunkEntry in loadedChunksT)
            {
                chunkEntry.UpdateNeighbors();
            }
        }
    }

    public class TerrainChunkNeighborhood
    {
        public TerrainChunk xUp { get; set; }

        public TerrainChunk xDown { get; set; }

        public TerrainChunk zUp { get; set; }

        public TerrainChunk zDown { get; set; }
    }

    public enum TerrainNeighbor
    {
        xUp,
        xDown,
        zUp,
        zDown
    }

    [System.Serializable]
    public class Vector2i
    {
        public int x;
        public int z;

        public Vector2i()
        {
            x = 0;
            z = 0;
        }

        public Vector2i(int x1, int z1)
        {
            x = x1;
            z = z1;
        }

        public override int GetHashCode()
        {
            return x.GetHashCode() ^ z.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            Vector2i other = obj as Vector2i;

            if (other == null)
            {
                return false;
            }

            return this.x == other.x && this.z == other.z;
        }

        public static bool IsEqual(Vector2i v1, Vector2i v2)
        {
            return v1.x == v2.x && v1.z == v2.z;
        }

        public override string ToString()
        {
            return "[" + x + "," + z + "]";
        }
    }

    public delegate void OnChunkGeneratedDelegate(int chunksLeftToGenerate);
}

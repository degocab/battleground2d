using UnityEngine;

namespace RTSToolkit
{
    public class SpawnQuickArmies : MonoBehaviour
    {
        RTSMaster rtsm;
        public int n = 200;
        public int nation = 0;
        public int nationToWhereToSpawn = 0;
        public bool usePlayerNation = false;

        public float radius = 70f;

        public int unitType = 11;
        public KeyCode key = KeyCode.Q;
        public bool randomiseRotation = true;

        void Start()
        {
            rtsm = RTSMaster.active;
            Random.InitState(88);
        }

        void Update()
        {
            if (Input.GetKeyDown(key))
            {
                Spawn();
            }
        }

        void Spawn()
        {
            if (usePlayerNation)
            {
                nation = Diplomacy.active.playerNation;
            }

            Vector3 nationPos = rtsm.nationPars[nationToWhereToSpawn].transform.position;

            for (int i = 0; i < n; i++)
            {
                Vector3 pos = TerrainProperties.RandomTerrainVectorCircleProc(nationPos, radius);
                Quaternion rot = rtsm.rtsUnitTypePrefabs[unitType].transform.rotation;

                if (randomiseRotation)
                {
                    rot = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f) * rtsm.rtsUnitTypePrefabs[unitType].transform.rotation;
                }

                if (rtsm.isMultiplayer)
                {
                    rtsm.rtsCameraNetwork.AddNetworkComponent(unitType, TerrainProperties.TerrainVectorProc(pos), rot, Diplomacy.active.GetNationNameFromId(nation), 1);
                }
                else
                {
                    GameObject go = Instantiate(rtsm.rtsUnitTypePrefabs[unitType], pos, rot);
                    string natName = rtsm.GetNationNameById(nation);
                    go.GetComponent<UnitPars>().Spawn(natName);
                }
            }
        }
    }
}

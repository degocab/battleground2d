using UnityEngine;

namespace RTSToolkit
{
    public class FallingRockSpawner : MonoBehaviour
    {
        public GameObject rockToSpawn;
        public float sizeRangeMin = 0.9f;
        public float sizeRangeMax = 1.1f;
        public float shapeVariation = 2f;

        public float spawnTimeMin = 0.5f;
        public float spawnTimeMax = 3f;

        public float velocityVariation = 10f;

        public float spawnTriggerDistance = 70f;

        float lastTime = 0f;
        float critTime = 0f;

        void Start()
        {
            critTime = Random.Range(spawnTimeMin, spawnTimeMax);

            if (RockPlacer.active != null)
            {
                RockPlacer.active.fallingRocks.Add(this);
            }
        }

        public void RunSpawner(float timeToAdd)
        {
            lastTime = lastTime + timeToAdd;

            if (lastTime > critTime)
            {
                lastTime = 0f;
                critTime = Random.Range(spawnTimeMin, spawnTimeMax);

                UnitPars up = RTSMaster.active.GetNearestUnit(transform.position);

                if (up != null)
                {
                    if (up.nation > 0 && up.nation < RTSMaster.active.nationPars.Count)
                    {
                        if (RTSMaster.active.nationPars[up.nation].isWizzardNation == false)
                        {
                            float r = (up.transform.position - transform.position).magnitude;

                            if (r < spawnTriggerDistance)
                            {
                                GameObject go = Instantiate(rockToSpawn, (transform.position + new Vector3(0f, 2f, 0f) + Random.insideUnitSphere), Random.rotation);
                                Vector3 shape = new Vector3(
                                    Random.Range(1f / shapeVariation, shapeVariation),
                                    Random.Range(1f / shapeVariation, shapeVariation),
                                    Random.Range(1f / shapeVariation, shapeVariation)
                                );

                                go.transform.localScale = Random.Range(sizeRangeMin, sizeRangeMax) * shape;
                                go.GetComponent<Rigidbody>().velocity = velocityVariation * Random.insideUnitSphere;
                            }

                            critTime = r / 5f;

                            if (critTime < spawnTimeMin)
                            {
                                critTime = spawnTimeMin;
                            }
                            if (critTime > 5f * spawnTimeMax)
                            {
                                critTime = 5f * spawnTimeMax;
                            }
                        }
                    }
                }
            }
        }

        void OnDestroy()
        {
            if (RockPlacer.active != null)
            {
                RockPlacer.active.fallingRocks.Remove(this);
            }
        }
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace RTSToolkit
{
    public class UnitParsType : MonoBehaviour
    {
        public float buildTime = 1f;
        public bool isBuilding = false;

        public bool isArcher = false;
        public bool isWizzard = false;
        public GameObject arrow = null;
        public Vector3 arrowOffset = Vector3.zero;
        public float velArrow = 20.0f;

        public bool hasBullets = false;
        public bool isWorker = false;

        public float searchDistance = 30f;

        public int maxAttackers = 20;
        public float stopDistOut = 2.5f;

        public int critFailedR = 7;

        public float attackWaiter = 2.0f;
        public float attackDelay = 1.5f;

        public float damageCoolDownTime = 0.4f;
        public float damageCoolDownMin = 1f;
        public float damageCoolDownMax = 4f;

        public float health = 100.0f;
        public float maxHealth = 100.0f;
        public float selfHealFactor = 4.0f;

        public float strength = 10.0f;
        public float defence = 10.0f;

        public List<AudioClip> attackSounds = new List<AudioClip>();
        public List<AudioClip> deathSounds = new List<AudioClip>();

        public int maxDeathCalls = 5;

        public float rEnclosed = 0.0f;

        public Vector3 unitCenter = Vector3.zero;
        public float unitSize = 1f;

        public string unitName = "";

        public List<string> levelNames = new List<string>();
        public List<Vector2> levelExpTimeGain = new List<Vector2>();

        public int totalLevel = 0;

        // 0 - life points    
        // 1 - attack 
        // 2 - defence 
        // 3 - building
        // 4 - wood cutting     
        // 5 - resource collection

        [HideInInspector] public int maxFailPath = 10;
        [HideInInspector] public int maxFakePathCount = 2;

        public bool buildSequenceMeshMode = false;
        public List<Mesh> buildSequenceMeshes = new List<Mesh>();
        public List<GameObject> buildSequencePrefabs = new List<GameObject>();
        [HideInInspector] public List<Material[]> buildSequenceMaterials = new List<Material[]>();

        public bool destroySequenceMeshMode = false;
        public List<Mesh> destroySequenceMeshes = new List<Mesh>();
        public List<GameObject> destroySequencePrefabs = new List<GameObject>();
        [HideInInspector] public List<Material[]> destroySequenceMaterials = new List<Material[]>();

        public List<ParticleSystem> smokes;
        public float smokeMinUpdateTime = 3f;
        public float smokeMaxUpdateTime = 9f;

        [HideInInspector] public List<GradientColorKey> defaultSmokeGradientKeys = new List<GradientColorKey>();
        [HideInInspector] public List<GradientAlphaKey> defaultSmokeGradientAlphaKeys = new List<GradientAlphaKey>();

        public List<EconomyResourceUnitPars> costs = new List<EconomyResourceUnitPars>();

#if UNITY_EDITOR
        public string buildSequenceMeshesPath;
        public string destroySequenceMeshesPath;
#endif

        public void Initialize(int rtsid)
        {
            levelNames.Clear();
            levelNames.Add("life points");
            levelNames.Add("attack");
            levelNames.Add("defence");
            levelNames.Add("building");
            levelNames.Add("wood cutting");
            levelNames.Add("resource collection");

            levelExpTimeGain.Clear();
            levelExpTimeGain.Add(new Vector2(0f, 0.5f));
            levelExpTimeGain.Add(new Vector2(0f, 0.1f));
            levelExpTimeGain.Add(new Vector2(0f, 0.1f));
            levelExpTimeGain.Add(new Vector2(0f, 0.5f));
            levelExpTimeGain.Add(new Vector2(0f, 0.1f));
            levelExpTimeGain.Add(new Vector2(0f, 0.1f));

            if (isBuilding)
            {
                buildSequenceMaterials.Clear();

                if (buildSequenceMeshMode == false)
                {
                    buildSequenceMeshes.Clear();

                    for (int i = 0; i < buildSequencePrefabs.Count; i++)
                    {
                        buildSequenceMeshes.Add(buildSequencePrefabs[i].GetComponent<MeshFilter>().sharedMesh);
                        buildSequenceMaterials.Add(buildSequencePrefabs[i].GetComponent<MeshRenderer>().sharedMaterials);
                    }
                }

                destroySequenceMaterials.Clear();

                if (destroySequenceMeshMode == false)
                {
                    destroySequenceMeshes.Clear();

                    for (int i = 0; i < destroySequencePrefabs.Count; i++)
                    {
                        destroySequenceMeshes.Add(destroySequencePrefabs[i].GetComponent<MeshFilter>().sharedMesh);
                        destroySequenceMaterials.Add(destroySequencePrefabs[i].GetComponent<MeshRenderer>().sharedMaterials);
                    }
                }
            }

            if (smokes != null && smokes.Count > 0)
            {
                defaultSmokeGradientKeys.Clear();
                for (int i = 0; i < smokes[0].colorOverLifetime.color.gradient.colorKeys.Length; i++)
                {
                    GradientColorKey gck = smokes[0].colorOverLifetime.color.gradient.colorKeys[i];
                    defaultSmokeGradientKeys.Add(new GradientColorKey(gck.color, gck.time));
                }

                defaultSmokeGradientAlphaKeys.Clear();
                for (int i = 0; i < smokes[0].colorOverLifetime.color.gradient.alphaKeys.Length; i++)
                {
                    GradientAlphaKey gak = smokes[0].colorOverLifetime.color.gradient.alphaKeys[i];
                    defaultSmokeGradientAlphaKeys.Add(new GradientAlphaKey(gak.alpha, gak.time));
                }
            }

            UnitPars up = GetComponent<UnitPars>();
            if (up != null)
            {
                up.rtsUnitId = rtsid;
                up.sqrSearchDistance = searchDistance * searchDistance;

                up.health = health;
                up.maxHealth = maxHealth;
                up.selfHealFactor = selfHealFactor;

                up.strength = strength;
                up.defence = defence;

                up.rEnclosed = rEnclosed;

                up.smokes = smokes;
            }
        }
    }
}

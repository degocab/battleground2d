using UnityEngine;
using System.Collections.Generic;

namespace RTSToolkitFree
{
    public class UnitPars : MonoBehaviour
    {
        public bool isMovable = true;

        public bool isReady = false;
        public bool isApproaching = false;
        public bool isAttacking = false;
        [HideInInspector] public bool isApproachable = true;
        public bool isHealing = false;
        public bool isImmune = false;
        public bool isDying = false;
        public bool isSinking = false;

        public UnitPars target = null;
        public List<UnitPars> attackers = new List<UnitPars>();

        public int noAttackers = 0;
        public int maxAttackers = 3;

        [HideInInspector] public float prevR;
        [HideInInspector] public int failedR = 0;
        public int critFailedR = 100;

        public float health = 100.0f;
        public float maxHealth = 100.0f;
        public float selfHealFactor = 10.0f;

        public float strength = 10.0f;
        public float defence = 10.0f;

        [HideInInspector] public int deathCalls = 0;
        public int maxDeathCalls = 5;

        [HideInInspector] public int sinkCalls = 0;
        public int maxSinkCalls = 5;

        [HideInInspector] public bool changeMaterial = true;

        public int nation = 1;

        void Start()
        {
            UnityEngine.AI.NavMeshAgent nma = GetComponent<UnityEngine.AI.NavMeshAgent>();
            
            if (nma != null)
            {
                nma.enabled = true;
            }
        }
    }
}

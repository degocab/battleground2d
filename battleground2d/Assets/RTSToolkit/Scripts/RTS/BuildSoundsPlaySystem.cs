using System.Collections.Generic;
using UnityEngine;

namespace RTSToolkit
{
    public class BuildSoundsPlaySystem : MonoBehaviour
    {
        public static BuildSoundsPlaySystem active;

        List<UnitPars> buildings = new List<UnitPars>();
        int innerLoopIndex = 0;
        public float updateTime = 1f;
        float deltaTime;

        public List<AudioClip> buildSounds = new List<AudioClip>();
        public bool useOnlyForPlayerNation = true;

        void Awake()
        {
            active = this;
        }

        void Start()
        {

        }

        float updateProgress = 0f;

        void Update()
        {
            deltaTime = Time.deltaTime;

            float updateProgressIncrement = (deltaTime / updateTime) * buildings.Count;
            updateProgress = updateProgress + updateProgressIncrement;

            int intUpdateProgress = (int)updateProgress;
            updateProgress = updateProgress - intUpdateProgress;
            int nToLoop = intUpdateProgress;

            for (int i = 0; i < nToLoop; i++)
            {
                if (buildings.Count < nToLoop)
                {
                    nToLoop = buildings.Count;
                }

                if (innerLoopIndex >= buildings.Count)
                {
                    innerLoopIndex = 0;
                }

                PlayRandomBuildSound(buildings[innerLoopIndex].transform.position);

                innerLoopIndex++;
            }
        }

        public void PlayRandomBuildSound(Vector3 pos)
        {
            if (buildSounds != null)
            {
                if (buildSounds.Count > 0)
                {
                    int isound = Random.Range(0, buildSounds.Count);

                    if (buildSounds[isound] != null)
                    {
                        Vector3 pos1 = 0.03f * pos + 0.97f * RTSCamera.active.transform.position;
                        AudioSource.PlayClipAtPoint(buildSounds[isound], pos1, 1f);
                    }
                }
            }
        }

        public void AddToSystem(UnitPars up)
        {
            RemoveFromSystem(up);

            bool playerNationPass = true;

            if (useOnlyForPlayerNation)
            {
                if (up.nation != Diplomacy.active.playerNation)
                {
                    playerNationPass = false;
                }
            }

            if (playerNationPass)
            {
                buildings.Add(up);
            }
        }

        public void RemoveFromSystem(UnitPars up)
        {
            buildings.Remove(up);
        }
    }
}

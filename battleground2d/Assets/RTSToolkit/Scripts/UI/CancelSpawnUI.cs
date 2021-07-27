using UnityEngine;

namespace RTSToolkit
{
    public class CancelSpawnUI : MonoBehaviour
    {
        public static CancelSpawnUI active;
        public GameObject cancelSpawnButton;

        void Awake()
        {
            active = this;
        }

        void Start()
        {

        }

        public void CancelSpawn()
        {
            SpawnPoint spawner = SelectionManager.active.selectedGoPars[0].thisSpawn;
            spawner.StopSpawning();
            DeActivate();
        }

        public void DeActivate()
        {
            ProgressCounterUI.active.DeActivate();
            cancelSpawnButton.SetActive(false);
        }

        public void Activate()
        {
            cancelSpawnButton.SetActive(true);
        }
    }
}

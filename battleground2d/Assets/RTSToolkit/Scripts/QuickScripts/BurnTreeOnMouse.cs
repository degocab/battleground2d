using System.Collections;
using UnityEngine;

namespace RTSToolkit
{
    public class BurnTreeOnMouse : MonoBehaviour
    {
        public KeyCode mainKey = KeyCode.B;
        public KeyCode burnSelectedUnitKey = KeyCode.N;

        void Start()
        {

        }

        void Update()
        {
            if (Input.GetKeyDown(mainKey))
            {
                StartCoroutine(BurnTree());
            }

            if (Input.GetKeyDown(burnSelectedUnitKey))
            {
                if (SelectionManager.active.selectedGoPars.Count > 0)
                {
                    UnitPars up = SelectionManager.active.selectedGoPars[0];
                    FireScaler.SetUpOnFire(up, up.transform.position, up.transform.rotation);
                }
            }
        }

        IEnumerator BurnTree()
        {
            ResourcePointObject rpo = RTSMaster.active.nationPars[Diplomacy.active.playerNation].resourcesCollection.GetTreeFromMouse();

            if (rpo != null)
            {
                FireScaler.SetTreeOnFire(rpo, TerrainProperties.TerrainVectorProc(rpo.position), Quaternion.identity);
            }

            yield return new WaitForEndOfFrame();
        }
    }
}

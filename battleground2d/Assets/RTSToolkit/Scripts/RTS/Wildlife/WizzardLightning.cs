using UnityEngine;

namespace RTSToolkit
{
    public class WizzardLightning : MonoBehaviour
    {
        public Vector3 strikeStartPosition = new Vector3(0, 1, 0);
        public float strikeTimeInterval = 2f;
        Lightning lightning;

        void Start()
        {
            lightning = Lightning.active;
        }

        public void TriggerStrike()
        {
            Vector3 lookingVector = transform.rotation * Vector3.forward;
            Vector3 pos = transform.position + transform.rotation * strikeStartPosition;
            lightning.CalculatePoints(pos, lookingVector, 200, 0.05f, 0f, 0.15f, Vector3.zero, 0f);
        }

        public void TriggerAreaRandomStrikesOld()
        {
            int numberOfStrikes = Random.Range(500, 550);

            for (int i = 0; i < numberOfStrikes; i++)
            {
                Quaternion randomRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                Vector3 lookingVector = randomRotation * Vector3.forward;
                Vector3 pos = TerrainProperties.RandomTerrainVectorCircleProc(transform.position, 100) + new Vector3(0f, 1f, 0f);
                float randomTime = GenericMath.RandomPow(1, 300, 20) - 1;
                lightning.CalculatePoints(pos, lookingVector, 500, 0.02f, 0.03f, 0.15f, Vector3.zero, randomTime);
            }
        }

        public void TriggerAreaRandomStrikes()
        {
            Lightning.AreaDecayLightning adl = new Lightning.AreaDecayLightning();
            adl.areaCenter = transform.position;
            lightning.AddAreaDecayLightning(adl);
        }
    }
}

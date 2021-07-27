using UnityEngine;

// This script allows to quickly rotate camera by a small angle
// and immediately rotate it back
// useful for refreshing tree bilboard lighting when time of day changes.

namespace RTSToolkit
{
    public class CameraJiggler : MonoBehaviour
    {
        public float jigglePeriod = 1f;
        public Vector3 jiggleRotationVector = new Vector3(0, 0.001f, 0);

        void Start()
        {

        }

        void Update()
        {
            transform.transform.rotation = Quaternion.Euler(transform.eulerAngles + jiggleRotationVector * Mathf.Sin(jigglePeriod * Time.time));
        }
    }
}

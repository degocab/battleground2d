using UnityEngine;

namespace RTSToolkit
{
    public class WindChanger : MonoBehaviour
    {
        public static WindChanger active;

        public float directionChangeFactor = 0.05f;
        public float speedChangeFactor = 0.003f;
        public float windMaximumSpeed = 1f;

        [HideInInspector] public Vector3 rotationVector = Vector3.zero;
        [HideInInspector] public float currentSpeed = 1f;

        WindZone windZone;

        void Awake()
        {
            active = this;
        }

        void Start()
        {
            windZone = GetComponent<WindZone>();
            angleToRotate = Random.Range(-directionChangeFactor, directionChangeFactor);
            speedChange = Random.Range(-speedChangeFactor, speedChangeFactor);
            i = 0;
        }

        void Update()
        {
            ChangeWind();
        }

        float angleToRotate;
        float speedChange;
        int i;

        void ChangeWind()
        {
            i++;
            if (i > 1000)
            {
                angleToRotate = Random.Range(-directionChangeFactor, directionChangeFactor);
                speedChange = Random.Range(-speedChangeFactor, speedChangeFactor);
                i = 0;
            }

            rotationVector = rotationVector + new Vector3(0f, angleToRotate, 0f);
            currentSpeed = currentSpeed + speedChange;

            if (rotationVector.y > 360f)
            {
                rotationVector = rotationVector - new Vector3(0f, 360f, 0f);
            }
            else if (rotationVector.y < 0f)
            {
                rotationVector = rotationVector + new Vector3(0f, 360f, 0f);
            }

            if (currentSpeed < 0f)
            {
                currentSpeed = 0f;
            }
            else if (currentSpeed > windMaximumSpeed)
            {
                currentSpeed = windMaximumSpeed;
            }

            transform.rotation = Quaternion.Euler(rotationVector);
            windZone.windMain = currentSpeed;
        }
    }
}

using UnityEngine;

namespace RTSToolkit
{
    public class UnitAnimationType : MonoBehaviour
    {
        public float spriteSize = 2.0f;
        public string modelName;

        public Vector3 offset = Vector3.zero;
        public bool castShadows = true;

        public UnitAnim idleAnimation;
        public UnitAnim walkAnimation;
        public UnitAnim runAnimation;
        public UnitAnim attackAnimation;
        public UnitAnim deathAnimation;

        public UnitAnim[] otherAnimations = new UnitAnim[0];
        public float maxMovementSpeed = 0f;

        void Start()
        {

        }
    }
}

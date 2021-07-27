using UnityEngine;
using System.Collections.Generic;

namespace RTSToolkit
{
    public class CloudsRenderingOrder : MonoBehaviour
    {
        public static CloudsRenderingOrder active;

        [HideInInspector] public ParticleSystem ps;
        public List<GradientColorKey> defaultGradientKeys = new List<GradientColorKey>();
        public List<GradientAlphaKey> defaultGradientAlphaKeys = new List<GradientAlphaKey>();

        void Awake()
        {
            active = this;
            ps = GetComponent<ParticleSystem>();

            for (int i = 0; i < ps.colorOverLifetime.color.gradient.colorKeys.Length; i++)
            {
                GradientColorKey gck = ps.colorOverLifetime.color.gradient.colorKeys[i];
                defaultGradientKeys.Add(new GradientColorKey(gck.color, gck.time));
            }

            for (int i = 0; i < ps.colorOverLifetime.color.gradient.alphaKeys.Length; i++)
            {
                GradientAlphaKey gak = ps.colorOverLifetime.color.gradient.alphaKeys[i];
                defaultGradientAlphaKeys.Add(new GradientAlphaKey(gak.alpha, gak.time));
            }
        }

        void Start()
        {
            Renderer rend = GetComponent<Renderer>();
            rend.material.renderQueue = 2800;
        }

        public void ChangeCloudsColor(Color col)
        {
            var col1 = ps.colorOverLifetime;
            Gradient grad = new Gradient();

            GradientColorKey[] defaultGradientKeysArray = defaultGradientKeys.ToArray();
            for (int i = 0; i < defaultGradientKeysArray.Length; i++)
            {
                defaultGradientKeysArray[i] = new GradientColorKey(defaultGradientKeysArray[i].color * col, defaultGradientKeysArray[i].time);
            }

            GradientAlphaKey[] defaultGradientAlphaKeysArray = defaultGradientAlphaKeys.ToArray();
            for (int i = 0; i < defaultGradientAlphaKeysArray.Length; i++)
            {
                defaultGradientAlphaKeysArray[i] = new GradientAlphaKey(defaultGradientAlphaKeysArray[i].alpha, defaultGradientAlphaKeysArray[i].time);
            }

            grad.SetKeys(defaultGradientKeysArray, defaultGradientAlphaKeysArray);
            col1.color = grad;
        }
    }
}

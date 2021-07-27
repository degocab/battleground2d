using System;
using UnityEngine;

namespace UnityStandardAssets.Effects
{
    public class ParticleSystemMultiplier : MonoBehaviour
    {
        // a simple script to scale the size, speed and lifetime of a particle system

        public float multiplier = 1;


        private void Start()
        {
            var systems = GetComponentsInChildren<ParticleSystem>();
            foreach (ParticleSystem system in systems)
            {
            	var psMain = system.main;
                psMain.startSize = psMain.startSize.constant*multiplier;
                psMain.startSpeed = psMain.startSpeed.constant*multiplier;
                psMain.startLifetime = psMain.startLifetime.constant*Mathf.Lerp(multiplier, 1, 0.5f);
                
//                 system.startSpeed *= multiplier;
//                 system.startLifetime *= Mathf.Lerp(multiplier, 1, 0.5f);
                system.Clear();
                system.Play();
            }
        }
    }
}

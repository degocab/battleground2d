using System.Collections.Generic;
using UnityEngine;

namespace RTSToolkit
{
    public class SelectionMarkParticle : MonoBehaviour
    {
        public static SelectionMarkParticle active;
        List<ParticleSystem> ptSystems = new List<ParticleSystem>();

        public Shader selectionMarkShader;

        public Texture2D selectionMarkTexturePlayer;
        public Texture2D selectionMarkTextureOther;
        public Texture2D selectionMarkTextureEnemy;

        public Texture2D selectionMarkHorizontalTexturePlayer;
        public Texture2D selectionMarkHorizontalTextureOther;
        public Texture2D selectionMarkHorizontalTextureEnemy;

        public Gradient colorGradient;

        public SelectionMarkMode selectionMarkMode = SelectionMarkMode.CameraFacing;
        public enum SelectionMarkMode { CameraFacing, Horizontal };

        public Vector3 offset;

        Vector3 camPos;
        Quaternion camRot;

        public float minParticleSize = 0.01f;

        public bool useSelectionMarks = true;

        void Awake()
        {
            active = this;
        }

        void Start()
        {
            CreateParticleSystem(0);
            CreateParticleSystem(1);
            CreateParticleSystem(2);
        }

        void CreateParticleSystem(int i_nat)
        {
            GameObject ptSystemGo = new GameObject();

            if (i_nat == 0)
            {
                ptSystemGo.name = "SelectionMarkPlayer";
            }

            if (i_nat == 1)
            {
                ptSystemGo.name = "SelectionMarkOther";
            }

            if (i_nat == 2)
            {
                ptSystemGo.name = "SelectionMarkEnemy";
            }

            ParticleSystem ptSystem = ptSystemGo.AddComponent<ParticleSystem>();
            ptSystems.Add(ptSystem);

            ParticleSystem.EmissionModule em = ptSystem.emission;
            em.enabled = false;

            ParticleSystemRenderer psr = ptSystem.GetComponent<ParticleSystemRenderer>();
            psr.sortMode = ParticleSystemSortMode.Distance;
            psr.minParticleSize = minParticleSize;
            psr.maxParticleSize = 10f;

            ParticleSystem.MainModule pmain = ptSystem.main;
            pmain.maxParticles = 100000;

            ptSystem.GetComponent<Renderer>().material = new Material(selectionMarkShader);

            if (selectionMarkMode == SelectionMarkMode.CameraFacing)
            {
                if (i_nat == 0)
                {
                    ptSystem.GetComponent<Renderer>().material.mainTexture = selectionMarkTexturePlayer;
                }

                if (i_nat == 1)
                {
                    ptSystem.GetComponent<Renderer>().material.mainTexture = selectionMarkTextureOther;
                }

                if (i_nat == 2)
                {
                    ptSystem.GetComponent<Renderer>().material.mainTexture = selectionMarkTextureEnemy;
                }
            }

            if (selectionMarkMode == SelectionMarkMode.Horizontal)
            {
                if (i_nat == 0)
                {
                    ptSystem.GetComponent<Renderer>().material.mainTexture = selectionMarkHorizontalTexturePlayer;
                }

                if (i_nat == 1)
                {
                    ptSystem.GetComponent<Renderer>().material.mainTexture = selectionMarkHorizontalTextureOther;
                }

                if (i_nat == 2)
                {
                    ptSystem.GetComponent<Renderer>().material.mainTexture = selectionMarkHorizontalTextureEnemy;
                }
            }
        }

        void Update()
        {
            SelectionManager sm = SelectionManager.active;

            if (useSelectionMarks && (sm.selectedGoPars.Count > 0))
            {
                for (int i = 0; i < 3; i++)
                {
                    if (ptSystems[i].gameObject.activeSelf == false)
                    {
                        ptSystems[i].gameObject.SetActive(true);
                    }
                }

                int n0 = 0;
                int n1 = 0;
                int n2 = 0;

                for (int i = 0; i < sm.selectedGoPars.Count; i++)
                {
                    UnitPars up = sm.selectedGoPars[i];
                    int markType = MarkType(up);

                    if (markType == 0)
                    {
                        n0++;
                    }

                    if (markType == 1)
                    {
                        n1++;
                    }

                    if (markType == 2)
                    {
                        n2++;
                    }
                }

                ParticleSystem.Particle[] pool0 = new ParticleSystem.Particle[n0];
                ParticleSystem.Particle[] pool1 = new ParticleSystem.Particle[n1];
                ParticleSystem.Particle[] pool2 = new ParticleSystem.Particle[n2];

                camPos = Camera.main.transform.position;
                camRot = Camera.main.transform.rotation;

                n0 = 0;
                n1 = 0;
                n2 = 0;

                for (int i = 0; i < sm.selectedGoPars.Count; i++)
                {
                    UnitPars up = sm.selectedGoPars[i];
                    int markType = MarkType(up);

                    if (markType == 0)
                    {
                        SetParticle(ref pool0, n0, up);
                        n0++;
                    }

                    if (markType == 1)
                    {
                        SetParticle(ref pool1, n1, up);
                        n1++;
                    }

                    if (markType == 2)
                    {
                        SetParticle(ref pool2, n2, up);
                        n2++;
                    }
                }

                ptSystems[0].SetParticles(pool0, n0);
                ptSystems[1].SetParticles(pool1, n1);
                ptSystems[2].SetParticles(pool2, n2);
            }
            else
            {
                for (int i = 0; i < 3; i++)
                {
                    if (ptSystems[i].gameObject.activeSelf)
                    {
                        ptSystems[i].SetParticles(null, 0);
                        ptSystems[i].gameObject.SetActive(false);
                    }
                }
            }
        }

        void SetParticle(ref ParticleSystem.Particle[] pool, int i, UnitPars up)
        {
            if (selectionMarkMode == SelectionMarkMode.CameraFacing)
            {
                pool[i].position = 0.5f * (up.transform.position + up.unitParsType.unitCenter + camPos);
                pool[i].startSize = up.unitParsType.unitSize;
                pool[i].rotation = 0f;
            }

            if (selectionMarkMode == SelectionMarkMode.Horizontal)
            {
                pool[i].position = up.transform.position + offset;
                pool[i].startSize = 2f * up.unitParsType.unitSize;
                Vector3 rot1 = (Quaternion.Inverse(camRot) * Quaternion.LookRotation(TerrainProperties.TerrainNormalVectorProc(up.transform.position), Vector3.up)).eulerAngles;
                pool[i].rotation3D = rot1;
            }

            pool[i].angularVelocity = 0f;
            pool[i].velocity = new Vector3(0f, 0f, 0f);
            pool[i].startLifetime = 10f;
            pool[i].remainingLifetime = 9f;
            pool[i].startColor = colorGradient.Evaluate(up.health / up.maxHealth);
        }

        int MarkType(UnitPars up)
        {
            if (up.nation != Diplomacy.active.playerNation)
            {
                if ((up.nation >= 0) && (up.nation < Diplomacy.active.relations[Diplomacy.active.playerNation].Count))
                {
                    if (Diplomacy.active.relations[Diplomacy.active.playerNation][up.nation] != 1)
                    {
                        return 1;
                    }
                    else
                    {
                        return 2;
                    }
                }
            }

            return 0;
        }
    }
}

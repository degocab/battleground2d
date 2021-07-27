using UnityEngine;
using UnityEngine.UI;

namespace RTSToolkit
{
    public class GraphicsSettings : MonoBehaviour
    {
        public static GraphicsSettings active;

        public Toggle materialInstancing;
        public Toggle meshInstancing;

        public InputField shadowCastDistance;
        public InputField shadowReceiveDistance;
        public InputField cameraFarClippingPlane;

        public Dropdown waterDetails;
        public Dropdown qualityPreset;
        public Dropdown terrainResolution;

        public Toggle fireLights;
        public Toggle fireArrowLights;
        public Toggle buildingLights;

        void Awake()
        {
            active = this;
        }

        void Start()
        {
            ReceiveShadowCastDistance();
            ReceiveShadowReceiveDistance();
            ReceiveQualityPreset();
            ReceiveCameraFarClippingPlane();
            SwitchFireLights();
        }

        public void ChangeWaterShader()
        {
            GenerateTerrain gt = GenerateTerrain.active;

            if (gt != null)
            {
                GameObject waterGo = gt.water;

                if (waterGo != null)
                {
                    UnityStandardAssets.Water.Water waterComp = waterGo.GetComponent<UnityStandardAssets.Water.Water>();

                    if (waterComp != null)
                    {
                        if (waterDetails.value == 0)
                        {
                            waterComp.waterMode = UnityStandardAssets.Water.Water.WaterMode.Simple;
                        }

                        if (waterDetails.value == 1)
                        {
                            waterComp.waterMode = UnityStandardAssets.Water.Water.WaterMode.Reflective;
                        }

                        if (waterDetails.value == 2)
                        {
                            waterComp.waterMode = UnityStandardAssets.Water.Water.WaterMode.Refractive;
                        }
                    }
                }
            }
        }

        public void ChangeTerrainResolution()
        {
            GenerateTerrain gt = GenerateTerrain.active;

            if (gt != null)
            {
                int tr_value = terrainResolution.value;

                if (tr_value == 0)
                {
                    gt.SwithResolutionRuntime(2000, 256);
                }

                if (tr_value == 1)
                {
                    gt.SwithResolutionRuntime(4000, 256);
                }

                if (tr_value == 2)
                {
                    gt.SwithResolutionRuntime(4000, 512);
                }

                if (tr_value == 3)
                {
                    gt.SwithResolutionRuntime(1000, 128);
                }

                if (tr_value == 4)
                {
                    gt.SwithResolutionRuntime(1000, 256);
                }

                if (tr_value == 5)
                {
                    gt.SwithResolutionRuntime(500, 64);
                }

                if (tr_value == 6)
                {
                    gt.SwithResolutionRuntime(500, 128);
                }

                if (tr_value == 7)
                {
                    gt.SwithResolutionRuntime(500, 256);
                }

                if (tr_value == 8)
                {
                    gt.SwithResolutionRuntime(250, 32);
                }

                if (tr_value == 9)
                {
                    gt.SwithResolutionRuntime(250, 64);
                }

                if (tr_value == 10)
                {
                    gt.SwithResolutionRuntime(250, 128);
                }

                if (tr_value == 11)
                {
                    gt.SwithResolutionRuntime(250, 256);
                }
            }
        }

        public void SwitchUnitsIntsancedShader()
        {
            RenderMeshModels rmm = RenderMeshModels.active;

            for (int i1 = 0; i1 < rmm.renderModels.Count; i1++)
            {
                RenderMeshLODs l1 = rmm.renderModels[i1];

                for (int i2 = 0; i2 < l1.renderAnimations.Count; i2++)
                {
                    RenderMeshAnimations l2 = l1.renderAnimations[i2];

                    if (l1.renderAnimationsWrapper[i2].lodMode == 0)
                    {
                        for (int i3 = 0; i3 < l2.renderMeshAnimations.Count; i3++)
                        {
                            RenderMesh l3 = l2.renderMeshAnimations[i3];

                            for (int i4 = 0; i4 < l3.mats.Length; i4++)
                            {
                                if (materialInstancing.isOn)
                                {
                                    l3.mats[i4].shader = Shader.Find("Instanced/InstancedShader");
                                    meshInstancing.gameObject.transform.parent.gameObject.SetActive(true);
                                }
                                else
                                {
                                    meshInstancing.isOn = false;
                                    rmm.useMeshInstancing = false;
                                    meshInstancing.gameObject.transform.parent.gameObject.SetActive(false);

                                    shadowCastDistance.gameObject.transform.parent.gameObject.SetActive(true);
                                    shadowReceiveDistance.gameObject.transform.parent.gameObject.SetActive(true);

                                    l3.mats[i4].shader = Shader.Find("Standard");
                                }
                            }
                        }
                    }
                }
            }
        }

        public void SwitchMeshInstancing()
        {
            RenderMeshModels rmm = RenderMeshModels.active;

            if (meshInstancing.isOn)
            {
                rmm.useMeshInstancing = true;
                shadowCastDistance.gameObject.transform.parent.gameObject.SetActive(false);
                shadowReceiveDistance.gameObject.transform.parent.gameObject.SetActive(false);
            }
            else
            {
                rmm.useMeshInstancing = false;
                shadowCastDistance.gameObject.transform.parent.gameObject.SetActive(true);
                shadowReceiveDistance.gameObject.transform.parent.gameObject.SetActive(true);
            }
        }

        public void SubmitShadowCastDistance()
        {
            string str = shadowCastDistance.text;
            float f;

            if (float.TryParse(str, out f))
            {
                RenderMeshModels.active.shadowCastDistance = f;
            }
        }

        public void SubmitShadowReceiveDistance()
        {
            string str = shadowReceiveDistance.text;
            float f;

            if (float.TryParse(str, out f))
            {
                RenderMeshModels.active.shadowReceiveDistance = f;
            }
        }

        public void ReceiveShadowCastDistance()
        {
            shadowCastDistance.text = RenderMeshModels.active.shadowCastDistance.ToString();
        }

        public void ReceiveShadowReceiveDistance()
        {
            shadowReceiveDistance.text = RenderMeshModels.active.shadowReceiveDistance.ToString();
        }

        public void SubmitCameraFarClippingPlane()
        {
            string str = cameraFarClippingPlane.text;
            float f;

            if (float.TryParse(str, out f))
            {
                if ((f + 1f) < Camera.main.nearClipPlane)
                {
                    f = Camera.main.nearClipPlane + 1f;
                    cameraFarClippingPlane.text = f.ToString();
                }

                Camera.main.farClipPlane = f;
            }
        }

        public void ReceiveCameraFarClippingPlane()
        {
            cameraFarClippingPlane.text = Camera.main.farClipPlane.ToString();
        }

        public void SubmitQualityPreset()
        {
            QualitySettings.SetQualityLevel(qualityPreset.value, true);
        }

        public void ReceiveQualityPreset()
        {
            qualityPreset.value = QualitySettings.GetQualityLevel();
        }

        public void SwitchFireLights()
        {
            RTSMaster.active.buildingFirePrefab.GetComponent<FireScaler>().lightGo.GetComponent<Light>().enabled = fireLights.isOn;
            FireScaler[] allObjects = Object.FindObjectsOfType<FireScaler>();

            for (int i = 0; i < allObjects.Length; i++)
            {
                FireScaler fs = allObjects[i];
                fs.lightGo.GetComponent<Light>().enabled = fireLights.isOn;
            }
        }

        public void SwitchFireArrowLights()
        {
            for (int i = 0; i < RTSMaster.active.rtsUnitTypePrefabsUpt.Count; i++)
            {
                UnitParsType upt = RTSMaster.active.rtsUnitTypePrefabsUpt[i];

                if (upt != null)
                {
                    GameObject arr = upt.arrow;

                    if (arr != null)
                    {
                        ArrowPars ap = arr.GetComponent<ArrowPars>();

                        if (ap != null)
                        {
                            if (ap.fireLight != null)
                            {
                                ap.fireLight.enabled = fireArrowLights.isOn;
                            }
                        }
                    }
                }
            }

            ArrowPars[] allObjects = Object.FindObjectsOfType<ArrowPars>();

            for (int i = 0; i < allObjects.Length; i++)
            {
                ArrowPars ap = allObjects[i];

                if (ap.fireLight != null)
                {
                    ap.fireLight.enabled = fireArrowLights.isOn;
                }
            }
        }

        public void SwitchBuildingLights()
        {
            TimeOfDay.active.showNightLights = buildingLights.isOn;

            for (int i = 0; i < TimeOfDay.active.nightPointLights.Count; i++)
            {
                TimeOfDay.NighPointLight npl = TimeOfDay.active.nightPointLights[i];

                if (npl.isLightOn)
                {
                    if (npl.light != null)
                    {
                        npl.light.enabled = buildingLights.isOn;
                    }
                }
            }
        }
    }
}

using UnityEngine;
using System.Collections.Generic;

namespace RTSToolkit
{
    public class TimeOfDay : MonoBehaviour
    {
        public static TimeOfDay active;

        Light lgth;
        Light moonLight;
        float secondsInDay;
        public float speed = 60f;

        public Vector3 northPoleVector = new Vector3(1f, 1f, 0f);

        float curAngle = 90f;
        float sunHeight;

        [HideInInspector] public List<Color> sunPalette = new List<Color>();
        public Gradient sunPalette1;

        public float upSunAngleColor = 20f;
        public float lowSunAngleColor = -20f;

        public float buildingLightSunAngleUp = 5f;
        public float buildingLightSunAngleLow = 0f;

        public List<NighPointLight> nightPointLights = new List<NighPointLight>();

        public bool showNightLights = true;
        Color ambientColor;

        [HideInInspector] public Color currentSunColor;
        bool useTOD_SYSTEM_FREE_SKY = false;

        Material skyMaterial;

        [HideInInspector] public Texture moonTexture;
        Transform moonTransform;
        Transform moonLightTransform;

        [HideInInspector] public Texture starsTexture;
        [HideInInspector] public Texture starsNoiseTexture;

        public Color nightColor;
        public float nightColorMultiplier = 1f;
        public float ambientNightColorMultiplier = 30f;

        [HideInInspector] public float currentDayTimeHrs = 0f;

        void Awake()
        {
            active = this;
        }

        void Start()
        {
            secondsInDay = 24f * 60f * 60f;
            lgth = GetComponent<Light>();
            ambientColor = RenderSettings.ambientLight;

            currentDayTimeHrs = (transform.rotation.eulerAngles.y) / 15f;

            if (currentDayTimeHrs >= 24f)
            {
                currentDayTimeHrs = currentDayTimeHrs - 24f;
            }

            skyMaterial = RenderSettings.skybox;

            if (skyMaterial.name == "TOD_SYSTEM_FREE_SKY")
            {
                useTOD_SYSTEM_FREE_SKY = true;
                GameObject mg = new GameObject();
                mg.name = "Moon";

                GameObject mlg = new GameObject();
                moonLightTransform = mlg.transform;
                mlg.name = "MoonLight";
                moonLight = mlg.AddComponent<Light>();
                moonLight.intensity = 0.5f;
                moonLight.color = nightColor;
                moonLight.type = LightType.Directional;
                moonLight.shadows = LightShadows.Soft;
                moonTransform = mg.transform;
            }
        }

        float timePassed = 0f;
        float tUpdateBuildingSmoke = 0f;
        float tUpdateNightPointLights = 0f;

        void Update()
        {
            float dt = Time.deltaTime;

            timePassed = timePassed + dt;
            if (timePassed > 0.01f)
            {
                timePassed = 0f;
                UpdateProceed();
            }

            tUpdateBuildingSmoke = tUpdateBuildingSmoke + dt;
            if (tUpdateBuildingSmoke > 0.3f)
            {
                tUpdateBuildingSmoke = 0f;
                UpdateBuildingSmoke();
            }

            tUpdateNightPointLights = tUpdateNightPointLights + dt;
            if (tUpdateNightPointLights > 1f)
            {
                tUpdateNightPointLights = 0f;
                UpdateNightPointLights();
            }
        }

        void UpdateProceed()
        {
            Vector3 curDir = transform.rotation * new Vector3(0f, 0f, 1f);
            float da = 360f * Time.deltaTime / secondsInDay;
            curDir = GenericMath.RotAround(-speed * da, curDir, northPoleVector);
            transform.rotation = Quaternion.LookRotation(curDir);

            curAngle = curAngle + speed * da;
            if (curAngle > 360f)
            {
                curAngle = curAngle - 360f;
            }

            currentDayTimeHrs = (transform.rotation.eulerAngles.y) / 15f + 6f;
            if (currentDayTimeHrs >= 24f)
            {
                currentDayTimeHrs = currentDayTimeHrs - 24f;
            }

            UnityStandardAssets.ImageEffects.ColorCorrectionCurves ccc = null;
            if (RTSCamera.active != null)
            {
                ccc = RTSCamera.active.gameObject.GetComponent<UnityStandardAssets.ImageEffects.ColorCorrectionCurves>();
            }

            float sgn = -curDir.y / Mathf.Abs(curDir.y);
            float angl = sgn * GenericMath.Angle3d(curDir, new Vector3(curDir.x, 0f, curDir.z));

            sunHeight = angl;

            float colval = GenericMath.Interpolate(angl, lowSunAngleColor, upSunAngleColor, 0f, 1f);
            if (colval > 0.99999f)
            {
                colval = 0.99999f;
            }
            else if (colval < 0.0001f)
            {
                colval = 0.0001f;
            }

            float ambColVal = GenericMath.Interpolate(angl, -30f, 0f, 0f, 1f);
            if (ambColVal > 0.99999f)
            {
                ambColVal = 0.99999f;
            }
            else if (ambColVal < 0.0001f)
            {
                ambColVal = 0.0001f;
            }

            if (ccc != null)
            {
                ccc.saturation = ambColVal;
            }

            Color colr = new Color(0.7f * colval, 0.7f * colval, 0.7f * colval, 1f);
            Color sunColor = sunPalette1.Evaluate(1f - (angl + 90f) / 180f);

            lgth.intensity = 1.0f * colval;

            currentSunColor = colval * sunColor;

            if (CloudsRenderingOrder.active != null)
            {
                CloudsRenderingOrder.active.ChangeCloudsColor(currentSunColor);
            }

            Color nightColor1 = new Color(nightColorMultiplier * nightColor.r, nightColorMultiplier * nightColor.g, nightColorMultiplier * nightColor.b, 1f) * (1f - colval);

            Color ambientLightColourCurrent = ambColVal * (new Color(sunColor.r * ambientColor.r, sunColor.g * ambientColor.g, sunColor.b * ambientColor.b, 1f));

            float r = Mathf.Max(ambientLightColourCurrent.r, ambientNightColorMultiplier * nightColor1.r);
            float g = Mathf.Max(ambientLightColourCurrent.g, ambientNightColorMultiplier * nightColor1.g);
            float b = Mathf.Max(ambientLightColourCurrent.b, ambientNightColorMultiplier * nightColor1.b);
            RenderSettings.ambientLight = new Color(r, g, b, 1);

            Color fogColorCurrent = colr * currentSunColor;

            r = Mathf.Max(fogColorCurrent.r, 0.05f);
            g = Mathf.Max(fogColorCurrent.g, 0.05f);
            b = Mathf.Max(fogColorCurrent.b, 0.05f);
            RenderSettings.fogColor = new Color(r, g, b, 1);

            lgth.color = sunColor;

            if (useTOD_SYSTEM_FREE_SKY)
            {
                Color skyTint = Color.black;
                float atmosphereThickness = 1.6f;

                skyMaterial.SetColor("_SkyTint", skyTint);
                skyMaterial.SetFloat("_AtmosphereThickness", atmosphereThickness);
                skyMaterial.EnableKeyword("ATMOSPHERICNIGHTCOLOR");
                skyMaterial.DisableKeyword("SIMPLENIGHTCOLOR");
                skyMaterial.SetColor("_NightColor", nightColor1);
                skyMaterial.EnableKeyword("HORIZONFADE");
                skyMaterial.SetFloat("_HorizonFade", 0.3f);

                skyMaterial.EnableKeyword("MIEPHASE");
                skyMaterial.SetFloat("_SunSize", 0.03f);
                skyMaterial.SetColor("_SunColor", sunColor);
                skyMaterial.SetVector("_SunDir", -transform.forward);

                moonTransform.position = transform.position;
                moonTransform.rotation = transform.rotation;
                moonTransform.localScale = new Vector3(-1, 1, 1);

                skyMaterial.SetMatrix("_MoonMatrix", moonTransform.worldToLocalMatrix);
                skyMaterial.SetTexture("_MoonTexture", moonTexture);
                skyMaterial.SetFloat("_MoonSize", 0.2f);
                skyMaterial.SetColor("_MoonColor", new Color(0.3f, 0.3f, 0.3f, 1f));
                skyMaterial.SetFloat("_MoonIntensity", 1f);

                skyMaterial.EnableKeyword("MOONHALO");
                skyMaterial.EnableKeyword("MOON");
                skyMaterial.SetVector("_MoonDir", transform.forward);
                skyMaterial.SetColor("_MoonHaloColor", new Color(0.004f, 0.004f, 0.01f, 1f));
                skyMaterial.SetFloat("_MoonHaloSize", 0.2f);
                skyMaterial.SetFloat("_MoonHaloIntensity", 1f);

                skyMaterial.EnableKeyword("STARS");
                skyMaterial.DisableKeyword("STARSTWINKLE");

                Color starsColor = Color.white - ambColVal * sunColor;
                float starsIntensity = 1f;

                float polex = Mathf.Atan(northPoleVector.z / northPoleVector.y) * 180f / Mathf.PI;
                float polez = Mathf.Atan(northPoleVector.x / northPoleVector.y) * 180f / Mathf.PI;

                skyMaterial.SetMatrix("_StarsMatrix", Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(new Vector3(-polex, -curAngle, polez)), Vector3.one));

                skyMaterial.SetTexture("_StarsCubemap", starsTexture);
                skyMaterial.SetColor("_StarsColor", starsColor);
                skyMaterial.SetFloat("_StarsIntensity", starsIntensity);

                Matrix4x4 starsNoiseMatrix = Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(Time.time, 0, 0), Vector3.one);
                skyMaterial.EnableKeyword("STARSTWINKLE");
                skyMaterial.SetTexture("_StarsNoiseCubemap", starsNoiseTexture);
                skyMaterial.SetMatrix("_StarsNoiseMatrix", starsNoiseMatrix);
                skyMaterial.SetFloat("_StarsTwinkle", 0.9f);

                if (sunHeight < 0)
                {
                    moonLightTransform.rotation = moonTransform.rotation * Quaternion.Euler(180f, 0f, 0f);

                    float mcolval = GenericMath.Interpolate(angl, lowSunAngleColor - 20, upSunAngleColor - 20, 0f, 1f);
                    if (mcolval > 0.99999f)
                    {
                        mcolval = 0.99999f;
                    }
                    else if (mcolval < 0.0001f)
                    {
                        mcolval = 0.0001f;
                    }

                    moonLight.intensity = (1.0f - mcolval);
                    moonLight.color = GenericMath.NormalizeColor(nightColor);
                }
            }
        }

        void UpdateBuildingSmoke()
        {
            for (int i = 0; i < RTSMaster.active.allUnits.Count; i++)
            {
                UnitPars up = RTSMaster.active.allUnits[i];

                if (up.smokes != null && up.smokes.Count > 0)
                {
                    up.ChangeSmokeColor(currentSunColor);
                }
            }
        }

        void UpdateNightPointLights()
        {
            for (int i = 0; i < nightPointLights.Count; i++)
            {
                NighPointLight npl = nightPointLights[i];

                if (sunHeight > npl.sunHeight)
                {
                    if (npl.isLightOn == true)
                    {
                        if (showNightLights)
                        {
                            if (npl.light != null)
                            {
                                npl.light.enabled = false;
                            }
                        }

                        npl.isLightOn = false;
                    }
                }

                if (sunHeight <= npl.sunHeight)
                {
                    if (npl.isLightOn == false)
                    {
                        if (showNightLights)
                        {
                            if (npl.light != null)
                            {
                                npl.light.enabled = true;
                            }
                        }

                        npl.isLightOn = true;
                    }
                }
            }
        }

        public void AddNightPointLight(Light light1)
        {
            RemoveNightPointLight(light1);

            NighPointLight npl = new NighPointLight();
            npl.light = light1;
            npl.isLightOn = light1.enabled;
            npl.sunHeight = Random.Range(buildingLightSunAngleLow, buildingLightSunAngleUp);
            nightPointLights.Add(npl);
        }

        public void RemoveNightPointLight(Light light1)
        {
            for (int i = 0; i < nightPointLights.Count; i++)
            {
                if (light1 == nightPointLights[i].light)
                {
                    nightPointLights.RemoveAt(i);
                    return;
                }
            }
        }

        public static string DaysHoursMinutes(float sec)
        {
            System.TimeSpan timeSpan = System.TimeSpan.FromSeconds(TimeOfDay.active.speed * sec);

            string days = timeSpan.Days.ToString() + "d ";
            if (timeSpan.Days == 0)
            {
                days = "";
            }

            string hours = timeSpan.Hours.ToString() + "h ";
            if (timeSpan.Hours == 0)
            {
                hours = "";
            }

            string minutes = timeSpan.Minutes.ToString() + "m ";

            return days + hours + minutes;
        }

        public class NighPointLight
        {
            public Light light;
            public float sunHeight = 0f;
            public bool isLightOn = false;
        }
    }
}

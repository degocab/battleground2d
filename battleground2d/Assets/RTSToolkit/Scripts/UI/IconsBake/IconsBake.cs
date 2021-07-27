using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace RTSToolkit
{
    public class IconsBake : MonoBehaviour
    {
        public List<IconsBakeItem> iconsToBake;

        void Start()
        {
            StartCoroutine(StartSpawning(0, true));
        }

        void Update()
        {

        }

        IEnumerator StartSpawning(int i, bool initialWait)
        {
            if (initialWait)
            {
                yield return new WaitForSeconds(3f);
            }

            if (i < iconsToBake.Count)
            {
                Spawn(i);
            }
            else
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
            }
        }

        void Spawn(int i)
        {
            IconsBakeItem item = iconsToBake[i];
            Vector3 pos = TerrainProperties.TerrainVectorProc(transform.position);
            Quaternion rot = Quaternion.Euler(0f, item.rotationY, 0f) * RTSMaster.active.rtsUnitTypePrefabs[item.rtsUnitId].transform.rotation;

            List<UnitPars> ups = new List<UnitPars>();

            for (int j = 0; j < item.objectOffsets.Count; j++)
            {
                GameObject go = Instantiate(RTSMaster.active.rtsUnitTypePrefabs[item.rtsUnitId], pos + item.objectOffsets[j], rot);
                string natName = RTSMaster.active.GetNationNameById(Diplomacy.active.playerNation);
                UnitPars up = go.GetComponent<UnitPars>();
                up.Spawn(natName);
                StartCoroutine(PlayAnimation(go, item));
                ups.Add(up);
            }

            float rEnclosed = RTSMaster.active.rtsUnitTypePrefabsUpt[item.rtsUnitId].rEnclosed;

            RTSCamera.active.transform.position = new Vector3(
                pos.x,
                pos.y + item.cameraDistance * Mathf.Sin(Mathf.Deg2Rad * 45f),
                pos.z - item.cameraDistance * Mathf.Cos(Mathf.Deg2Rad * 45f)
            ) + item.offset;

            RTSCamera.active.transform.LookAt(pos + item.offset);

            StartCoroutine(TakeImage(i, ups));
        }

        IEnumerator PlayAnimation(GameObject go, IconsBakeItem item)
        {
            if (!string.IsNullOrEmpty(item.animationName))
            {
                UnitAnimation ua = go.GetComponent<UnitAnimation>();

                if (ua != null)
                {
                    yield return new WaitForEndOfFrame();
                    ua.PlayAnimation(item.animationName);
                }
            }

            yield return null;
        }

        IEnumerator TakeImage(int i1, List<UnitPars> ups)
        {
            IconsBakeItem item = iconsToBake[i1];

            if (item.delay > 0)
            {
                yield return new WaitForSeconds(item.delay);
            }

            string fname = item.name;

            if (fname == string.Empty)
            {
                fname = "DemoIcon";
            }

            yield return new WaitForEndOfFrame();

            int res = 128;

            int width = Screen.width;
            int height = Screen.height;

            int centralW = (int)(width / 2.0f);
            int centralH = (int)(height / 2.0f);

            int xBeg = centralW - res / 2;
            int yBeg = centralH - res / 2;

            int xEnd = centralW + res / 2;
            int yEnd = centralH + res / 2;

            Texture2D texture = new Texture2D(res, res, TextureFormat.ARGB32, false);
            texture.ReadPixels(new Rect(xBeg, yBeg, xEnd, yEnd), 0, 0);
            texture.Apply();

            Color[] pix = texture.GetPixels();
            pix = NormalizeAlpha(pix, texture);

            if (item.grayscale)
            {
                pix = ApplyGrayscale(pix);
            }

            pix = CutElipseInMidle(pix, 0.4f * res, texture);

            texture.SetPixels(pix);
            texture.Apply();

            byte[] bytes = texture.EncodeToPNG();

            string path = System.IO.Path.Combine(Application.dataPath + "/UnitIcons/", fname);
            System.IO.FileInfo file = new System.IO.FileInfo(path);
            file.Directory.Create();

            File.WriteAllBytes(path + ".png", bytes);

#if UNITY_EDITOR
            AssetDatabase.Refresh();

            TextureImporter ti = (TextureImporter)TextureImporter.GetAtPath("Assets/UnitIcons/" + fname + ".png");
            ti.textureType = TextureImporterType.Sprite;
            ti.SaveAndReimport();
#endif

            yield return new WaitForEndOfFrame();

            for (int i = 0; i < ups.Count; i++)
            {
                RTSMaster.active.DestroyUnit(ups[i]);
            }

            StartCoroutine(StartSpawning(i1 + 1, false));
        }

        Color[] NormalizeAlpha(Color[] pix, Texture2D tx)
        {
            Color[] pix2 = pix;

            int w = tx.width;
            int h = tx.height;

            for (int i = 0; i < pix.Length; i++)
            {
                pix2[i] = new Color(pix[i].r, pix[i].g, pix[i].b, 1f);
            }

            return pix2;
        }

        Color[] CutElipseInMidle(Color[] pix, float r, Texture2D tx)
        {
            Color[] pix2 = pix;

            int w = tx.width;
            int h = tx.height;

            for (int i = 0; i < w; i++)
            {
                for (int j = 0; j < h; j++)
                {
                    int k = w * j + i;

                    float rf = ElipseDistance(1f * i, 1f * j, 0.5f * w, 0.5f * h, 0.5f * w, 0.5f * h);

                    float rmin = 0.8f;
                    float rmax = 1f;

                    float grad = LinearInterpolation(rf, rmin, 1f, rmax, 0f);

                    if (rf > rmax)
                    {
                        pix2[k] = Color.clear;
                    }

                    if ((rf > rmin) && (rf <= rmax))
                    {
                        pix2[k] = new Color(pix[k].r, pix[k].g, pix[k].b, grad * pix[k].a);
                    }
                }
            }

            return pix2;
        }

        Color[] ApplyGrayscale(Color[] pix)
        {
            int length = pix.Length;

            for (int i = 0; i < length; i++)
            {
                float grayscale = pix[i].grayscale;
                pix[i] = new Color(grayscale, grayscale, grayscale, pix[i].a);
            }

            return pix;
        }

        float ElipseDistance(float x, float y, float x0, float y0, float a, float b)
        {
            return (((x - x0) / a) * ((x - x0) / a) + ((y - y0) / b) * ((y - y0) / (b)));
        }

        float LinearInterpolation(float x, float x0, float y0, float x1, float y1)
        {
            return (y0 + (y1 - y0) * ((x - x0) / (x1 - x0)));
        }

        public void LoadDefault()
        {
            if(iconsToBake == null)
            {
                iconsToBake = new List<IconsBakeItem>();
            }

            iconsToBake.Clear();

            iconsToBake.Add(new IconsBakeItem("CentralBuildingIco", 0, 31f, 405f, new Vector3(0f, 2f, 0f), null, -135f, false, ""));
            iconsToBake.Add(new IconsBakeItem("BarracksIco", 1, 21f, 355f, new Vector3(0f, 1.5f, 0f), null, -135f, false, ""));
            iconsToBake.Add(new IconsBakeItem("WoodCutterIco", 2, 21f, 150f, new Vector3(0f, 1f, 0f), null, -135f, false, ""));
            iconsToBake.Add(new IconsBakeItem("HouseIco", 3, 11f, 250f, new Vector3(0f, 1.5f, 0f), null, -135f, false, ""));
            iconsToBake.Add(new IconsBakeItem("ResearchCenterIco", 4, 16f, 385f, new Vector3(0f, 1.5f, 0f), null, -135f, false, ""));
            iconsToBake.Add(new IconsBakeItem("MiningPointIco", 5, 21f, 150f, new Vector3(0f, 1.5f, 0f), null, -135f, false, ""));
            iconsToBake.Add(new IconsBakeItem("FactoryIco", 6, 21f, 450f, new Vector3(0f, 0f, 0f), null, -45f, false, ""));
            iconsToBake.Add(new IconsBakeItem("StableIco", 7, 26f, 380f, new Vector3(0f, 1f, 0f), null, -135f, false, ""));
            iconsToBake.Add(new IconsBakeItem("WindmillIco", 8, 26f, 300f, new Vector3(2f, 5f, 0f), null, 135f, false, ""));
            iconsToBake.Add(new IconsBakeItem("TowerIco", 10, 21f, 280f, new Vector3(0f, 10f, 0f), null, -135f, false, ""));

            iconsToBake.Add(new IconsBakeItem("FenceIco", 9, 11f, 100f, new Vector3(0f, 1f, 0f), new List<Vector3>()
            {
                new Vector3(-4,0,-4),
                new Vector3(-3,0,-3),
                new Vector3(-2,0,-2),
                new Vector3(-1,0,-1),
                new Vector3(0,0,0),
                new Vector3(1,0,1),
                new Vector3(2,0,2),
                new Vector3(3,0,3),
                new Vector3(4,0,4)
            }, 45f, false, ""));



            iconsToBake.Add(new IconsBakeItem("CentralBuildingGrayIco", 0, 31f, 405f, new Vector3(0f, 2f, 0f), null, -135f, true, ""));
            iconsToBake.Add(new IconsBakeItem("BarracksGrayIco", 1, 21f, 355f, new Vector3(0f, 1.5f, 0f), null, -135f, true, ""));
            iconsToBake.Add(new IconsBakeItem("WoodCutterGrayIco", 2, 21f, 150f, new Vector3(0f, 1f, 0f), null, -135f, true, ""));
            iconsToBake.Add(new IconsBakeItem("HouseGrayIco", 3, 11f, 250f, new Vector3(0f, 1.5f, 0f), null, -135f, true, ""));
            iconsToBake.Add(new IconsBakeItem("ResearchCenterGrayIco", 4, 16f, 385f, new Vector3(0f, 1.5f, 0f), null, -135f, true, ""));
            iconsToBake.Add(new IconsBakeItem("MiningPointGrayIco", 5, 21f, 150f, new Vector3(0f, 1.5f, 0f), null, -135f, true, ""));
            iconsToBake.Add(new IconsBakeItem("FactoryGrayIco", 6, 21f, 450f, new Vector3(0f, 0f, 0f), null, -45f, true, ""));
            iconsToBake.Add(new IconsBakeItem("StableGrayIco", 7, 26f, 380f, new Vector3(0f, 1f, 0f), null, -135f, true, ""));
            iconsToBake.Add(new IconsBakeItem("WindmillGrayIco", 8, 26f, 300f, new Vector3(2f, 5f, 0f), null, 135f, true, ""));
            iconsToBake.Add(new IconsBakeItem("TowerGrayIco", 10, 21f, 280f, new Vector3(0f, 10f, 0f), null, -135f, true, ""));

            iconsToBake.Add(new IconsBakeItem("FenceGrayIco", 9, 11f, 100f, new Vector3(0f, 1f, 0f), new List<Vector3>()
            {
                new Vector3(-4,0,-4),
                new Vector3(-3,0,-3),
                new Vector3(-2,0,-2),
                new Vector3(-1,0,-1),
                new Vector3(0,0,0),
                new Vector3(1,0,1),
                new Vector3(2,0,2),
                new Vector3(3,0,3),
                new Vector3(4,0,4)
            }, 45f, true, ""));
        }

        [System.Serializable]
        public class IconsBakeItem
        {
            public string name;
            public int rtsUnitId;
            public float delay;
            public float cameraDistance;
            public Vector3 offset;
            public List<Vector3> objectOffsets;
            public float rotationY;
            public bool grayscale;
            public string animationName;

            public IconsBakeItem() { }

            public IconsBakeItem(
                string _name,
                int _rtsUnitId,
                float _delay,
                float _cameraDistance,
                Vector3 _offset,
                List<Vector3> _objectOffsets,
                float _rotationY,
                bool _grayscale,
                string _animationName
            )
            {
                name = _name;
                rtsUnitId = _rtsUnitId;
                delay = _delay;
                cameraDistance = _cameraDistance;
                offset = _offset;
                objectOffsets = _objectOffsets;

                if (objectOffsets == null || objectOffsets.Count == 0)
                {
                    objectOffsets = new List<Vector3>();
                    objectOffsets.Add(Vector3.zero);
                }

                rotationY = _rotationY;
                grayscale = _grayscale;
                animationName = _animationName;
            }
        }
    }
}

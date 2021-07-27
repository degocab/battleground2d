using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace RTSToolkit
{
    public class PSpriteLoader : MonoBehaviour
    {
        public static PSpriteLoader active;
        public List<List<List<ParticleSystemNode>>> psnLevels;
        [HideInInspector] public Texture2D emptyTex;
        public GameObject prefab;

        [HideInInspector] public List<string> modelName;
        [HideInInspector] public List<string> animationName;
        int nAnim = 0;

        [HideInInspector] public List<int> animIndOffset;
        [HideInInspector] public List<int> numberOfColumns;
        [HideInInspector] public List<int> numFrames;

        [HideInInspector] public List<float> duration;
        [HideInInspector] public List<int> isAssetBundle;

        [HideInInspector] public List<int> horRotLevels;
        [HideInInspector] public List<int> vertRotLevels;

        [HideInInspector] public List<int> repeatableAnimations;

        Vector3 cameraPosition;
        Vector3 cameraForward;

        Quaternion cameraRotation;
        [HideInInspector] public List<UnitAnimation> clientSpritesGo;

        int texturesBufferLoading = 0;
        [HideInInspector] public List<Texture2D> spriteTextures = new List<Texture2D>();

        List<TextureBundleLoader> textureBundlesToLoad;
        List<TextureBundleLoader> loadingTextureBundles;
        List<TextureBundleLoader> loadedTextureBundles;

        public string directoriesFile = "directories";

        public bool useApplicationDataPathPrefixForBundles = true;
        public string assetBundlesDirectory;
        public string secondaryAssetBundlesUrl = "https://chanfort.github.io/3dSprites3/";

        public Shader particleShader;

        bool useAssetBundles = false;
        public bool unloadAssetBundles = true;

        void Awake()
        {
            active = this;

            Transform camTransform = Camera.main.transform;
            cameraPosition = camTransform.position;
            cameraForward = camTransform.forward;
            cameraRotation = camTransform.rotation;

            textureBundlesToLoad = new List<TextureBundleLoader>();
            loadingTextureBundles = new List<TextureBundleLoader>();
            loadedTextureBundles = new List<TextureBundleLoader>();

            CreateEmptyTexture();
            LoadModelNames();
            LoadRotationalLevelIndices();
            SetAnimIndexOffset();
        }

        void Start()
        {
            LoadTextures();

            if (useAssetBundles)
            {
                StartCoroutine(LoadTextureBundles());
            }

            StartCoroutine(CorReadPositions());
            StartCoroutine(CorUpdateRotIndices());
            StartCoroutine(CorUpdatePositions());
        }

        // Loads model and animation names from directories.txt file
        void LoadModelNames()
        {
            string filePath = "3dSprites/";
            string fileName = directoriesFile;

            TextAsset textRes = Resources.Load<TextAsset>(filePath + fileName);
            StringReader textReader = new StringReader(textRes.text);
            string snumEntries = textReader.ReadLine();

            int numEntries = int.Parse(snumEntries);
            nAnim = numEntries;

            for (int i = 0; i < numEntries; i++)
            {
                string entrie = textReader.ReadLine();
                modelName.Add(entrie);
                entrie = textReader.ReadLine();
                animationName.Add(entrie);
            }
        }

        void LoadRotationalLevelIndices()
        {
            string[] lines = new string[9];

            for (int i = 0; i < nAnim; i++)
            {
                string locAnimationName = animationName[i];
                string filePath = "3dSprites/config/";
                string fileName = modelName[i] + "_" + locAnimationName + "_config";

                TextAsset textRes = Resources.Load<TextAsset>(filePath + fileName);
                StringReader textReader = new StringReader(textRes.text);

                lines[0] = textReader.ReadLine();
                lines[1] = textReader.ReadLine();
                lines[2] = textReader.ReadLine();
                lines[3] = textReader.ReadLine();

                lines[4] = textReader.ReadLine();
                lines[5] = textReader.ReadLine();
                lines[6] = textReader.ReadLine();

                lines[7] = textReader.ReadLine();
                lines[8] = textReader.ReadLine();

                numberOfColumns.Add(int.Parse(lines[0]));
                numFrames.Add(int.Parse(lines[1]));
                horRotLevels.Add(int.Parse(lines[2]));
                vertRotLevels.Add(int.Parse(lines[3]));

                repeatableAnimations.Add(int.Parse(lines[4]));

                duration.Add(float.Parse(lines[7]));
                isAssetBundle.Add(int.Parse(lines[8]));

                if (isAssetBundle[isAssetBundle.Count - 1] == 1)
                {
                    useAssetBundles = true;
                }
            }
        }

        void SetAnimIndexOffset()
        {
            for (int i = 0; i <= nAnim; i++)
            {
                animIndOffset.Add(0);
            }

            animIndOffset[0] = 0;
            for (int i = 1; i <= nAnim; i++)
            {
                animIndOffset[i] = animIndOffset[i - 1] + vertRotLevels[i - 1] * horRotLevels[i - 1];
            }
        }

        void LoadTextures()
        {
            psnLevels = new List<List<List<ParticleSystemNode>>>();

            for (int ia = 0; ia < nAnim; ia++)
            {
                psnLevels.Add(new List<List<ParticleSystemNode>>());
                TextureBundleLoader tbl = null;

                if (useAssetBundles)
                {
                    SetBundleMaterial2(ia, vertRotLevels[ia], horRotLevels[ia]);
                    tbl = textureBundlesToLoad[textureBundlesToLoad.Count - 1];
                }

                spriteManagers = new List<GameObject>();

                for (int j = 0; j < vertRotLevels[ia]; j++)
                {
                    psnLevels[ia].Add(new List<ParticleSystemNode>());
                    int jj = j + 1;

                    for (int i = 0; i < horRotLevels[ia]; i++)
                    {
                        int ii = i + 1;

                        InstMultipleSpriteManagers(ia, jj, ii, tbl);
                    }
                }
            }
        }

        List<GameObject> spriteManagers = new List<GameObject>();

        void InstMultipleSpriteManagers(int ia, int jj, int ii, TextureBundleLoader tbl)
        {
            GameObject go = Instantiate(prefab);
            go.name = "SM_" + modelName[ia] + "_" + animationName[ia] + "_" + jj + "_" + ii;
            go.transform.position = new Vector3(0f, 0f, 0f);

            TextureTile tt = null;

            if (useAssetBundles == false)
            {
                tt = new TextureTile(LoadTexture(ia, jj, ii));
            }
            else
            {
                tt = new TextureTile(tbl.textures[(jj - 1) * tbl.hRotIndex + (ii - 1)]);
                tt.material = tbl.materials[(jj - 1) * tbl.hRotIndex + (ii - 1)];
            }

            int n = 0;
            Vector3[] pos = new Vector3[n];

            for (int i = 0; i < n; i++)
            {
                pos[i].x = Random.Range(-20f, 20f);
                pos[i].y = Random.Range(-10f, 10f);
                pos[i].z = Random.Range(10f, 20f);
            }

            ParticleSystemNode psn1 = new ParticleSystemNode(pos, go, tt);
            psn1.repeatableAnimation = repeatableAnimations[ia];
            psn1.duration = duration[ia];

            psnLevels[ia][jj - 1].Add(psn1);

            spriteManagers.Add(go);
        }

        public Texture2D LoadTexture(int ia, int jj, int ii)
        {
            string tex_filePath = "3dSprites/png/" + modelName[ia] + "/" + animationName[ia] + "/";
            string tex_fileName = modelName[ia] + "_" + animationName[ia] + "_" + jj + "_" + ii;

            return (Resources.Load<Texture2D>(tex_filePath + tex_fileName));
        }

        IEnumerator CorReadPositions()
        {
            while (true)
            {
                yield return new WaitForEndOfFrame();
                ReadPositions();
            }
        }

        IEnumerator CorUpdateRotIndices()
        {
            while (true)
            {
                yield return new WaitForEndOfFrame();
                UpdateRotIndices();
            }
        }

        IEnumerator CorUpdateModelSpriteSwitches()
        {
            while (true)
            {
                yield return new WaitForEndOfFrame();
                UpdateModelSpriteSwitches();
            }
        }

        IEnumerator CorUpdatePositions()
        {
            while (true)
            {
                yield return new WaitForEndOfFrame();
                UpdatePositions();
            }
        }

        public void UpdatePositions()
        {
            for (int ia = 0; ia < psnLevels.Count; ia++)
            {
                for (int jj = 0; jj < psnLevels[ia].Count; jj++)
                {
                    for (int ii = 0; ii < psnLevels[ia][jj].Count; ii++)
                    {
                        psnLevels[ia][jj][ii].RefreshPositions();
                    }
                }
            }
        }

        public void ReadPositions()
        {
            Transform camTransform = Camera.main.transform;
            cameraPosition = camTransform.position;
            cameraForward = camTransform.forward;
            cameraRotation = camTransform.rotation;

            for (int i = 0; i < clientSpritesGo.Count; i++)
            {
                UnitAnimation sl = clientSpritesGo[i];

                if (sl.particleNode != null)
                {
                    sl.particleNode.position = sl.transform.position + sl.unitAnimationType.offset;
                    sl.particleNode.rotation = sl.transform.rotation;
                }
            }
        }

        public void UpdateRotIndices()
        {
            for (int ia = 0; ia < psnLevels.Count; ia++)
            {
                for (int jj = 0; jj < psnLevels[ia].Count; jj++)
                {
                    for (int ii = 0; ii < psnLevels[ia][jj].Count; ii++)
                    {

                        ParticleSystemNode psn1 = psnLevels[ia][jj][ii];

                        if (psn1.systemGo.activeSelf == true)
                        {
                            for (int kk = 0; kk < psn1.transforms.Count; kk++)
                            {

                                UnitAnimation pt_tr = psn1.transforms[kk];

                                if (pt_tr == null)
                                {
                                    psn1.ForceRemoveParticleAt(kk);
                                }
                                else
                                {

                                    int j1 = GetVerRotIndex(psn1.pn_pool[kk].position, psn1.pn_pool[kk].rotation, psnLevels[ia].Count, psnLevels[ia][jj].Count);
                                    int i1 = GetHorRotIndex(psn1.pn_pool[kk].position, psn1.pn_pool[kk].rotation, psnLevels[ia].Count, psnLevels[ia][jj].Count);

                                    if ((j1 != jj) || (i1 != ii))
                                    {
                                        if ((j1 >= 0) && (i1 >= 0))
                                        {
                                            if (j1 < psnLevels[ia].Count)
                                            {
                                                if (i1 < psnLevels[ia][j1].Count)
                                                {
                                                    int kk1 = psn1.transforms.IndexOf(pt_tr);
                                                    psn1.tr_Rem.Add(pt_tr);

                                                    ParticleNode ptn = psn1.pn_pool[kk1];
                                                    ptn.particle = psn1.pool[kk1];

                                                    psnLevels[ia][j1][i1].pn_Add.Add(ptn);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            for (int ia = 0; ia < psnLevels.Count; ia++)
            {
                for (int jj = 0; jj < psnLevels[ia].Count; jj++)
                {
                    for (int ii = 0; ii < psnLevels[ia][jj].Count; ii++)
                    {
                        psnLevels[ia][jj][ii].AddAndRemoveParticles();
                    }
                }
            }
        }

        // Get horizontal rotational level	
        public int GetHorRotIndex(Vector3 pos, Quaternion rot, int vRotLevels, int hRotLevels)
        {
            Vector2 camForwardXZ = new Vector2(cameraForward.x, cameraForward.z);
            float camVertRot = cameraRotation.eulerAngles.y;

            Vector3 relaltivePos = pos - cameraPosition;
            Vector2 relaltivePosXZ = new Vector2(relaltivePos.x, relaltivePos.z);

            float cameraAngleCorrection = SignedAngleBetween2d(camForwardXZ, relaltivePosXZ);

            int finYindex = (int)(((rot.eulerAngles.y - camVertRot) - cameraAngleCorrection) / (360f / hRotLevels));

            if (finYindex > hRotLevels - 1)
            {
                finYindex = finYindex - hRotLevels;
            }
            else if (finYindex < 0)
            {
                finYindex = finYindex + hRotLevels;
                if (finYindex < 0)
                {
                    finYindex = finYindex + hRotLevels;
                }

            }

            return finYindex;
        }

        // Get vertical rotational level
        public int GetVerRotIndex(Vector3 pos, Quaternion rot, int vRotLevels, int hRotLevels)
        {
            Vector3 relaltivePos = pos - cameraPosition;

            float vertAngle = Angle3d(-relaltivePos, new Vector3(0f, 1f, 0f));
            int vertRotIndexCam = vRotLevels - 1 - (int)(vertAngle / (90f / vRotLevels));

            if (vertRotIndexCam < 0)
            {
                vertRotIndexCam = 0;
            }

            return vertRotIndexCam;
        }

        float SignedAngleBetween2d(Vector2 a, Vector2 b)
        {
            float angle = Angle2d(a, b);
            float sign = Mathf.Sign(a.y * b.x - a.x * b.y);

            // angle in [-179,180]
            float signed_angle = angle * sign;

            // angle in [0,360] (not used but included here for completeness)
            // float angle360 =  (signed_angle + 180) % 360;
            return signed_angle;
        }

        public static float Angle2d(Vector2 a, Vector2 b)
        {
            double ax = (double)a.x;
            double ay = (double)a.y;

            double bx = (double)b.x;
            double by = (double)b.y;

            double dotd = ax * bx + ay * by;

            double aMag = System.Math.Sqrt(ax * ax + ay * ay);
            double bMag = System.Math.Sqrt(bx * bx + by * by);

            double aCos = System.Math.Acos(dotd / (aMag * bMag));

            return (float)(aCos * 180 / 3.14159265359);
        }

        public static float Angle3d(Vector3 a, Vector3 b)
        {
            double ax = (double)a.x;
            double ay = (double)a.y;
            double az = (double)a.z;

            double bx = (double)b.x;
            double by = (double)b.y;
            double bz = (double)b.z;

            double dotd = ax * bx + ay * by + az * bz;

            double aMag = System.Math.Sqrt(ax * ax + ay * ay + az * az);
            double bMag = System.Math.Sqrt(bx * bx + by * by + bz * bz);

            double aCos = System.Math.Acos(dotd / (aMag * bMag));

            return (float)(aCos * 180 / 3.14159265359);
        }

        public static float Dot(Vector3 a, Vector3 b)
        {
            return a.x * b.x + a.y * b.y + a.z * b.z;
        }

        public int GetAnimationIndexByModelAndName(string mName, string aName)
        {
            int animationIndex = -1;

            for (int i = 0; i < nAnim; i++)
            {
                if ((mName == modelName[i]) && (aName == animationName[i]))
                {
                    animationIndex = i;
                }
            }

            return animationIndex;
        }

        public void PlayAnimation(UnitAnimation sl, string animationToPlay)
        {

            int animationIndex = GetAnimationIndexByModelAndName(sl.unitAnimationType.modelName, animationToPlay);

            if (animationIndex >= 0)
            {
                Vector3 slPos = sl.transform.position;
                Quaternion slRot = sl.transform.rotation;

                int vlev = GetVerRotIndex(slPos, slRot, psnLevels[animationIndex].Count, psnLevels[animationIndex][0].Count);
                int hlev = GetHorRotIndex(slPos, slRot, psnLevels[animationIndex].Count, psnLevels[animationIndex][0].Count);
                if (sl.particleNode == null)
                {
                    ParticleSystemNode psn1 = psnLevels[animationIndex][vlev][hlev];
                    psn1.AddParticleFromUnitPars(sl);
                    sl.animName = animationToPlay;

                }
                else if ((sl.particleNode != null) && (sl.particleNode.ptSystemNode == null))
                {
                    ParticleSystemNode psn2 = psnLevels[animationIndex][vlev][hlev];
                    psn2.AddParticleFromUnitPars(sl);
                    sl.animName = animationToPlay;
                }
                else
                {
                    int prevAnimIndex = GetAnimationIndexByModelAndName(sl.unitAnimationType.modelName, sl.animName);

                    if ((prevAnimIndex >= 0) && (prevAnimIndex != animationIndex))
                    {

                        ParticleSystemNode psn_old = psnLevels[prevAnimIndex][vlev][hlev];
                        ParticleSystemNode psn_new = psnLevels[animationIndex][vlev][hlev];

                        int kk1 = psn_old.transforms.IndexOf(sl);

                        if (kk1 < 0)
                        {
                            for (int ia = 0; ia < psnLevels.Count; ia++)
                            {
                                for (int jj = 0; jj < psnLevels[ia].Count; jj++)
                                {
                                    for (int ii = 0; ii < psnLevels[ia][jj].Count; ii++)
                                    {
                                        int id1 = psnLevels[ia][jj][ii].transforms.IndexOf(sl);
                                        if (id1 > -1)
                                        {
                                            kk1 = id1;
                                            psn_old = psnLevels[ia][jj][ii];
                                        }
                                    }
                                }
                            }
                        }

                        if (kk1 >= 0)
                        {
                            ParticleNode ptn = psn_old.pn_pool[kk1];
                            ForceAddRemoveParticle(psn_new, psn_old, ptn, sl);

                            sl.animName = animationToPlay;
                        }
                    }
                }
            }
        }

        public void RemoveAnimation(UnitAnimation sl)
        {
            if (sl != null)
            {
                if (sl.particleNode != null)
                {
                    if (sl.particleNode.ptSystemNode != null)
                    {
                        sl.particleNode.ptSystemNode.ForceRemoveParticle(sl);
                        sl.particleNode.ptSystemNode = null;
                    }
                }
            }
        }

        void UpdateModelSpriteSwitches()
        {
            for (int i = 0; i < clientSpritesGo.Count; i++)
            {
                UnitAnimation goC = clientSpritesGo[i];
                CheckSpriteMode(goC);
            }
        }

        public void SetModelAnimation(UnitAnimation spL)
        {
            GameObject tgo = spL.gameObject;

            if (spL.isSpriteEnabled == false)
            {
                int iiii = animationName.IndexOf(spL.animName);
                if (iiii >= 0)
                {
                    spL.repeatableAnimations = repeatableAnimations[iiii];
                }

                foreach (Transform child in tgo.transform)
                {
                    child.gameObject.SetActive(true);
                }

                if (tgo.GetComponent<Animation>() != null)
                {
                    Animation anim = tgo.GetComponent<Animation>();

                    if (spL.repeatableAnimations != 0)
                    {
                        if (!anim.IsPlaying(spL.animName))
                        {
                            anim.wrapMode = WrapMode.Loop;
                            anim[spL.animName].time = 0f;
                            anim.Play(spL.animName);
                        }
                    }
                    else if (spL.repeatableAnimations == 0)
                    {
                        if (Time.time - spL.particleNode.nonRepeatableStartTime < anim[spL.animName].length)
                        {
                            anim.wrapMode = WrapMode.Once;
                            anim[spL.animName].time = Time.time - spL.particleNode.nonRepeatableStartTime;
                            anim.Play(spL.animName);
                        }
                        else
                        {
                            anim.wrapMode = WrapMode.Once;
                            anim[spL.animName].time = anim[spL.animName].length;
                            anim.Play(spL.animName);
                        }
                    }
                }
            }
        }

        public void CheckSpriteMode(UnitAnimation sl)
        {
            if ((cameraPosition - sl.transform.position).magnitude < sl.bilboardDistance)
            {
                if (sl.isSpriteEnabled)
                {
                    sl.DisableSprite();
                    RemoveAnimation(sl);
                    SetModelAnimation(sl);
                    sl.isSpriteEnabled = false;
                }
            }
            else
            {
                if (sl.isSpriteEnabled == false)
                {
                    sl.EnableSprite();
                    PlayAnimation(sl, sl.animName);
                    sl.isSpriteEnabled = true;
                }
            }
        }

        public void ForceSpriteModeLoad(UnitAnimation node)
        {
            UnitAnimation sl = node;

            if (sl != null)
            {
                if ((RenderMeshModels.cameraPosition - node.transform.position).magnitude < sl.bilboardDistance)
                {
                    sl.isSpriteEnabled = false;
                    PlayAnimation(sl, sl.animName);

                    sl.DisableSprite();
                    RemoveAnimation(sl);

                    sl.isSpriteEnabled = false;
                }
                else
                {
                    PlayAnimation(sl, sl.animName);
                    sl.isSpriteEnabled = true;
                }
            }
        }

        public void SetBundleMaterial2(int ia, int jj, int ii)
        {
            List<Material> mats = new List<Material>();

            for (int j1 = 0; j1 < jj; j1++)
            {
                for (int i1 = 0; i1 < ii; i1++)
                {
                    Material mat2 = new Material(particleShader);

                    mat2.EnableKeyword("_ALPHATEST_ON");
                    mat2.SetFloat("_Mode", 2f);
                    mat2.SetFloat("_Glossiness", 0f);
                    mat2.mainTexture = emptyTex;

                    mats.Add(mat2);
                }
            }

            TextureBundleLoader tbl1 = new TextureBundleLoader(ia, jj, ii, mats);

            texturesBufferLoading = texturesBufferLoading + 1;
            textureBundlesToLoad.Add(tbl1);
        }

        IEnumerator LoadTextureBundles()
        {
            while (textureBundlesToLoad.Count > 0)
            {
                for (int i = 0; i < textureBundlesToLoad.Count; i++)
                {
                    if (loadingTextureBundles.Count < 1)
                    {
                        loadingTextureBundles.Add(textureBundlesToLoad[i]);
#if URTS_WWW
					StartCoroutine(DownloadTextureBundleWWW(textureBundlesToLoad[i],1));
#else
                        StartCoroutine(DownloadTextureBundle(textureBundlesToLoad[i], 1));
#endif
                    }
                }

                for (int i = 0; i < loadingTextureBundles.Count; i++)
                {
                    textureBundlesToLoad.Remove(loadingTextureBundles[i]);
                }

                yield return new WaitForEndOfFrame();
            }
        }

        IEnumerator DownloadTextureBundle(TextureBundleLoader tbl, int attempt)
        {
#if !UNITY_WEBPLAYER
            int ia = tbl.globalIndex;
            int jj = tbl.vRotIndex;
            int ii = tbl.hRotIndex;

            string bundlePrefix = "";

#if UNITY_WEBPLAYER
		bundlePrefix = "WebPlayer_bnd/";
#endif
#if UNITY_STANDALONE
            bundlePrefix = "StandaloneOSXUniversal_bnd/";
#endif
#if UNITY_ANDROID
		bundlePrefix = "Android_bnd/";
#endif
#if UNITY_IOS
		bundlePrefix = "iOS_bnd/";
#endif
#if UNITY_WEBGL
		bundlePrefix = "WebGL_bnd/";
#endif
            string path =
                "file://" +
                Application.dataPath +
                "/uRTS/Resources/3dSprites/" +
                bundlePrefix +
                modelName[ia] +
                "/" +
                animationName[ia] +
                "/" +
                modelName[ia] +
                "_" +
                animationName[ia] +
                ".unity3d";

            string abd1 = assetBundlesDirectory;
            string abd2 = secondaryAssetBundlesUrl;

            if (attempt == 2)
            {
                abd1 = abd2;
            }

#if UNITY_WEBPLAYER || UNITY_WEBGL
		abd1 = abd2;
		attempt = 2;
#endif

            if ((useApplicationDataPathPrefixForBundles) && (attempt == 1))
            {
                path =
                    "file://" +
                    Application.dataPath +
                    abd1 +
                    bundlePrefix +
                    modelName[ia] +
                    "/" +
                    animationName[ia] +
                    "/" +
                    modelName[ia] +
                    "_" +
                    animationName[ia] +
                    ".unity3d";

            }
            else
            {
                path =
                    abd1 +
                    bundlePrefix +
                    modelName[ia] +
                    "/" +
                    animationName[ia] +
                    "/" +
                    modelName[ia] +
                    "_" +
                    animationName[ia] +
                    ".unity3d";
            }

            while (!Caching.ready)
            {
                yield return null;
            }

            UnityEngine.Networking.UnityWebRequest webRequest = new UnityEngine.Networking.UnityWebRequest(path);
            UnityEngine.Networking.DownloadHandlerAssetBundle handler = new UnityEngine.Networking.DownloadHandlerAssetBundle(webRequest.url, 0);
            webRequest.downloadHandler = handler;
            yield return webRequest.SendWebRequest();

            if (webRequest.isNetworkError)
            {
                if (attempt == 1)
                {
                    Debug.Log("First attempt at path " + path + " fails, trying now to download from supporter website");
                    StartCoroutine(DownloadTextureBundle(tbl, 2));
                }
                if (attempt == 2)
                {
                    Debug.Log("Second attempt to download from " + path + " failed ! You can manually download 3dSprite AssetBundles from " + secondaryAssetBundlesUrl);
                    loadingTextureBundles.Remove(tbl);
                    textureBundlesToLoad.Remove(tbl);

                    textureBundlesToLoad.Add(tbl);
                }

                yield return null;
            }
            else
            {
                AssetBundle bundle = handler.assetBundle;
                Debug.Log("Loaded");

                for (int j1 = 0; j1 < jj; j1++)
                {
                    for (int i1 = 0; i1 < ii; i1++)
                    {
                        tbl.textures[j1 * tbl.hRotIndex + i1] = (Texture2D)bundle.LoadAsset(modelName[ia] + "_" + animationName[ia] + "_" + (j1 + 1).ToString() + "_" + (i1 + 1).ToString());

                        spriteTextures.Add(tbl.textures[j1 * tbl.hRotIndex + i1]);

                        tbl.materials[j1 * tbl.hRotIndex + i1].mainTexture = tbl.textures[j1 * tbl.hRotIndex + i1];
                    }
                }

                if (unloadAssetBundles)
                {
                    bundle.Unload(false);
                }

                webRequest.Dispose();
                loadingTextureBundles.Remove(tbl);
                textureBundlesToLoad.Remove(tbl);
                loadedTextureBundles.Add(tbl);
            }
#endif
            yield return null;
        }

#if URTS_WWW
    IEnumerator DownloadTextureBundleWWW(TextureBundleLoader tbl, int attempt)
    {
        int ia = tbl.globalIndex;
        int jj = tbl.vRotIndex;
        int ii = tbl.hRotIndex;

        string bundlePrefix = "";

#if UNITY_WEBPLAYER
		bundlePrefix = "WebPlayer_bnd/";
#endif
#if UNITY_STANDALONE
        bundlePrefix = "StandaloneOSXUniversal_bnd/";
#endif
#if UNITY_ANDROID
		bundlePrefix = "Android_bnd/";
#endif
#if UNITY_IOS
		bundlePrefix = "iOS_bnd/";
#endif
#if UNITY_WEBGL
		bundlePrefix = "WebGL_bnd/";
#endif

        string path =
            "file://" +
            Application.dataPath +
            "/uRTS/Resources/3dSprites/" +
            bundlePrefix +
            modelName[ia] +
            "/" +
            animationName[ia] +
            "/" +
            modelName[ia] +
            "_" +
            animationName[ia] +
            ".unity3d";

        string abd1 = assetBundlesDirectory;
        string abd2 = secondaryAssetBundlesUrl;

        if (attempt == 2)
        {
            abd1 = abd2;
        }

#if UNITY_WEBPLAYER || UNITY_WEBGL
		abd1 = abd2;
		attempt = 2;
#endif

        if ((useApplicationDataPathPrefixForBundles) && (attempt == 1))
        {
            path = 
				"file://" +
				Application.dataPath +
				abd1 +
				bundlePrefix +
				modelName[ia] +
				"/" +
				animationName[ia] +
				"/" +
				modelName[ia] +
				"_" +
				animationName[ia] +
				".unity3d";

        }
        else
        {
            path = 
				abd1 +
				bundlePrefix +
				modelName[ia] +
				"/" +
				animationName[ia] +
				"/" +
				modelName[ia] +
				"_" +
				animationName[ia] +
				".unity3d";
        }
        
        while (!Caching.ready)
        {
            yield return null;
        }

        int buildTarget = 5;

#if UNITY_WEBGL
		buildTarget = 6;
#endif

        WWW www = WWW.LoadFromCacheOrDownload(path, buildTarget);
        yield return www;
        
        if (!string.IsNullOrEmpty(www.error))
        {
            if (attempt == 1)
            {
                Debug.Log("First attempt at path " + path + " fails, trying now to download from supporter website");
                StartCoroutine(DownloadTextureBundleWWW(tbl, 2));
            }

            if (attempt == 2)
            {
                Debug.Log("Second attempt to download from " + path + " failed ! You can manually download 3dSprite AssetBundles from" + secondaryAssetBundlesUrl);
                loadingTextureBundles.Remove(tbl);
                textureBundlesToLoad.Remove(tbl);

                textureBundlesToLoad.Add(tbl);
            }

            yield return null;
        }
        else
        {
            AssetBundle bundle = www.assetBundle;
            
            for (int j1 = 0; j1 < jj; j1++)
            {
                for (int i1 = 0; i1 < ii; i1++)
                {
                    tbl.textures[j1 * tbl.hRotIndex + i1] = (Texture2D)bundle.LoadAsset(modelName[ia] + "_" + animationName[ia] + "_" + (j1 + 1).ToString() + "_" + (i1 + 1).ToString());
                    spriteTextures.Add(tbl.textures[j1 * tbl.hRotIndex + i1]);
                    tbl.materials[j1 * tbl.hRotIndex + i1].mainTexture = tbl.textures[j1 * tbl.hRotIndex + i1];
                }
            }

            if (unloadAssetBundles)
            {
                bundle.Unload(false);
            }

            www.Dispose();
            loadingTextureBundles.Remove(tbl);
            textureBundlesToLoad.Remove(tbl);
            loadedTextureBundles.Add(tbl);
        }
    }
#endif

        public void ClearDownloadsCache()
        {
            Caching.ClearCache();
        }

        public void DestroyTexture(Texture2D tx)
        {
            DestroyImmediate(tx, true);
            Resources.UnloadAsset(tx);
        }

        public void UnloadAllTextures()
        {
            int N = loadedTextureBundles.Count;
            for (int i = 0; i < N; i++)
            {
                DestroyTexture(loadedTextureBundles[0].texture);
                Material mat = loadedTextureBundles[0].material;

                Destroy(mat);
                loadedTextureBundles.RemoveAt(0);
            }
            Resources.UnloadUnusedAssets();
            System.GC.Collect();
            Resources.UnloadUnusedAssets();
            System.GC.Collect();
        }

        public void UnlodadSpriteManagers()
        {
            for (int i = 0; i < spriteManagers.Count; i++)
            {
                if (spriteManagers[i] != null)
                {
                    Destroy(spriteManagers[i]);
                }
            }

            spriteManagers.Clear();
        }

        public void OnApplicationQuit()
        {
            UnloadAllTextures();
            UnlodadSpriteManagers();
        }

        public void CreateEmptyTexture()
        {
            emptyTex = new Texture2D(64, 64, TextureFormat.ARGB32, false);

            for (int i = 0; i < 64; i++)
            {
                for (int j = 0; j < 64; j++)
                {
                    emptyTex.SetPixel(i, j, Color.clear);
                }
            }

            emptyTex.Apply();
        }

        //////////////////////////////////////////////////////////////////////////////////////////

        public static void ForceAddRemoveParticle(ParticleSystemNode newNode, ParticleSystemNode oldNode, ParticleNode pn, UnitAnimation pt_tr)
        {
            if (oldNode.systemGo != null)
            {
                List<ParticleSystem.Particle> old_all = oldNode.pool.ToList();

                int j = oldNode.transforms.IndexOf(pt_tr);
                Vector3 pos1 = pn.spriteGOModule.transform.position;

                if (j >= 0)
                {
                    pos1 = old_all[j].position;
                    old_all.RemoveAt(j);
                    oldNode.transforms.RemoveAt(j);
                    oldNode.pn_pool.RemoveAt(j);
                }

                if (oldNode.systemGo.activeSelf == false)
                {
                    if (old_all.Count > 0)
                    {
                        oldNode.systemGo.SetActive(true);
                    }
                }
                else if (oldNode.systemGo.activeSelf == true)
                {
                    if (old_all.Count == 0)
                    {
                        oldNode.systemGo.SetActive(false);
                    }
                }

                List<ParticleSystem.Particle> new_all = newNode.pool.ToList();

                pn.ptSystemNode = newNode;

                pn.particle.startLifetime = newNode.duration * 10f;
                pn.particle.position = pos1;
                pn.particle.remainingLifetime = newNode.duration * (10f - pn.animationPhase);

                if (newNode.repeatableAnimation == 0)
                {
                    float phaseNow = (Time.time - pn.nonRepeatableStartTime) / (newNode.duration);
                    pn.particle.remainingLifetime = newNode.duration * (10f - phaseNow);

                    if (phaseNow >= 1f)
                    {
                        pn.particle.remainingLifetime = 0.1f;
                    }
                }

                new_all.Add(pn.particle);
                newNode.transforms.Add(pn.spriteGOModule);
                newNode.pn_pool.Add(pn);

                if (newNode.systemGo.activeSelf == false)
                {
                    if (new_all.Count > 0)
                    {
                        newNode.systemGo.SetActive(true);
                    }
                }
                else if (newNode.systemGo.activeSelf == true)
                {
                    if (new_all.Count == 0)
                    {
                        newNode.systemGo.SetActive(false);
                    }
                }

                newNode.pool = new_all.ToArray();
                newNode.ptSystem.SetParticles(newNode.pool, newNode.pool.Length);

                oldNode.pool = old_all.ToArray();
                oldNode.ptSystem.SetParticles(oldNode.pool, oldNode.pool.Length);
            }
        }
    }

    [System.Serializable]
    public class ParticleSystemNode
    {
        public ParticleSystem ptSystem;
        public ParticleSystem.Particle[] pool;
        public List<UnitAnimation> transforms;

        public List<ParticleNode> pn_pool;
        public ParticleSystemRenderer pr;

        public Vector3[] positions;
        public TextureTile textureTile;
        public GameObject systemGo;

        public List<ParticleNode> pn_Add;
        public List<UnitAnimation> tr_Rem;

        public int repeatableAnimation = 1;
        public float duration = 1f;

        public ParticleSystemNode(Vector3[] pos, GameObject go, TextureTile tt)
        {
            int n = pos.Length;

            ptSystem = go.GetComponent<ParticleSystem>();

            pr = (ParticleSystemRenderer)ptSystem.GetComponent<Renderer>();

            pool = new ParticleSystem.Particle[n];
            transforms = new List<UnitAnimation>();
            pn_pool = new List<ParticleNode>();

            pn_Add = new List<ParticleNode>();
            tr_Rem = new List<UnitAnimation>();

            for (int i = 0; i < n; i++)
            {
                transforms.Add(null);
            }

            for (int i = 0; i < n; i++)
            {
                pool[i].position = pos[i];
                pool[i].angularVelocity = 0f;
                pool[i].rotation = 0f;
                pool[i].velocity = new Vector3(0f, 0f, 0f);
                pool[i].startLifetime = 10f;
                pool[i].remainingLifetime = 10f - Random.Range(0f, 1f);
                pool[i].startSize = 1f;
                pool[i].startColor = Color.white;
            }

            pr.renderMode = ParticleSystemRenderMode.Billboard;
            pr.material = tt.material;

            ptSystem.SetParticles(pool, n);

            positions = pos;
            textureTile = tt;
            systemGo = go;

            if (n == 0)
            {
                systemGo.SetActive(false);
            }
        }

        public void AddParticleFromUnitPars(UnitAnimation sl)
        {

            if (systemGo.activeSelf == false)
            {
                systemGo.SetActive(true);
            }

            List<ParticleSystem.Particle> all = pool.ToList();
            ParticleNode ptn = null;

            ParticleSystem.Particle pt = new ParticleSystem.Particle();
            pt.position = sl.transform.position + sl.unitAnimationType.offset;
            pt.angularVelocity = 0f;
            pt.rotation = 0f;
            pt.velocity = Vector3.zero;

            pt.startSize = sl.unitAnimationType.spriteSize;
            pt.startColor = Color.white;

            pt.startLifetime = duration * 10f;
            pt.remainingLifetime = duration * (10f - Random.Range(0f, 1f));

            all.Add(pt);
            transforms.Add(sl);

            if (sl.particleNode == null)
            {
                ptn = new ParticleNode(this, pt, sl);
                sl.particleNode = ptn;
                sl.particleNode.spriteGOModule = sl;
            }
            else
            {
                ptn = sl.particleNode;
            }

            ptn.ptSystemNode = this;
            ptn.animationPhase = 0f;
            pn_pool.Add(ptn);
            pool = all.ToArray();
            ptSystem.SetParticles(pool, pool.Length);
        }

        public void AddAndRemoveParticles()
        {
            if ((pn_Add.Count > 0) || (tr_Rem.Count > 0))
            {
                List<ParticleSystem.Particle> all = pool.ToList();

                for (int i = 0; i < pn_Add.Count; i++)
                {
                    ParticleNode pn1 = pn_Add[i];
                    pn1.ptSystemNode = this;

                    pn1.particle.startLifetime = duration * 10f;
                    pn1.particle.remainingLifetime = duration * (10f - pn1.animationPhase);

                    if (repeatableAnimation == 0)
                    {
                        float phaseNow = (Time.time - pn1.nonRepeatableStartTime) / duration;
                        pn1.particle.remainingLifetime = duration * (10f - phaseNow);
                        if (phaseNow >= 1f)
                        {
                            pn1.particle.remainingLifetime = 0.1f;
                        }
                    }

                    all.Add(pn1.particle);
                    transforms.Add(pn1.spriteGOModule);
                    pn_pool.Add(pn1);
                }

                for (int i = 0; i < tr_Rem.Count; i++)
                {
                    int j = transforms.IndexOf(tr_Rem[i]);
                    if (j >= 0)
                    {
                        all.RemoveAt(j);
                        transforms.RemoveAt(j);
                        pn_pool.RemoveAt(j);
                    }
                }

                pn_Add.Clear();
                tr_Rem.Clear();

                if (systemGo.activeSelf == false)
                {
                    if (all.Count > 0)
                    {
                        systemGo.SetActive(true);
                    }
                }
                else if (systemGo.activeSelf == true)
                {
                    if (all.Count == 0)
                    {
                        systemGo.SetActive(false);
                    }
                }

                pool = all.ToArray();
                ptSystem.SetParticles(pool, pool.Length);
            }
        }

        public void ForceRemoveParticle(UnitAnimation pt_tr)
        {
            if (systemGo != null)
            {
                List<ParticleSystem.Particle> all = pool.ToList();
                int j = transforms.IndexOf(pt_tr);

                if (j >= 0)
                {
                    all.RemoveAt(j);
                    transforms.RemoveAt(j);
                    pn_pool.RemoveAt(j);
                }

                if (systemGo.activeSelf == false)
                {
                    if (all.Count > 0)
                    {
                        systemGo.SetActive(true);
                    }
                }
                else if (systemGo.activeSelf == true)
                {
                    if (all.Count == 0)
                    {
                        systemGo.SetActive(false);
                    }
                }

                pool = all.ToArray();
                ptSystem.SetParticles(pool, pool.Length);
            }
        }

        public void ForceRemoveParticleAt(int id)
        {
            List<ParticleSystem.Particle> all = pool.ToList();
            int j = id;

            if (j >= 0)
            {
                all.RemoveAt(j);
                transforms.RemoveAt(j);
                pn_pool.RemoveAt(j);
            }

            if (systemGo.activeSelf == false)
            {
                if (all.Count > 0)
                {
                    systemGo.SetActive(true);
                }
            }
            else if (systemGo.activeSelf == true)
            {
                if (all.Count == 0)
                {
                    systemGo.SetActive(false);
                }
            }

            pool = all.ToArray();
            ptSystem.SetParticles(pool, pool.Length);
        }

        public void ForceAddParticle(ParticleNode pn)
        {
            List<ParticleSystem.Particle> all = pool.ToList();

            pn.ptSystemNode = this;

            pn.particle.startLifetime = duration * 10f;
            pn.particle.position = pn.spriteGOModule.transform.position;
            pn.particle.remainingLifetime = duration * (10f - pn.animationPhase);

            if (repeatableAnimation == 0)
            {
                float phaseNow = (Time.time - pn.nonRepeatableStartTime) / duration;
                pn.particle.remainingLifetime = duration * (10f - phaseNow);

                if (phaseNow >= 1f)
                {
                    pn.particle.remainingLifetime = 0.1f;
                }
            }

            all.Add(pn.particle);
            transforms.Add(pn.spriteGOModule);
            pn_pool.Add(pn);

            if (systemGo.activeSelf == false)
            {
                if (all.Count > 0)
                {
                    systemGo.SetActive(true);
                }
            }
            else if (systemGo.activeSelf == true)
            {
                if (all.Count == 0)
                {
                    systemGo.SetActive(false);
                }
            }

            pool = all.ToArray();
            ptSystem.SetParticles(pool, pool.Length);
        }

        public void RefreshPositions()
        {
            int n = pool.Length;

            for (int i = 0; i < n; i++)
            {
                pool[i].position = pn_pool[i].position;
                float fullDuration = duration;
                float animationPhase = pn_pool[i].spriteGOModule.a_phase + Time.deltaTime / fullDuration;

                if (repeatableAnimation != 0)
                {
                    if (animationPhase > 1f)
                    {
                        animationPhase = animationPhase - 1f;
                    }
                }
                else
                {
                    if (animationPhase > 0.9f)
                    {
                        animationPhase = 0.9f;
                    }
                }

                pn_pool[i].animationPhase = animationPhase;
                pn_pool[i].spriteGOModule.a_phase = animationPhase;

                float newLifetime = pool[i].startLifetime - fullDuration * pn_pool[i].animationPhase;
                pool[i].remainingLifetime = newLifetime;
                pn_pool[i].particle.remainingLifetime = newLifetime;
            }

            ptSystem.SetParticles(pool, n);
        }
    }

    public class ParticleNode
    {
        public ParticleSystemNode ptSystemNode;
        public ParticleSystem.Particle particle;
        public Vector3 position;
        public Quaternion rotation;
        public UnitAnimation spriteGOModule;
        public float animationPhase = 0f;
        public float nonRepeatableStartTime = 0f;

        public ParticleNode(ParticleSystemNode ptSystemNode1, ParticleSystem.Particle particle1, UnitAnimation sl)
        {
            ptSystemNode = ptSystemNode1;
            particle = particle1;
            spriteGOModule = sl;
        }
    }

    [System.Serializable]
    public class TextureTile
    {
        public Texture2D texture;
        public Material material;

        public TextureTile(Texture2D tex, int iRow, int iCol, int nRows, int nCols)
        {
            texture = GetTextureTile(tex, iRow, iCol, nRows, nCols);
            material = new Material(PSpriteLoader.active.particleShader);
            material.mainTexture = texture;
        }

        public TextureTile(Texture2D tex)
        {
            texture = tex;
            material = new Material(PSpriteLoader.active.particleShader);
            material.SetColor("_EmissionColor", new Color(0.5f, 0.5f, 0.5f, 1f));
            material.mainTexture = texture;
        }

        Texture2D GetTextureTile(Texture2D tex, int iRow, int iCol, int nRows, int nCols)
        {
            int width = tex.width;
            int height = tex.height;

            int iMin = width / nCols * (iCol);
            int iMax = width / nCols * (iCol + 1);

            int jMin = height / nRows * (iRow);
            int jMax = height / nRows * (iRow + 1);

            Texture2D texture = new Texture2D(width / nCols, height / nRows, TextureFormat.ARGB32, false);
            Color[] pixels = new Color[(width / nCols) * (height / nRows)];

            for (int i = iMin; i < iMax; i++)
            {
                for (int j = jMin; j < jMax; j++)
                {
                    int k = (i - iMin) + (j - jMin) * (iMax - iMin);
                    pixels[k] = tex.GetPixel(i, j);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();

            return texture;
        }
    }

    public class TextureBundleLoader
    {
        public int globalIndex;
        public int vRotIndex;
        public int hRotIndex;
        public Material material;
        public Texture2D texture;

        public List<Material> materials = new List<Material>();
        public List<Texture2D> textures = new List<Texture2D>();

        public string url;

        public TextureBundleLoader(int globalIndex1, int vRotIndex1, int hRotIndex1, List<Material> materials1)
        {
            globalIndex = globalIndex1;
            vRotIndex = vRotIndex1;
            hRotIndex = hRotIndex1;
            materials = materials1;

            for (int i = 0; i < materials1.Count; i++)
            {
                textures.Add((Texture2D)materials1[i].mainTexture);
            }
        }
    }
}

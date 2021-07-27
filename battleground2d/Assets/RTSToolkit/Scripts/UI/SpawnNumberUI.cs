using UnityEngine;
using UnityEngine.UI;

namespace RTSToolkit
{
    public class SpawnNumberUI : MonoBehaviour
    {
        public static SpawnNumberUI active;

        public Text txt;
        SpawnPoint spawner;
        int counter = 1;

        [HideInInspector] public bool scrollMode = false;

        void Awake()
        {
            active = this;
        }

        void Start()
        {

        }

        void Update()
        {
            if (scrollMode)
            {
                UpdateInner();
            }
        }

        void UpdateInner()
        {
            int pass = 0;

#if UNITY_STANDALONE || UNITY_WEBPLAYER || UNITY_WEBGL
            if (Input.GetAxis("Mouse ScrollWheel") > 0)
            {
                pass = 1;
            }

            if (Input.GetAxis("Mouse ScrollWheel") < 0)
            {
                pass = -1;
            }
#endif
#if UNITY_IPHONE || UNITY_ANDROID
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Moved)
        {
            if (Input.GetTouch(0).deltaPosition.y > 0)
            {
                pass = 1;
            }
            else if (Input.GetTouch(0).deltaPosition.y < 0)
            {
                pass = -1;
            }
        }

#if UNITY_EDITOR
        if (Input.GetAxis("Mouse ScrollWheel") > 0)
        {
            pass = 1;
        }

        if (Input.GetAxis("Mouse ScrollWheel") < 0)
        {
            pass = -1;
        }
#endif
#endif
            if (pass != 0)
            {
                int increm = 1;

                if (spawner == null)
                {
                    SelectionManager selM = SelectionManager.active;

                    if (selM.selectedGoPars.Count > 0)
                    {
                        if (selM.selectedGoPars[0].gameObject.GetComponent<SpawnPoint>() != null)
                        {
                            spawner = selM.selectedGoPars[0].gameObject.GetComponent<SpawnPoint>();
                        }
                    }
                }

                if (spawner != null)
                {
                    increm = spawner.formationSize;
                }

                if (pass == 1)
                {
                    counter = GetNearestIncrementUp(counter, increm);

                    if (counter > 255)
                    {
                        counter = increm;
                    }
                }
                else if (pass == -1)
                {
                    counter = GetNearestIncrementDown(counter, increm);

                    if (counter < 1)
                    {
                        counter = GetNearestIncrementDown(255, increm);
                    }
                }

                if (txt != null)
                {
                    txt.text = counter.ToString();
                }
            }
        }

        public int GetNearestIncrementUp(int orig, int part)
        {
            int newValue = orig;

            for (int i = orig; i < (orig + part + 1); i++)
            {
                if (i % part == 0)
                {
                    newValue = i;
                }
            }

            return newValue;
        }

        public int GetNearestIncrementDown(int orig, int part)
        {
            int newValue = orig;

            for (int i = orig; i > (orig - part - 1); i--)
            {
                if (i % part == 0)
                {
                    newValue = i;
                }
            }

            return newValue;
        }

        public void StartSpawning(UnitPars model)
        {
            SelectionManager selM = SelectionManager.active;
            spawner = selM.selectedGoPars[0].gameObject.GetComponent<SpawnPoint>();

            if (spawner != null)
            {
                spawner.numberOfObjects = counter;
                spawner.model = model.gameObject;

                spawner.StartSpawning();
                ProgressCounterUI.active.Activate();
                ProgressCounterUI.active.UpdateText(1, counter);
            }

            counter = 1;

            if (txt != null)
            {
                txt.text = counter.ToString();
            }

            spawner = null;
        }

        public void EnableScrollMode()
        {
            txt.gameObject.SetActive(true);
            scrollMode = true;
            counter = 1;

            if (txt != null)
            {
                txt.text = counter.ToString();
            }
        }

        public void DisableScrollMode()
        {
            txt.gameObject.SetActive(false);
            scrollMode = false;
        }
    }
}

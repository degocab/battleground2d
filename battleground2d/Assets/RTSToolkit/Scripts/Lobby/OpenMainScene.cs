using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace RTSToolkit
{
    public class OpenMainScene : MonoBehaviour
    {
        public Slider progressBar;
        public Text textForLoading;
        public Image imageToChangeColor;
        public Color inactiveColor;
        bool isMainSceneLoading = false;
        public string sceneToOpen = "Main";

        void Start()
        {

        }

        public void OpenMainSceneTrigger()
        {
            if (isMainSceneLoading == false)
            {
                isMainSceneLoading = true;
                StartCoroutine(LoadMainSceneAsync());
            }
        }

        IEnumerator LoadMainSceneAsync()
        {

            if (textForLoading != null)
            {
                textForLoading.text = "Loading...";
            }

            if (imageToChangeColor != null)
            {
                imageToChangeColor.color = inactiveColor;
            }

            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneToOpen, LoadSceneMode.Single);

            while (!asyncLoad.isDone)
            {
                if (progressBar != null)
                {
                    progressBar.value = asyncLoad.progress;
                }

                yield return null;
            }
        }
    }
}

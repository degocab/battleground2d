using UnityEngine;
using UnityEngine.UI;

namespace RTSToolkit
{
    [ExecuteInEditMode]
    public class DynamicGrid : MonoBehaviour
    {
        public GridLayoutGroup grid;
        bool isRuntime = false;

        public float elementRelativeHeight = 0.05f;
        public float elementRelativeWidth = 0.1f;

        [HideInInspector] public Vector2 screenSize = new Vector2(0f, 0f);
        [HideInInspector] public Vector2 prevScreenSize = new Vector2(0f, 0f);

        void Start()
        {
            isRuntime = Application.isPlaying;
            screenSize = new Vector2(Screen.width, Screen.height);

            if (grid != null)
            {
                UpdateGrid();
                prevScreenSize = screenSize;
            }
        }

        void Update()
        {
            isRuntime = Application.isPlaying;

            if (isRuntime)
            {
                screenSize = new Vector2(Screen.width, Screen.height);
            }
            else
            {
                screenSize = GetMainGameViewSize();
            }

            if (grid != null)
            {
                if (screenSize != prevScreenSize)
                {
                    UpdateGrid();
                    prevScreenSize = screenSize;
                }
            }
        }

        void UpdateGrid()
        {
            grid.cellSize = new Vector2(screenSize.x * elementRelativeWidth, screenSize.y * elementRelativeHeight);
        }

        public static Vector2 GetMainGameViewSize()
        {
            System.Type T = System.Type.GetType("UnityEditor.GameView,UnityEditor");
            System.Reflection.MethodInfo GetSizeOfMainGameView = T.GetMethod("GetSizeOfMainGameView", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            System.Object Res = GetSizeOfMainGameView.Invoke(null, null);
            return (Vector2)Res;
        }
    }
}

using UnityEngine;

namespace RTSToolkit
{
    public class LobbyCustomCursor : MonoBehaviour
    {
        public Texture2D cursor;
        public Color cursorColor;

        void Start()
        {
            Cursor.visible = false;
        }

        void Update()
        {

        }

        void OnGUI()
        {
            Vector2 mousePosition = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);

            GUI.color = cursorColor;
            GUI.DrawTexture(GetRect(mousePosition), cursor);
        }

        public Rect GetRect(Vector2 mousePos)
        {
            int size = 32;
            return (new Rect(mousePos.x - size, mousePos.y - size, 2 * size, 2 * size));
        }
    }
}

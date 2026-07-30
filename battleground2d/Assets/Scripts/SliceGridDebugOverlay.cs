using Unity.Collections;
using Unity.Mathematics;
using UnityEditor.U2D.Path.GUIFramework;
using UnityEngine;

public class SliceGridDebugOverlay : MonoBehaviour
{
    private GUIStyle _labelStyle;
    private GUIStyle _shadowStyle;
    private Texture2D _bgTex;

    public bool Enabled = true;

    // must match SliceGridSystem
    public float2 Origin = new float2(-32f, -16f);

    private GUIStyle _style;

    void OnGUI()
    {
        //if (!Enabled) return;
        //if (!SliceGridSystem.SliceStateMap.IsCreated) return;

        //var cam = Camera.main;
        //if (cam == null) return;

        //if (_labelStyle == null)
        //{
        //    _labelStyle = new GUIStyle(GUI.skin.label)
        //    {
        //        fontSize = 10,
        //        richText = true,
        //        alignment = TextAnchor.UpperLeft,
        //        wordWrap = true,
        //        padding = new RectOffset(4, 4, 2, 2),
        //        normal = { textColor = Color.white }
        //    };

        //    _shadowStyle = new GUIStyle(_labelStyle);
        //    _shadowStyle.normal.textColor = Color.black;

        //    // 1x1 background texture (reused)
        //    _bgTex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        //    _bgTex.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.65f));
        //    _bgTex.Apply();
        //}


        //// Snapshot keys/values for safe iteration
        //var kva = SliceGridSystem.SliceStateMap.GetKeyValueArrays(Allocator.Temp);

        //for (int i = 0; i < kva.Keys.Length; i++)
        //{
        //    int key = kva.Keys[i];
        //    SliceStateData acc = kva.Values[i];

        //    int2 cell = SliceGridUtil.DecodeKey(key);
        //    SliceGridUtil.CellBounds(cell, Origin, out var min, out var max);

        //    // ✅ upper-left corner of the cell (world space)
        //    // your grid is in XY with Z=0 (based on DrawCell)
        //    Vector3 worldUL = new Vector3(min.x, max.y, 0f);

        //    Vector3 screen = cam.WorldToScreenPoint(worldUL);
        //    if (screen.z < 0f) continue; // behind camera

        //    // Convert to OnGUI coords (y flipped)
        //    float guiX = screen.x + 2f;
        //    float guiY = (Screen.height - screen.y) + 2f;

        //    string text =
        //        $"<b>{cell.x},{cell.y}</b>\n" +
        //        $"C: {acc.SmoothedControl:P0}\n" +
        //        $"M: {acc.Momentum}\n" +
        //        $"I: {acc.SmoothedIntensity:P0}\n" +
        //        $"S: {acc.State}";

        //    // Bigger rect so it doesn't clip
        //    var rect = new Rect(guiX, guiY, 100f, 85f);

        //    // Background box
        //    GUI.DrawTexture(rect, _bgTex);

        //    // Shadow/outline (draw text 4 times with small offsets)
        //    var shadowRect = rect;
        //    shadowRect.x += 1; shadowRect.y += 1; GUI.Label(shadowRect, text, _shadowStyle);
        //    shadowRect.x -= 2; GUI.Label(shadowRect, text, _shadowStyle);
        //    shadowRect.y -= 2; GUI.Label(shadowRect, text, _shadowStyle);
        //    shadowRect.x += 2; GUI.Label(shadowRect, text, _shadowStyle);

        //    // Main text
        //    GUI.Label(rect, text, _labelStyle);
        //}

        //kva.Dispose();
    }
}

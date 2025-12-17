using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

public class SliceGridDebugOverlay : MonoBehaviour
{
    public bool Enabled = true;

    // must match SliceGridSystem
    public float2 Origin = new float2(-32f, -16f);

    private GUIStyle _style;

    void OnGUI()
    {
        if (!Enabled) return;
        if (!SliceGridSystem.SliceMap.IsCreated) return;

        var cam = Camera.main;
        if (cam == null) return;

        if (_style == null)
        {
            _style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                richText = true
            };
        }

        // Snapshot keys/values for safe iteration
        var kva = SliceGridSystem.SliceMap.GetKeyValueArrays(Allocator.Temp);

        for (int i = 0; i < kva.Keys.Length; i++)
        {
            int key = kva.Keys[i];
            SliceAccum acc = kva.Values[i];

            int2 cell = SliceGridUtil.DecodeKey(key);
            SliceGridUtil.CellBounds(cell, Origin, out var min, out var max);

            // ✅ upper-left corner of the cell (world space)
            // your grid is in XY with Z=0 (based on DrawCell)
            Vector3 worldUL = new Vector3(min.x, max.y, 0f);

            Vector3 screen = cam.WorldToScreenPoint(worldUL);
            if (screen.z < 0f) continue; // behind camera

            // Convert to OnGUI coords (y flipped)
            float guiX = screen.x + 2f;
            float guiY = (Screen.height - screen.y) + 2f;

            float sum = acc.AllyStrength + acc.EnemyStrength;
            float control = (sum <= 1e-5f) ? 0f : (acc.AllyStrength - acc.EnemyStrength) / sum;

            string text =
                $"<b>{cell.x},{cell.y}</b>\n" +
                $"A:{acc.AllyStrength:F1}\n" +
                $"E:{acc.EnemyStrength:F1}\n" +
                $"C:{control:F2}";

            GUI.Label(new Rect(guiX, guiY, 90, 60), text, _style);
        }

        kva.Dispose();
    }
}

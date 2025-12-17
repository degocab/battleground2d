using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OrderDebugUI : MonoBehaviour
{
    public static string Text;
    public static float TimeRemaining;

    void OnGUI()
    {
        if (TimeRemaining <= 0f)
            return;

        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 24;
        style.normal.textColor = Color.white;

        GUI.Label(
            new Rect(20, 20, 600, 40),
            Text,
            style
        );
    }

    void Update()
    {
        TimeRemaining -= Time.deltaTime;
    }
}

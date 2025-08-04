using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class SnapperTool : EditorWindow
{
    [MenuItem("Tools/Snapper Tool")]
    public static void OpenTool() => GetWindow<SnapperTool>();

    private float sliderValue = 0.5f;


    void OnEnable() => Selection.selectionChanged += Repaint;
    void OnDisable() => Selection.selectionChanged -= Repaint;

    public void OnGUI()
    {
        using (new EditorGUI.DisabledScope( Selection.gameObjects.Length == 0))
        {
            GUILayout.Label("Adjust the value", EditorStyles.boldLabel);

            float rawValue = GUILayout.HorizontalSlider(sliderValue, 0.25f, 2f);
            sliderValue = Mathf.Round(rawValue * 4f) / 4f;

            EditorGUILayout.Space(10);

            GUILayout.Label("Current Value: " + sliderValue.ToString("F2"));

            if (GUILayout.Button("Snap Selection"))
            {
                SnapSelection(sliderValue);
            }
        }
    }

    private void SnapSelection(float snapIncrement = 1f)
    {
        const string UNDO_STR_SNAP = "snap objects";
        foreach (GameObject go in Selection.gameObjects)
        {
            Undo.RecordObject(go.transform, UNDO_STR_SNAP);
            go.transform.position = go.transform.position.Round();
        }
    }

    private void SetSnapIncrement(float increment)
    {
        Debug.Log("Setting snap increment to: " + increment);
    }
}

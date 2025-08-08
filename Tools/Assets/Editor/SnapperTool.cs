using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;

public class SnapperTool : EditorWindow
{
    public enum GridType
    {
        Cartesian,
        Polar
    }

    [MenuItem("Tools/Snapper Tool")]
    public static void OpenTool() => GetWindow<SnapperTool>();

    private float sliderValue = 0.5f;

    // ---------- things to save
    public float gridSize = 1f;
    public GridType gridType = GridType.Cartesian;
    public int angularDivisions = 24;

    // ----------
    SerializedObject so;
    SerializedProperty propGridSize;
    SerializedProperty propGridType;
    SerializedProperty propAngularDivisions;
    // ----------

    const float TAU = 6.28318530718f;

    void OnEnable()
    {
        so = new SerializedObject(this);

        // ----------
        propGridSize = so.FindProperty("gridSize");
        propGridType = so.FindProperty("gridType");
        propAngularDivisions = so.FindProperty("angularDivisions");
        // ----------

        //load saved configuration 
        gridSize = EditorPrefs.GetFloat("SNAPPER_TOOL_gridSize", 1f);
        gridType = (GridType)EditorPrefs.GetInt("SNAPPER_TOOL_gridType", 0);
        angularDivisions = EditorPrefs.GetInt("SNAPPER_TOOL_gridSize", 24);

        Selection.selectionChanged += Repaint;
        SceneView.duringSceneGui += DuringSceneGUI;
    }

    void OnDisable()
    {
        //save current configuration 
        EditorPrefs.SetFloat("SNAPPER_TOOL_gridSize", gridSize);
        EditorPrefs.SetInt("SNAPPER_TOOL_gridType", (int)gridType);
        EditorPrefs.SetInt("SNAPPER_TOOL_gridSize", angularDivisions);

        Selection.selectionChanged -= Repaint;
        SceneView.duringSceneGui -= DuringSceneGUI;
    }

    public void OnGUI()
    {
        so.Update();        //don't forget this call!
        EditorGUILayout.PropertyField(propGridType);
        EditorGUILayout.PropertyField(propGridSize);
        if (gridType == GridType.Polar)
        {
            EditorGUILayout.PropertyField(propAngularDivisions);
            propAngularDivisions.intValue = Mathf.Max(4, propAngularDivisions.intValue);
        }

        so.ApplyModifiedProperties();


        using (new EditorGUI.DisabledScope( Selection.gameObjects.Length == 0))
        {
            /*
            GUILayout.Label("Adjust the value", EditorStyles.boldLabel);

            float rawValue = GUILayout.HorizontalSlider(sliderValue, 0.25f, 2f);
            sliderValue = Mathf.Round(rawValue * 4f) / 4f;

            EditorGUILayout.Space(10);

            GUILayout.Label("Current Value: " + sliderValue.ToString("F2"));
            */ 

            if (GUILayout.Button("Snap Selection"))
            {
                SnapSelection(sliderValue);
            }
        }
    }

    void DuringSceneGUI(SceneView sceneView)
    {
        Handles.zTest = CompareFunction.LessEqual;
        //Handles.DrawLine(Vector3.zero, Vector3.up);
        const float gridDrawExtent = 16;

        if (gridType == GridType.Cartesian)
            DrawGridCartesian(gridDrawExtent);
        else
            DrawGridPolar(gridDrawExtent);
    }

    void DrawGridPolar(float gridDrawExtent)
    {
        int ringCount = Mathf.RoundToInt(gridDrawExtent / gridSize);
        float radiusOuter = (ringCount - 1) * gridSize;

        //skip the first one, because it will have 0 radius 
        //radial grid (rings)
        for (int i = 1; i < ringCount; i++)
        {
            Handles.DrawWireDisc(Vector3.zero, Vector3.up, i * gridSize);
        }

        //angular grid (lines)
        for (int i = 0; i < angularDivisions; i++)
        {
            float t = i / (float)angularDivisions;
            float angRad = t * TAU;     //converted percentage turns to radians

            float x = Mathf.Cos(angRad);
            float y = Mathf.Sin(angRad);

            Vector3 dir = new Vector3(x, 0f, y);

            Handles.DrawAAPolyLine(Vector3.zero, dir * radiusOuter);
        }
    }

    void DrawGridCartesian(float gridDrawExtent)
    {
        int lineCount = Mathf.RoundToInt((gridDrawExtent * 2) / gridSize);

        if (lineCount % 2 == 0)
            lineCount++;

        int halfLineCount = lineCount / 2;

        for (int i = 0; i < lineCount; i++)
        {
            float intOffset = i - halfLineCount;
            float xCoord = intOffset * gridSize;
            float zCoord1 = halfLineCount * gridSize;
            float zCoord0 = -halfLineCount * gridSize;

            Vector3 p0 = new Vector3(xCoord, 0f, zCoord0);
            Vector3 p1 = new Vector3(xCoord, 0f, zCoord1);

            Handles.DrawAAPolyLine(p0, p1);

            p0 = new Vector3(zCoord0, 0f, xCoord);
            p1 = new Vector3(zCoord1, 0f, xCoord);
            Handles.DrawAAPolyLine(p0, p1);
        }
    }

    private void SnapSelection(float snapIncrement = 1f)
    {
        const string UNDO_STR_SNAP = "snap objects";
        foreach (GameObject go in Selection.gameObjects)
        {
            Undo.RecordObject(go.transform, UNDO_STR_SNAP);
            go.transform.position = GetSnappedPosition(go.transform.position);
                //go.transform.position.Round(gridSize);
        }
    }


    Vector3 GetSnappedPosition(Vector3 posOriginal)
    {
        //snapping the x and y
        if (gridType == GridType.Cartesian) 
            return posOriginal.Round(gridSize);

        //snapping the distance and angle 
        if (gridType == GridType.Polar)
        {
            //distance from the center of the object
            Vector2 vec = new Vector2(posOriginal.x, posOriginal.z);
            float dist = vec.magnitude;
            float distSnapped = dist.Round(gridSize);
            //-----

            //Atan2 takes a direction and gives an angle
            float angRad = Mathf.Atan2(vec.y, vec.x);   //this is 0 to TAU
            float angTurns = angRad / TAU; //this is 0 to 1
            float angTurnsSnapped = angTurns.Round(1f / angularDivisions);
            float angRadSnapped = angTurnsSnapped * TAU;

            Vector2 dirSnapped = new Vector2(Mathf.Cos(angRadSnapped), Mathf.Sin(angRadSnapped));
            Vector2 snappedVec = dirSnapped * distSnapped;

            return new Vector3(snappedVec.x, posOriginal.y, snappedVec.y);
        }

        return default;
    }
}

/*
   private void SetSnapIncrement(float increment)
   {
       Debug.Log("Setting snap increment to: " + increment);
   } */

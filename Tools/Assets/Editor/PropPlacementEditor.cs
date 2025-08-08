using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;

public class PropPlacementEditor : EditorWindow
{
    [MenuItem("Tools/Prop Placement")]
    public static void OpenPropTool() => GetWindow<PropPlacementEditor>();

    public float radius = 2f;
    public int spawnCount = 8;

    SerializedObject so;
    SerializedProperty propRadius;
    SerializedProperty propSpawnCount;

    private Vector2[] randomPoints; //this doesn't need to be serialized because they don't need to be saved! :D

    private void OnEnable()
    {
        so = new SerializedObject(this);
        propRadius = so.FindProperty("radius");
        propSpawnCount = so.FindProperty("spawnCount");

        SceneView.duringSceneGui += DuringSceneGUI;
        GenerateRandomPoints();
    }

    private void OnDisable() => SceneView.duringSceneGui -= DuringSceneGUI;

    private void OnGUI()
    {
        so.Update();

        EditorGUILayout.PropertyField(propRadius);
        propRadius.floatValue = propRadius.floatValue.AtLeast(1);
        EditorGUILayout.PropertyField(propSpawnCount);
        propSpawnCount.intValue = propSpawnCount.intValue.AtLeast(1);

        //this is what actually changes thing in the editor view in realtime.
        //like if you change the radius in the editor window, the radius will draw accordingly in the scene view
        if (so.ApplyModifiedProperties())
        {
            GenerateRandomPoints();
            SceneView.RepaintAll(); //this repaints the scene immediately
        }

        //if clicked left mouse button in the editor window
        if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
        {
            GUI.FocusControl(null);
            Repaint();
        }
    }

    //this is called per scene view that you have open
    void DuringSceneGUI(SceneView sceneView)
    {
        Handles.zTest = CompareFunction.LessEqual;

        Transform camTransform = sceneView.camera.transform;

        if (Event.current.type == EventType.MouseMove)
        {
            sceneView.Repaint();
        }

        bool holdingAlt = (Event.current.modifiers & EventModifiers.Alt) != 0;  //modifiers is a bitfield
        //change radius with scroll wheel 
        if (holdingAlt == true && Event.current.type == EventType.ScrollWheel)
        {
            float scrollDirection = Mathf.Sign(Event.current.delta.y);  //sign makes sure it's -1, 1 or 0
            so.Update();

            propRadius.floatValue += scrollDirection;

            so.ApplyModifiedProperties();
            Repaint(); //updates the editor window 

            Event.current.Use();    //this consumes the event, don't let it fall through 
        }

        Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
        //Ray ray = new Ray(camTransform.position, camTransform.forward);

        if (Physics.Raycast (ray, out RaycastHit hit))
        {
            //----- setting up tangent space ----- 
            Vector3 hitNormal = hit.normal;
            Vector3 hitTangent = Vector3.Cross(hitNormal, camTransform.up).normalized;
            Vector3 hitBitangent = Vector3.Cross(hitNormal, hitTangent);

            //----- drawing the points inside of the sphere ----- 
            foreach (Vector2 randomPoint in randomPoints)
            {
                Vector3 rayOrigin = hit.point + (hitTangent * randomPoint.x + hitBitangent * randomPoint.y) * radius;
                rayOrigin += hitNormal * 2; //offset margin 

                Vector3 rayDirection = -hit.normal; //rayDirection is negated version of the normal
                Ray ptRay = new Ray(rayOrigin, rayDirection);

                if (Physics.Raycast(ptRay, out RaycastHit ptHit))
                {
                    DrawSphere(ptHit.point);
                    Handles.DrawAAPolyLine(ptHit.point, ptHit.point + ptHit.normal);
                }
            }

            // ----- drawing the line and radius -----
            Handles.color = Color.red;
            Handles.DrawAAPolyLine(1, hit.point, hit.point + hitTangent);
            Handles.color = Color.green;
            Handles.DrawAAPolyLine(1, hit.point, hit.point + hitBitangent);
            Handles.color = Color.blue;
            Handles.DrawAAPolyLine(1, hit.point, hit.point + hitNormal);

            Handles.DrawWireDisc(hit.point, hit.normal, radius);
        }
    }

    private void GenerateRandomPoints()
    {
        randomPoints = new Vector2[spawnCount];

        for(int i = 0; i < spawnCount; i++)
        {
            randomPoints[i] = Random.insideUnitCircle;  //random point inside of a unit circle
        }
    }

    private void DrawSphere(Vector3 pos)
    {
        Handles.SphereHandleCap(-1, pos, Quaternion.identity, 0.1f, EventType.Repaint);
    }
}

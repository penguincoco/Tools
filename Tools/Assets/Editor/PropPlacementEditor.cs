using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using System.Linq;

public class PropPlacementEditor : EditorWindow
{
    [MenuItem("Tools/Prop Placement")]
    public static void OpenPropTool() => GetWindow<PropPlacementEditor>();

    public float radius = 2f;
    public int spawnCount = 8;
    public GameObject spawnPrefab = null;
    public Material previewMaterial;

    SerializedObject so;
    SerializedProperty propRadius;
    SerializedProperty propSpawnCount;
    SerializedProperty propSpawnPrefab;
    SerializedProperty propPreviewMaterial;

    public struct Data
    {
        public Vector2 pointInDisc;
        public float randomAngleDegrees;

        public void SetRandomValues()
        {
            pointInDisc = Random.insideUnitCircle;
            randomAngleDegrees = Random.value * 360;
        }
    }

    private Data[] randomPoints; //this doesn't need to be serialized because they don't need to be saved! :D
    GameObject[] prefabs;

    private void OnEnable()
    {
        so = new SerializedObject(this);
        propRadius = so.FindProperty("radius");
        propSpawnCount = so.FindProperty("spawnCount");
        propSpawnPrefab = so.FindProperty("spawnPrefab");
        propPreviewMaterial = so.FindProperty("previewMaterial");

        SceneView.duringSceneGui += DuringSceneGUI;
        GenerateRandomPoints();

        //load prefabs
        string[] guids = AssetDatabase.FindAssets("t:prefab", new[] { "Assets/Prefabs" });
        IEnumerable<string> paths = guids.Select(AssetDatabase.GUIDToAssetPath);
        prefabs = paths.Select(AssetDatabase.LoadAssetAtPath<GameObject>).ToArray();

        /*
        foreach (string path in paths)
            Debug.Log(path);
        */
    }

    private void OnDisable() => SceneView.duringSceneGui -= DuringSceneGUI;

    //OnGUI is the UI of the editor window, but not the scene view
    private void OnGUI()
    {
        so.Update();

        EditorGUILayout.PropertyField(propRadius);
        propRadius.floatValue = propRadius.floatValue.AtLeast(1);
        EditorGUILayout.PropertyField(propSpawnCount);
        propSpawnCount.intValue = propSpawnCount.intValue.AtLeast(1);
        EditorGUILayout.PropertyField(propSpawnPrefab);
        EditorGUILayout.PropertyField(propPreviewMaterial);

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
        // ---------- Editor buttons for changing which prefab to spawn ----------
        Handles.BeginGUI();

        Rect rect = new Rect(8, 8, 64, 64);

        foreach(GameObject prefab in prefabs)
        {
            Texture icon = AssetPreview.GetAssetPreview(prefab);

            if (GUI.Button(rect, new GUIContent(icon)))
            {
                spawnPrefab = prefab;
            }
            rect.y += rect.height + 2;  //UI, it's from the top left and += meanss drawing downwards
        }

        Handles.EndGUI();

        // ---------- End editor buttons for changing which prefab to spawn ----------

        Handles.zTest = CompareFunction.LessEqual;

        Transform camTransform = sceneView.camera.transform;

        if (Event.current.type == EventType.MouseMove)
            sceneView.Repaint();

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

            List<Pose> hitPoses = new List<Pose>();

            Ray GetTangentRay(Vector2 tangentSpacePos)
            {
                Vector3 rayOrigin = hit.point + (hitTangent * tangentSpacePos.x + hitBitangent * tangentSpacePos.y) * radius;
                rayOrigin += hitNormal * 2; //offset margin 
                Vector3 rayDirection = -hit.normal; //rayDirection is negated version of the normal

                return new Ray(rayOrigin, rayDirection);
            }

            //----- drawing the points inside of the sphere ----- 
            foreach (Data rndDataPoint in randomPoints)
            {
                Ray ptRay = GetTangentRay(rndDataPoint.pointInDisc);

                if (Physics.Raycast(ptRay, out RaycastHit ptHit))
                {
                    //Quaternion rot = Quaternion.Euler(90f, 0f, 0f) * Quaternion.LookRotation(hitPoint.normal); //this doesn't work because order matters when multiplying Quaternions!
                    //calculate rotation and assign to pose together with position 
                    float randAngDeg = Random.value * 360;
                    Quaternion randRot = Quaternion.Euler(0f, 0f, rndDataPoint.randomAngleDegrees);
                    Quaternion rot = Quaternion.LookRotation(ptHit.normal) * (randRot * Quaternion.Euler(90f, 0f, 0f));

                    //if the point hits, add it to hitPoints list 
                    Pose pose = new Pose(ptHit.point, rot);
                    hitPoses.Add(pose);

                    DrawSphere(ptHit.point);
                    Handles.DrawAAPolyLine(ptHit.point, ptHit.point + ptHit.normal);

                    // ---------- preview the object about to be spawned ----------
                    //pose matrix
                    if (spawnPrefab != null)
                    {
                        Matrix4x4 poseToWorldMatrix = Matrix4x4.TRS(pose.position, pose.rotation, Vector3.one);

                        MeshFilter[] filters = spawnPrefab.GetComponentsInChildren<MeshFilter>();

                        foreach (MeshFilter filter in filters)
                        {
                            Matrix4x4 childToPoseMatrix = filter.transform.localToWorldMatrix;
                            Matrix4x4 childToWorldMatrix = poseToWorldMatrix * childToPoseMatrix;

                            Mesh mesh = filter.sharedMesh;
                            //Material mat = filter.GetComponent<MeshRenderer>().sharedMaterial;
                            //mat.SetPass(0);
                            //Material mat = spawnPrefab.GetComponent<MeshRenderer>().sharedMaterial;
                            previewMaterial.SetPass(0);
                            Graphics.DrawMeshNow(mesh, childToWorldMatrix);
                        }
                    }

                    /*
                    Mesh mesh = spawnPrefab.GetComponent<MeshFilter>().sharedMesh;
                    //Material mat = spawnPrefab.GetComponent<MeshRenderer>().sharedMaterial;
                    previewMaterial.SetPass(0);
                    Graphics.DrawMeshNow(mesh, pose.position, pose.rotation);
                    */
                }
            }

            //when you press space, spawn items! 
            //this is in UI, need to do input system for UI, not Input.GetKey
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Space)
            {
                TrySpawnObjects(hitPoses);
            }

            // ----- drawing the line and radius -----
            Handles.color = Color.red;
            Handles.DrawAAPolyLine(1, hit.point, hit.point + hitTangent);
            Handles.color = Color.green;
            Handles.DrawAAPolyLine(1, hit.point, hit.point + hitBitangent);
            Handles.color = Color.blue;
            Handles.DrawAAPolyLine(1, hit.point, hit.point + hitNormal);

            const int circleDetail = 64;
            Vector3[] circlePoints = new Vector3[circleDetail];
            for(int i = 0; i < circleDetail; i++)
            {

            }

            Handles.DrawWireDisc(hit.point, hit.normal, radius);
        }
    }

    void TrySpawnObjects(List<Pose> poses)
    {
        if (spawnPrefab == null)
            return;

        foreach (Pose pose in poses)
        {
            //spawn prefab
            GameObject spawnedPrefab = (GameObject)PrefabUtility.InstantiatePrefab(spawnPrefab);
            Undo.RegisterCreatedObjectUndo(spawnedPrefab, "Spawn Objects");
            spawnedPrefab.transform.position = pose.position;
            spawnedPrefab.transform.rotation = pose.rotation;
        }
        //update the points
        GenerateRandomPoints();
    }

    private void GenerateRandomPoints()
    {
        randomPoints = new Data[spawnCount];

        for(int i = 0; i < spawnCount; i++)
        {
            randomPoints[i].SetRandomValues();  //random point inside of a unit circle
        }
    }

    private void DrawSphere(Vector3 pos)
    {
        Handles.SphereHandleCap(-1, pos, Quaternion.identity, 0.1f, EventType.Repaint);
    }
}

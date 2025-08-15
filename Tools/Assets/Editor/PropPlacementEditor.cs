using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using System.Linq;
using Random = UnityEngine.Random;

//passed by copies of the value, not a reference
//for numbers, you don't want to refer to the original
public struct SpawnData
{
    public Vector2 pointInDisc;
    public float randomAngleDegrees;
    public GameObject prefab;

    public void SetRandomValues(List<GameObject> prefabs)
    {
        pointInDisc = Random.insideUnitCircle;
        randomAngleDegrees = Random.value * 360;
        prefab = prefabs.Count == 0 ? null : prefabs[Random.Range(0, prefabs.Count)];
    }
}

//classes refer to an object 
public class SpawnPoint
{
    public SpawnData spawnData;
    public Vector3 position;
    public Quaternion rotation;
    public bool isValid = false;

    public Vector3 up => rotation * Vector3.up;

    public SpawnPoint(Vector3 position, Quaternion rotation, SpawnData spawnData)
    {
        this.spawnData = spawnData;
        this.position = position;
        this.rotation = rotation;

        //check if the object's location is valid. e.g. it shouldn't interpenetrate with an object above it? 
        //do a raycast to check if it hits within a distance :0 

        if (spawnData.prefab != null)
        {
            SpawnablePrefab spawnablePrefab = spawnData.prefab.GetComponent<SpawnablePrefab>();
            if (spawnablePrefab == null)
                isValid = true;
            else
            {
                float h = spawnablePrefab.height;
                Ray ray = new Ray(position, up);

                isValid = Physics.Raycast(ray, h) == false;
            }
        }
    }
}

public class PropPlacementEditor : EditorWindow
{
    [MenuItem("Tools/Prop Placement")]
    public static void OpenPropTool() => GetWindow<PropPlacementEditor>();

    public float radius = 2f;
    public int spawnCount = 8;
    public GameObject spawnPrefab = null;

    SerializedObject so;
    SerializedProperty propRadius;
    SerializedProperty propSpawnCount;
    SerializedProperty propSpawnPrefab;
    //SerializedProperty propPreviewMaterial;

    SpawnData[] spawnDataPoints;
    //private SpawnData[] randomPoints; //this doesn't need to be serialized because they don't need to be saved! :D
    GameObject[] prefabs;
    List<GameObject> spawnPrefabs;

    Material materialInvalid;

    [SerializeField] bool[] prefabSelectionStates;    //don't drop data when recompiling scripts

    private void OnEnable()
    {
        so = new SerializedObject(this);
        propRadius = so.FindProperty("radius");
        propSpawnCount = so.FindProperty("spawnCount");
        propSpawnPrefab = so.FindProperty("spawnPrefab");
        //propPreviewMaterial = so.FindProperty("previewMaterial");

        SceneView.duringSceneGui += DuringSceneGUI;
        spawnPrefabs = new List<GameObject>(); 

        GenerateRandomPoints();

        // ---------- Create the material ----------
        //the path is the path to the Shader, not the path in the folders
        Shader sh = Shader.Find("Unlit/InvalidSpawn");
        materialInvalid = new Material(sh);
        // ---------- End Create the Material ----------

        //load prefabs
        string[] guids = AssetDatabase.FindAssets("t:prefab", new[] { "Assets/Prefabs" });
        IEnumerable<string> paths = guids.Select(AssetDatabase.GUIDToAssetPath);
        prefabs = paths.Select(AssetDatabase.LoadAssetAtPath<GameObject>).ToArray();

        if (prefabSelectionStates == null || prefabSelectionStates.Length != prefabs.Length)
        {
            prefabSelectionStates = new bool[prefabs.Length];
        }

        /*
        foreach (string path in paths)
            Debug.Log(path);
        */
    }

    private void OnDisable()
    {
        //delete the material when the window is closed, because an asset was created 
        DestroyImmediate(materialInvalid);
        SceneView.duringSceneGui -= DuringSceneGUI;
    }

    //OnGUI is the UI of the editor window, but not the scene view
    private void OnGUI()
    {
        so.Update();

        EditorGUILayout.PropertyField(propRadius);
        propRadius.floatValue = propRadius.floatValue.AtLeast(1);
        EditorGUILayout.PropertyField(propSpawnCount);
        propSpawnCount.intValue = propSpawnCount.intValue.AtLeast(1);
        EditorGUILayout.PropertyField(propSpawnPrefab);
        //EditorGUILayout.PropertyField(propPreviewMaterial);

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

        for(int i = 0; i < prefabs.Length; i++)
        {
            GameObject prefab = prefabs[i];
            Texture icon = AssetPreview.GetAssetPreview(prefab);

            EditorGUI.BeginChangeCheck();
            //GUI.Button(rect, new GUIContent(icon))
            prefabSelectionStates[i] = GUI.Toggle(rect, prefabSelectionStates[i], new GUIContent(icon));
            if (EditorGUI.EndChangeCheck())
            {
                spawnPrefabs.Clear();

                for (int j = 0; j < prefabs.Length; j++)
                    if (prefabSelectionStates[j])
                        spawnPrefabs.Add(prefabs[j]);

                GenerateRandomPoints();
            }
            rect.y += rect.height + 2;  //UI, it's from the top left and += meanss drawing downwards
        }

        Handles.EndGUI();

        // ---------- End editor buttons for changing which prefab to spawn ----------

        Handles.zTest = CompareFunction.LessEqual;
        Transform camTransform = sceneView.camera.transform;

        if (Event.current.type == EventType.MouseMove)
            sceneView.Repaint();

        //change radius with scroll wheel 
        bool holdingAlt = (Event.current.modifiers & EventModifiers.Alt) != 0;  //modifiers is a bitfield
        if (holdingAlt == true && Event.current.type == EventType.ScrollWheel)
        {
            float scrollDirection = Mathf.Sign(Event.current.delta.y);  //sign makes sure it's -1, 1 or 0
            so.Update();

            propRadius.floatValue += scrollDirection;

            so.ApplyModifiedProperties();
            Repaint(); //updates the editor window 

            Event.current.Use();    //this consumes the event, don't let it fall through 
        }

        //when you press space, spawn items! 
        //this is in UI, need to do input system for UI, not Input.GetKey
        if (TryRaycastFromCamera(camTransform.up, out Matrix4x4 tangentToWorld))
        {
            List<SpawnPoint> spawnPoints = GetSpawnPoints(tangentToWorld);

            if (Event.current.type == EventType.Repaint)
            {
                DrawCircleRegion(tangentToWorld);
                DrawSpawnPreviews(spawnPoints, sceneView.camera);
            }

            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Space)
                TrySpawnObjects(spawnPoints);
        }
    }

    void TrySpawnObjects(List<SpawnPoint> spawnPoints)
    {
        //this means we have no prefabs to spawn selected
        if (spawnPrefabs.Count == 0)
            return;

        foreach (SpawnPoint spawnPoint in spawnPoints)
        {
            if (spawnPoint.isValid == false)
                continue;

            //spawn prefab
            GameObject spawnedPrefab = (GameObject)PrefabUtility.InstantiatePrefab(spawnPoint.spawnData.prefab);
            Undo.RegisterCreatedObjectUndo(spawnedPrefab, "Spawn Objects");
            spawnedPrefab.transform.position = spawnPoint.position;
            spawnedPrefab.transform.rotation = spawnPoint.rotation;
        }
        //update the points
        GenerateRandomPoints();
    }

    private void GenerateRandomPoints()
    {
        spawnDataPoints = new SpawnData[spawnCount];

        for(int i = 0; i < spawnCount; i++)
            spawnDataPoints[i].SetRandomValues(spawnPrefabs);  //random point inside of a unit circle
    }

    private void DrawSphere(Vector3 pos)
    {
        Handles.SphereHandleCap(-1, pos, Quaternion.identity, 0.1f, EventType.Repaint);
    }

    bool TryRaycastFromCamera(Vector2 cameraUp, out Matrix4x4 tangentToWorldMtx)
    {
        Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            // setting up tangent space
            Vector3 hitNormal = hit.normal;
            Vector3 hitTangent = Vector3.Cross(hitNormal, cameraUp).normalized;
            Vector3 hitBitangent = Vector3.Cross(hitNormal, hitTangent);
            tangentToWorldMtx = Matrix4x4.TRS(hit.point, Quaternion.LookRotation(hitNormal, hitBitangent), Vector3.one);
            return true;
        }

        tangentToWorldMtx = default;
        return false;
    }

    void DrawSpawnPreviews(List<SpawnPoint> spawnPoints, Camera cam)
    {
        foreach (SpawnPoint spawnPoint in spawnPoints)
        {
            if (spawnPoint.spawnData.prefab != null)
            {
                // draw preview of all meshes in the prefab
                Matrix4x4 poseToWorld = Matrix4x4.TRS(spawnPoint.position, spawnPoint.rotation, Vector3.one);
                DrawPrefab(spawnPoint.spawnData.prefab, poseToWorld, cam, spawnPoint.isValid);
            }
            else 
            {
                // prefab missing, draw sphere and normal on surface instead
                Handles.SphereHandleCap(-1, spawnPoint.position, Quaternion.identity, 0.1f, EventType.Repaint);
                Handles.DrawAAPolyLine(spawnPoint.position, spawnPoint.position + spawnPoint.up);
            }
        }
    }

    void DrawPrefab(GameObject prefab, Matrix4x4 poseToWorld, Camera cam, bool valid)
    {
        MeshFilter[] filters = prefab.GetComponentsInChildren<MeshFilter>();
        foreach (MeshFilter filter in filters)
        {
            Matrix4x4 childToPose = filter.transform.localToWorldMatrix;
            Matrix4x4 childToWorldMtx = poseToWorld * childToPose;
            Mesh mesh = filter.sharedMesh;
            Material mat = valid ? filter.GetComponent<MeshRenderer>().sharedMaterial : materialInvalid;
            Graphics.DrawMesh(mesh, childToWorldMtx, mat, 0, cam);
        }
    }

    List<SpawnPoint> GetSpawnPoints(Matrix4x4 tangentToWorld)
    {
        List<SpawnPoint> hitPoses = new List<SpawnPoint>();

        foreach(SpawnData rndDataPoint in spawnDataPoints)
        {
            //create ray for this point 
            Ray ptRay = GetCircleRay(tangentToWorld, rndDataPoint.pointInDisc);
            if (Physics.Raycast(ptRay, out RaycastHit ptHit))
            {
                Quaternion randRot = Quaternion.Euler(0f, 0f, rndDataPoint.randomAngleDegrees);
                Quaternion rot = Quaternion.LookRotation(ptHit.normal) * (randRot * Quaternion.Euler(90f, 0f, 0f));
                SpawnPoint spawnPoint = new SpawnPoint(ptHit.point, rot, rndDataPoint);
                hitPoses.Add(spawnPoint);
            }
        }

        return hitPoses;
    }

    /*
    List<SpawnPoint> GetSpawnPoints(Matrix4x4 tangentToWorld)
    {
        List<SpawnPoint> hitSpawnPoints = new List<SpawnPoint>();
        foreach (SpawnData rndDataPoint in spawnDataPoints)
        {
            // create ray for this point
            Ray ptRay = GetCircleRay(tangentToWorld, rndDataPoint.pointInDisc);
            // raycast to find point on surface
            if (Physics.Raycast(ptRay, out RaycastHit ptHit))
            {
                // calculate rotation and assign to pose together with position
                Quaternion randRot = Quaternion.Euler(0f, 0f, rndDataPoint.randAngleDeg);
                Quaternion rot = Quaternion.LookRotation(ptHit.normal) * (randRot * Quaternion.Euler(90f, 0f, 0f));
                SpawnPoint spawnPoint = new SpawnPoint(ptHit.point, rot, rndDataPoint);
                hitSpawnPoints.Add(spawnPoint);
            }
        }

        return hitSpawnPoints;
    }
    */

    Ray GetCircleRay(Matrix4x4 tangentToWorld, Vector2 pointInCircle)
    {
        Vector3 normal = tangentToWorld.MultiplyVector(Vector3.forward);
        Vector3 rayOrigin = tangentToWorld.MultiplyPoint3x4(pointInCircle * radius);
        rayOrigin += normal * 2; // offset margin thing
        Vector3 rayDirection = -normal;
        return new Ray(rayOrigin, rayDirection);
    }

    void DrawCircleRegion(Matrix4x4 localToWorld)
    {
        DrawAxes(localToWorld);
        // draw circle adapted to the terrain
        const int circleDetail = 128;
        Vector3[] ringPoints = new Vector3[circleDetail];
        for (int i = 0; i < circleDetail; i++)
        {
            float t = i / ((float)circleDetail - 1); // go back to 0/1 position
            const float TAU = 6.28318530718f;
            float angRad = t * TAU;
            Vector2 dir = new Vector2(Mathf.Cos(angRad), Mathf.Sin(angRad));
            Ray r = GetCircleRay(localToWorld, dir);
            if (Physics.Raycast(r, out RaycastHit cHit))
            {
                ringPoints[i] = cHit.point + cHit.normal * 0.02f;
            }
            else
            {
                ringPoints[i] = r.origin;
            }
        }

        Handles.DrawAAPolyLine(ringPoints);
    }

    void DrawAxes(Matrix4x4 localToWorld)
    {
        Vector3 pt = localToWorld.MultiplyPoint3x4(Vector3.zero);
        Handles.color = Color.red;
        Handles.DrawAAPolyLine(6, pt, pt + localToWorld.MultiplyVector(Vector3.right));
        Handles.color = Color.green;
        Handles.DrawAAPolyLine(6, pt, pt + localToWorld.MultiplyVector(Vector3.up));
        Handles.color = Color.blue;
        Handles.DrawAAPolyLine(6, pt, pt + localToWorld.MultiplyVector(Vector3.forward));
    }
}

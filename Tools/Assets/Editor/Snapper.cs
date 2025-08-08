using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public static class Snapper
{
    const string UNDO_STR_SNAP = "snap objects";

    [MenuItem("Edit/Snap Selected Object %&S", isValidateFunction: true)]
    public static bool SnapValidate() 
    {
        //checking if you have anything selected
        return Selection.gameObjects.Length > 0;
    }

    [MenuItem("Edit/Snap Selected Object %&S")]
    public static void Snap()
    {
        foreach (GameObject go in Selection.gameObjects)
        {
            //Undo.RecordObject() is not free! Only do this on things you will be editing!
            Undo.RecordObject(go.transform, UNDO_STR_SNAP);
            //this snaps the position
            go.transform.position = go.transform.position.Round();
        }
    }
}

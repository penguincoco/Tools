using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CanEditMultipleObjects]
[CustomEditor(typeof(BarrelType))]
public class BarrelTypeEditor : Editor
{
    SerializedObject so;
    SerializedProperty propRadius;
    SerializedProperty propDamage;
    SerializedProperty propColor;

    //OnEnable is like Awake for inspector
    private void OnEnable()
    {
        so = serializedObject;
        propRadius = so.FindProperty("radius");
        propDamage = so.FindProperty("damage");
        propColor = so.FindProperty("color");
    }

    public override void OnInspectorGUI()
    {
        so.Update();

        EditorGUILayout.PropertyField(propRadius);
        EditorGUILayout.PropertyField(propDamage);
        EditorGUILayout.PropertyField(propColor);

        if (so.ApplyModifiedProperties())
        {
            ExplosiveBarrelManager.UpdateAllBarrelColors();
        }

        so.ApplyModifiedProperties();
    }
}

/*
 * 
 *  //explicit positioning using Rect
    //GUI, EditorGUI,
    //
    //implicit positioning, auto-layout
    //GUILayout, EditorGUILayout
    public enum Things
    {
        Bleep, Bloop, Blap
    }
    Things thing;

 * 
        //GUILayout.BeginHorizontal();

        using (new GUILayout.HorizontalScope()) //creates a type and when it leaves the scope, it calls a function inside HorizontalScope() that disposes of the function
        {
            GUILayout.Label("Things: ", GUI.skin.button);
            GUILayout.Label("Things: ", EditorStyles.toolbar);

            if (GUILayout.Button("Do a thing"))
            {
            }
            thing = (Things)EditorGUILayout.EnumPopup((System.Enum)thing);
        }

        EditorGUILayout.ObjectField("Assign here: ", null, typeof(Transform), true);

        //GUILayout.BeginHorizontal();
*/

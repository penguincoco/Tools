using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(BarrelType))]
public class BarrelTypeEditor : Editor
{
    public override void OnInspectorGUI()
    {

        //this has ignored everything about serialization! We're modifying the raw values immediately. 
        //if you edit something, you need to make sure the data is marked as dirty (it's changed) 

        //undo doesn't work
        BarrelType barrel = target as BarrelType;
        barrel.radius = EditorGUILayout.FloatField("Radius ", barrel.radius);
        barrel.damage = EditorGUILayout.FloatField("Damage ", barrel.damage);
        barrel.color = EditorGUILayout.ColorField("Color ", barrel.color);
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

using System.Collections;
using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class ExplosiveBarrelManager : MonoBehaviour
{
    public static List<ExplosiveBarrel> allBarrels = new List<ExplosiveBarrel>();

    public static void UpdateAllBarrelColors()
    {
        foreach (ExplosiveBarrel barrel in allBarrels)
        {
            barrel.TryApplyColor();
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Handles.zTest = CompareFunction.LessEqual;

        foreach(ExplosiveBarrel barrel in allBarrels)
        {
            if (barrel.type == null)
                continue;

            Vector3 managerPos = transform.position;
            Vector3 barrelPos = barrel.transform.position;
            float halfHeight = (managerPos.y - barrelPos.y) * 0.5f;
            Vector3 offset = Vector3.up * halfHeight;

            Handles.DrawBezier(transform.position,
                barrelPos,
                managerPos - offset,
                barrelPos + offset,
                barrel.type.color,
                EditorGUIUtility.whiteTexture,
                1f
                );

            //Handles.DrawAAPolyLine(transform.position, barrel.transform.position);
        }

        Handles.color = Color.white;
    }
#endif
}

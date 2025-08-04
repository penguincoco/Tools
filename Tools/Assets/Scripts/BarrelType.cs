using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class BarrelType : ScriptableObject
{
    [Range(1f, 8f)]
    [SerializeField] public float radius = 1;

    [SerializeField] public float damage = 10;
    [SerializeField] public Color color = Color.red;
}

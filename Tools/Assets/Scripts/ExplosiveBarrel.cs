using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class ExplosiveBarrel : MonoBehaviour
{
    static readonly int shaderPropertyColor = Shader.PropertyToID("_BaseColor");    //this makes property assignments much faster, this is an optimization!

    [SerializeField] public BarrelType type;
    
    //every time you do an assembly reload, it will be null
    MaterialPropertyBlock mpb;
    public MaterialPropertyBlock Mpb
    {
        get
        {
            if (mpb == null)
                mpb = new MaterialPropertyBlock();
            return mpb;
        }
    }

    private void OnDrawGizmos()
    {
        if (type == null)
            return;

        Handles.color = type.color;
        //Gizmos.DrawWireSphere(transform.position, radius);
        Handles.DrawWireDisc(transform.position, transform.up, type.radius);
        Handles.color = Color.white;
    }

    private void OnValidate()
    {
        TryApplyColor();
    }

    private void OnEnable()
    {
        TryApplyColor();
        ExplosiveBarrelManager.allBarrels.Add(this);
    }
    private void OnDisable() => ExplosiveBarrelManager.allBarrels.Remove(this);

    public void TryApplyColor()
    {
        if (type == null)
            return;

        MeshRenderer renderer = GetComponent<MeshRenderer>();
        Mpb.SetColor(shaderPropertyColor, type.color);
        renderer.SetPropertyBlock(Mpb);
    }
}

/*
editor code: what is an asset, vs. what is data?
don't instantiate materials, and don't instantiate materials. We can get mesh leaks lol
a shit ton of meshes will get loaded into memory 
or meshes that leak into the scene, and get saved into the scene... 


Material Property Block

this call actually duplicates the material, assigns that material to the MeshRenderer, then sets the color of the duplicate.
this is a quirk of Unity lol. duplicated is an extra draw call, can no longer be batched with the other materials
this also leaks assets! creates multiple materials in the editor, doesn't clean up the materials when you leave the editor
duplicates the material
GetComponent<MeshRenderer>().material.color = Color.red;

sharedMaterial actually changes the material in the project. 
this can be dangerous because everyone will have different values for the material depending on what you're doing in the editor
modifies the asset
GetComponent<MeshRenderer>().sharedMaterial.color = Color.red;

GetComponent<MeshFilter>().mesh has the same issue as the material

make materials, but not leak materials into the scene
Shader shader = Shader.Find("Default/Diffuse");
Material mat = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
this means this asset will not be saved. If you leave the scene, it will unload the asset
*/
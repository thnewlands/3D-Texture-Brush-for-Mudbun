using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(SDFTextureCollection))]
public class SDFTextureCollectionEditor : Editor
{
    public override void OnInspectorGUI()
    {
        if (DrawDefaultInspector())
        {
            var collection = target as SDFTextureCollection;
            collection.Regenerate();
            Debug.Log("Values changed!");
        }
    }
}

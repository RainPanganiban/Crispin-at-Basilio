using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class MaterialToCelConverter : EditorWindow
{
    [MenuItem("Tools/Cel Shading/Convert Selected to Cel")]
    public static void ConvertSelected()
    {
        GameObject[] selectedObjects = Selection.gameObjects;
        if (selectedObjects.Length == 0)
        {
            Debug.LogWarning("Please select at least one GameObject with a Renderer.");
            return;
        }

        Shader celShader = Shader.Find("Custom/CelShader");
        if (celShader == null)
        {
            Debug.LogError("Could not find 'Custom/CelShader'. Make sure the shader is in your project.");
            return;
        }

        int count = 0;
        foreach (GameObject obj in selectedObjects)
        {
            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                Material[] materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    if (materials[i] == null) continue;
                    
                    // Skip if already cel shader
                    if (materials[i].shader == celShader) continue;

                    Undo.RecordObject(materials[i], "Convert to Cel Shader");
                    materials[i].shader = celShader;
                    count++;
                }
            }
        }

        Debug.Log($"Successfully converted {count} materials to Cel Shading!");
    }
}

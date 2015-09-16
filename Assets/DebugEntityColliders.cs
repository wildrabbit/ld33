using UnityEngine;
using System.Collections.Generic;

public class DebugEntityColliders : MonoBehaviour 
{
    static Material lineMaterial;
    static void CreateLineMaterial()
    {
        if (!lineMaterial)
        {
            // Unity has a built-in shader that is useful for drawing
            // simple colored things.
            var shader = Shader.Find("Hidden/Internal-Colored");
            lineMaterial = new Material(shader);
            lineMaterial.hideFlags = HideFlags.HideAndDontSave;
            // Turn on alpha blending
            lineMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            lineMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            // Turn backface culling off
            lineMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
            // Turn off depth writes
            lineMaterial.SetInt("_ZWrite", 0);
        }
    }
    // Update is called once per frame
	void Update () 
    {
	
	}

    void OnPostRender()
    {
        CreateLineMaterial();
        lineMaterial.SetPass(0);
        GL.PushMatrix();
        GL.LoadProjectionMatrix(Camera.main.projectionMatrix);
        GL.modelview = Camera.main.worldToCameraMatrix;

        List<Entity> entities = GameplayManager.Instance.AllEntities;
        for (int i = 0; i < entities.Count; ++i)
        {
            entities[i].DebugRender();
        }

        GL.PopMatrix();
    }
}

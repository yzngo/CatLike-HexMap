using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class PostProcessingConfig : MonoBehaviour
{
    public Material mat;
    // Start is called before the first frame update
    void Start()
    {
        mat.SetFloat("_Saturation", 0);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

public class CustomRenderPipeline : RenderPipeline
{
    protected override void Render(ScriptableRenderContext context, Camera[] cameras) {
        var cmd = new CommandBuffer();
        cmd.ClearRenderTarget(true, true, Color.red);
        
        // schedule command
        context.ExecuteCommandBuffer(cmd);
        cmd.Release();

        // execute command 
        context.Submit();
    }
}



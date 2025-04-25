using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class SSPRFeature : ScriptableRendererFeature
{
    public class SSPRRenderPass : ScriptableRenderPass
    {
        const string bufferName = "SSPR";
        const string kernelNameSSPR = "SSPRCompute";
        const string kernelNameSSPRHole = "SSPRHole";
        static int ssprTexID = Shader.PropertyToID("_SSPRTexture");
        static int heightBufferID = Shader.PropertyToID("_SSPRHeightBuffer");

        RenderTargetIdentifier depthTex;
        RenderTargetIdentifier colorTex;
        RenderTargetIdentifier ssprTarget;
        RenderTargetIdentifier heightBuffer;

        public Material[] materials;
        public ComputeShader compute;
        public int planHeight;
        int kernelSSPR;
        int kernelSSPRHole;

        // 构造函数传入shader和计算着色器脚本
        public SSPRRenderPass(Material[] materials, ComputeShader compute, int planHeight, RenderPassEvent evt)
        {
            this.materials = materials;
            this.compute = compute;
            this.planHeight = planHeight;
            this.renderPassEvent = evt;
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            var render = renderingData.cameraData.renderer;
            kernelSSPR = compute.FindKernel(kernelNameSSPR);
            kernelSSPRHole = compute.FindKernel(kernelNameSSPRHole);

            depthTex = render.cameraDepthTarget;
            colorTex = render.cameraColorTarget;

            var cameraDescriptor = renderingData.cameraData.cameraTargetDescriptor;
            cameraDescriptor.enableRandomWrite = true;
            cameraDescriptor.msaaSamples = 1;
            cameraDescriptor.sRGB = false;
            cameraDescriptor.colorFormat = RenderTextureFormat.ARGB32;

            // 创建临时RT并获取其标识符
            cmd.GetTemporaryRT(ssprTexID, cameraDescriptor, FilterMode.Bilinear);
            ssprTarget = new RenderTargetIdentifier(ssprTexID);
            cmd.GetTemporaryRT(heightBufferID, cameraDescriptor, FilterMode.Bilinear);
            heightBuffer = new RenderTargetIdentifier(heightBufferID);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get(bufferName);
            SSPRCompute(cmd, ref renderingData);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            cmd.ReleaseTemporaryRT(ssprTexID); // 释放临时RT资源
            cmd.ReleaseTemporaryRT(heightBufferID);
        }

        void SSPRCompute(CommandBuffer cmd, ref RenderingData renderingData)
        {
            int width = renderingData.cameraData.camera.pixelWidth;
            int height = renderingData.cameraData.camera.pixelHeight;

            // 设置计算着色器参数
            cmd.SetComputeFloatParam(compute, "_height", planHeight);
            cmd.SetComputeVectorParam(compute, "_SSPRSize", new Vector4(width, height, 1 / (float)width, 1 / (float)height));
            cmd.SetComputeTextureParam(compute, kernelSSPR, "_CameraDepthTexture", depthTex);
            cmd.SetComputeTextureParam(compute, kernelSSPR, "_CameraOpaqueTexture", colorTex);
            cmd.SetComputeTextureParam(compute, kernelSSPR, "_SSPRHeightBuffer", heightBuffer);
            cmd.SetComputeTextureParam(compute, kernelSSPR, "_SSPRTexture", ssprTarget); // 使用RenderTargetIdentifier
            cmd.SetComputeTextureParam(compute, kernelSSPRHole, "_SSPRTexture", ssprTarget);

            // 计算线程组数量
            int threadGroupX = Mathf.CeilToInt(width / 8.0f);
            int threadGroupY = Mathf.CeilToInt(height / 8.0f);

            // 调度计算着色器
            cmd.DispatchCompute(compute, kernelSSPR, threadGroupX, threadGroupY, 1);
            cmd.DispatchCompute(compute, kernelSSPRHole, threadGroupX, threadGroupY, 1);

            // 正确设置全局贴图，使用同一个ID确保Shader能找到
            cmd.SetGlobalTexture(ssprTexID, ssprTarget);
        }
    }


    SSPRRenderPass ssprPass;
    public SSPRSettings settings;

    public override void Create()
    {
        ssprPass = new SSPRRenderPass(settings.materials, settings.compute,settings.height, RenderPassEvent.AfterRenderingOpaques);
    }
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(ssprPass);
    }

    [Serializable]
    public class SSPRSettings
    {
        public Material[] materials;
        public ComputeShader compute;
        public int height;

        public SSPRSettings(Material[] materials, ComputeShader compute, int height)
        {
            this.materials = materials;
            this.compute = compute;
            this.height = height;
            this.height = height;
        }
    }
}



using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;


//https://github.com/Unity-Technologies/UniversalRenderingExamples/blob/master/Assets/Scripts/Runtime/RenderPasses/BlitPass.cs
public class RayMarchingFeature : ScriptableRendererFeature {
	class RayMarchingPass : ScriptableRenderPass {
		readonly string commandBufferName;

		RenderTargetIdentifier source;

		readonly ComputeShader computeShader;
		RenderTexture target;
		RenderTargetIdentifier targetIdentifier;

		struct MarchedSphere {
			Vector3 position;
			float radius;
		}

		public RayMarchingPass(ComputeShader computeShader, string commandBufferName) {
			this.computeShader = computeShader;
			this.commandBufferName = commandBufferName;
		}

		public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor) {
			InitRenderTexture();
			
			targetIdentifier = new RenderTargetIdentifier(target);
			cmd.SetGlobalTexture("_RayMarchingTexture", targetIdentifier);
		}
		
		public void Setup(RenderTargetIdentifier source) => this.source = source;

		public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData) {
			CommandBuffer cmd = CommandBufferPool.Get(commandBufferName);
			
			SetShaderParameters(cmd, renderingData.cameraData.camera);
			
			cmd.ClearRenderTarget(false, true, Color.clear);
			cmd.SetComputeTextureParam(computeShader, 0, "Result", target);
			int threadGroupsX = Mathf.CeilToInt(Screen.width / 8.0f);
			int threadGroupsY = Mathf.CeilToInt(Screen.height / 8.0f);
			cmd.DispatchCompute(computeShader, 0, threadGroupsX, threadGroupsY, 1);

			cmd.Blit(targetIdentifier, source);

			context.ExecuteCommandBuffer(cmd);
			CommandBufferPool.Release(cmd);
		}

		public override void FrameCleanup(CommandBuffer cmd) {}
		
		void InitRenderTexture() {
			if (target == null || target.width != Screen.width || target.height != Screen.height) {
				if (target != null)
					target.Release();

				target = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear) {
					enableRandomWrite = true
				};
				target.Create();
			}
		}

		void SetShaderParameters(CommandBuffer cmd, Camera camera) {
			cmd.SetComputeMatrixParam(computeShader, "_CameraToWorld", camera.cameraToWorldMatrix);
			cmd.SetComputeMatrixParam(computeShader, "_CameraInverseProjection", camera.projectionMatrix.inverse);
			cmd.SetComputeFloatParam(computeShader, "_Time", Time.time);
		}
	}

	[System.Serializable]
	public struct RayMarchingSettings {
		public ComputeShader computeShader;
	}

	public RayMarchingSettings settings;
	
	RayMarchingPass rayMarchingPass;

	public override void Create() =>
		rayMarchingPass = new RayMarchingPass(settings.computeShader, name) {
			renderPassEvent = RenderPassEvent.AfterRendering
		};

	// Here you can inject one or multiple render passes in the renderer.
	// This method is called when setting up the renderer once per-camera.
	public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData) {
		rayMarchingPass.Setup(renderer.cameraColorTarget);
		renderer.EnqueuePass(rayMarchingPass);
	}
}
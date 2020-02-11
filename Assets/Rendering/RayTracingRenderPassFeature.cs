using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;


//https://github.com/Unity-Technologies/UniversalRenderingExamples/blob/master/Assets/Scripts/Runtime/RenderPasses/BlitPass.cs
public class RayTracingRenderPassFeature : ScriptableRendererFeature {
	class RayTracingPass : ScriptableRenderPass {
		string commandBufferName;
		//RenderTargetHandle temporaryColorTexture;
		//Material rayTraceMaterial;
		//int rayTraceMaterialPassIndex;
		Material addMaterial;

		RenderTargetIdentifier source;
		RenderTargetHandle destination;
		
		ComputeShader computeShader;
		RenderTexture target;
		RenderTargetIdentifier targetIdentifier;

		uint currentSample = 0;

		public RayTracingPass(ComputeShader computeShader, string commandBufferName, Material rayTraceMaterial, int rayTraceMaterialPassIndex, Material addMaterial) {
			this.computeShader = computeShader;
			this.commandBufferName = commandBufferName;
			this.addMaterial = addMaterial;
			//this.rayTraceMaterial = rayTraceMaterial;
			//this.rayTraceMaterialPassIndex = rayTraceMaterialPassIndex;

			//temporaryColorTexture.Init("_TemporaryColorTexture");
		}

		// This method is called before executing the render pass.
		// It can be used to configure render targets and their clear state. Also to create temporary render target textures.
		// When empty this render pass will render to the active camera render target.
		// You should never call CommandBuffer.SetRenderTarget. Instead call <c>ConfigureTarget</c> and <c>ConfigureClear</c>.
		// The render pipeline will ensure target setup and clearing happens in an performance manner.
		public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor) {
			InitRenderTexture();
			
			targetIdentifier = new RenderTargetIdentifier(target);
			cmd.SetGlobalTexture("_RayTracingTexture", targetIdentifier);
		}
		
		public void Setup(RenderTargetIdentifier source, RenderTargetHandle destination) {
			this.source = source;
			this.destination = destination;
		}

		// Here you can implement the rendering logic.
		// Use <c>ScriptableRenderContext</c> to issue drawing commands or execute command buffers
		// https://docs.unity3d.com/ScriptReference/Rendering.ScriptableRenderContext.html
		// You don't have to call ScriptableRenderContext.submit, the render pipeline will call it at specific points in the pipeline.
		public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData) {
			CommandBuffer cmd = CommandBufferPool.Get(commandBufferName);
			
			//cmd.GetTemporaryRT(temporaryColorTexture.id, renderingData.cameraData.cameraTargetDescriptor, FilterMode.Point);
			Camera camera = renderingData.cameraData.camera;
			SetShaderParameters(camera);
			if (camera.transform.hasChanged) {
				currentSample = 0;
				camera.transform.hasChanged = false;
			}
		
			computeShader.SetTexture(0, "Result", target);
			int threadGroupsX = Mathf.CeilToInt(Screen.width / 8.0f);
			int threadGroupsY = Mathf.CeilToInt(Screen.height / 8.0f);
			computeShader.Dispatch(0, threadGroupsX, threadGroupsY, 1);

			addMaterial.SetFloat("_Sample", currentSample);
			Blit(cmd, targetIdentifier, source, addMaterial);
			if (currentSample < 1000)
				currentSample++;


			context.ExecuteCommandBuffer(cmd);
			CommandBufferPool.Release(cmd);
		}

		/// Cleanup any allocated resources that were created during the execution of this render pass.
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

		void SetShaderParameters(Camera camera) {
			computeShader.SetMatrix("_CameraToWorld", camera.cameraToWorldMatrix);
			computeShader.SetMatrix("_CameraInverseProjection", camera.projectionMatrix.inverse);
			computeShader.SetVector("_PixelOffset", new Vector2(Random.value, Random.value));
			computeShader.SetFloat("_Time", (float)EditorApplication.timeSinceStartup);
		}
	}

	[System.Serializable]
	public struct RayTracingSettings {
		public Material rayTraceMaterial;
		public int rayTraceMaterialPassIndex;
		public ComputeShader computeShader;
	}

	public RayTracingSettings settings = new RayTracingSettings();
	
	RayTracingPass m_ScriptablePass;
	Material addMaterial;

	public override void Create() {
		if (addMaterial == null)
			addMaterial = new Material(Shader.Find("Hidden/AddShader"));
		m_ScriptablePass = new RayTracingPass(settings.computeShader, name, settings.rayTraceMaterial, settings.rayTraceMaterial.passCount - 1, addMaterial) {
			renderPassEvent = RenderPassEvent.AfterRendering
		};
	}

	// Here you can inject one or multiple render passes in the renderer.
	// This method is called when setting up the renderer once per-camera.
	public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData) {
		m_ScriptablePass.Setup(renderer.cameraColorTarget, RenderTargetHandle.CameraTarget);
		renderer.EnqueuePass(m_ScriptablePass);
	}
}
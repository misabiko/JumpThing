using System.Collections.Generic;
using UnityEngine;
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
		ComputeBuffer sphereBuffer;

		GameObject rayMarchObject;

		struct Sphere {
			public Vector3 position;
			public float radius;
		}

		List<Sphere> spheres = new List<Sphere>();

		public RayMarchingPass(ComputeShader computeShader, string commandBufferName, LayerMask layerMask) {
			this.computeShader = computeShader;
			this.commandBufferName = commandBufferName;
			
			rayMarchObject = GameObject.Find("Sphere");
			spheres.Add(new Sphere() {
				position = rayMarchObject.transform.position,
				radius = Mathf.Max(rayMarchObject.transform.localScale.x, rayMarchObject.transform.localScale.y, rayMarchObject.transform.localScale.z)
			});
		}

		public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor) {
			InitRenderTexture();

			targetIdentifier = new RenderTargetIdentifier(target);
			cmd.SetGlobalTexture("_RayMarchingTexture", targetIdentifier);
		}

		public void Setup(RenderTargetIdentifier source) => this.source = source;

		public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData) {
			CommandBuffer cmd = CommandBufferPool.Get(commandBufferName);

			//Copy camera's texture to target
			cmd.Blit(source, targetIdentifier);

			//Passing target to the compute shader and drawing on it
			SetShaderParameters(cmd, renderingData.cameraData.camera);
			cmd.SetComputeTextureParam(computeShader, 0, "Result", targetIdentifier);
			int threadGroupsX = Mathf.CeilToInt(Screen.width / 8.0f);
			int threadGroupsY = Mathf.CeilToInt(Screen.height / 8.0f);
			cmd.DispatchCompute(computeShader, 0, threadGroupsX, threadGroupsY, 1);

			//Then copying target back to camera's texture
			cmd.Blit(targetIdentifier, source);

			context.ExecuteCommandBuffer(cmd);
			CommandBufferPool.Release(cmd);
		}

		public override void FrameCleanup(CommandBuffer cmd) {
			sphereBuffer.Dispose();
		}

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
			
			//float3 + float = 4, 4 * 4 byte = 16 as stride
			sphereBuffer = new ComputeBuffer(spheres.Count, 16);
			sphereBuffer.SetData(spheres);
			cmd.SetComputeBufferParam(computeShader, 0, "_Spheres", sphereBuffer);
			
			cmd.SetComputeVectorParam(computeShader, "_SomeSphere", rayMarchObject.transform.position);
			cmd.SetComputeFloatParam(computeShader, "_SomeSphereRadius", Mathf.Max(rayMarchObject.transform.localScale.x, rayMarchObject.transform.localScale.y, rayMarchObject.transform.localScale.z) / 2f);
		}
	}

	[System.Serializable]
	public struct RayMarchingSettings {
		public ComputeShader computeShader;
		public LayerMask layerMask;
	}

	public RayMarchingSettings settings;

	RayMarchingPass rayMarchingPass;

	public override void Create() =>
		rayMarchingPass = new RayMarchingPass(settings.computeShader, name, settings.layerMask) {
			renderPassEvent = RenderPassEvent.AfterRendering
		};

	// Here you can inject one or multiple render passes in the renderer.
	// This method is called when setting up the renderer once per-camera.
	public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData) {
		rayMarchingPass.Setup(renderer.cameraColorTarget);
		renderer.EnqueuePass(rayMarchingPass);
	}
}
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class RayMarchingFeature : ScriptableRendererFeature {
	class RayMarchingPass : ScriptableRenderPass {
		readonly string commandBufferName;

		RenderTargetIdentifier source;

		readonly ComputeShader computeShader;
		RenderTexture target;
		RenderTargetIdentifier targetIdentifier;
		public ComputeBuffer sphereBuffer;
		public ComputeBuffer lightBuffer;

		Transform sphereParent;
		GameObject[] sphereGOs;

		struct Sphere {
			public Vector3 position;
			public float radius;
		}

		List<Sphere> spheres = new List<Sphere>();
		List<Vector3> directionalLights = new List<Vector3>();

		public RayMarchingPass(ComputeShader computeShader, string commandBufferName, LayerMask layerMask) {
			this.computeShader = computeShader;
			this.commandBufferName = commandBufferName;

			sphereGOs = GameObject.FindGameObjectsWithTag("CloudPuff");
			sphereParent = GameObject.Find("Spheres").transform;
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

			SetShaderParameters(cmd, renderingData.cameraData.camera);
			BuildLightBuffer(cmd, renderingData.lightData.visibleLights);

			cmd.SetComputeTextureParam(computeShader, 0, "Result", targetIdentifier);
			cmd.DispatchCompute(
				computeShader,
				0,
				Mathf.CeilToInt(Screen.width / 8.0f),
				Mathf.CeilToInt(Screen.height / 8.0f),
				1
			);

			//Then copying target back to camera's texture
			cmd.Blit(targetIdentifier, source);

			context.ExecuteCommandBuffer(cmd);
			CommandBufferPool.Release(cmd);
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
			spheres.Clear();

			foreach (GameObject sphere in sphereGOs)
				spheres.Add(new Sphere() {
					position = sphere.transform.position,
					radius = Mathf.Max(sphere.transform.localScale.x, sphere.transform.localScale.y, sphere.transform.localScale.z) / 2f
				});

			sphereBuffer?.Release();
			sphereBuffer = new ComputeBuffer(sphereGOs.Length, 16);
			sphereBuffer.SetData(spheres);
			cmd.SetComputeBufferParam(computeShader, 0, "_Spheres", sphereBuffer);

			cmd.SetComputeFloatParam(computeShader, "_Smoother", sphereParent.GetComponent<RayMarchingController>().smoother);
		}

		void BuildLightBuffer(CommandBuffer cmd, NativeArray<VisibleLight> visibleLights) {
			directionalLights.Clear();
			foreach (VisibleLight light in visibleLights.Where(light => light.lightType == LightType.Directional))
				directionalLights.Add(light.light.transform.forward);

			lightBuffer?.Release();
			lightBuffer = new ComputeBuffer(visibleLights.Length, 12);
			lightBuffer.SetData(directionalLights);
			cmd.SetComputeBufferParam(computeShader, 0, "_DirectionalLights", lightBuffer);
		}
	}

	[System.Serializable]
	public struct RayMarchingSettings {
		public ComputeShader computeShader;
		public LayerMask layerMask;
	}

	public RayMarchingSettings settings;

	RayMarchingPass rayMarchingPass;

	public override void Create() {
		rayMarchingPass = new RayMarchingPass(settings.computeShader, name, settings.layerMask) {
			renderPassEvent = RenderPassEvent.AfterRendering
		};
	}

	// Here you can inject one or multiple render passes in the renderer.
	// This method is called when setting up the renderer once per-camera.
	public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData) {
		rayMarchingPass.Setup(renderer.cameraColorTarget);
		renderer.EnqueuePass(rayMarchingPass);
	}

	void OnDestroy() => rayMarchingPass.sphereBuffer?.Release();

	void OnDisable() => rayMarchingPass.sphereBuffer?.Release();
}
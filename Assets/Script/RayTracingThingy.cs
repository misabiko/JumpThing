using UnityEngine;

public class RayTracingThingy : MonoBehaviour {
	public ComputeShader computeShader;
	public Light directionalLight;
	
	RenderTexture target;
	
	void OnRenderImage(RenderTexture source, RenderTexture destination) => Render(destination);

	void Render(RenderTexture destination) {
		Debug.Log("Bip");
		InitRenderTexture();
		
		computeShader.SetTexture(0, "Result", target);
		int threadGroupsX = Mathf.CeilToInt(Screen.width / 8.0f);
		int threadGroupsY = Mathf.CeilToInt(Screen.height / 8.0f);
		computeShader.Dispatch(0, threadGroupsX, threadGroupsY, 1);
		
		Graphics.Blit(target, destination);
	}

	void InitRenderTexture() {
		if (target == null || target.width != Screen.width || target.height != Screen.height) {
			if (target != null)
				target.Release();
			Debug.Log("Boop");

			target = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear) {
				enableRandomWrite = true
			};
			target.Create();
		}
	}
}
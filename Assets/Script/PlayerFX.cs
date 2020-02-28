using Cinemachine;
using UnityEngine;

public class PlayerFX : MonoBehaviour {
	[TextArea] public string Note = "Must have SmokeTrail, LandingPoof and JumpEffect particle systems as children in that order.";
	
	public SimpleAnimation camAnim;

	ParticleSystem smokeTrail;
	ParticleSystem smokePoof;
	ParticleSystem jumpParticles;
	SFXPlayer sfxPlayer;

	void Awake() {
		smokeTrail = transform.GetChild(0).GetComponent<ParticleSystem>();
		smokePoof = transform.GetChild(1).GetComponent<ParticleSystem>();
		jumpParticles = transform.GetChild(2).GetComponent<ParticleSystem>();

		// not a fan of this :/
		sfxPlayer = transform.parent.GetComponentInChildren<SFXPlayer>();

		/*Camera mainCamera = Camera.main;
		if (mainCamera != null)
			camAnim = mainCamera
				.GetComponent<CinemachineBrain>()
				.ActiveVirtualCamera
				.VirtualCameraGameObject
				.GetComponent<SimpleAnimation>();*/
	}

	public void LandingPoof() {
		smokePoof.Play();
		sfxPlayer.Land();
	}

	public void CheckCrouchEffects(bool hasSmoke, bool hasCrouchEffect, bool wasGrounded) {
		if (!wasGrounded) {
			if (smokeTrail.isPlaying)	//TODO PROFILEME might not be necessary
				smokeTrail.Stop();
			if (sfxPlayer.DriftIsPlaying())
				StopCrouchEffects();
			return;
		}

		if (smokeTrail.isPlaying && !hasSmoke)
			smokeTrail.Stop();
		else if (!smokeTrail.isPlaying && hasSmoke)
			smokeTrail.Play();

		if (sfxPlayer.DriftIsPlaying() && !hasCrouchEffect)
			StopCrouchEffects();
		else if (!sfxPlayer.DriftIsPlaying() && hasCrouchEffect)
			PlayCrouchEffects();
	}

	void PlayCrouchEffects() {
		sfxPlayer.Drift();
		camAnim.Play("ZoomIn");
	}

	void StopCrouchEffects() {
		sfxPlayer.StopDrift();
		camAnim.Play("ZoomOut");
	}
}
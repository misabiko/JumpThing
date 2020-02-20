using UnityEngine;

public class SFXPlayer : MonoBehaviour {
	public AudioClip stepClip;
	public AudioClip landClip;
	public AudioClip driftClip;
	public float driftVolume = 0.2f;
	
	AudioSource audioSource;

	void Awake() => audioSource = GetComponent<AudioSource>();

	public void Step() => audioSource.PlayOneShot(stepClip);

	public void Land() => audioSource.PlayOneShot(landClip);
	
	public void Drift() {
		audioSource.volume = driftVolume;
		audioSource.PlayOneShot(driftClip);
	}

	public void Stop() {
		audioSource.Stop();
		audioSource.volume = 1f;
	}
}
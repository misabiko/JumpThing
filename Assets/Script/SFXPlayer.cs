using UnityEngine;

public class SFXPlayer : MonoBehaviour {
	public AudioClip stepClip;
	public AudioClip landClip;
	public AudioSource generalSource;

	public AudioSource driftSource;

	public void Step() => generalSource.PlayOneShot(stepClip);

	public void Land() => generalSource.PlayOneShot(landClip);
	
	public void Drift() => driftSource.Play();

	public void StopDrift() => driftSource.Stop();

	public bool DriftIsPlaying() => driftSource.isPlaying;
}
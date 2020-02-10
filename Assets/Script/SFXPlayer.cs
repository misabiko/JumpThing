using UnityEngine;

public class SFXPlayer : MonoBehaviour {
	public AudioClip stepClip;
	public AudioClip landClip;
	
	AudioSource audioSource;

	void Awake() => audioSource = GetComponent<AudioSource>();

	public void Step() => audioSource.PlayOneShot(stepClip);

	public void Land() => audioSource.PlayOneShot(landClip);
}
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour {
	public TextMeshProUGUI scoreText;
	public int scoreOffset;
	public float deathHeight;

	PlayerMovement movement;
	int score;
	Vector3 spawnPos;
	Quaternion spawnRot;

	void Awake() => movement = GetComponent<PlayerMovement>();

	void Start() {
		spawnPos = transform.position;
		spawnRot = transform.rotation;
	}

	void LateUpdate() {
		UpdateScore();
		
		if (transform.position.y < deathHeight)
			Death();
	}

	void Death() {
		movement.Teleport(spawnPos, spawnRot);
		score = 0;
	}

	void UpdateScore() {
		Vector3 distance = transform.position;
		distance.y = 0;
		
		score = Mathf.Max(score, (int)distance.magnitude - scoreOffset);
		scoreText.text = "Score: " + score;
	}

	public void OnPause(InputAction.CallbackContext context) {
		if (context.performed)
			Debug.Break();
	}
}
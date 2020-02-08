using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour {
	int score;
	public TextMeshProUGUI scoreText;

	void Update() {
		score = Mathf.Max(score, (int)transform.position.magnitude);
		scoreText.text = "Score: " + score;
	}
}
﻿using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour {
	[SerializeField] TextMeshProUGUI scoreText;
	[SerializeField] float deathHeight;
	
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
		score = Mathf.Max(score, (int)transform.position.magnitude);
		scoreText.text = "Score: " + score;
	}
}
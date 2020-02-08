﻿using Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

public class CheapCameraMovement : MonoBehaviour {
	public CinemachineFreeLook cam;
	public PlayerInput playerInput;
	
	public float camXSensitivity;
	public float camYSensitivity;

	Vector2 lookInput;

	void Start() {
		playerInput.actions["Look"].started += OnLook;
		playerInput.actions["Look"].performed += OnLook;
		playerInput.actions["Look"].canceled += OnLook;
	}

	void OnLook(InputAction.CallbackContext context) => lookInput = context.ReadValue<Vector2>();

	void Update() {
		cam.m_XAxis.Value += camXSensitivity * lookInput.x;
		cam.m_YAxis.Value -= camYSensitivity * lookInput.y;
	}
}
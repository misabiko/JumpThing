using System;
using System.Collections;
using Script;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PlayerMovement : MonoBehaviour {
	static readonly int MoveInputMagnitude = Animator.StringToHash("MoveInputMagnitude");
	static readonly int Jump = Animator.StringToHash("Jump");
	static readonly int AirBorn = Animator.StringToHash("AirBorn");
	static readonly int MoveInputWithWarmUp = Animator.StringToHash("MoveInput+WarmUpBoost");
	static readonly int Crouching = Animator.StringToHash("Crouching");
	
	public Transform camTransform;
	public Slider velocityBar;

	public PlayerData data;

	new Rigidbody rigidbody;
	Animator anim;
	Transform playerMesh;
	PlayerFX playerFX;
	
	Vector2 moveInput;
	Vector3 moveDirection;
	bool wasGrounded;
	Coroutine runWarmUp;
	float warmUpBoost;
	Vector3 colliderBottom;
	LayerMask layerMask;

	new Collider collider;
	float defaultFriction;
	float crouchFriction;
	bool crouch;

	void Awake() {
		rigidbody = GetComponent<Rigidbody>();
		
		anim = GetComponentInChildren<Animator>();
		playerMesh = anim.transform;
		
		collider = GetComponent<Collider>();
		defaultFriction = collider.material.dynamicFriction;
		crouchFriction = 0f;

		playerFX = GetComponentInChildren<PlayerFX>();

		colliderBottom = Vector3.down * (GetComponent<CapsuleCollider>().height / 2f - 0.01f);

		layerMask = LayerMask.GetMask("Player");
	}

	void Start() => velocityBar.maxValue = 1f + data.maxWarmUp;

	public void OnMove(InputAction.CallbackContext context) {
		moveInput = context.ReadValue<Vector2>();
		if (moveInput.sqrMagnitude > 1f)
			moveInput.Normalize();
	}

	public void OnJump(InputAction.CallbackContext context) {
		if (!context.performed) return;
		
		if (!IsGrounded()) return;
		
		rigidbody.AddForce(data.jumpForce * Vector3.up, ForceMode.Impulse);
		anim.SetTrigger(Jump);
		anim.SetBool(AirBorn, true);
		//jumpParticles.Play();
		//jumpAudio.Play();
	}

	bool IsGrounded() =>
		Physics.Raycast(transform.position + colliderBottom, Vector3.down, data.groundCheckDist, ~layerMask);

	void Update() {
		Vector3 camForward = camTransform.forward;
		camForward.y = 0;
		camForward.Normalize();
		
		moveDirection = camForward * moveInput.y + camTransform.right * moveInput.x;
		
		float stickStrength = moveDirection.magnitude;
		anim.SetFloat(MoveInputMagnitude, stickStrength);
		anim.SetFloat(MoveInputWithWarmUp, stickStrength + warmUpBoost);
		if (!crouch && wasGrounded)
			velocityBar.value = stickStrength + warmUpBoost;
		else
			velocityBar.value = 0f;

		bool crouchEffect = crouch && rigidbody.velocity.sqrMagnitude > 0.01f;
		playerFX.CheckCrouchEffects(stickStrength > data.runThreshold || crouchEffect, crouchEffect, wasGrounded);
	}

	void FixedUpdate() {
		if (!crouch) {
			if (wasGrounded)
				ApplyGroundedMovement();
			else
				ApplyAirBornMovement();
		}

		bool newGrounding = IsGrounded();
		UpdateGrounding(newGrounding);
	}

	void ApplyGroundedMovement() {
		Vector3 force = new Vector3();
		float maxSpeed;
		
		if (moveDirection.sqrMagnitude <= data.runThreshold * data.runThreshold) {
			force += moveDirection * data.accelJog;
			
			if (runWarmUp != null)
				StopWarmUp();

			maxSpeed = data.maxSpeedJog;
		}else {
			//Aligning the jog and run's linear functions
			Vector3 direction = moveDirection.normalized;
			force += (moveDirection - direction * data.runThreshold) * (data.accelRun + warmUpBoost) + direction * (data.runThreshold * data.accelJog);

			if (runWarmUp == null)
				runWarmUp = StartCoroutine(WarmUpRun());

			maxSpeed = data.maxSpeedRun;
		}

		rigidbody.AddForce(force);
		ClampFlatVel(maxSpeed);
		
		if (moveDirection != Vector3.zero)
			AlignGroundedRotation();
	}

	void AlignGroundedRotation() {
		Quaternion goalRot = Quaternion.LookRotation(moveDirection);
		Quaternion slerp = Quaternion.Slerp(transform.rotation, goalRot, data.turnSpeed * moveDirection.magnitude * Time.fixedDeltaTime);
			
		rigidbody.rotation = slerp;
	}

	void ApplyAirBornMovement() {
		float dotMultiplier = Mathf.Abs(Vector3.Dot(transform.forward, moveDirection.normalized));
		rigidbody.AddForce(moveDirection * (dotMultiplier * data.accelAirBorn));
		
		if (moveDirection != Vector3.zero)
			AlignAirBornRotation();
	}

	void AlignAirBornRotation() {
		Quaternion goalRot = Quaternion.Euler(data.airBornAngle * moveInput.y, playerMesh.rotation.eulerAngles.y, -data.airBornAngle * moveInput.x);
		
		Quaternion slerp = Quaternion.Slerp(playerMesh.rotation, goalRot, data.turnSpeed * moveDirection.magnitude * Time.fixedDeltaTime);
			
		playerMesh.rotation = slerp;
	}

	void ClampFlatVel(float maxSpeed) {
		Vector3 flatVel = rigidbody.velocity;
		flatVel.y = 0;

		if (flatVel.magnitude > maxSpeed)
			flatVel = flatVel.normalized * maxSpeed;
		flatVel.y = rigidbody.velocity.y;
		rigidbody.velocity = flatVel;
	}

	void UpdateGrounding(bool isGrounded) {
		anim.SetBool(AirBorn, !isGrounded);

		//if just landed
		if (isGrounded && !wasGrounded) {
			playerFX.LandingPoof();

			playerMesh.localRotation = Quaternion.identity;
		}

		wasGrounded = isGrounded;
	}

	public void Teleport(Vector3 pos, Quaternion rot) {
		ResetYVel();
		rigidbody.position = pos;
		rigidbody.rotation = rot;
	}

	void ResetYVel() => rigidbody.velocity = new Vector3(rigidbody.velocity.x, 0f, rigidbody.velocity.z);

	IEnumerator WarmUpRun() {
		yield return new WaitForSeconds(data.warmUpDelay);

		while (true) {
			warmUpBoost += data.warmUpSpeed;
			
			if (warmUpBoost >= data.maxWarmUp) {
				warmUpBoost = data.maxWarmUp;
				break;
			}
			
			yield return null;
		}
	}

	void StopWarmUp() {
		StopCoroutine(runWarmUp);
		runWarmUp = null;
		warmUpBoost = 0f;
	}

	public void OnCrouch(InputAction.CallbackContext context) => SetCrouching(context.ReadValue<float>() > data.crouchThreshold);

	void SetCrouching(bool crouch) {
		this.crouch = crouch;
		collider.material.dynamicFriction = crouch ? crouchFriction : defaultFriction;
		anim.SetBool(Crouching, crouch);
	}
}
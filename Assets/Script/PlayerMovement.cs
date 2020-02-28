using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PlayerMovement : MonoBehaviour {
	static readonly int MoveInputMagnitude = Animator.StringToHash("MoveInputMagnitude");
	static readonly int Jump = Animator.StringToHash("Jump");
	static readonly int AirBorn = Animator.StringToHash("AirBorn");
	static readonly int MoveInputWithWarmUp = Animator.StringToHash("MoveInput+WarmUpBoost");
	
	public Transform camTransform;
	public Animator anim;
	public ParticleSystem smokeTrail;
	public ParticleSystem smokePoof;
	public ParticleSystem jumpParticles;
	public SFXPlayer sfxPlayer;
	public Transform playerMesh;
	public SimpleAnimation camAnim;
	public Slider velocityBar;
	
	public float accelJog;
	public float accelRun;
	public float accelAirBorn;
	public float maxSpeedJog;
	public float maxSpeedRun;
	public float runThreshold;	//Must match animator's transition conditions
	public float jumpForce;
	public float turnSpeed;
	public float airBornAngle;
	public float groundCheckDist;
	
	public float warmUpDelay;
	public float warmUpSpeed;
	public float maxWarmUp;

	new Rigidbody rigidbody;
	Vector2 moveInput;
	Vector3 moveDirection;
	bool wasGrounded;
	Coroutine runWarmUp;
	float warmUpBoost;
	Vector3 colliderBottom;
	LayerMask layerMask;

	public AudioSource jumpAudio;
	public TextMeshProUGUI velText;
	

	Vector3 drift;
	new Collider collider;
	float defaultFriction;
	float crouchFriction;
	public float crouchThreshold = 0.2f;
	bool crouch;
	static readonly int Crouching = Animator.StringToHash("Crouching");

	void Awake() {
		rigidbody = GetComponent<Rigidbody>();
		collider = GetComponent<Collider>();
		defaultFriction = collider.material.dynamicFriction;
		crouchFriction = 0f;

		colliderBottom = Vector3.down * (GetComponent<CapsuleCollider>().height / 2f - 0.01f);

		layerMask = LayerMask.GetMask("Player");
	}

	void Start() => velocityBar.maxValue = 1f + maxWarmUp;

	public void OnMove(InputAction.CallbackContext context) {
		moveInput = context.ReadValue<Vector2>();
		if (moveInput.sqrMagnitude > 1f)
			moveInput.Normalize();
	}

	public void OnJump(InputAction.CallbackContext context) {
		if (!context.performed) return;
		
		if (!IsGrounded()) return;
		
		rigidbody.AddForce(jumpForce * Vector3.up, ForceMode.Impulse);
		anim.SetTrigger(Jump);
		anim.SetBool(AirBorn, true);
		//jumpParticles.Play();
		//jumpAudio.Play();
	}

	bool IsGrounded() =>
		Physics.Raycast(transform.position + colliderBottom, Vector3.down, groundCheckDist, ~layerMask);

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
		CheckCrouchEffects(stickStrength > runThreshold || crouchEffect, crouchEffect);
	}

	void CheckCrouchEffects(bool hasSmoke, bool hasCrouchEffect) {
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
		
		if (moveDirection.sqrMagnitude <= runThreshold * runThreshold) {
			force += moveDirection * accelJog;
			
			if (runWarmUp != null)
				StopWarmUp();

			maxSpeed = maxSpeedJog;
		}else {
			//Aligning the jog and run's linear functions
			Vector3 direction = moveDirection.normalized;
			force += (moveDirection - direction * runThreshold) * (accelRun + warmUpBoost) + direction * (runThreshold * accelJog);

			if (runWarmUp == null)
				runWarmUp = StartCoroutine(WarmUpRun());

			maxSpeed = maxSpeedRun;
		}

		rigidbody.AddForce(force);
		ClampFlatVel(maxSpeed);
		
		if (moveDirection != Vector3.zero)
			AlignGroundedRotation();
	}

	void AlignGroundedRotation() {
		Quaternion goalRot = Quaternion.LookRotation(moveDirection);
		Quaternion slerp = Quaternion.Slerp(transform.rotation, goalRot, turnSpeed * moveDirection.magnitude * Time.fixedDeltaTime);
			
		rigidbody.rotation = slerp;
	}

	void ApplyAirBornMovement() {
		float dotMultiplier = Mathf.Abs(Vector3.Dot(transform.forward, moveDirection.normalized));
		rigidbody.AddForce(moveDirection * (dotMultiplier * accelAirBorn));
		
		if (moveDirection != Vector3.zero)
			AlignAirBornRotation();
	}

	void AlignAirBornRotation() {
		Quaternion goalRot = Quaternion.Euler(airBornAngle * moveInput.y, playerMesh.rotation.eulerAngles.y, -airBornAngle * moveInput.x);
		
		Quaternion slerp = Quaternion.Slerp(playerMesh.rotation, goalRot, turnSpeed * moveDirection.magnitude * Time.fixedDeltaTime);
			
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
			smokePoof.Play();
			sfxPlayer.Land();

			playerMesh.localRotation = Quaternion.identity;
			drift = Vector3.zero;
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
		yield return new WaitForSeconds(warmUpDelay);

		while (true) {
			warmUpBoost += warmUpSpeed;
			
			if (warmUpBoost >= maxWarmUp) {
				warmUpBoost = maxWarmUp;
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

	public void OnCrouch(InputAction.CallbackContext context) => SetCrouching(context.ReadValue<float>() > crouchThreshold);

	void SetCrouching(bool crouch) {
		this.crouch = crouch;
		collider.material.dynamicFriction = crouch ? crouchFriction : defaultFriction;
		anim.SetBool(Crouching, crouch);
	}
}
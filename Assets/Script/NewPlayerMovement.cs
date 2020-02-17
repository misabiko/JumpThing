using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class NewPlayerMovement : MonoBehaviour {
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
	
	public float accelJog;
	public float accelRun;
	public float maxSpeedJog;
	public float maxSpeedRun;
	public float runThreshold;	//Must match animator's transition conditions
	public float jumpForce;
	public float turnSpeed;
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

	void Awake() {
		rigidbody = GetComponent<Rigidbody>();
		
		PlayerInput playerInput = GetComponent<PlayerInput>();

		playerInput.actions["Move"].started += OnMove;
		playerInput.actions["Move"].performed += OnMove;
		playerInput.actions["Move"].canceled += OnMove;

		playerInput.actions["Jump"].started += OnJump;

		colliderBottom = Vector3.down * (GetComponent<CapsuleCollider>().height / 2f - 0.01f);

		layerMask = LayerMask.GetMask("Player");
	}

	void OnMove(InputAction.CallbackContext context) {
		moveInput = context.ReadValue<Vector2>();
		if (moveInput.sqrMagnitude > 1f)
			moveInput.Normalize();
	}

	void OnJump(InputAction.CallbackContext context) {
		if (!IsGrounded()) return;
		
		rigidbody.AddForce(jumpForce * Vector3.up, ForceMode.Impulse);
		anim.SetTrigger(Jump);
		anim.SetBool(AirBorn, true);
		jumpParticles.Play();
		jumpAudio.Play();
	}

	bool IsGrounded() {
		bool result = Physics.Raycast(transform.position + colliderBottom, Vector3.down, groundCheckDist, ~layerMask);
		velText.text = "Grounded: " + result;
		return result;
	}

	/*void OnDrawGizmos() {
		Gizmos.color = Color.green;
		Gizmos.DrawLine(transform.position + colliderBottom, transform.position + colliderBottom + Vector3.down * groundCheckDist);
	}*/

	void Update() {
		Vector3 camForward = camTransform.forward;
		camForward.y = 0;
		camForward.Normalize();
		
		moveDirection = camForward * moveInput.y + camTransform.right * moveInput.x;
		
		float stickStrength = moveDirection.magnitude;
		anim.SetFloat(MoveInputMagnitude, stickStrength);
		anim.SetFloat(MoveInputWithWarmUp, stickStrength + warmUpBoost);
		CheckSmokeParticles(stickStrength);
	}

	void CheckSmokeParticles(float stickStrength) {
		if (wasGrounded) {
			if (smokeTrail.isPlaying)	//TODO PROFILEME might not be necessary
				smokeTrail.Stop();
			return;
		}

		if (smokeTrail.isPlaying && stickStrength <= runThreshold)
			smokeTrail.Stop();
		else if (!smokeTrail.isPlaying && stickStrength > runThreshold)
			smokeTrail.Play();
	}

	void FixedUpdate() {
		Vector3 force = new Vector3();
		float maxSpeed;
		
		if (moveDirection.sqrMagnitude <= runThreshold * runThreshold) {
			force += moveDirection * accelJog;
			
			if (runWarmUp != null)
				StopWarmUp();

			maxSpeed = maxSpeedJog;
		}else {
			Vector3 direction = moveDirection.normalized;
			force += (moveDirection - direction * runThreshold) * (accelRun + warmUpBoost) + direction * (runThreshold * accelJog);

			if (runWarmUp == null)
				runWarmUp = StartCoroutine(WarmUpRun());

			maxSpeed = maxSpeedRun;
		}

		rigidbody.AddForce(force);
		ClampFlatVel(maxSpeed);
		
		if (moveDirection != Vector3.zero) {
			Quaternion goalRot = Quaternion.LookRotation(moveDirection);
			Quaternion slerp = Quaternion.Slerp(transform.rotation, goalRot, turnSpeed * moveDirection.magnitude * Time.fixedDeltaTime);
			
			rigidbody.rotation = slerp;
		}

		UpdateGrounding(IsGrounded());
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
		/*if (isGrounded)
			ResetYVel();*/
		
		anim.SetBool(AirBorn, !isGrounded);

		if (isGrounded != wasGrounded) {
			smokePoof.Play();
			sfxPlayer.Land();
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
}
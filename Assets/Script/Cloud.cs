using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

public class Cloud : MonoBehaviour {
	public float offset = 0.5f;
	public float radius = 1.5f;
	public float velocity = 0.1f;
	public float angleVel = 0.1f;
	public float maxDist = 2f;
	public float minDelay = 0.25f;
	public float growth = 1f;
	public float smalling = -0.25f;

	public Transform parent;
	float lastMove;
	Transform first;

	void Start() {
		for (int i = 0; i < parent.childCount; i++)
			MoveToFront(parent.GetChild(i));

		transform.hasChanged = false;
		first = parent.GetChild(0);
	}

	void Update() {
		transform.position += Time.deltaTime * transform.forward * velocity;
		transform.rotation *= Quaternion.AngleAxis(Time.deltaTime * angleVel, Vector3.up);

		for (int i = 0; i < parent.childCount; i++) {
			Transform child = parent.GetChild(i);
			Vector3 dir = child.position - transform.position;
			dir.y = 0f;
			if (child.localScale.x >= 0)
				child.localScale += dir.magnitude * Vector3.one * (growth * Time.deltaTime * (Vector3.Angle(transform.forward, dir) > 90f ? smalling : 1f));
		}

		CheckRespawnCircles();
	}

	void CheckRespawnCircles() {
		if (!transform.hasChanged || Time.time - lastMove < minDelay) return;
		
		Transform child = parent.GetChild(parent.childCount - 1);
		if (Vector3.Magnitude(transform.position - child.position) > maxDist)
			MoveToFront(child);

		lastMove = Time.time;
		transform.hasChanged = false;
	}

	void MoveToFront(Transform sphere) {
		Vector2 randomCircle = radius * Random.insideUnitCircle;
		sphere.position = transform.position + transform.forward * offset + transform.right * randomCircle.x + transform.up * randomCircle.y;
		sphere.localScale = Vector3.zero;
		
		sphere.SetSiblingIndex(0);
	}
}
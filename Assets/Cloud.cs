using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cloud : MonoBehaviour {
	class Bulb {
		public float delay;
		public float maxSize;
		public float lastMove;
		public Vector3 direction;
	}

	List<Bulb> bulbs;
	public float maxSize = 2f;
	public float minSize = 0.75f;
	public float maxDelay = 1f;
	public float minDelay = 0.5f;
	public float radius = 0.5f;
	[Range(0, 0.5f)]
	public float smoothVel = 0.8f;
	public Transform target;
	public float noise = 0.1f;
	float actualMaxDelay;

	void Start() {
		bulbs = new List<Bulb>();
		for (int i = 0; i < transform.childCount; i++)
			bulbs.Add(new Bulb {
				delay = minDelay + Random.value * (maxDelay - minDelay),
				maxSize = minSize + Random.value * (maxSize - minSize),
				direction = Random.insideUnitSphere
			});
	}

	void Update() {
		for (int i = 0; i < transform.childCount; i++) {
			Transform child = transform.GetChild(i);
			float t = (Time.time % bulbs[i].delay) / bulbs[i].delay;
			child.localScale = bulbs[i].maxSize * Vector3.one * Mathf.Abs(t - 0.5f);
			child.position += (bulbs[i].delay / maxDelay) * smoothVel * (t * t  - 0.5f) * bulbs[i].direction;
			if (Time.time - bulbs[i].lastMove > bulbs[i].delay) {
				child.position = target.position;
				bulbs[i].lastMove = Time.time;
				bulbs[i].direction = Random.insideUnitSphere;
			}
		}
	}
}
using UnityEngine;
using System.Collections;

public class Rocket : MonoBehaviour {
	public int fuel = 500;
	private int startingFuel;
	public bool inFlight = false;
	private ConstantForce cf;

	void Start() {
		startingFuel = fuel;
		cf = GetComponent<ConstantForce> ();
		cf.enabled = false;
	}

	void FixedUpdate() {
		if (inFlight) {
			if (fuel > 0) {
				cf.enabled = true;
				fuel -= 1;

			} else {
				inFlight = false;
				fuel = startingFuel;
			}

		} else {
			cf.enabled = false;
		}
	}

	public void Launch() {
		inFlight = true;
	}
}

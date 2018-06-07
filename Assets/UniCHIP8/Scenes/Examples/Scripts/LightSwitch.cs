using UnityEngine;
using System.Collections;

public class LightSwitch : MonoBehaviour {

	public bool switchState = false;
	public Light targetLight;

	void ToggleSwitch() {
		targetLight.enabled = !targetLight.enabled;
	}
}

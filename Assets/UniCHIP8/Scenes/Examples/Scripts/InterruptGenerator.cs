using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class InterruptGenerator : UniCHIP8Node {
	public UniCHIP8 target;
	public int interruptNumber = 0;

	[Space(10)]
	public GameObject int0_gameObject1;
	public GameObject int0_gameObject2;
	
	[Space(10)]
	public string int1_data = "Hello UniCHIP8"; // Fixme: update this value from an InputField

	public void IssueInterrupt() {
		if (target != null) {
			switch(interruptNumber) {
			case 0:
				// Simulate a collision
				target.WriteASCIIString(0x20, int0_gameObject1.name);
				target.WriteASCIIString(0x40, int0_gameObject2.name);
				break;

			case 1:
				target.WriteASCIIString(target.dataPortAddress, int1_data);
				break;

			default:
				break;
			}

			target.Interrupt(interruptNumber);
		}
	}

	// A receiving UnitCHIP will issue an Interrupt 1 when it the Receive() method is called via a router

	public void SendData() {
		if (router != null && target != null) {
			router.SendMessage ("Data", target.name + "|" + int1_data);
		}
	}

	public void SendData(string text) {
		if (router != null && target != null) {
			router.SendMessage ("Data", target.name + "|" + text);
		}
	}
	
}

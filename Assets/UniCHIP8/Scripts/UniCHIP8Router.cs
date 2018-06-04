using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class UniCHIP8Router : MonoBehaviour {
	
	[Tooltip("The list of nodes in this routers' network.")] public List<GameObject> gameObjects;
	
	void Awake () {
		gameObjects = new List<GameObject> ();
	}
	
	void RegisterNode(GameObject go) {
		gameObjects.Add (go);
	}

	void UnregisterNode(GameObject go) {
		gameObjects.Remove (go);
	}

	void DestroyNode(GameObject go) {
		gameObjects.Remove (go);
		Destroy (go);
	}

	void Reset() {
		gameObjects.ForEach( go => {
			if (go != null && go.GetComponent<UniCHIP8Node>().destroyOnReset)
				DestroyNode (go);
		} );
	}

	private string[] ParseEnvelope(string data) {
		return data.Split (new string[] { "|" }, System.StringSplitOptions.None);
	}

	void Command(string data) {
		// Command Message Format:
		//	Target Name | Command Data
		//
		// Command Data formats are tilde-separated strings. The first record is a command verb, the remainder are arguments.
		//
		// Transform-Command Data Format (args: 1 float):
		//  verb~(float value)
		//
		// 	  where verb is one of: moveX, moveY, moveZ,
		//                          rotateX, rotateY,rotateZ, 
		//                          scaleX, scaleY, scaleZ
		//
		//    example:
		//	    Test Cube|moveX~3
		//
		//
		// Transform-Command Data Format (args: 3 floats [a Vector3]):
		//
		//	verb~(float x)~(float y)~(float z)
		//
		//	  where verb is one of: move, rotate or scale
		//
		//`   example:
		//	    Test Cube|rotate~0~45~0
		//
		//
		// Other Command Data Formats
		//
		//	call~(string methodName)
		//  reparent~(string parentName)
		//  addMaterial (void)
		//  setMaterialColor~(int r, int g, int b, float a)
		//  destroy (void)
		//
		// Example:
		//
		//	UniCHIP8|call~Beep

		string[] parts = ParseEnvelope(data);
		string targetName = parts [0];
		string commandData = parts [1];

		GameObject target = gameObjects.Find (go => go != null && go.name == targetName);

		if (target != null)
			target.SendMessage ("Execute", commandData);
		else
			print ("Unable to forward command message: target object \"" + targetName + "\" not found, command was: " + commandData);
	}

	// FIXME: DRY
	void Data(string data) {
		string[] parts = ParseEnvelope(data);
		string targetName = parts [0];
		string messageData = parts [1];
		
		GameObject target = gameObjects.Find (go => go != null && go.name == targetName);
		
		if (target != null)
			target.SendMessage ("Receive", messageData);
		else
			print ("Unable to forward data message: target object \"" + targetName + "\" not found, message was: " + messageData);
	}
}

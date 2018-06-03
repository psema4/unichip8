﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class UniCHIP8Router : MonoBehaviour {

	private static UniCHIP8Router _instance;
	public UniCHIP8Router Instance { get { return _instance; } }

	public List<GameObject> gameObjects;
	
	void Awake () {
		gameObjects = new List<GameObject> ();

		if (_instance == null)
			_instance = this;

		else if (_instance != this)
			Destroy(gameObject.GetComponent(_instance.GetType()));

		DontDestroyOnLoad(gameObject);
	}


	void RegisterNode(GameObject gameObject) {
		gameObjects.Add (gameObject);
	}

	private string[] ParseEnvelope(string data) {
		return data.Split (new string[] { "|" }, System.StringSplitOptions.None);
	}

	void Command(string data) {
		// Command Message Format:
		//	Target Name | Command Data
		//
		// Transform-Command Data Format:
		//	verb ~ float ~ float ~ float
		//
		//	where verb is one of: move, rotate or scale
		//
		//`Example:
		//
		//	Test Cube|rotate~0~45~0
		//
		// Other Command Data Formats
		//
		//	call ~ method name
		//
		// Example:
		//
		//	UniCHIP8|call~Beep

		string[] parts = ParseEnvelope(data);
		string targetName = parts [0];
		string commandData = parts [1];

		GameObject target = gameObjects.Find (go => go.name == targetName);

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
		
		GameObject target = gameObjects.Find (go => go.name == targetName);
		
		if (target != null)
			target.SendMessage ("Receive", messageData);
		else
			print ("Unable to forward data message: target object \"" + targetName + "\" not found, message was: " + messageData);
	}
}

using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class ManaStatusScript : MonoBehaviour {

	Text text;

	// Use this for initialization
	void Start () {
		text = GetComponent<Text> ();
	}
	
	// Update is called once per frame
	void Update () {
		text.text = "Mana:\n" + ManaManagerScript.instance.currentMana;
	}
}

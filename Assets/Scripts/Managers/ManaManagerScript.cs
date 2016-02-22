using UnityEngine;
using System.Collections;

public class ManaManagerScript : MonoBehaviour {

	public static ManaManagerScript instance; //singleton instance
	public int startingMana;
	public int currentMana { get; set; }

	// Use this for initialization
	void Start () {
		instance = this;
		currentMana = startingMana;
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}

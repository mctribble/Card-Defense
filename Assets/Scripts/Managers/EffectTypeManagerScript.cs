using UnityEngine;
using System.Collections;
using System;

//this class is responsible for taking XMLEffects and returning an IEffect to match
public class EffectTypeManagerScript : MonoBehaviour {

	//singleton instance
	public static EffectTypeManagerScript instance;

	// Use this for initialization
	void Start () {
		instance = this;
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	//TODO: find a cleaner way to implement this?
	public IEffect parse (XMLEffect xe) {
		IEffect ie;
		switch (xe.name) {

		case "drawCard": 				ie = new EffectDrawCard(); break;
		case "budgetPercentageChange": 	ie = new EffectBudgetPercentageChange(); break;
		default: throw new NotImplementedException("Effect type " + xe.name + " is not implemented.");
	
		}
		ie.strength = xe.strength;
		return ie;
	}
}

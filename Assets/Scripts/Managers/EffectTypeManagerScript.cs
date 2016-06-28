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

    //instantiates and initializes an effect object from the xmlEffect.  returns null if that effect doesnt exist
	//TODO: find a cleaner way to implement this?
	public IEffect parse (XMLEffect xe) {
		IEffect ie;
		switch (xe.name) {
            case "addCharges":              ie = new EffectAddCharges(); break;
            case "allTowersLifespanBonus":  ie = new EffectAllTowersLifespanBonus(); break;
            case "budgetPercentageChange":  ie = new EffectBudgetPercentageChange(); break;
            case "changeWaveType":          ie = new EffectChangeWaveType(); break;
            case "damagePlayer":            ie = new EffectDamagePlayer(); break;
            case "discardRandomCard":       ie = new EffectDiscardRandom(); break;
            case "drawCard": 				ie = new EffectDrawCard(); break;
            case "returnsToTopOfDeck":      ie = new EffectReturnsToTopOfDeck(); break;
            case "shuffle":                 ie = new EffectShuffle(); break;
            case "timePercentageChange":    ie = new EffectTimePercentageChange(); break;
		    default:                        Debug.LogWarning("Effect type " + xe.name + " is not implemented."); ie = null; break;
		}
		ie.strength = xe.strength;
        ie.argument = xe.argument;
		return ie;
	}
}

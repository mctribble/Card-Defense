using UnityEngine;
using System.Collections;

//all effects in this file take place instantly with no particular target

//draws x cards
public class EffectDrawCard : IEffectInstant {

	//generic interface
	public TargetingType targetingType { get { return TargetingType.none; } }	//this effect doesnt need a target
	public EffectType effectType { get { return EffectType.instant; } }         //this is an instant effect
    public float strength { get; set; }                                         //how strong this effect is.  (unused in this effect)
    public string argument { get; set; }                                        //effect argument (unused in this effect)

    //this effect
    public string Name { get { return "Draw " + strength + " cards"; } }		//returns name and strength
	public void trigger() {
		for (uint i = 0; i < strength; i++)
			GameObject.FindGameObjectWithTag("Hand").SendMessage ("drawCard");
	}

}

//increases lifespan of all towers by x
public class EffectAllTowersLifespanBonus : IEffectInstant
{

    //generic interface
    public TargetingType targetingType { get { return TargetingType.none; } }   //this effect doesnt need a target
    public EffectType effectType { get { return EffectType.instant; } }         //this is an instant effect
    public float strength { get; set; }                                         //how strong this effect is.  (unused in this effect)
    public string argument { get; set; }                                        //effect argument (unused in this effect)

    //this effect
    public string Name { get { return "All towers have their lifespan increased by " + strength + "."; } }        //returns name and strength
    public void trigger()
    {
        //create an upgrade that only increases lifespan
        UpgradeData lifespanUpgrade = new UpgradeData();
        lifespanUpgrade.waveBonus = Mathf.RoundToInt(strength);

        //apply it to all towers
        GameObject[] towers = GameObject.FindGameObjectsWithTag("Tower");
        foreach (GameObject t in towers)
            t.SendMessage("Upgrade", lifespanUpgrade);
    }

}

//shuffles the deck
public class EffectShuffle : IEffectInstant
{

    //generic interface
    public TargetingType targetingType { get { return TargetingType.none; } }   //this effect doesnt need a target
    public EffectType effectType { get { return EffectType.instant; } }         //this is an instant effect
    public float strength { get; set; }                                         //how strong this effect is.  (unused in this effect)
    public string argument { get; set; }                                        //effect argument (unused in this effect)

    //this effect
    public string Name { get { return "Shuffle the deck."; } }        //returns name and strength
    public void trigger()
    {
        DeckManagerScript.instance.Shuffle();
    }

}

//damages the player
public class EffectDamagePlayer : IEffectInstant
{

    //generic interface
    public TargetingType targetingType { get { return TargetingType.none; } }   //this effect doesnt need a target
    public EffectType effectType { get { return EffectType.instant; } }         //this is an instant effect
    public float strength { get; set; }                                         //how strong this effect is.  (unused in this effect)
    public string argument { get; set; }                                        //effect argument (unused in this effect)

    //this effect
    public string Name { get { return "The player takes " + strength + " damage."; } }        //returns name and strength
    public void trigger()
    {
        DeckManagerScript.instance.Damage(Mathf.RoundToInt(strength));
    }

}
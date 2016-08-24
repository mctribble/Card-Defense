using System;
using UnityEngine;
using Vexe.Runtime.Types;

//all effects in this file take place instantly with no particular target

//draws x cards
public class EffectDrawCard : IEffectInstant
{
    [Hide] public string cardName { get; set; } //name of the card containing this effect
    [Hide] public TargetingType targetingType { get { return TargetingType.none; } }   //this effect doesnt need a target
    [Hide] public EffectType effectType { get { return EffectType.instant; } }         //this is an instant effect
    [Show, Display(2)] public float strength { get; set; }                             //number of cards
    [Hide] public string argument { get; set; }                                        //effect argument (unused in this effect)

    [Hide] public string Name { get { return "Draw up to " + strength + " cards"; } } //returns name and strength
    [Show, Display(1)] public string XMLName { get { return "drawCard"; } } //name used to refer to this effect in XML

    public void trigger() { HandScript.playerHand.StartCoroutine(HandScript.playerHand.drawCards( Mathf.FloorToInt(strength) ) ); }
}

//draws x enemy cards
public class EffectDrawEnemyCard : IEffectInstant
{
    [Hide] public string cardName { get; set; } //name of the card containing this effect
    [Hide] public TargetingType targetingType { get { return TargetingType.none; } }   //this effect doesnt need a target
    [Hide] public EffectType effectType { get { return EffectType.instant; } }         //this is an instant effect
    [Show, Display(2)] public float strength { get; set; }                             //number of cards
    [Hide] public string argument { get; set; }                                        //effect argument (unused in this effect)
    
    [Hide] public string Name { get { return "Draw up to " + strength + " enemy cards"; } } //returns name and strength
    [Show, Display(1)] public string XMLName { get { return "drawEnemyCard"; } } //name used to refer to this effect in XML
    
    public void trigger() { HandScript.enemyHand.StartCoroutine(HandScript.enemyHand.drawCards( Mathf.FloorToInt(strength) ) ); }
}

//increases lifespan of all towers by x
public class EffectAllTowersLifespanBonus : IEffectInstant
{
    [Hide] public string cardName { get; set; } //name of the card containing this effect
    [Hide] public TargetingType targetingType { get { return TargetingType.none; } }   //this effect doesnt need a target
    [Hide] public EffectType effectType { get { return EffectType.instant; } }         //this is an instant effect
    [Show, Display(2)] public float strength { get; set; }                             //# of waves
    [Hide] public string argument { get; set; }                                        //effect argument (unused in this effect)

    [Hide] public string Name { get { return "All towers last " + strength + " waves longer."; } } //returns name and strength
    [Show, Display(1)] public string XMLName { get { return "allTowersLifespanBonus"; } } //name used to refer to this effect in XML

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
    [Hide] public string cardName { get; set; } //name of the card containing this effect
    [Hide] public TargetingType targetingType { get { return TargetingType.none; } }   //this effect doesnt need a target
    [Hide] public EffectType effectType { get { return EffectType.instant; } }         //this is an instant effect
    [Hide] public float strength { get; set; }                                         //how strong this effect is.  (unused in this effect)
    [Hide] public string argument { get; set; }                                        //effect argument (unused in this effect)

    //this effect
    [Hide] public string Name { get { return "Shuffle the deck."; } }        //returns name and strength
    [Show] public string XMLName { get { return "shuffle"; } } //name used to refer to this effect in XML

    public void trigger()
    {
        DeckManagerScript.instance.Shuffle();
    }
}

//damages the player
public class EffectDamagePlayer : IEffectInstant
{
    [Hide] public string cardName { get; set; } //name of the card containing this effect
    [Hide] public TargetingType targetingType { get { return TargetingType.none; } }   //this effect doesnt need a target
    [Hide] public EffectType effectType { get { return EffectType.instant; } }         //this is an instant effect
    [Show, Display(2)] public float strength { get; set; }                             //how much damage
    [Hide] public string argument { get; set; }                                        //effect argument (unused in this effect)

    [Hide] public string Name { get { return "The player takes " + strength + " damage."; } } //returns name and strength
    [Show, Display(1)] public string XMLName { get { return "damagePlayer"; } } //name used to refer to this effect in XML

    public void trigger()
    {
        DeckManagerScript.instance.Damage(Mathf.RoundToInt(strength));
    }
}

//rolls an x-sided die.  the result can be fetched from a static variable and used by other effects.
class EffectDieRoll : IEffectInstant
{
    [Hide] public string cardName { get; set; } //name of the card containing this effect
    [Hide] public TargetingType targetingType { get { return TargetingType.none; } }   //property effects are not targeted
    [Hide] public EffectType effectType { get { return EffectType.instant; } }         //effect type
    [Show, Display(2)] public float strength { get; set; }                             //effect strength (max roll)
    [Hide] public string argument { get; set; }                                        //effect argument (unused in this effect)

    //this effect
    [Hide] public string Name { get { return "roll a " + strength + "-sided die:"; } } //returns name and strength
    [Show, Display(1)] public string XMLName { get { return "dieRoll"; } } //name used to refer to this effect in XML.
    [Show, Display(3)] public static int roll = -1;

    public void trigger()
    {
        int rollMax = Mathf.RoundToInt(strength);
        if (rollMax < 2)
        {
            MessageHandlerScript.Warning("<" + cardName + "> " + XMLName + " could not roll the die because it has less than 2 sides.");
            roll = -1;
        }
        else
        {
            roll = UnityEngine.Random.Range(0, rollMax) + 1;
        }
    }
}
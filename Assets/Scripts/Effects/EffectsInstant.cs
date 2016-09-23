using System;
using UnityEngine;
using Vexe.Runtime.Types;

//all effects in this file take place instantly with no particular target.  This base effect handles behavior common to them all
public abstract class BaseEffectInstant : BaseEffect, IEffectInstant
{
    [Hide] public override TargetingType targetingType { get { return TargetingType.none; } } //this effect doesnt need a target
    [Hide] public override EffectType    effectType    { get { return EffectType.instant; } } //this is an instant effect

    public abstract void trigger();
}

//draws x cards
public class EffectDrawCard : BaseEffectInstant
{
    [Hide] public override string Name { get { return "Draw up to " + strength + " cards"; } } //returns name and strength
    [Show] public override string XMLName { get { return "drawCard"; } } //name used to refer to this effect in XML

    public override void trigger() { HandScript.playerHand.StartCoroutine(HandScript.playerHand.drawCards( Mathf.FloorToInt(strength) ) ); }
}

//draws x enemy cards
public class EffectDrawEnemyCard : BaseEffectInstant
{
    [Hide] public override string Name { get { return "Draw up to " + strength + " enemy cards"; } } //returns name and strength
    [Show] public override string XMLName { get { return "drawEnemyCard"; } } //name used to refer to this effect in XML

    public override void trigger() { HandScript.enemyHand.StartCoroutine(HandScript.enemyHand.drawCards( Mathf.FloorToInt(strength) ) ); }
}

//increases lifespan of all towers by x
public class EffectAllTowersLifespanBonus : BaseEffectInstant
{
    [Hide] public override string Name { get { return "All towers last " + strength + " waves longer."; } } //returns name and strength
    [Show] public override string XMLName { get { return "allTowersLifespanBonus"; } } //name used to refer to this effect in XML

    public override void trigger()
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
public class EffectShuffle : BaseEffectInstant
{
    [Hide] public override string Name { get { return "Shuffle the deck."; } }        //returns name and strength
    [Show] public override string XMLName { get { return "shuffle"; } } //name used to refer to this effect in XML

    public override void trigger()
    {
        DeckManagerScript.instance.Shuffle();
    }
}

//damages the player
public class EffectDamagePlayer : BaseEffectInstant
{
    [Hide] public override string Name { get { return "The player takes " + strength + " damage."; } } //returns name and strength
    [Show] public override string XMLName { get { return "damagePlayer"; } } //name used to refer to this effect in XML

    public override void trigger()
    {
        DeckManagerScript.instance.Damage(Mathf.RoundToInt(strength));
    }
}

//rolls an x-sided die.  the result can be fetched from a static variable and used by other effects.
class EffectDieRoll : BaseEffectInstant
{
    [Hide] public override string Name { get { return "roll a " + strength + "-sided die:"; } } //returns name and strength
    [Show] public override string XMLName { get { return "dieRoll"; } } //name used to refer to this effect in XML.
    [Show] public static int roll = -1;

    public override void trigger()
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

//player score increases by X
public class EffectScore : BaseEffectInstant
{
    [Hide] public override string Name //returns name and strength
    {
        get
        {
            string result = "Score: ";
            int bonus = Mathf.RoundToInt(strength);

            if (bonus >= 0)
                result += '+';
            result += bonus;

            return result;
        }
    } 

    [Show] public override string XMLName { get { return "dieRoll"; } } //name used to refer to this effect in XML.

    public override void trigger()
    {
        ScoreManagerScript.instance.bonusPoints += Mathf.RoundToInt(strength);
    }
}
using UnityEngine;
using Vexe.Runtime.Types;

//all effects in this file take place instantly with no particular target

//draws x cards
public class EffectDrawCard : IEffectInstant
{
    [Hide] public TargetingType targetingType { get { return TargetingType.none; } }   //this effect doesnt need a target
    [Hide] public EffectType effectType { get { return EffectType.instant; } }         //this is an instant effect
    [Show, Display(2)] public float strength { get; set; }                             //number of cards
    [Hide] public string argument { get; set; }                                        //effect argument (unused in this effect)

    [Hide] public string Name { get { return "Draw up to " + strength + " cards"; } } //returns name and strength

    [Show, Display(1)] public string XMLName { get { return "drawCard"; } } //name used to refer to this effect in XML

    public void trigger()
    {
        for (uint i = 0; i < strength; i++)
            GameObject.FindGameObjectWithTag("Hand").SendMessage("drawCard");
    }
}

//increases lifespan of all towers by x
public class EffectAllTowersLifespanBonus : IEffectInstant
{
    [Hide] public TargetingType targetingType { get { return TargetingType.none; } }   //this effect doesnt need a target
    [Hide] public EffectType effectType { get { return EffectType.instant; } }         //this is an instant effect
    [Show, Display(2)] public float strength { get; set; }                                         //# of waves
    [Hide] public string argument { get; set; }                                        //effect argument (unused in this effect)

    [Hide] public string Name { get { return "All towers have their lifespan increased by " + strength + "."; } } //returns name and strength

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
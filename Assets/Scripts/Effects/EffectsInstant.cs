using System;
using System.Collections.Generic;
using UnityEngine;
using Vexe.Runtime.Types;
using System.Linq;
using System.Collections;

/// <summary>
/// instant effects take place instantly with no particular target.  This base effect handles behavior common to them all
/// </summary>
public abstract class BaseEffectInstant : BaseEffect, IEffectInstant
{
    [Hide] public override TargetingType targetingType { get { return TargetingType.none; } } //this effect doesnt need a target
    [Hide] public override EffectType    effectType    { get { return EffectType.instant; } } //this is an instant effect

    public abstract void trigger();
}

//player conjures X cards, which are chosen randomly from all available un-modded cards and added directly to the hand as tokens
public class EffectConjureCard : BaseEffectInstant
{
    [Hide] public override string Name { get { return "Conjure " + strength + " cards"; } } 
    [Show] public override string XMLName { get { return "conjureCard"; } }

    public override void trigger()
    {
        int conjureCount = Mathf.RoundToInt(strength);
        List<PlayerCard> conjuredCards = new List<PlayerCard>();

        for (int i = 0; i < conjureCount; i++)
        {
            //get the card data and make a copy that is flagged as a token
            PlayerCardData conjuredType = CardTypeManagerScript.instance.getRandomCardType().clone();
            conjuredType.isToken = true;

            //make a PlayerCard from it
            PlayerCard newCard = new PlayerCard();
            newCard.data = conjuredType;
            newCard.charges = conjuredType.cardMaxCharges;

            //store it
            conjuredCards.Add(newCard);
        }

        //"draw" them
        PlayerHandScript.instance.StartCoroutine(PlayerHandScript.instance.drawCards(conjuredCards.ToArray(), true));
    }
}

//enemy conjures X cards, which are new waves with the same budget as the highest budget wave already in the hand
public class EffectConjureEnemyCard : BaseEffectInstant
{
    [Hide] public override string Name { get { return "Enemy conjures " + strength + " cards"; } } 
    [Show] public override string XMLName { get { return "conjureEnemyCard"; } }

    public override void trigger() { EnemyHandScript.instance.StartCoroutine(triggerCoroutine()); }
    private IEnumerator triggerCoroutine()
    {
        //avoid exceptions by waiting for the enemy hand to have something in it
        while (EnemyHandScript.instance.currentHandSize == 0)
            yield return null;

        yield return EnemyHandScript.instance.StartCoroutine(EnemyHandScript.instance.waitForReady()); //wait for things to stop moving around before examining the hand

        List<WaveData> conjuredWaves = new List<WaveData>();                           //list to hold the waves
        int conjureCount  = Mathf.RoundToInt(strength);                                //how many to conjure
        int conjureBudget = EnemyHandScript.instance.IncomingWaves.Max(w => w.budget); //budget for conjured waves
        float conjureTime = EnemyHandScript.instance.IncomingWaves.Max(w => w.time);   //time for conjured waves

        List<EnemyData> conjureTypes = EnemyTypeManagerScript.instance.types.enemyTypes.Where(ed => ed.name != "Ping").ToList(); //we can conjure any enemy type that is not Ping

        //create the waves, each with a random type and the above budget and time.
        for (int i = 0; i < conjureCount; i++)
            conjuredWaves.Add(new WaveData(conjureTypes[UnityEngine.Random.Range(0, conjureTypes.Count)], conjureBudget, conjureTime, true));

        //apply wave effects and update ranks on conjured waves
        for (int i = 0; i < conjuredWaves.Count; i++)
        {
            if (conjuredWaves[i].enemyData.effectData != null)
                foreach (IEffect e in conjuredWaves[i].enemyData.effectData.effects)
                    if (e.triggersAs(EffectType.wave))
                        conjuredWaves[i] = ((IEffectWave)e).alteredWaveData(conjuredWaves[i]);

            conjuredWaves[i].recalculateRank();
        }

        //"draw" them
        EnemyHandScript.instance.StartCoroutine(EnemyHandScript.instance.drawCards(conjuredWaves));
    }
}

//player conjures X copies of the card named Y
public class EffectConjureSpecificCard : BaseEffectInstant
{
    [Hide] public override string Name
    {
        get
        {
            int conjureCount = Mathf.RoundToInt(strength);
            if (conjureCount == 1)
                return "Conjure: " + argument;
            else
                return "Conjure " + conjureCount + "x: " + argument;
        }
    } 
    [Show] public override string XMLName { get { return "conjureSpecificCard"; } }

    public override void trigger()
    {
        //figure out how many to conjure
        int conjureCount = Mathf.RoundToInt(strength);

        //conjure them
        for (int i = 0; i < conjureCount; i++)
            PlayerHandScript.instance.drawToken(argument);
    }
}

//player conjures X spells, which are chosen randomly from all available un-modded spells and added directly to the hand as tokens
public class EffectConjureSpellCard : BaseEffectInstant
{
    [Hide] public override string Name { get { return "Conjure " + strength + " spell cards"; } } 
    [Show] public override string XMLName { get { return "conjureSpellCard"; } }

    public override void trigger()
    {
        int conjureCount = Mathf.RoundToInt(strength);
        List<PlayerCard> conjuredCards = new List<PlayerCard>();

        for (int i = 0; i < conjureCount; i++)
        {
            //get the card data and make a copy that is flagged as a token
            PlayerCardData conjuredType = CardTypeManagerScript.instance.getRandomCardType(PlayerCardType.spell).clone();
            conjuredType.isToken = true;

            //make a PlayerCard from it
            PlayerCard newCard = new PlayerCard();
            newCard.data = conjuredType;
            newCard.charges = conjuredType.cardMaxCharges;

            //store it
            conjuredCards.Add(newCard);
        }

        //"draw" them
        PlayerHandScript.instance.StartCoroutine(PlayerHandScript.instance.drawCards(conjuredCards.ToArray(), true));
    }
}

//player conjures X towers, which are chosen randomly from all available un-modded towers and added directly to the hand as tokens
public class EffectConjureTowerCard : BaseEffectInstant
{
    [Hide] public override string Name { get { return "Conjure " + strength + " tower cards"; } } 
    [Show] public override string XMLName { get { return "conjureTowerCard"; } }

    public override void trigger()
    {
        int conjureCount = Mathf.RoundToInt(strength);
        List<PlayerCard> conjuredCards = new List<PlayerCard>();

        for (int i = 0; i < conjureCount; i++)
        {
            //get the card data and make a copy that is flagged as a token
            PlayerCardData conjuredType = CardTypeManagerScript.instance.getRandomCardType(PlayerCardType.tower).clone();
            conjuredType.isToken = true;

            //make a PlayerCard from it
            PlayerCard newCard = new PlayerCard();
            newCard.data = conjuredType;
            newCard.charges = conjuredType.cardMaxCharges;

            //store it
            conjuredCards.Add(newCard);
        }

        //"draw" them
        PlayerHandScript.instance.StartCoroutine(PlayerHandScript.instance.drawCards(conjuredCards.ToArray(), true));
    }
}

//player conjures X upgradess, which are chosen randomly from all available un-modded upgrades and added directly to the hand as tokens
public class EffectConjureUpgradeCard : BaseEffectInstant
{
    [Hide] public override string Name { get { return "Conjure " + strength + " spell cards"; } }
    [Show] public override string XMLName { get { return "conjureUpgradeCard"; } }

    public override void trigger()
    {
        int conjureCount = Mathf.RoundToInt(strength);
        List<PlayerCard> conjuredCards = new List<PlayerCard>();

        for (int i = 0; i < conjureCount; i++)
        {
            //get the card data and make a copy that is flagged as a token
            PlayerCardData conjuredType = CardTypeManagerScript.instance.getRandomCardType(PlayerCardType.upgrade).clone();
            conjuredType.isToken = true;

            //make a PlayerCard from it
            PlayerCard newCard = new PlayerCard();
            newCard.data = conjuredType;
            newCard.charges = conjuredType.cardMaxCharges;

            //store it
            conjuredCards.Add(newCard);
        }

        //"draw" them
        PlayerHandScript.instance.StartCoroutine(PlayerHandScript.instance.drawCards(conjuredCards.ToArray(), true));
    }
}

//draws x cards
public class EffectDrawCard : BaseEffectInstant
{
    [Hide] public override string Name { get { return "Draw " + strength + " cards"; } } //returns name and strength
    [Show] public override string XMLName { get { return "drawCard"; } } //name used to refer to this effect in XML

    public override void trigger() { PlayerHandScript.instance.StartCoroutine(PlayerHandScript.instance.drawCards( Mathf.FloorToInt(strength) ) ); }
}

//draws x enemy cards
public class EffectDrawEnemyCard : BaseEffectInstant
{
    [Hide] public override string Name { get { return "Draw " + strength + " enemy cards"; } } //returns name and strength
    [Show] public override string XMLName { get { return "drawEnemyCard"; } } //name used to refer to this effect in XML

    public override void trigger() { EnemyHandScript.instance.StartCoroutine(EnemyHandScript.instance.drawCards( Mathf.FloorToInt(strength) ) ); }
}

//player draws X spells from their deck.  if there are not enough, "Improvised Spell" tokens are created instead.
public class EffectDrawSpellCard : BaseEffectInstant
{
    [Hide] public override string Name { get { return "Draw " + strength + " spell cards"; } } //returns name and strength
    [Show] public override string XMLName { get { return "drawSpellCard"; } } //name used to refer to this effect in XML

    public override void trigger()
    {
        //setup
        int numToDraw = Mathf.FloorToInt(strength);
        PlayerCard[] cards = new PlayerCard[numToDraw];

        //fill the array with the Cards we want to draw
        for (int i = 0; i < numToDraw; i++)
        {
            //attempt to draw from the deck
            PlayerCard? drawn = DeckManagerScript.instance.DrawCardType(PlayerCardType.spell);

            if (drawn != null)
            {
                //the draw succeeded, so we can use it directly
                cards[i] = drawn.Value;
            }
            else
            {
                //the draw failed, so make a new card from thin air using the "Improvised Spell" token.
                PlayerCard newCard = new PlayerCard();
                newCard.data = CardTypeManagerScript.instance.getCardByName("Improvised Spell");
                newCard.charges = newCard.data.cardMaxCharges;
                cards[i] = newCard;
            }

        }

        //tell the hand to draw these specific Cards.
        PlayerHandScript.instance.StartCoroutine(PlayerHandScript.instance.drawCards( cards ) );
    }
}

//player draws X towers from their deck.  if there are not enough, "Improvised Tower" tokens are created instead.
public class EffectDrawTowerCard : BaseEffectInstant
{
    [Hide] public override string Name { get { return "Draw " + strength + " tower cards"; } } //returns name and strength
    [Show] public override string XMLName { get { return "drawTowerCard"; } } //name used to refer to this effect in XML

    public override void trigger()
    {
        //setup
        int numToDraw = Mathf.FloorToInt(strength);
        PlayerCard[] cards = new PlayerCard[numToDraw];

        //fill the array with the Cards we want to draw
        for (int i = 0; i < numToDraw; i++)
        {
            //attempt to draw from the deck
            PlayerCard? drawn = DeckManagerScript.instance.DrawCardType(PlayerCardType.tower);

            if (drawn != null)
            {
                //the draw succeeded, so we can use it directly
                cards[i] = drawn.Value;
            }
            else
            {
                //the draw failed, so make a new card from thin air using the "Improvised Tower" token.
                PlayerCard newCard = new PlayerCard();
                newCard.data = CardTypeManagerScript.instance.getCardByName("Improvised Tower");
                newCard.charges = newCard.data.cardMaxCharges;
                cards[i] = newCard;
            }

        }

        //tell the hand to draw these specific Cards.
        PlayerHandScript.instance.StartCoroutine(PlayerHandScript.instance.drawCards( cards ) );
    }
}

//player draws X Upgrades from their deck.  if there are not enough, "Improvised Upgrade" tokens are created instead.
public class EffectDrawUpgradeCard : BaseEffectInstant
{
    [Hide] public override string Name { get { return "Draw " + strength + " upgrade cards"; } } //returns name and strength
    [Show] public override string XMLName { get { return "drawUpgradeCard"; } } //name used to refer to this effect in XML

    public override void trigger()
    {
        //setup
        int numToDraw = Mathf.FloorToInt(strength);
        PlayerCard[] cards = new PlayerCard[numToDraw];

        //fill the array with the Cards we want to draw
        for (int i = 0; i < numToDraw; i++)
        {
            //attempt to draw from the deck
            PlayerCard? drawn = DeckManagerScript.instance.DrawCardType(PlayerCardType.upgrade);

            if (drawn != null)
            {
                //the draw succeeded, so we can use it directly
                cards[i] = drawn.Value;
            }
            else
            {
                //the draw failed, so make a new card from thin air using the "Improvised Upgrade" token.
                PlayerCard newCard = new PlayerCard();
                newCard.data = CardTypeManagerScript.instance.getCardByName("Improvised Upgrade");
                newCard.charges = newCard.data.cardMaxCharges;
                cards[i] = newCard;
            }

        }

        //tell the hand to draw these specific Cards.
        PlayerHandScript.instance.StartCoroutine(PlayerHandScript.instance.drawCards(cards));
    }
}

//increases lifespan of all towers by x
public class EffectAllTowersLifespanBonus : BaseEffectInstant
{
    [Hide] public override string Name { get { return "All towers get an upgrade to last " + strength + " waves longer."; } } //returns name and strength
    [Show] public override string XMLName { get { return "allTowersLifespanBonus"; } } //name used to refer to this effect in XML

    public override void trigger()
    {
        //create an upgrade that only increases lifespan
        UpgradeData lifespanUpgrade = new UpgradeData();
        lifespanUpgrade.waveBonus = Mathf.RoundToInt(strength);

        //apply it to all towers
        GameObject[] towers = GameObject.FindGameObjectsWithTag("Tower");
        foreach (GameObject t in towers)
        {
            if (parentData.propertyEffects.noUpgradeCost)
                t.SendMessage("FreeUpgrade", lifespanUpgrade);
            else
                t.SendMessage("Upgrade", lifespanUpgrade);
        }
    }
}

//applies upgrade Y to all active towers that can support it
public class EffectUpgradeAllTowers : BaseEffectInstant
{
    [Hide] public override string Name { get { return "Applies a " + argument + " to all towers that can receive it"; } } //returns name and strength
    [Show] public override string XMLName { get { return "upgradeAllTowers"; } } //name used to refer to this effect in XML

    public override void trigger()
    {
        //fetch the upgrade in question
        PlayerCardData card = CardTypeManagerScript.instance.getCardByName(argument);
        UpgradeData upgrade = card.upgradeData;
        EffectData  effects = card.effectData;

        if (upgrade == null)
        {
            Debug.LogWarning("Could not find upgrade: " + argument);
            return;
        }

        //apply it to all towers
        GameObject[] towers = GameObject.FindGameObjectsWithTag("Tower");
        foreach (GameObject t in towers)
        {
            if (parentData.propertyEffects.noUpgradeCost)
                t.SendMessage("FreeUpgrade", upgrade);
            else
                t.SendMessage("Upgrade", upgrade);
            
            if (effects != null)
                t.SendMessage("AddEffects", effects);
        }
    }
}

//shuffles the deck
public class EffectShuffle : BaseEffectInstant
{
    [Hide] public override string Name { get { return "Shuffle the deck."; } } //returns name and strength
    [Show] public override string XMLName { get { return "shuffle"; } } //name used to refer to this effect in XML

    public override void trigger()
    {
        DeckManagerScript.instance.Shuffle();
    }
}

//damages the player
public class EffectDamagePlayer : BaseEffectInstant
{
    [Hide] public override string Name { get { return "you take " + strength + " damage"; } } //returns name and strength
    [Show] public override string XMLName { get { return "damagePlayer"; } } //name used to refer to this effect in XML

    public override void trigger()
    {
        DeckManagerScript.instance.Damage(Mathf.RoundToInt(strength));
    }
}

//deals X damage to the players hand
public class EffectDamageHand : BaseEffectInstant
{
    [Hide] public override string Name { get { return "deals " + strength + " damage to your hand"; } }
    [Show] public override string XMLName { get { return "damageHand"; } }

    public override void trigger()
    {
        DeckManagerScript.instance.DamageHand(Mathf.FloorToInt(strength));
    }
}


//rolls an x-sided die.  the result can be fetched from argument and used by other effects.
class EffectDieRoll : BaseEffectInstant
{
    [Hide] public override string Name { get { return "roll a " + strength + "-sided die:"; } } //returns name and strength
    [Show] public override string XMLName { get { return "dieRoll"; } } //name used to refer to this effect in XML.

    //this effect also triggers as a property effect, since its result is used as a property
    public override bool triggersAs(EffectType triggerType)
    {
        return base.triggersAs(triggerType) || (triggerType == EffectType.property);
    }

    public override void trigger()
    {
        int rollMax = Mathf.RoundToInt(strength);
        if (rollMax < 2)
        {
            Debug.LogWarning("<" + cardName + "> " + XMLName + " could not roll the die because it has less than 2 sides.");
            argument = null;
        }
        else
        {
            int roll = (UnityEngine.Random.Range(0, rollMax) + 1); //die roll
            argument = roll.ToString(); //store in argument

            //update the parent's propertyEffects, since they may have been cached before the roll was made
            PropertyEffects? curProps = parentData.propertyEffects;
            PropertyEffects newProps;

            if (curProps == null)
                newProps = new PropertyEffects();
            else
                newProps = curProps.Value;

            newProps.dieRoll = roll;
            parentData.propertyEffects = newProps;
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

    [Show] public override string XMLName { get { return "score"; } } //name used to refer to this effect in XML.

    public override void trigger()
    {
        ScoreManagerScript.instance.bonusPoints += Mathf.RoundToInt(strength);
    }
}
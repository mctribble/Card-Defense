using UnityEngine;
using Vexe.Runtime.Types;

/// <summary>
/// instant effects take place instantly with no particular target.  This base effect handles behavior common to them all
/// </summary>
public abstract class BaseEffectInstant : BaseEffect, IEffectInstant
{
    [Hide] public override TargetingType targetingType { get { return TargetingType.none; } } //this effect doesnt need a target
    [Hide] public override EffectType    effectType    { get { return EffectType.instant; } } //this is an instant effect

    public abstract void trigger();
}

//draws x cards
public class EffectDrawCard : BaseEffectInstant
{
    [Hide] public override string Name { get { return "Draw " + strength + " cards"; } } //returns name and strength
    [Show] public override string XMLName { get { return "drawCard"; } } //name used to refer to this effect in XML

    public override void trigger() { HandScript.playerHand.StartCoroutine(HandScript.playerHand.drawCards( Mathf.FloorToInt(strength) ) ); }
}

//draws x enemy cards
public class EffectDrawEnemyCard : BaseEffectInstant
{
    [Hide] public override string Name { get { return "Draw " + strength + " enemy cards"; } } //returns name and strength
    [Show] public override string XMLName { get { return "drawEnemyCard"; } } //name used to refer to this effect in XML

    public override void trigger() { HandScript.enemyHand.StartCoroutine(HandScript.enemyHand.drawCards( Mathf.FloorToInt(strength) ) ); }
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
        HandScript.playerHand.StartCoroutine(HandScript.playerHand.drawCards( cards ) );
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
        HandScript.playerHand.StartCoroutine(HandScript.playerHand.drawCards( cards ) );
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
        HandScript.playerHand.StartCoroutine(HandScript.playerHand.drawCards(cards));
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
    [Hide] public override string Name { get { return "player takes " + strength + " damage."; } } //returns name and strength
    [Show] public override string XMLName { get { return "damagePlayer"; } } //name used to refer to this effect in XML

    public override void trigger()
    {
        DeckManagerScript.instance.Damage(Mathf.RoundToInt(strength));
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
            MessageHandlerScript.Warning("<" + cardName + "> " + XMLName + " could not roll the die because it has less than 2 sides.");
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
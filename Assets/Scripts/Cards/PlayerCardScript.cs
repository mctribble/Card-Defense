﻿using UnityEngine;
using System.Collections;
using Vexe.Runtime.Types;
using UnityEngine.UI;
using System.Xml.Serialization;
using System.ComponentModel;
using UnityEngine.EventSystems;
using System;

/// <summary>
/// complete definition for a card type as seen in XML
/// not to be confused with PlayerCardType, which is just how the card is classified in game
/// </summary>
/// <seealso cref="CardType"/>
[System.Serializable]
public class PlayerCardData : System.Object
{
    //set by card type manager to indicate if this card game from a base game file or a mod file
    [XmlIgnore] public bool isModded;

    //card data
    [XmlAttribute("Type")] public PlayerCardType cardType; //determines aspects of how the card should behave and the meaning of other values.  See enum dec for more info
    [XmlAttribute("Name")] public string         cardName; //name of the card

    [XmlAttribute("Description")]
    [DefaultValue("")]
    public string   cardDescription; //description of the card

    [XmlAttribute("Charges")]
    public int      cardMaxCharges;  //how many charges this card starts with

    [DefaultValue("Default_Art")]
    [XmlAttribute("Art")]
    [Show]
    public string cardArtName { get; set; }

    [DefaultValue("Default_Sprite")]
    [XmlAttribute("Tooltip")]
    [Show]
    public string tooltipSpriteName { get; set; }

    /// <summary>
    /// tokens are cards/entities generated by cards during play but are not valid when creating a deck 
    /// tokens in an XMLDeck render that deck invalid, and they do not appear in the editor.
    /// ex: the drawTower effect will provide the token "Improvised Tower" when there are no towers in the deck to draw.
    /// </summary>
    [XmlAttribute("Token")]
    [Show]
    public bool isToken { get; set; } 

    [VisibleWhen("towerDataSpecified")] public TowerData towerData; //contains info needed to summon a tower.  Only used in tower cards.

    [XmlIgnore]
    public bool towerDataSpecified
    {
        get { return cardType == PlayerCardType.tower; }
        set { }
    }

    [VisibleWhen("upgradeDataSpecified")] public UpgradeData upgradeData; //contains info needed to upgrade a tower.  Only used in upgrade cards.

    [XmlIgnore]
    public bool upgradeDataSpecified
    {
        get { return cardType == PlayerCardType.upgrade; }
        set { }
    }

    public EffectData effectData; //contains info needed to apply effects. O Can be provided for any card, but upgrades currently ignore them

    //only write effect data if there is data to write
    [XmlIgnore]
    public bool effectDataSpecified
    {
        get { return (effectData != null) && (effectData.XMLEffects.Count != 0); }
        set { }
    }

    /// <summary>
    /// slow function that returns a complete description of this card
    /// </summary>
    public string getDescription()
    {
        //init
        string description = "";

        //add info depending on card type
        switch (cardType)
        {
            case PlayerCardType.spell:
                break;

            case PlayerCardType.tower:
                //present tower stats
                description += "Max Upgrades: " + towerData.upgradeCap + '\n' +
                               "does " + towerData.attackPower + " damage\n";

                if (towerData.rechargeTime > 1.0f)
                    description += "attacks every " + towerData.rechargeTime.ToString("###0.##") + " seconds\n";
                else
                    description += "attacks <color=lime>" + (1 / towerData.rechargeTime).ToString("###0.##") + " times per second</color>\n";

                description += '(' + (towerData.attackPower/towerData.rechargeTime).ToString("###0.##") + " per second)\n" +
                               "Range: " + towerData.range;

                //lifespan
                if ((effectData == null) || (effectData.propertyEffects.infiniteTowerLifespan == false))
                    description += "\nLifespan: " + towerData.lifespan;
                else
                    description += "\nLifespan: <color=green>∞</color>";

                //ammo, if applicable
                if ((effectData != null) && (effectData.propertyEffects.limitedAmmo != null))
                    description += "\nAmmo: " + effectData.propertyEffects.limitedAmmo.Value;

                break;

            case PlayerCardType.upgrade:

                //present upgrade stats
                if (effectData == null || effectData.propertyEffects.infiniteTowerLifespan == false)
                {
                    if (upgradeData.waveBonus > 0) { description += "lifespan: +" + upgradeData.waveBonus + '\n'; }
                    if (upgradeData.waveBonus < 0) { description += "lifespan: " + upgradeData.waveBonus + '\n'; } 
                }
                else
                    description += "Lifespan: <color=green>∞</color>\n";

                if (effectData != null)
                    if (effectData.propertyEffects.limitedAmmo != null)
                        description += "<color=red>Limited Ammo: " + effectData.propertyEffects.limitedAmmo + "</color>\n";

                if (upgradeData.attackMultiplier   > 1) { description += "damage: +"    + (upgradeData.attackMultiplier - 1).ToString("P1")   + '\n'; }
                if (upgradeData.rangeMultiplier    > 1) { description += "range: +"     + (upgradeData.rangeMultiplier - 1).ToString("P1")    + '\n'; }
                if (upgradeData.rechargeMultiplier > 1) { description += "recharge: +"  + (upgradeData.rechargeMultiplier - 1).ToString("P1") + '\n'; }
                if (upgradeData.attackModifier     > 0) { description += "damage: +"    + upgradeData.attackModifier.ToString()               + '\n'; }
                if (upgradeData.rangeModifier      > 0) { description += "range: +"     + upgradeData.rangeModifier.ToString()                + '\n'; }
                if (upgradeData.rechargeModifier   > 0) { description += "recharge: +"  + upgradeData.rechargeModifier.ToString() + 's'       + '\n'; }

                if (upgradeData.attackMultiplier   < 1) { description += "damage: -"    + (1 - upgradeData.attackMultiplier).ToString("P1")   + '\n'; }
                if (upgradeData.rangeMultiplier    < 1) { description += "range: -"     + (1 - upgradeData.rangeMultiplier).ToString("P1")    + '\n'; }
                if (upgradeData.rechargeMultiplier < 1) { description += "recharge: -"  + (1 - upgradeData.rechargeMultiplier).ToString("P1") + '\n'; }
                if (upgradeData.attackModifier     < 0) { description += "damage: -"    + upgradeData.attackModifier.ToString()               + '\n'; }
                if (upgradeData.rangeModifier      < 0) { description += "range: -"     + upgradeData.rangeModifier.ToString()                + '\n'; }
                if (upgradeData.rechargeModifier   < 0) { description += "recharge: -"  + upgradeData.rechargeModifier.ToString() + 's'       + '\n'; }

                //cut off excess \n
                description = description.TrimEnd('\n');
                break;
        }

        //if there are effects, add them to the description
        if (effectData != null)
        {
            //make sure the effects have been parsed
            if (effectData.effects.Count == 0) { effectData.parseEffects(cardName); }

            //add a line of text to the description for each
            foreach (IEffect e in effectData.effects)
                if (e.Name != null)
                    description += "\n-" + e.Name;
        }

        //end with the flavor text found in the card file
        if ( (cardDescription != null) && (cardDescription.Length > 0) )
            description += "\n<i>" + cardDescription + "</i>";

        description = description.Trim(); //trim excess whitespace

        //return it
        return description;
    }

    /// <summary>
    /// creates and returns copy of this PlayerCardData
    /// </summary>
    public PlayerCardData clone()
    {
        PlayerCardData clone = new PlayerCardData();

        clone.cardArtName = cardArtName;
        clone.cardDescription = cardDescription;
        clone.cardMaxCharges = cardMaxCharges;
        clone.cardName = cardName;
        clone.cardType = cardType;
        clone.isModded = isModded;
        clone.isToken = isToken;
        clone.tooltipSpriteName = tooltipSpriteName;
        clone.towerData = towerData;
        clone.upgradeData = upgradeData;
        
        if (effectData != null)
            clone.effectData = effectData.clone();

        return clone;
    }

    //returns a short string for the debugger
    public override string ToString() { return cardName; }
}

/// <summary>
/// represents an XML upgrade definition
/// </summary>
[System.Serializable]
public class UpgradeData : System.Object
{
    //multiplicative modifiers
    [XmlAttribute("RechargeMult")] public float rechargeMultiplier = 1.0f;
    [XmlAttribute("RangeMult")]    public float rangeMultiplier    = 1.0f;
    [XmlAttribute("DamageMult")]   public float attackMultiplier   = 1.0f;

    //absolute modifiers
    [XmlAttribute("RechargeMod")]  public float rechargeModifier   = 0.0f;
    [XmlAttribute("RangeMod")]     public float rangeModifier      = 0.0f;
    [XmlAttribute("DamageMod")]    public float attackModifier     = 0.0f;
    [XmlAttribute("WaveBonus")]    public int waveBonus = 0;

    //provides a short string for the debugger
    public override string ToString()
    {
        string result = "{";

        if (waveBonus          > 0) { result += "lifespan+ "  +  waveBonus                              + ","; }
        if (attackMultiplier   > 1) { result += "damage+ "    + (attackMultiplier - 1).ToString("P1")   + ","; }
        if (rangeMultiplier    > 1) { result += "range+ "     + (rangeMultiplier - 1).ToString("P1")    + ","; }
        if (rechargeMultiplier > 1) { result += "recharge+ "  + (rechargeMultiplier - 1).ToString("P1") + ","; }
        if (attackModifier     > 0) { result += "damage+ "    +  attackModifier.ToString()              + ","; }
        if (rangeModifier      > 0) { result += "range+ "     +  rangeModifier.ToString()               + ","; }
        if (rechargeModifier   > 0) { result += "recharge+ "  +  rechargeModifier.ToString() + 's'      + ","; }

        if (waveBonus          < 0) { result += "lifespan- "  +      waveBonus                          + ","; }
        if (attackMultiplier   < 1) { result += "damage- "    + (1 - attackMultiplier).ToString("P1")   + ","; }
        if (rangeMultiplier    < 1) { result += "range- "     + (1 - rangeMultiplier).ToString("P1")    + ","; }
        if (rechargeMultiplier < 1) { result += "recharge- "  + (1 - rechargeMultiplier).ToString("P1") + ","; }
        if (attackModifier     < 0) { result += "damage- "    +      attackModifier.ToString()          + ","; }
        if (rangeModifier      < 0) { result += "range- "     +      rangeModifier.ToString()           + ","; }
        if (rechargeModifier   < 0) { result += "recharge- "  +      rechargeModifier.ToString() + 's'  + ","; }

        result += "}";

        return result;
    }
}

//represents a card on screen that belongs to the player
public class PlayerCardScript : CardScript, IPointerClickHandler
{
    //references
    [VisibleWhen("shouldShowRefs")] public GameObject tooltipPrefab; //prefab used to create a tooltip
    [VisibleWhen("shouldShowRefs")] public GameObject towerPrefab;   //prefab used to create a tower object
    [VisibleWhen("shouldShowRefs")] public Image      art;           //reference to card art image

    //discard data
    [VisibleWhen("shouldShowRefs")] public Vector3 discardPauseLocation;   //location to pause mid-discard so the player can see what is going away (local space)
    [VisibleWhen("shouldShowRefs")] public float   discardPauseTime;       //how long to pause there
    [VisibleWhen("shouldShowRefs")] public Vector3 discardDestroyLocation; //where to go before destroying ourself (local space)
    [VisibleWhen("shouldShowRefs")] public float   discardFadeTime;        //speed to fade out the card when it is being destroyed
    [Hide]                          public Image   deckImage;              //if being returned to the deck, we flip face down and aim to line up with this image.  Value set when the card is drawn

    [VisibleWhen("shouldShowRefs")] public AudioClip[] cardDestroyedSounds; //sounds to play if this card is destroyed

    public PlayerCard card;             //holds data specific to the card itself.  namely, the definition and number of charges remaining.

    private GameObject tooltipInstance; //instance of the tooltip object, if present

    public override bool discardable { get { return (card.data.effectData == null) || (card.data.effectData.propertyEffects.cannotBeDiscarded == false); } } //returns whether or not this card can be discarded
    public override string cardName  { get { return card.data.cardName; } } //returns the name of the card

    // Use this for initialization
    protected override void Awake()
    {
        base.Awake();
        tooltipInstance = null;
    }

    /// <summary>
    /// [COROUTINE] discards this card
    /// </summary>
    public override IEnumerator Discard()
    {
        state = CardState.discarding; //set the state so that other behavior on this card gets suspended
        //remove ourselves from the hand, if present
        if (hand != null)
            hand.SendMessage("Discard", this);

        //if the card has charges left, tined it back to the deck immediately without waiting for the animation
        if (card.charges > 0)
        {
            //send card to top or bottom depending on the presence of "returnsToTopOfDeck" property effect
            if ((card.data.effectData != null) && (card.data.effectData.propertyEffects.returnsToTopOfDeck))
            {
                DeckManagerScript.instance.addCardAtTop(card);
                transform.SetAsLastSibling();
            }
            else
            {
                DeckManagerScript.instance.addCardAtBottom(card);
                transform.SetAsFirstSibling();
            }
        }

        //if discardPauseTime is larger than 0, go to discardPauseLocation before doing anything else
        if (discardPauseTime > 0.0f)
        {
            //move there
            while (transform.localPosition != discardPauseLocation)
            {
                transform.localPosition = Vector3.MoveTowards(transform.localPosition, discardPauseLocation, (motionSpeed * Time.deltaTime));
                yield return null;
            }

            yield return new WaitForSeconds(discardPauseTime); //pause there a moment
        }

        //animate differently depending on where we are going
        if (card.charges > 0)
        {
            //flip over
            if (faceDown == false)
                yield return StartCoroutine(flipCoroutine());

            yield return null; //wait a frame to make sure the flip routine is done and we dont get stuck turning in two directions at once.

            //scale and turn to align with the deck.
            StartCoroutine(scaleToVector(deckImage.transform.localScale));
            StartCoroutine(turnToQuaternion(deckImage.transform.rotation));

            //find the destination in local space
            Vector3 targetPos = transform.InverseTransformPoint(deckImage.transform.position);

            //move there
            while (transform.localPosition != targetPos)
            {
                transform.localPosition = Vector3.MoveTowards(transform.localPosition, targetPos, (motionSpeed * Time.deltaTime));
                yield return null;
            }

            //die
            Destroy(gameObject);
            yield break;
        }
        else
        {
            //card is out of charges, and must be destroyed

            //sound
            int soundToPlay = UnityEngine.Random.Range(0, cardDestroyedSounds.Length);
            audioSource.clip = cardDestroyedSounds[soundToPlay];
            audioSource.Play();

            //fadeout
            art.CrossFadeAlpha(0.0f, discardFadeTime, false);
            title.CrossFadeAlpha(0.0f, discardFadeTime, false);
            description.CrossFadeAlpha(0.0f, discardFadeTime, false);
            cardFront.CrossFadeAlpha(0.0f, discardFadeTime, false);
            cardBack.CrossFadeAlpha(0.0f, discardFadeTime, false);

            //animate until we get to the destroy location
            while (transform.localPosition != discardDestroyLocation) 
            {
                //movement
                transform.localPosition = Vector3.MoveTowards(transform.localPosition, discardDestroyLocation, (motionSpeed * Time.deltaTime)); //movement

                //scale
                transform.localScale = Vector3.MoveTowards(transform.localScale, Vector3.zero, (scaleSpeed * Time.deltaTime));

                yield return null;
            }

            //die
            Destroy(gameObject);
            yield break;
        }
    }

    /// <summary>
    /// saves a reference to the image of the deck so we can return there if we need to later
    /// if this card returns to the deck when it is discarded, it will line itself up with this image before destroying itself
    /// </summary>
    public void SetDeckImage(Image di)
    {
        deckImage = di;
    }

    //this one needs a coroutine, so the event handler just passes the buck
    public void OnPointerClick(PointerEventData eventData) { StartCoroutine(OnClick()); }

    /// <summary>
    /// [COROUTINE] handles the card being clicked on
    /// </summary>
    private IEnumerator OnClick()
    {
        GameObject[] cards = GameObject.FindGameObjectsWithTag ("Card"); //used for sending messages to all other cards

        //if already casting, cancel it
        if (tooltipInstance != null)
        {
            ////if upgrade, tell towers so they can go back to the normal view
            //if (card.data.cardType == PlayerCardType.upgrade)
            //    foreach (GameObject tower in GameObject.FindGameObjectsWithTag("Tower"))
            //        tower.SendMessage("hideUpgradeInfo");

            state = CardState.idle;  //reset state
                                 //send a message to all cards except this one to tell them to come back out
            foreach (GameObject c in cards)
                if (c != this.gameObject)
                    c.SendMessage("Show");
            //get rid of the tooltip
            GameObject.DestroyImmediate(tooltipInstance);
            yield break;
        }

        //ignore this event if hidden or discarding
        if (hidden || (state == CardState.discarding))
            yield break;

        //start casting when clicked
        state = CardState.casting;

        //send a message to all cards except this one to tell them to hide
        foreach (GameObject c in cards)
            if (c != this.gameObject)
                c.SendMessage("Hide");

        if (card.data.cardType == PlayerCardType.spell && card.data.effectData.cardTargetingType == TargetingType.none)
        {
            //this card has no target
            //apply effects
            foreach (IEffect e in card.data.effectData.effects)
            {
                //effect must be handled differently based on how they trigger
                bool effectApplied = false;

                if (e.triggersAs(EffectType.wave))
                {
                    EnemyHandScript.instance.applyWaveEffect((IEffectWave)e);
                    LevelManagerScript.instance.UpdateWaveStats();
                    EnemyHandScript.instance.updateEnemyCards();
                    effectApplied = true;
                }

                if (e.triggersAs(EffectType.instant))
                {
                    ((IEffectInstant)e).trigger();
                    effectApplied = true;
                }

                if (e.triggersAs(EffectType.self))
                {
                    ((IEffectSelf)e).trigger(ref card, gameObject);
                    effectApplied = true;
                }

                //these effects are never applied, or already applied elsewhere, so we'll just treat it as if we have to suppress the warning
                if (e.triggersAs(EffectType.property) ||
                    e.triggersAs(EffectType.cardDrawn))
                {
                    effectApplied = true;
                }

                if (effectApplied == false)
                    Debug.LogWarning("I dont know how to apply " + e.XMLName + " on a card.");
            }

            //perform steps that must be done on every cast
            Cast();

            yield break; //we're done now
        }

        //this card has a target.  create a tooltip object to handle casting.
        tooltipInstance = Instantiate(tooltipPrefab, Vector3.zero, Quaternion.identity);    //instantiate prefab

        //set sprite with WWW, even on PC *twitch*
        string spritePath = "";
        if (Application.platform != RuntimePlatform.WebGLPlayer)
            spritePath = "file:///";
        spritePath += Application.streamingAssetsPath + "/Art/Sprites/" + card.data.tooltipSpriteName;
        WWW www = new WWW (spritePath);
        yield return www;
        tooltipInstance.GetComponentInChildren<Image>().sprite = Sprite.Create(
            www.texture,
            new Rect(0, 0, www.texture.width, www.texture.height),
            new Vector2(0.5f, 0.5f));

        tooltipInstance.SendMessage("SetParent", this); //tell tooltip who spawned it so it can call back later

        ////if upgrade, tell towers so they can show the relevant info
        //if (card.data.cardType == PlayerCardType.upgrade)
        //    foreach (GameObject tower in GameObject.FindGameObjectsWithTag("Tower"))
        //        tower.SendMessage("showUpgradeInfo");

        //if tower, pass range to the tooltip
        if (card.data.cardType == PlayerCardType.tower)
            tooltipInstance.SendMessage("SetRange", card.data.towerData.range);

        //if its a spell, tell it what kind of spell it is
        if (card.data.cardType == PlayerCardType.spell)
        {
            Debug.Assert(card.data.effectData.cardTargetingType != TargetingType.noCast); //if an effect on this card is not meant for casting, it should not be here
            tooltipInstance.SendMessage("SetTargetingType", card.data.effectData.cardTargetingType);
        }
    }

    /// <summary>
    /// instructs the card to hide off screen
    /// </summary>
    public override void Hide()
    {
        //ignore if discarding
        if (state == CardState.discarding)
            return;

        //cards hide just underneath the center of the screen
        targetLocation.x = 0;
        targetLocation.y = transform.root.GetComponent<RectTransform>().rect.yMin - 200;

        state = CardState.moving;       //mark this card as in motion
        hidden = true;              //mark this card as hidden
    }

    /// <summary>
    /// (tower cards only) summons the tower at the given location
    /// </summary>
    private void SummonTower(Vector3 location)
    {
        //summon tower
        GameObject instance = (GameObject) UnityEngine.Object.Instantiate (towerPrefab, location, Quaternion.identity);
        card.data.towerData.towerName = card.data.cardName;
        instance.SendMessage("SetData", card.data.towerData);
        if (card.data.effectData != null)
            instance.SendMessage("SetEffectData", card.data.effectData);

        //trigger effects
        instance.SendMessage("triggerOnTowerSpawned");

        //perform steps that must be done on every cast
        Cast();
    }

    /// <summary>
    /// (upgrade cards only) upgrades the given tower
    /// </summary>
    public void UpgradeTower(TowerScript target)
    {
        //send upgrade data to the target tower using a different message depending on whether or not it is free
        if ((card.data.effectData != null) && (card.data.effectData.propertyEffects.noUpgradeCost))
            target.FreeUpgrade(card.data.upgradeData);
        else
            target.Upgrade(card.data.upgradeData);

        //if there are effects on the card, send them over too
        if ((card.data.effectData != null) && (card.data.effectData.effects.Count > 0))
            target.AddEffects(card.data.effectData);

        //perform steps that must be done on every cast
        Cast();
    }

    /// <summary>
    /// performs steps that must be done whenever a player card of any type is cast
    /// </summary>
    private void Cast()
    {
        //send a message to all cards to tell them to show themselves
        GameObject[] cards = GameObject.FindGameObjectsWithTag ("Card");
        foreach (GameObject c in cards)
            c.SendMessage("Show");

        //remove charge.
        card.charges -= 1;
        updateChargeText();

        //discard self
        StartCoroutine(Discard());
    }

    /// <summary>
    /// [COROUTINE] saves card definition data and updates components as necessary
    /// </summary>
    private IEnumerator SetCard(PlayerCard c)
    {
        //save the data
        card = c;

        //colorize card front based on card type
        switch (c.data.cardType)
        {
            case PlayerCardType.tower:   cardFront.color = towerColor;   break;
            case PlayerCardType.upgrade: cardFront.color = upgradeColor; break;
            case PlayerCardType.spell:   cardFront.color = spellColor;   break;
        }

        //update card text
        updateChargeText();
        updateDescriptionText();

        //load art with WWW, even on PC (yes, really!  I couldn't find an easier way to do this and still let the user access the image files)
        string artPath = "";
        if (Application.platform != RuntimePlatform.WebGLPlayer)
            artPath = "file:///";
        artPath += Application.streamingAssetsPath + "/Art/Card Art/" + card.data.cardArtName;
        WWW www = new WWW (artPath); //load file
        yield return www; //wait for it to load

        if (www.error == null)
            art.sprite = Sprite.Create(www.texture, new Rect(0, 0, www.texture.width, www.texture.height), new Vector2(0.5f, 0.5f));
        else
            art.sprite = Resources.Load<Sprite>("Sprites/Error");

        //gray out card border if token
        if (c.data.isToken)
            cardFront.color = tokenColor;
    }

    /// <summary>
    /// updates card charge counts
    /// </summary>
    public void updateChargeText()
    {
        title.text = card.data.cardName + "\n" + card.charges + "/" + card.data.cardMaxCharges;
    }

    /// <summary>
    /// updates the card description text.
    /// </summary>
    public override void updateDescriptionText()
    {
        description.text = card.data.getDescription();
    }

    /// <summary>
    /// triggers all effects on this card that are meant to fire when the card is drawn
    /// </summary>
    public override void triggerOnDrawnEffects()
    {
        if (card.data.effectData != null)
            foreach (IEffect ie in card.data.effectData.effects)
                if (ie.triggersAs(EffectType.cardDrawn))
                    ((IEffectInstant)ie).trigger();
    }
}

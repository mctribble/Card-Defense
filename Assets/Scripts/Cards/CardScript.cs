using System.Collections;
using System.ComponentModel;
using System.Xml;
using System.Xml.Serialization;
using UnityEngine;
using UnityEngine.UI;
using Vexe.Runtime.Types;

//represents what type of card this is
public enum CardType
{
    tower,      //summons a tower with the given stats
    upgrade,    //increases/decreases the target towers stats
    spell		//other effects
}

//represents everything needed to summon a tower
[System.Serializable]
public class TowerData : System.Object
{
    [XmlIgnore] public string towerName; //name of the card which summoned this tower.  Populated when the tower is summoned

    [DefaultValue("Default_Sprite")]
    [XmlAttribute("Sprite")]
    public string towerSpriteName { get; set; }

    [DefaultValueAttribute(0.0f), XmlAttribute("Recharge")] public float rechargeTime; //how long it takes tthe tower to charge
    [DefaultValueAttribute(0.0f), XmlAttribute("Range")]    public float range;        //how far away from itself, in world space, the tower can shoot
    [DefaultValueAttribute(0.0f), XmlAttribute("Damage")]   public float attackPower;  // amount of damage this dower does before any modifiers susch as armor
    [DefaultValueAttribute(1.0f), XmlAttribute("Lifespan")] public int   lifespan;	   // amount of waves this tower remains on the field
}

//represents everything needed to upgrade a tower
[System.Serializable]
public class UpgradeData : System.Object
{
    //multiplicative modifiers
    [DefaultValueAttribute(1.0f), XmlAttribute("RechargeMult")] public float rechargeMultiplier = 1.0f;

    [DefaultValueAttribute(1.0f), XmlAttribute("RangeMult")]    public float rangeMultiplier    = 1.0f;
    [DefaultValueAttribute(1.0f), XmlAttribute("DamageMult")]   public float attackMultiplier   = 1.0f;

    //absolute modifiers
    [DefaultValueAttribute(0.0f), XmlAttribute("RechargeMod")]  public float rechargeModifier   = 0.0f;

    [DefaultValueAttribute(0.0f), XmlAttribute("RangeMod")]     public float rangeModifier      = 0.0f;
    [DefaultValueAttribute(0.0f), XmlAttribute("DamageMod")]    public float attackModifier     = 0.0f;
    [DefaultValueAttribute(0),    XmlAttribute("WaveBonus")]    public int waveBonus = 0;
}

[System.Serializable]
public class CardData : System.Object
{
    //card data
    [XmlAttribute("Type")] public CardType cardType; //determines aspects of how the card should behave and the meaning of other values.  See enum dec for more info

    [XmlAttribute("Name")] public string   cardName; //name of the card

    [XmlAttribute("Description")]
    [DefaultValue("")]
    public string   cardDescription; //description of the card

    [XmlAttribute("Charges")]
    public int      cardMaxCharges;  //how much it costs to use this card

    [DefaultValue("Default_Art")]
    [XmlAttribute("Art")]
    public string cardArtName { get; set; }

    [DefaultValue("Default_Sprite")]
    [XmlAttribute("Tooltip")]
    public string tooltipSpriteName { get; set; }

    public TowerData towerData; //contains info needed to summon a tower.  Only used in tower cards.

    [XmlIgnore]
    public bool TowerDataSpecified
    {
        get { return cardType == CardType.tower; }
        set { }
    }

    public UpgradeData upgradeData; //contains info needed to upgrade a tower.  Only used in upgrade cards.

    [XmlIgnore]
    public bool upgradeDataSpecified
    {
        get { return cardType == CardType.upgrade; }
        set { }
    }

    public EffectData effectData; //contains info needed to apply effects. O Can be provided for any card, but upgrades currently ignore them

    [XmlIgnore]
    public bool EffectDataSpecified
    {
        get { return (effectData != null) && (effectData.XMLEffects.Count != 0); } //only write effect data if there is data to write
        set { }
    }

    //returns description text for this card type
    public string getDescription()
    {
        //init
        string description = "";

        //add info depending on card type
        switch (cardType)
        {
            case CardType.spell:
                break;

            case CardType.tower:
                //present tower stats
                description += "Damage: " + towerData.attackPower + '\n' +
                               "Range: " + towerData.range + '\n' +
                               "Fires in: " + towerData.rechargeTime + "s\n" +
                               "Lifespan: " + towerData.lifespan + '\n';
                break;

            case CardType.upgrade:
                //present upgrade stats
                if (upgradeData.waveBonus          != 0) { description += "lifespan: +"  + upgradeData.waveBonus                               + '\n'; }
                if (upgradeData.attackMultiplier   != 1) { description += "damage: +"    + (upgradeData.attackMultiplier - 1).ToString("P1")   + '\n'; }
                if (upgradeData.rangeMultiplier    != 1) { description += "range: +"     + (upgradeData.rangeMultiplier - 1).ToString("P1")    + '\n'; }
                if (upgradeData.rechargeMultiplier != 1) { description += "recharge: +"  + (upgradeData.rechargeMultiplier - 1).ToString("P1") + '\n'; }
                if (upgradeData.attackModifier     != 0) { description += "damage: +"    + upgradeData.attackModifier.ToString()               + '\n'; }
                if (upgradeData.rangeModifier      != 0) { description += "range: +"     + upgradeData.rangeModifier.ToString()                + '\n'; }
                if (upgradeData.rechargeModifier   != 0) { description += "recharge: +"  + upgradeData.rechargeModifier.ToString()             + "s\n"; }
                if (upgradeData.waveBonus          != 0) { description += "lifespan: -"  + upgradeData.waveBonus                               + '\n'; }
                if (upgradeData.attackMultiplier   != 1) { description += "damage: -"    + (1 - upgradeData.attackMultiplier).ToString("P1")   + '\n'; }
                if (upgradeData.rangeMultiplier    != 1) { description += "range: -"     + (1 - upgradeData.rangeMultiplier).ToString("P1")    + '\n'; }
                if (upgradeData.rechargeMultiplier != 1) { description += "recharge: -"  + (1 - upgradeData.rechargeMultiplier).ToString("P1") + '\n'; }
                if (upgradeData.attackModifier     != 0) { description += "damage: -"    + upgradeData.attackModifier.ToString()               + '\n'; }
                if (upgradeData.rangeModifier      != 0) { description += "range: -"     + upgradeData.rangeModifier.ToString()                + '\n'; }
                if (upgradeData.rechargeModifier   != 0) { description += "recharge: -"  + upgradeData.rechargeModifier.ToString()             + "s\n"; }
                break;
        }

        //if there are effects, add them to the description
        if (effectData != null)
        {
            //make sure the effects have been parsed
            if (effectData.effects.Count == 0) { effectData.parseEffects(); }

            //add a line of text to the description for each
            foreach (IEffect e in effectData.effects)
            {
                description += "\n<Color=#" + e.effectType.ToString("X") + ">" + e.Name + "</Color>";
            }
        }

        //end with the flavor text found in the card file
        description += "\n<i>" + cardDescription + "</i>";

        //return it
        return description;
    }
}

public class CardScript : BaseBehaviour
{
    public float      motionSpeed;      //speed in pixels/second this card can animate
    public Vector2    discardLocation;  //location to discard self to
    public Card       card;             //holds data specific to the card itself
    public GameObject tooltipPrefab;    //used to create a tooltip
    public GameObject towerPrefab;      //prefab used to create a tower object
    public Image      art;              //reference to card art image
    public Text       title;            //reference to card name text
    public Text       description;      //reference to card description text

    private GameObject hand;            //reference to the hand object managing this card
    private Vector2    idleLocation;    //location this card sits when it is resting
    private Vector2    targetLocation;  //location this card will move towards if it is not already there
    private GameObject tooltipInstance; //instance of the tooltip object, if present
    private bool       hidden;          //whether or not the card is hiding off screen
    private int        siblingIndex;    //used to put card back where it belongs in the sibling list after it is brought to front for readability

    //simple FSM
    private enum State
    {
        idle,
        moving,
        casting,
        discarding
    }

    private State state;

    // Use this for initialization
    private void Awake()
    {
        //start with the target being the location it was spawned at
        idleLocation = transform.localPosition;
        targetLocation = idleLocation;

        //start idle
        state = State.idle;

        tooltipInstance = null;
    }

    //called by the hand to pass a reference to said hand
    private void SetHand(GameObject go)
    {
        hand = go;
    }

    // Update is called once per frame
    private void Update()
    {
        //bail early if idle
        if (state == State.idle)
            return;

        //calculate new position
        Vector2 newPosition = Vector2.MoveTowards(transform.localPosition,
                                                  targetLocation,
                                                  motionSpeed * Time.deltaTime);
        //move there
        transform.localPosition = newPosition;

        //go idle or die if reached target
        if (newPosition == targetLocation)
        {
            if (state == State.discarding)
            {
                Destroy(gameObject);
            }
            else
            {
                state = State.idle;
            }
        }
    }

    private void OnMouseEnter()
    {
        //ignore this event if hidden or discarding
        if (hidden || (state == State.discarding))
            return;

        siblingIndex = transform.GetSiblingIndex(); //save the current index for later
        transform.SetAsLastSibling(); //move to front

        //tell card to move up when moused over
        targetLocation = idleLocation;
        targetLocation.y += 150;
        state = State.moving;
    }

    private void OnMouseExit()
    {
        //ignore this event if hidden or discarding
        if (hidden || (state == State.discarding))
            return;

        transform.SetSiblingIndex(siblingIndex); //restore to old position in the draw order

        //tell card to reset when no longer moused over
        targetLocation = idleLocation;
        state = State.moving;
    }

    private IEnumerator OnClick()
    {
        GameObject[] cards = GameObject.FindGameObjectsWithTag ("Card"); //used for sending messages to all other cards

        //if already casting, cancel it
        if (tooltipInstance != null)
        {
            state = State.idle;  //reset state
                                 //send a message to all cards except this one to tell them to come back out
            foreach (GameObject c in cards)
                if (c != this.gameObject)
                    c.SendMessage("Show");
            //get rid of the tooltip
            GameObject.DestroyImmediate(tooltipInstance);
            yield break;
        }

        //ignore this event if hidden or discarding
        if (hidden || (state == State.discarding))
            yield break;

        //start casting when clicked
        state = State.casting;

        //send a message to all cards except this one to tell them to hide
        foreach (GameObject c in cards)
            if (c != this.gameObject)
                c.SendMessage("Hide");

        //this card has no target
        if (card.data.cardType == CardType.spell && card.data.effectData.targetingType == TargetingType.none)
        {
            //apply effects
            foreach (IEffect e in card.data.effectData.effects)
            {
                //effect must be handled differently based on effect type
                switch (e.effectType)
                {
                    case EffectType.wave:
                        LevelManagerScript.instance.data.waves[LevelManagerScript.instance.currentWave] =
                            ((IEffectWave)e).alteredWaveData(LevelManagerScript.instance.data.waves[LevelManagerScript.instance.currentWave]);

                        LevelManagerScript.instance.UpdateWaveStats();
                        break;

                    case EffectType.instant:
                        ((IEffectInstant)e).trigger();
                        break;

                    case EffectType.self:
                        ((IEffectSelf)e).trigger(ref card, gameObject);
                        break;

                    case EffectType.discard:
                        break; //discard effects are handled elsewhere

                    default:
                        Debug.LogError("I dont know how to apply an effect of type " + e.effectType);
                        break;
                }
            }
            //perform steps that must be done on every cast
            Cast();

            yield break; //we're done now
        }

        //this card has a target.  create a tooltip object to handle casting.
        tooltipInstance = (GameObject)Instantiate(tooltipPrefab, Vector3.zero, Quaternion.identity);    //instantiate prefab

        //set sprite with *twitch* WWW *twitch*
        WWW www = new WWW ("file:///" + Application.dataPath + "/StreamingAssets/Art/Sprites/" + card.data.tooltipSpriteName + ".png");
        yield return www;
        tooltipInstance.GetComponentInChildren<Image>().sprite = Sprite.Create(
            www.texture,
            new Rect(0, 0, www.texture.width, www.texture.height),
            new Vector2(0.5f, 0.5f));

        tooltipInstance.SendMessage("SetParent", this); //tell tooltip who spawned it so it can call back later

        //if tower, pass range to the tooltip
        if (card.data.cardType == CardType.tower)
            tooltipInstance.SendMessage("SetRange", card.data.towerData.range);

        //if its a spell, tell it what kind of spell it is
        if (card.data.cardType == CardType.spell)
            tooltipInstance.SendMessage("SetTargetingType", card.data.effectData.targetingType);
    }

    private void Hide()
    {
        //ignore if discarding
        if (state == State.discarding)
            return;

        //cards hide just underneath the center of the screen
        targetLocation.x = 0;
        targetLocation.y = transform.root.GetComponent<RectTransform>().rect.yMin - 200;

        state = State.moving;       //mark this card as in motion
        hidden = true;              //mark this card as hidden
    }

    private void Show()
    {
        //ignore if not hidden
        if (hidden == false)
            return;

        //ignore if discarding
        if (state == State.discarding)
            return;

        //go back to where it was spawned
        targetLocation = idleLocation;
        state = State.moving;

        hidden = false;//clear hidden flag
    }

    private void SummonTower(Vector3 location)
    {
        //summon tower
        GameObject instance = (GameObject) UnityEngine.Object.Instantiate (towerPrefab, location, Quaternion.identity);
        card.data.towerData.towerName = card.data.cardName;
        instance.SendMessage("SetData", card.data.towerData);
        if (card.data.effectData != null)
            instance.SendMessage("SetEffectData", card.data.effectData);

        //perform steps that must be done on every cast
        Cast();
    }

    private void UpgradeTower(GameObject target)
    {
        //send upgrade data to the target tower
        target.SendMessage("Upgrade", card.data.upgradeData);

        //if there are effects on the card, send them over too
        if ( (card.data.effectData != null) && (card.data.effectData.effects.Count > 0) )
            target.SendMessage("AddEffects", card.data.effectData);

        //perform steps that must be done on every cast
        Cast();
    }

    //discards this card
    private void Discard()
    {
        state = State.discarding;
        targetLocation = discardLocation;
        hand.SendMessage("Discard", gameObject);

        //run discard effects
        bool discardCancelled = false;
        if (card.data.cardType == CardType.spell)
        {
            foreach (IEffect e in card.data.effectData.effects)
            {
                if (e.effectType == EffectType.discard)
                {
                    discardCancelled = discardCancelled || ((IEffectDiscard)e).trigger(ref card);
                }
            }
        }

        //return here if the discard has been cancelled by one of the effects
        if (discardCancelled) return;

        //If any charges are left, return this card to the deck
        if (card.charges > 0)
        {
            DeckManagerScript.instance.addCardAtBottom(card);
        }
    }

    //performs steps that must be done whenever a card of any type is cast
    private void Cast()
    {
        //send a message to all cards to tell them to show themselves
        GameObject[] cards = GameObject.FindGameObjectsWithTag ("Card");
        foreach (GameObject c in cards)
            c.SendMessage("Show");

        //remove charge.
        card.charges -= 1;

        //discard self
        Discard();
    }

    private void SetIdleLocation(Vector2 newIdle)
    {
        idleLocation = newIdle; //update location

        //if card is not hidden or dying, tell it to relocate itself
        if ((hidden == false) && (state != State.discarding))
        {
            state = State.moving;
            targetLocation = idleLocation;
        }
    }

    //saves card definition data and updates components as necessarry
    private IEnumerator SetCard(Card c)
    {
        //save the data
        card = c;

        //update card text
        title.text = card.data.cardName + "\n" + card.charges + "/" + card.data.cardMaxCharges;
        updateDescriptionText();

        //load art with WWW (yes, really!  I couldn't find an easier way to do this and still let the user access the image files)
        WWW www = new WWW ("file:///" + Application.dataPath + "/StreamingAssets/Art/Card Art/" + card.data.cardArtName + ".png"); //load file
        yield return www; //wait for it to load
        art.sprite = Sprite.Create(www.texture, new Rect(0, 0, www.texture.width, www.texture.height), new Vector2(0.5f, 0.5f));
    }

    //helper function.  updates the card description text.
    private void updateDescriptionText()
    {
        description.text = card.data.getDescription();
    }
}
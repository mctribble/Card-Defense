using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Xml;
using System.Xml.Serialization;
using System.ComponentModel;
using System;
using System.IO;

//represents what type of card this is
public enum CardType {
	tower,		//summons a tower with the given stats
	upgrade,	//increases/decreases the target towers stats
	spell		//other effects
}

//represents everything needed to summon a tower
[System.Serializable]
public class TowerData : System.Object {
	[XmlIgnore] public string towerName;	//name of the card which summoned this tower.  Populated when the tower is summoned

	[DefaultValue("Default_Sprite")]
	[XmlAttribute("Sprite")] public string towerSpriteName { get; set; }

	[DefaultValueAttribute(0.0f), XmlAttribute("Recharge")]	public float 	rechargeTime;	//how long it takes tthe tower to charge
	[DefaultValueAttribute(0.0f), XmlAttribute("Range")]	public float	range;			//how far away from itself, in world space, the tower can shoot
	[DefaultValueAttribute(0.0f), XmlAttribute("Damage")]	public float	attackPower;	// amount of damage this dower does before any modifiers susch as armor
    [DefaultValueAttribute(1.0f), XmlAttribute("Lifespan")] public int      lifespan;	    // amount of waves this tower remains on the field
}

//represents everything needed to upgrade a tower
[System.Serializable]
public class UpgradeData : System.Object {
	//multiplicative modifiers
	[DefaultValueAttribute(1.0f), XmlAttribute("RechargeMult")]	public float rechargeMultiplier	= 1.0f;
	[DefaultValueAttribute(1.0f), XmlAttribute("RangeMult")]	public float rangeMultiplier	= 1.0f;
	[DefaultValueAttribute(1.0f), XmlAttribute("DamageMult")]	public float attackMultiplier	= 1.0f;

	//absolute modifiers
	[DefaultValueAttribute(0.0f), XmlAttribute("RechargeMod")]	public float rechargeModifier 	= 0.0f;
	[DefaultValueAttribute(0.0f), XmlAttribute("rangeMod")]		public float rangeModifier		= 0.0f;
	[DefaultValueAttribute(0.0f), XmlAttribute("DamageMod")]	public float attackModifier		= 0.0f;

    [DefaultValueAttribute(0), XmlAttribute("WaveBonus")]    public int waveBonus = 0;
}

//represents an effect in XML
[System.Serializable]
public class XMLEffect : System.Object {
	[XmlAttribute] public string name;
	[XmlAttribute] public float strength;
}

//represents everything needed to apply card effects
[System.Serializable]
public class EffectData : System.Object {
	//list of effects from xml
	[XmlArray("Effects")]
	[XmlArrayItem("Effect")]
	public List<XMLEffect> XMLeffects = new List<XMLEffect> ();
	[XmlIgnore]
	public bool effectsSpecified //hide effect list if it is empty
	{
		get { return XMLeffects.Count > 0; }
		set {}
	}
	
	[XmlIgnore]	public List<IEffect> effects = new List<IEffect> (); //list of effect classes
	[XmlIgnore] public TargetingType targetingType {
		get {
			if (effects.Count == 0) parseEffects(); //make sure we have actual code references to the effects

			//return the target type of the first effect that requires a target.  no card can have effects that target two different types of things
			foreach (IEffect e in effects) {
				if (e.targetingType != TargetingType.none)
					return e.targetingType;
			}
			return TargetingType.none; //if no effect needs a target, return none
		}
	}

	//translates the XMLeffects into code references
	private void parseEffects() {
		foreach (XMLEffect xe in XMLeffects)
			effects.Add (EffectTypeManagerScript.instance.parse (xe));
	}
	
}

[System.Serializable]
public class CardData : System.Object {

	//card data
	[XmlAttribute("Type")] public CardType 		cardType;	 	    //determines aspects of how the card should behave and the meaning of other values.  See enum dec for more info
	[XmlAttribute("Name")] public string 		cardName;		    //name of the card
	[XmlAttribute("Description")] public string cardDescription;    //description of the card
	[XmlAttribute("Charges")] public int 		cardMaxCharges; 	//how much it costs to use this card

	[DefaultValue("Default_Art")]
	[XmlAttribute("Art")] public string cardArtName { get; set; }

	[DefaultValue("Default_Sprite")]
	[XmlAttribute("Tooltip")] public string tooltipSpriteName { get; set; }
	
	public TowerData TowerData;			//contains info needed to summon a tower.  Only used in tower cards. 
	[XmlIgnore]
	public bool TowerDataSpecified {
		get { return cardType == CardType.tower; }
		set {}
	}

	public UpgradeData upgradeData;		//contains info needed to upgrade a tower.  Only used in upgrade cards.
	[XmlIgnore]
	public bool upgradeDataSpecified {
		get { return cardType == CardType.upgrade; }
		set {}
	}

	public EffectData EffectData;		//contains info needed to apply effects.  Only used in effect cards.
	[XmlIgnore]
	public bool EffectDataSpecified {
		get { return cardType == CardType.spell; }
		set {}
	}
}

public class CardScript : MonoBehaviour {

	public float motionSpeed;			//speed in pixels/second this card can animate
	public Vector2 discardLocation;		//location to discard self to
	public Card card;   				//holds data specific to the card itself
	public GameObject tooltipPrefab;	//used to create a tooltip
	public GameObject 	towerPrefab;	//prefab used to create a tower object
	public Image art;					//reference to card art image
	public Text  title;					//reference to card name text
	public Text  description;			//reference to card description text

	private GameObject hand;			//reference to the hand object managing this card
	private Vector2 idleLocation;		//location this card sits when it is resting
	private Vector2 targetLocation;		//location this card will move towards if it is not already there
	private GameObject tooltipInstance;	//instance of the tooltip object, if present
	private bool hidden;				//whether or not the card is hiding off screen
	private int siblingIndex;			//used to put card back where it belongs in the sibling list after it is brought to front for readability

	//simple FSM
	private enum State{
		idle,
		moving,
		casting,
		discarding
	}
	private 
		State state;

	// Use this for initialization
	void Awake () {
		//start with the target being the location it was spawned at
		idleLocation = transform.localPosition;
		targetLocation = idleLocation;

		//start idle
		state = State.idle;

		tooltipInstance = null;
	}

	//called by the hand to pass a reference to said hand
	void SetHand(GameObject go){
		hand = go;
	}
	
	// Update is called once per frame
	void Update () {
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
		if (newPosition == targetLocation) {
			if (state == State.discarding){
				Destroy(gameObject);
			} else {
				state = State.idle;
			}
		}
	}

	void OnMouseEnter(){
		//ignore this event if hidden or discarding
		if (hidden || (state == State.discarding))
			return;

		siblingIndex = transform.GetSiblingIndex (); //save the current index for later
		transform.SetAsLastSibling (); //move to front

		//tell card to move up when moused over
		targetLocation = idleLocation;
		targetLocation.y += 150;
		state = State.moving;
	}

	void OnMouseExit(){
		//ignore this event if hidden or discarding
		if (hidden || (state == State.discarding))
			return;

		transform.SetSiblingIndex (siblingIndex); //restore to old position in the draw order

		//tell card to reset when no longer moused over
		targetLocation = idleLocation;
		state = State.moving;
	}

	IEnumerator OnClick(){
		GameObject[] cards = GameObject.FindGameObjectsWithTag ("Card"); //used for sending messages to all other cards
		
		//if already casting, cancel it
		if (tooltipInstance != null) {
			state = State.idle;  //reset state
			//send a message to all cards except this one to tell them to come back out
			foreach (GameObject c in cards)
				if (c != this.gameObject)
					c.SendMessage ("Show");
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
				c.SendMessage ("Hide");

		//this card has no target
		if (card.data.cardType == CardType.spell && card.data.EffectData.targetingType == TargetingType.none) {
			//apply effects
			foreach (IEffect e in card.data.EffectData.effects) {
				//effect must be handled differently based on effect type
				switch (e.effectType) {
				case EffectType.wave:
					LevelManagerScript.instance.data.waves[LevelManagerScript.instance.currentWave] = 
						((IEffectWave)e).alteredWaveData(LevelManagerScript.instance.data.waves[LevelManagerScript.instance.currentWave]);
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
			Cast ();

            yield break; //we're done now
		}

		//this card has a target.  create a tooltip object to handle casting.
		tooltipInstance = (GameObject)Instantiate (tooltipPrefab, Vector3.zero, Quaternion.identity); 	//instantiate prefab

		//set sprite with *twitch* WWW *twitch*
		WWW www = new WWW ("file:///" + Application.dataPath + "/StreamingAssets/Art/Sprites/" + card.data.tooltipSpriteName + ".png");
		yield return www;
		tooltipInstance.GetComponentInChildren<Image> ().sprite = Sprite.Create(
			www.texture,
			new Rect(0, 0, www.texture.width, www.texture.height),
			new Vector2(0.5f, 0.5f));					

		tooltipInstance.SendMessage ("SetParent", gameObject);											//tell tooltip who spawned it so it can call back later
		tooltipInstance.SendMessage ("SetCardType", card.data.cardType);										//tell tooltip what kind of card it is

		//if tower, pass range to the tooltip
		if (card.data.cardType == CardType.tower)
			tooltipInstance.SendMessage ("SetRange", card.data.TowerData.range);

		//if its a spell, tell it what kind of spell it is
		if (card.data.cardType == CardType.spell)
			tooltipInstance.SendMessage ("SetTargetingType", card.data.EffectData.targetingType);			
	}

	void Hide(){
		//ignore if discarding
		if (state == State.discarding)
			return;

		//cards hide just underneath the center of the screen
		targetLocation.x = 0;
		targetLocation.y = transform.root.GetComponent<RectTransform>().rect.yMin - 200;

		state = State.moving;		//mark this card as in motion
		hidden = true;				//mark this card as hidden
	}

	void Show(){
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

	void SummonTower(Vector3 location){
		//summon tower
		GameObject instance = (GameObject) UnityEngine.Object.Instantiate (towerPrefab, location, Quaternion.identity);
        card.data.TowerData.towerName = card.data.cardName;
		instance.SendMessage("SetData", card.data.TowerData);

		//perform steps that must be done on every cast
		Cast ();
	}

	void UpgradeTower(GameObject target) {
		//send upgrade data to the target tower
		target.SendMessage ("Upgrade", card.data.upgradeData);

		//perform steps that must be done on every cast
		Cast ();
	}

    //discards this card
    void Discard()
    {
        state = State.discarding;
        targetLocation = discardLocation;
        hand.SendMessage("Discard", gameObject);

        //run discard effects
        bool discardCancelled = false;
        if (card.data.cardType == CardType.spell)
        {
            foreach (IEffect e in card.data.EffectData.effects)
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
	void Cast() {
		//send a message to all cards to tell them to show themselves
		GameObject[] cards = GameObject.FindGameObjectsWithTag ("Card");
		foreach (GameObject c in cards)
			c.SendMessage ("Show");

        //remove charge.  
        card.charges -= 1;

        //discard self
        Discard();
    }

	void SetIdleLocation(Vector2 newIdle){

		idleLocation = newIdle; //update location

		//if card is not hidden or dying, tell it to relocate itself
		if ( (hidden == false ) && (state != State.discarding) ) {
			state = State.moving;
			targetLocation = idleLocation;
		}
	}

	//saves card definition data and updates components as necessarry
	IEnumerator SetCard(Card c) {
        card = c;
		title.text = card.data.cardName + "\n" + card.charges + "/" + card.data.cardMaxCharges;
		description.text = card.data.cardDescription;

		//load art with WWW (yes, really!  I couldn't find an easier way to do this and still let the user access the image files)
		WWW www = new WWW ("file:///" + Application.dataPath + "/StreamingAssets/Art/Card Art/" + card.data.cardArtName + ".png"); //load file
		yield return www; //wait for it to load
		art.sprite = Sprite.Create (www.texture, new Rect (0, 0, www.texture.width, www.texture.height), new Vector2 (0.5f, 0.5f));
	}
}

﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using UnityEngine;
using UnityEngine.UI;
using Vexe.Runtime.Types;

/// <summary>
/// represents a group of enemies
/// </summary>
[System.Serializable]
public class WaveData
{
    //enemy type name, annotated to give a popup in the inspector
    private string[] getEnemyNames() { return EnemyTypeManagerScript.instance.getEnemyNames(); }
    [XmlAttribute]
    [Popup("getEnemyNames",CaseSensitive = true, Filter = true, HideUpdate = true, TextField = true)]
    public string type;

    //indicates whether or not this wave was randomly generated.  Random waves are not written back to the file when saving level definitions
    [XmlIgnore][Comment("random waves are not saved to the level file.",helpButton:true)]
    public bool isRandomWave;

    //indicates whether or not this wave is a "token".  token waves did not come from the enemy deck, meaning they would be either survivor waves or conjured from an effect of some sort
    public bool isToken;

    //like ToString(), but returns a shorter, simpler result
    public string ToShortString()
    {
        return data.name + " " + data.currentRank.ToRomanNumeral() + "x" + spawnCount;
    }

    //wave budget.  
    [XmlAttribute]
    public int budget;

    [XmlAttribute]    public float  time;
    [XmlAttribute]    public string message;
    [Hide, XmlIgnore] public int    forcedSpawnCount; //if this is negative, no spawn count was forced
    [Hide, XmlIgnore] public int    spawnedThisWave;  //number of enemies in this wave that have already been spawned

    [XmlIgnore] private EnemyData data;
    [XmlIgnore] public  EnemyData enemyData
    {
        get
        {
            if (isSurvivorWave)
                return null;

            if ( (data == null) || (data.name != type) )
                data = EnemyTypeManagerScript.instance.getEnemyTypeByName(type).clone();

            return data;
        }
        set
        {
            data = value;
            recalculateRank();
        }
    }

    [XmlIgnore] private List<EnemyScript> enemyList;

    //default.  these values are meant to be VERY noticeable if a wave is left with default data
    public WaveData()
    {
        type = "Swarm";
        budget = 999999999;
        time = 300.0f;
        message = null;
        forcedSpawnCount = -1;
        spawnedThisWave = 0;
        isRandomWave = false;
        isToken = false;
    }

    /// <summary>
    /// constructor with specific data
    /// </summary>
    /// <param name="waveType">data for the enemy to be spawned</param>
    /// <param name="waveBudget">budget for the wave</param>
    /// <param name="waveTime">how long the wave takes to spawn</param>
    /// <param name="tokenWave">whether or not this wave is a token</param>
    public WaveData(EnemyData waveType, int waveBudget, float waveTime, bool tokenWave = false)
    {
        type = waveType.name;
        enemyData = waveType;
        budget = waveBudget;
        time = waveTime;
        message = null;
        forcedSpawnCount = -1;
        spawnedThisWave = 0;
        isRandomWave = false;
        recalculateRank();
        isToken = tokenWave;
    }

    //survivor wave constructor
    public WaveData(List<EnemyScript> enemies, int spawnCount, int totalRemainingHealth, float waveTime)
    {
        data = null; //enemy data doesnt apply for survival waves, since they have multiple enemy types
        budget = 0; //enemy conjuring uses the highest budget, so we want this to be 0 so it doesnt mess with that.
        time = waveTime;
        message = null;
        forcedSpawnCount = -1;
        spawnedThisWave = 0;
        enemyList = enemies;
        forcedSpawnCount = enemies.Count;
        isRandomWave = false;
        isToken = true;
    }

    /// <summary>
    /// updates the rank based on the current wave stats.  This should be called whenever something happens that could change the spawnCount
    /// </summary>
    public void recalculateRank()
    {
        //skip on survivor waves, since rank doesnt mean anything for them
        if (isSurvivorWave)
            return;

        //set rank
        int oldRank = enemyData.currentRank;
        enemyData.currentRank = 1;
        while (spawnCount > enemyData.rankInfo.rankUpSpawnCount)
            enemyData.currentRank++;

        //if rank changed, call effects
        if (oldRank != enemyData.currentRank)
            if (enemyData.effectData != null)
                foreach (IEffect ie in enemyData.effectData.effects)
                    if (ie.triggersAs(EffectType.rank))
                        ((IEffectRank)ie).rankChanged(enemyData.currentRank);
    }

    //returns number of enemies to spawn this wave.
    public int spawnCount
    {
        get
        {
            int result = 0;

            if (forcedSpawnCount > 0)
                result = forcedSpawnCount;
            else if (enemyData == null)
                result = enemyList.Count;
            else
                result = Mathf.FloorToInt(budget / enemyData.currentSpawnCost);

            //always spawn at least one enemy
            if (result < 1)
                result = 1;

            return result;
        }
    }

    //returns the total health of all remaining enemies
    public int totalRemainingHealth
    {
        get
        {
            if (enemyList == null)
                return (spawnCount - spawnedThisWave) * enemyData.currentMaxHealth;
            else
                return enemyList.Sum(x => x.curHealth);
        }
    }

    //returns whether or not this is a survivor wave
    public bool isSurvivorWave { get { return enemyList != null; } }

    /// <summary>
    /// used for survivor waves.  reactivates the first survivor on the list and removes it from said list
    /// </summary>
    public void spawn()
    {
        //error if the enemy list is empty
        if ((enemyList == null) || (enemyList.Count == 0))
        {
            Debug.LogError("WaveData can't spawn an enemy because the list is already empty");
            return;
        }

        EnemyScript e = enemyList.First();
        enemyList.Remove(e);
        if (e != null)
            e.gameObject.SetActive(true);
        EnemyManagerScript.instance.EnemySpawned(e);
    }

    //returns a user-friendly string showing the contents of this wave for the debugger
    public override string ToString()
    {
        if (Application.isPlaying)
        {
            if (isSurvivorWave)
                return spawnCount + " Survivors {Ti: " + time.ToString("F1") + "}";
            else
                return type + " " + enemyData.currentRank.ToRomanNumeral() + "(" + spawnCount + ")" + " {Bu: " + budget + " Ti: " + time.ToString("F1") + "}";
        }
        else
        {
            return "<start game to see>";
        }
    }
}

/// <summary>
/// represents an enemy wave being shown on the screen as a card
/// </summary>
public class EnemyCardScript : CardScript
{
    [VisibleWhen("shouldShowRefs")] public Vector2    discardDisplacement; //where to move from the idle position when discarding
    [VisibleWhen("shouldShowRefs")] public GameObject art;                 //parent of card art images

    //wave stats
    [Hide] public int spawnCount;           //number of enemies that still need spawning in this wave
    [Hide] public int totalRemainingHealth; //total health of all enemies that still need spawning
    public WaveData   wave;                 //wave associated with this card
    private string    enemyType;            //name of the enemy type currently depicted.  Cached to detect enemy type changes

    private List<EnemyScript> survivorList; //if this is a survivor waves, holds references to the existing enemies that are to reappear when this wave attacks

    public override string cardName { get { return wave.ToShortString(); } } //returns the name of the card

    //init
    protected override void Awake()
    {
        base.Awake();
        survivorList = null;
    }
  
    /// <summary>
    /// sets up the wave using the given data
    /// </summary>
    public void SetWave(WaveData w)
    {
        wave = w;
        description.text = w.enemyData.getDescription();

        //set the art
        Color unitColor = wave.enemyData.unitColor.toColor();

        Sprite enemySprite = EnemyTypeManagerScript.instance.getEnemySprite(wave.enemyData.spriteName);

        foreach (Image i in art.GetComponentsInChildren<Image>())
        {
            i.sprite = enemySprite;
            i.color = unitColor;
        }

        //set the name
        enemyType = w.enemyData.name;

        //gray out card border if token
        if (w.isToken)
            cardFront.color = tokenColor;
    }

    /// <summary>
    /// sets up this card to represent survivors from the previous round
    /// </summary>
    public void SurvivorWave()
    {
        description.text = "These are survivors from the previous round, come to attack again.  Other cards cannot alter this wave.";

        //use a list of surviving enemies to initialize the card, and remove said list from the enemy manager so that it makes a new one for anything that survives this wave instead of putting them back into the same one
        wave = null;
        survivorList = EnemyManagerScript.instance.survivors;
        EnemyManagerScript.instance.survivors = null;

        //search the list to find what we need to finish the setup
        spawnCount = 0;
        totalRemainingHealth = 0;
        int numTypesFound = 0;
        string[] typesFound = new string[6];
        SpriteRenderer[] spritesToSet = new SpriteRenderer[6];

        //error catch: purge survivors that are actually dead
        survivorList.RemoveAll(go => go == null);

        foreach (EnemyScript e in survivorList)
        {
            spawnCount++;
            totalRemainingHealth += e.curHealth;

            if ( (numTypesFound <= 5) && (typesFound.Contains(e.enemyTypeName) == false) )
            {
                typesFound[numTypesFound] = e.enemyTypeName;
                spritesToSet[numTypesFound] = e.enemyImage;
                numTypesFound++;
            }
        }

        //set the sprites
        Image[] artImages = art.GetComponentsInChildren<Image>();
        int spriteIndex = 0;
        foreach(Image i in artImages)
        {
            i.sprite = spritesToSet[spriteIndex].sprite;
            i.color = spritesToSet[spriteIndex].color;
            spriteIndex = (spriteIndex + 1) % numTypesFound;
        }

        //setup the WaveData object
        wave = new WaveData(survivorList, spawnCount, totalRemainingHealth, LevelManagerScript.instance.currentWaveTime);

        //gray out the card border
        cardFront.color = tokenColor;
    }

    // Update is called once per frame
    protected override void Update()
    {
        base.Update();

        //update title text (???????x????)
        if (survivorList == null)
            title.text = "<color=#" + wave.enemyData.unitColor.toHex() + ">" + wave.enemyData.name + " " + wave.enemyData.currentRank.ToRomanNumeral() + "</color> x" + (wave.spawnCount - wave.spawnedThisWave);
        else
            title.text = "Survivors x" + (wave.spawnCount - wave.spawnedThisWave);

        //if this is not a survivor wave, and the enemy type changed, update description and art as well
        if ((survivorList == null) && (enemyType != wave.enemyData.name))
        {
            description.text = wave.enemyData.getDescription();
            foreach (Image i in art.GetComponentsInChildren<Image>())
                i.color = wave.enemyData.unitColor.toColor();
            enemyType = wave.enemyData.name;
        }
    }

    //discards this card
    public override IEnumerator Discard()
    {
        //bail if this card has already been destroyed
        if (gameObject == null)
            yield break;

        state = CardState.discarding;
        hand.SendMessage("Discard", this);

        //move to discard location
        Vector3 discardLocation = idleLocation + discardDisplacement; 
        while (transform.localPosition != discardLocation)
        {
            transform.localPosition = Vector3.MoveTowards(transform.localPosition, discardLocation, (motionSpeed * Time.deltaTime));
            yield return null;
        }

        //destroy ourselves
        Destroy(gameObject);
    }

    //update stats for this wave
    public void updateWaveStats()
    {
        spawnCount = wave.spawnCount;
        totalRemainingHealth = wave.totalRemainingHealth;
    }

    /// <summary>
    /// applies the given effect to the wave, provided it is not a survivor wave
    /// </summary>
    public void applyWaveEffect(IEffectWave e)
    {
        if (wave.isSurvivorWave == false)
        {
            wave = e.alteredWaveData(wave);
            updateWaveStats();
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

        //cards hide just above the center of the screen
        targetLocation.x = 0;
        targetLocation.y = transform.root.GetComponent<RectTransform>().rect.yMax + 200;

        state = CardState.moving;       //mark this card as in motion
        hidden = true;              //mark this card as hidden
    }

    /// <summary>
    /// updates the card description text.
    /// </summary>
    public override void updateDescriptionText()
    {
        description.text = wave.enemyData.getDescription();
    }

    /// <summary>
    /// triggers all effects on this card that are meant to fire when the card is drawn.  This also shows any wave messages, if they are present
    /// </summary>
    public override void triggerOnDrawnEffects()
    {
        StartCoroutine(triggerOnDrawnEffectsCoroutine());
    }

    private IEnumerator triggerOnDrawnEffectsCoroutine()
    {
        //wait for cards to be done moving around
        yield return EnemyHandScript.instance.waitForReady();
        yield return PlayerHandScript.instance.waitForReady();

        if (wave.isSurvivorWave == false)
        {
            //effects
            if (wave.enemyData.effectData != null)
                foreach (IEffect ie in wave.enemyData.effectData.effects)
                    if (ie.triggersAs(EffectType.cardDrawn))
                        ((IEffectInstant)ie).trigger();

            //wave messages
            if (wave.message != null && wave.message != "")
                MessageHandlerScript.ShowNoYield(wave.message);
        }
    }
}
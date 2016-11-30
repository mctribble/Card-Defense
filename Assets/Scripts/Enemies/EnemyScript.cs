﻿using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;
using UnityEngine;
using UnityEngine.UI;
using Vexe.Runtime.Types;

/// <summary>
/// XML representation of a color.  components are floats from 0 to 1.
/// </summary>
[System.Serializable]
public class XMLColor
{
    [XmlAttribute][fSlider(0.0f,1.0f)] public float r;
    [XmlAttribute][fSlider(0.0f,1.0f)] public float g;
    [XmlAttribute][fSlider(0.0f,1.0f)] public float b;
    [XmlAttribute][fSlider(0.0f,1.0f)] public float a;

    //returns a unity color
    public Color toColor()
    {
        return new Color(r, g, b, a);
    }

    //returns a hex string for use with rich text formatting and such
    public string toHex()
    {
        //each value is multiplied by 255, rounded off, and then added to the result string as a hex value
        return
            Mathf.RoundToInt(r * 255).ToString("X2") +
            Mathf.RoundToInt(g * 255).ToString("X2") +
            Mathf.RoundToInt(b * 255).ToString("X2") +
            Mathf.RoundToInt(a * 255).ToString("X2");
    }

    public override string ToString()
    {
        return "{" + r.ToString("F2") + ", " + g.ToString("F2") + ", " + b.ToString("F2") + ", " + a.ToString("F2") + "}";
    }
};

/// <summary>
/// contains everything needed to define an enemy type
/// </summary>
[System.Serializable]
public class EnemyData
{
    //indicates whether this enemy definition
    [XmlIgnore][Comment("Modded enemy definitions are not saved from the inspector.")]
    public bool isModded = false;

    [XmlAttribute]             public string name;       //used to identify this enemy type
    [XmlAttribute][iMin(1)]    public int    spawnCost;	 //used for wave generation: more expensive enemies spawn in smaller numbers
    [XmlAttribute][iMin(1)]    public int    attack;     //number of charges knocked off if the enemy reaches the goal
    [XmlAttribute][iMin(1)]    public int    maxHealth;  //max health
    [XmlAttribute][fMin(0.1f)] public float  unitSpeed;  //speed, measured in distance/second

    public XMLColor   unitColor;  //used to colorize the enemy sprite
    public EffectData effectData; //specifies which effects are attached to this enemy type and what their parameters are

    //only write effect data if there is data to write
    [XmlIgnore]
    public bool effectDataSpecified
    {
        get { return (effectData != null) && (effectData.XMLEffects.Count != 0); }
        set { }
    }

    [DefaultValue("Enemy_Basic")]
    [XmlAttribute("sprite")]
    public string spriteName { get; set; }

    /// <summary>
    /// slow function that returns a description of this enemy type
    /// </summary>
    public string getDescription()
    {
        //enemy stats
        string description = "Health: " + maxHealth + '\n' +
                             "Attack: " + attack    + '\n' +
                             "Speed: "  + unitSpeed;

        //effects
        if ((effectData != null) && (effectData.effects.Count > 0))
            foreach (IEffect e in effectData.effects)
                if (e.Name != null)
                    description += "\n" + "<Color=#" + e.effectColorHex + ">-" + e.Name + "</Color>";

        return description;
    }

    /// <summary>
    /// makes a copy of this enemyData
    /// </summary>
    public EnemyData clone()
    {
        EnemyData clone = (EnemyData)this.MemberwiseClone();
        if (effectData != null)
            clone.effectData = this.effectData.clone();
        return clone;
    }

    public override string ToString() { return name; } //for friendlier display in debugger
};

/// <summary>
/// represents an active enemy in the world
/// </summary>
public class EnemyScript : BaseBehaviour
{
    public string         enemyTypeName;      //name of this type of enemy
    public List<Vector2>  path;               //list of points this unit must go to
    public int            currentDestination; //index in the path that indicates the current destination
    public Vector2        startPos;           //position where this enemy was spawned
    public int            curHealth;          //current health
    public int            expectedHealth;     //what health will be after all active shots reach this enemy
    public SpriteRenderer enemyImage;         //sprite component for this enemy
    public Image          healthbar;          //image used for the health bar
    public Image          deathBurst;         //image used for the death explosion
    public float          deathBurstSize;     //max scale of the death burst
    public float          deathBurstTime;     //time taken to animate death burst

    //sound settings
    public AudioSource audioSource;     //source to use to play sounds from
    public AudioClip[] enemyHitSounds;  //sounds to play when the player is hurt.  one of these is chosen at random.
    public AudioClip[] deathSounds;     //sounds to play when this enemy is dead.  one of these is chosen at random.
    public static int  maxSoundsAtOnce; //limit to how many sounds can be played at once, shared across ALL enemies
    private static int curSoundsAtOnce; //number of sounds currently playing, shared across ALL enemies

    //enemy data
    public int        damage;        
    public int        maxHealth;
    public float      baseUnitSpeed;    
    public float      unitSpeed; 
    public EffectData effectData;

    //used for health bar
    public Color    healthyColor; //color when healthy
    public Color    dyingColor;   //color when near death

    public bool goalFinalChance; //this is true for the very short period between reaching the goal and dealing damage to give attacks a final chance to kill this enemy

    // Use this for initialization
    private void Awake()
    {
        //init vars
        curHealth = maxHealth;
        expectedHealth = maxHealth;
        startPos = transform.position;
        enemyImage = GetComponent<SpriteRenderer>();
        goalFinalChance = false;
    }

    // LateUpdate is called once per frame, after other objects have done a regular Update().  We use LateUpdate to make sure bullets get to move first this frame.
    private void LateUpdate()
    {
        //clean out the effect list every 32 frames
        if (effectData != null)
            if ((Time.frameCount % 32) == 0)
                effectData.cleanEffects();

        //skip update if dead
        if (curHealth <= 0)
            return;

        //skip update if paused for the reachedGoal() coroutine
        if (goalFinalChance)
            return;

        //tick periodic effects.  this uses a helper function so the effectData class can save performance and not search the list every frame
        if (effectData != null)
            effectData.triggerAllPeriodicEnemy(this, Time.deltaTime);

        moveForwardByTime(Time.deltaTime);

        //update health bar fill and color
        float normalizedHealth = (float)curHealth / (float)maxHealth;
        healthbar.color = Color.Lerp(dyingColor, healthyColor, normalizedHealth);
        healthbar.fillAmount = normalizedHealth;
    }

    /// <summary>
    /// moves the enemy forward this far in time.  
    /// separate from update function so it can be called elsewhere, such as during enemy spawning
    /// </summary>
    public void moveForwardByTime(float time)
    {
        //bail immediately if time is 0 or negative
        if (time <= 0)
            return;

        float distanceToTravel = unitSpeed * time;                                                           //calculate distance to travel
        Vector2 prevLocation = transform.position;                                                           //fetch current location
        Vector2 newLocation = Vector2.MoveTowards(prevLocation, path[currentDestination], distanceToTravel); //perform movement
        distanceToTravel -= Vector2.Distance(prevLocation, newLocation);                                     //check how far we actually moved

        //if we havent moved far enough yet, repeat until we have (with a small margin of error to account for float math)
        while (distanceToTravel > 0.001f) 
        {
            currentDestination++; //since we didn't travel the full distance, we reached our destination and need to advance to the next one
            
            //if we hit the end of the line, stop moving and handle that instead
            if (path.Count == currentDestination)
            {
                transform.position = new Vector3(newLocation.x, newLocation.y, transform.position.z); //save position
                StartCoroutine(reachedGoal()); //handle having reached the goal
                return; //bail early
            }

            prevLocation = newLocation;
            newLocation = Vector2.MoveTowards(prevLocation, path[currentDestination], distanceToTravel);
            distanceToTravel -= Vector2.Distance(prevLocation, newLocation);
        }
        

        //save position
        transform.position = new Vector3(newLocation.x, newLocation.y, transform.position.z);

        //if reached the current destination, attempt to move to the next one
        if (path[currentDestination] == newLocation)
        {
            currentDestination++;

            //if we reached the end, trigger the proper coroutine
            if (path.Count == currentDestination)
                StartCoroutine(reachedGoal());
        }
    }

    /// <summary>
    /// [COROUTINE] handles the enemy reaching the goal
    /// </summary>
    private IEnumerator reachedGoal()
    {
        //pause normal enemy behavior while we handle this
        goalFinalChance = true;

        //if we are expecting damage that hasnt happened yet, give those a couple frames to land before continuing (normal bullets will "blink" to the enemy instantaneously.  see bulletScript.cs)
        if (expectedHealth < curHealth)
        {
            yield return null; //(force pause to be at least 2 frames no matter how bad the framerate is)
            yield return null;
        }

        //if we died in that period, bail from this coroutine to let onDeath handle things
        if (curHealth <= 0)
            yield break;

        //if we are still expecting damage after that window, cancel it.
        if (expectedHealth < curHealth)
            expectedHealth = curHealth;

        //trigger effects...
        if (effectData != null)
            foreach (IEffect e in effectData.effects)
                if (e.triggersAs(EffectType.enemyReachedGoal))
                    ((IEffectEnemyReachedGoal)e).trigger(this);

        //damage player...
        if (damage > 0)
        {
            DeckManagerScript.instance.SendMessage("Damage", damage);
            MessageHandlerScript.instance.spawnPlayerDamageText(transform.localPosition, damage);
        }

        ScoreManagerScript.instance.flawless = false;

        //reset the enemy
        transform.position = startPos;
        currentDestination = 0;
        goalFinalChance = false;

        //update wave stats
        LevelManagerScript.instance.deadThisWave++;
        LevelManagerScript.instance.totalRemainingHealth -= curHealth;

        //report it as a survivor, and then disable it until it is spawned into the next wave
        EnemyManagerScript.instance.EnemySurvived(gameObject);
        gameObject.SetActive(false);        

        yield break; //done
    }

    /// <summary>
    /// tracks damage that WILL arrive so that towers dont keep shooting something that is about to be dead
    /// </summary>
    public void onExpectedDamage(ref DamageEventData e)
    {
        //deal with effects that need to happen when we expect damage
        if (effectData != null)
            foreach (IEffect i in effectData.effects)
                if (i.triggersAs(EffectType.enemyDamaged))
                    ((IEffectEnemyDamaged)i).expectedDamage(ref e);

        //expect to take damage
        expectedHealth -= Mathf.CeilToInt(e.rawDamage);

        //if a death is expected, report self as dead so towers ignore this unit
        if (expectedHealth <= 0)
            EnemyManagerScript.instance.SendMessage("EnemyExpectedDeath", gameObject);
    }

    /// <summary>
    /// deals damage to the enemy.  (make sure to call onExpectedDamage() first to avoid targeting bugs)
    /// </summary>
    public void onDamage(DamageEventData e)
    {
        //dont bother if we are already dead
        if (curHealth <= 0)
            return;

        //trigger enemyDamaged effects
        if (effectData != null)
        {
            foreach (IEffect i in effectData.effects)
            {
                if (i.triggersAs(EffectType.enemyDamaged))
                {
                    //DEBUG CHECK: actualDamage() should not alter damage value
                    float checkDamage = e.rawDamage;

                    ((IEffectEnemyDamaged)i).actualDamage(ref e);

                    //DEBUG CHECK: actualDamage() should not alter damage value
                    if (checkDamage != e.rawDamage)
                        Debug.LogWarning(i.XMLName + " has altered rawDamage in actualDamage()!");
                }
            }
        }

        //copy periodic effects from the attack onto this unit so they can take effect
        if (e.effects != null)
        {
            foreach (IEffect toCopy in e.effects.effects)
            {
                if (toCopy.triggersAs(EffectType.periodic))
                {
                    //found an effect we need to copy.  first make sure we have an object to copy it to.
                    if (effectData == null)
                        effectData = new EffectData();

                    //then copy it
                    effectData.Add(EffectData.cloneEffect(toCopy));
                }
            }
        }

        //take damage
        int damage = Mathf.CeilToInt(e.rawDamage);
        damage = System.Math.Min(damage, curHealth);
        curHealth -= damage;
        LevelManagerScript.instance.totalRemainingHealth -= damage;

        //sound
        if (damage > 0)
        {
            int soundToPlay = Random.Range(0, enemyHitSounds.Length);
            audioSource.clip = enemyHitSounds[soundToPlay];
            //audioSource.Play();
            StartCoroutine(playRespectLimit(audioSource)); //plays the sound, if we are not at the sound cap
        }

        if (curHealth <= 0)
        {
            //catch a recurring issue where the enemy does not get the expected damage event and thus dies without expecting it
            if (expectedHealth > 0)
            {
                Debug.LogWarning("Enemy did not expect to die, but did anyway!  This can cause targeting issues as towers attack an enemy that will die anyway.");
                expectedHealth = 0;
                EnemyManagerScript.instance.EnemyExpectedDeath(gameObject);
            }

            //if dead, report the kill to the tower that shot it
            LevelManagerScript.instance.deadThisWave++;

            //(bugfix: works around race condition where an enemy is deactivated and then dies immediately afterward
            if (gameObject.activeSelf == false)
                gameObject.SetActive(true);

            //and start the death coroutine
            StartCoroutine(onDeath());
        }
    }

    /// <summary>
    /// triggers any effects on this enemy that are meant to run when the enemy spawns.
    /// </summary>
    public void triggerOnEnemySpawned()
    {
        if (effectData != null)
            foreach (IEffect ie in effectData.effects)
                if (ie.triggersAs(EffectType.enemySpawned))
                    ((IEffectOnEnemySpawned)ie).onEnemySpawned(this);
    }

    //stores a new path for this unit to follow
    private void SetPath(List<Vector2> p)
    {
        path = p;               //save path
        currentDestination = 0; //go towards the first destination
    }

    //stores the data specific to this type of enemy
    private System.Collections.IEnumerator SetData(EnemyData d)
    {
        enemyTypeName  = d.name;
        damage         = d.attack;
        maxHealth      = d.maxHealth;
        curHealth      = d.maxHealth;
        expectedHealth = d.maxHealth;
        unitSpeed      = d.unitSpeed;
        baseUnitSpeed  = d.unitSpeed;

        if (d.effectData != null)
            effectData = d.effectData.clone();
        else
            effectData = null;

        this.GetComponent<SpriteRenderer>().color = d.unitColor.toColor();

        //yes, I know its awkward, but we're setting the sprite with WWW, even on PC
        string spritePath = "";
        if (Application.platform != RuntimePlatform.WebGLPlayer)
            spritePath = "file:///";
        spritePath += Application.streamingAssetsPath + "/Art/Sprites/" + d.spriteName;
        WWW www = new WWW (spritePath);
        yield return www;

        if (www.error == null)
            enemyImage.sprite = Sprite.Create(www.texture, new Rect(0, 0, www.texture.width, www.texture.height), new Vector2(0.5f, 0.5f));
        else
            enemyImage.sprite = Resources.Load<Sprite>("Sprites/Error");
    }

    /// <summary>
    /// returns the distance from this enemy's current position to the goal, following its current path
    /// </summary>
    public float distanceToGoal()
    {
        //distance is 0 if we are at the goal
        if (path.Count == currentDestination)
            return 0.0f;

        float result = Vector2.Distance(transform.position, path[currentDestination]); //start with distance to the current destination...

        //..and add the length of each subsequent segment
        for (int segment = currentDestination + 1; segment < path.Count; segment++)
            result += Vector2.Distance(path[segment - 1], path[segment]);

        return result;
    }

    /// <summary>
    /// [COROUTINE] handles enemy death
    /// </summary>
    private IEnumerator onDeath()
    {
        //sound
        int soundToPlay = Random.Range(0, deathSounds.Length);
        audioSource.clip = deathSounds[soundToPlay];
        //audioSource.Play();
        StartCoroutine(playRespectLimit(audioSource)); //plays the sound, if we are not at the sound cap

        //disable normal images and turn on the death burst instead
        enemyImage.enabled = false;
        healthbar.enabled = false;
        deathBurst.enabled = true;
        deathBurst.color = enemyImage.color;

        //expand burst to deathBurstSize over deathBurstTime
        float timer = 0.0f;
        float curScale = 0.0f;
        while (timer < deathBurstTime)
        {
            timer += Time.deltaTime;
            curScale = Mathf.Lerp(0, deathBurstSize, (timer / deathBurstTime) );
            deathBurst.rectTransform.localScale = new Vector3(curScale, curScale, 1 );
            yield return null;
        }

        //trigger effects
        if (effectData != null)
            foreach (IEffect ie in effectData.effects)
                if (ie.triggersAs(EffectType.death))
                    ((IEffectDeath)ie).onEnemyDeath(this);

        //hide the burst so there is no visual indication we are still here
        deathBurst.enabled = false;

        //wait for sound to finish before destroying the object
        while (audioSource.isPlaying)
            yield return null;

        Destroy(gameObject);
        yield break;
    }

    /// <summary>
    /// plays a sound from the given source if the limit of simultaneous sounds has not been reached.
    /// Also tracks number of sounds playing
    /// </summary>
    private IEnumerator playRespectLimit(AudioSource source)
    {
        //skip if at the cap
        if (curSoundsAtOnce == maxSoundsAtOnce)
            yield break;

        //otherwise, play the sound and track it
        curSoundsAtOnce++;
        source.Play();
        while (source.isPlaying)
            yield return null;
        curSoundsAtOnce--;
    }
}
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;
using UnityEngine;
using UnityEngine.UI;
using Vexe.Runtime.Types;

//a Color class that is nicely formatted in xml
[System.Serializable]
public class XMLColor
{
    [XmlAttribute] public float r;
    [XmlAttribute] public float g;
    [XmlAttribute] public float b;
    [XmlAttribute] public float a;

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
        return "{" + r.ToString("2F") + ", " + g.ToString("2F") + ", " + b.ToString("2F") + ", " + a.ToString("2F") + "}";
    }
};

//contains everything needed to define an enemy type
[System.Serializable]
public class EnemyData
{
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

    public string getDescription()
    {
        string description = "Health: " + maxHealth + '\n' +
                             "Attack: " + attack    + '\n' +
                             "Speed: "  + unitSpeed;

        if ((effectData != null) && (effectData.effects.Count > 0))
            foreach (IEffect e in effectData.effects)
                description += "\n" + "<Color=#" + e.effectColorHex + ">" + e.Name + "</Color>";

        return description;
    }

    public override string ToString() { return name; }
};

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

    //moves the enemy forward this far in time.  seperate from update function so it can be called elsewhere, such as during enemy spawning to account for low frame rate
    private void moveForwardByTime(float time)
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

    //handles the enemy reaching the goal
    private IEnumerator reachedGoal()
    {
        //pause normal enemy behavior while we handle this
        goalFinalChance = true;

        //if we are expecting damage that hasnt happened yet, give those a moment to land before continuing
        if (expectedHealth < curHealth)
        {
            yield return null; //(force pause to be at least 2 frames no matter how bad the framerate is)
            yield return new WaitForSeconds(0.5f);
        }

        //if we are still expecting damage after that window, cancel it and tell attackers to abort
        if (expectedHealth < curHealth)
        {
            expectedHealth = curHealth;
            GameObject[] bullets = GameObject.FindGameObjectsWithTag("Bullet");
            foreach (GameObject b in bullets)
                b.SendMessage("AbortAttack", gameObject);
        }

        //trigger effects...
        if (effectData != null)
            foreach (IEffect e in effectData.effects)
                if (e.triggersAs(EffectType.enemyReachedGoal))
                    ((IEffectEnemyReachedGoal)e).trigger(this);

        //damage player...
        DeckManagerScript.instance.SendMessage("Damage", damage);
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

    //tracks damage that WILL arrive so that towers dont keep shooting something that is about to be dead
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

    //receives damage
    public void onDamage(DamageEventData e)
    {
        //dont bother if we are already dead
        if (curHealth <= 0)
            return;

        //enemyDamaged effects get triggered, others are copied to the enemy
        if (effectData != null)
            foreach (IEffect i in effectData.effects)
                if (i.triggersAs(EffectType.enemyDamaged))
                    ((IEffectEnemyDamaged)i).actualDamage(ref e);

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

        if (curHealth <= 0)
        {
            //catch a recurring issue where the enemy does not get thee xpected damage event and thus dies without expecting it
            if (expectedHealth > 0)
            {
                Debug.LogWarning("Enemy did not expect to die, but did anyway!  This can cause targeting issues as towers attack an enemy that will die anyway.");
                expectedHealth = 0;
                EnemyManagerScript.instance.EnemyExpectedDeath(gameObject);
            }

            //if dead, report the kill to the tower that shot it
            LevelManagerScript.instance.deadThisWave++;

            //and start the death coroutine
            StartCoroutine(onDeath());
        }
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
        enemyTypeName           = d.name;
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

        //yes, I know its awkward, but we're setting the sprite with WWW.
        WWW www = new WWW ("file:///" + Application.dataPath + "/StreamingAssets/Art/Sprites/" + d.spriteName);
        yield return www;
        enemyImage.sprite = Sprite.Create(www.texture, new Rect(0, 0, www.texture.width, www.texture.height), new Vector2(0.5f, 0.5f));
    }

    //returns the distance from this enemy's current position to the goal, following its current path
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

    //handles enemy death
    private IEnumerator onDeath()
    {
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

        Destroy(gameObject);
        yield break;
    }
}
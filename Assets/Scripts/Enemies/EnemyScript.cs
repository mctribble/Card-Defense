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
};

//contains everything needed to define an enemy type
[System.Serializable]
public class EnemyData
{
    [XmlAttribute] public string     name;       //used to identify this enemy type
    [XmlAttribute] public int        spawnCost;	 //used for wave generation: more expensive enemies spawn in smaller numbers
    [XmlAttribute] public int        attack;     //number of charges knocked off if the enemy reaches the goal
    [XmlAttribute] public int        maxHealth;  //max health
    [XmlAttribute] public float      unitSpeed;  //speed, measured in distance/second
                   public XMLColor   unitColor;  //used to colorize the enemy sprite
                   public EffectData effectData; //specifies which effects are attached to this enemy type and what their parameters are

    [DefaultValue("Enemy_Basic")]
    [XmlAttribute("sprite")]
    public string spriteName { get; set; }
};

public class EnemyScript : BaseBehaviour
{
    public List<Vector2>  path;               //list of points this unit must go to
    public int            currentDestination; //index in the path that indicates the current destination
    public Vector2        startPos;           //position where this enemy was spawned
    public int            curHealth;          //current health
    public int            expectedHealth;     //what health will be after all active shots reach this enemy
    public SpriteRenderer enemyImage;         //sprite component for this enemy

    //enemy data
    public int        damage;        
    public int        maxHealth;     
    public float      unitSpeed; 
    public EffectData effectData;

    //used for health bar
    public Color    healthyColor; //color when healthy
    public Color    dyingColor;   //color when near death

    // Use this for initialization
    private void Awake()
    {
        //init vars
        curHealth = maxHealth;
        expectedHealth = maxHealth;
        startPos = transform.position;
        enemyImage = GetComponent<SpriteRenderer>();
    }

    // LateUpdate is called once per frame, after other objects have done a regular Update().  We use LateUpdate to make sure bullets get to move first this frame.
    private void LateUpdate()
    {
        //tick periodic effects.  this uses a helper function so the effectData class can save performance and not search the list every frame
        if (effectData != null)
            effectData.triggerAllPeriodicEnemy(this, Time.deltaTime);

        Vector2 curLocation = transform.position; //fetch current location
        Vector2 newLocation = Vector2.MoveTowards (curLocation, path[currentDestination], unitSpeed * Time.deltaTime); //calculate new location

        //save position
        transform.position = new Vector3(newLocation.x, newLocation.y, transform.position.z);

        //if reached the current destination, attempt to move to the next one
        if (curLocation == newLocation)
        {
            currentDestination++;

            if (path.Count == currentDestination)
            {
                //reached the end.  trigger effects...
                if (effectData != null)
                    foreach (IEffect e in effectData.effects)
                        if (e.effectType == EffectType.enemyReachedGoal)
                            ((IEffectEnemyReachedGoal)e).trigger(this);

                //damage player...
                DeckManagerScript.instance.SendMessage("Damage", damage);

                //...and go back to start for another lap
                transform.position = startPos;
                currentDestination = 0;

                //if the enemy is not expected to die, update the enemy list with the new pathing info
                if (expectedHealth > 0)
                    EnemyManagerScript.instance.SendMessage("EnemyPathChanged", gameObject);
            }
        }

        //update health bar fill and color
        Image healthbar = gameObject.GetComponentInChildren<Image> ();
        float normalizedHealth = (float)curHealth / (float)maxHealth;
        healthbar.color = Color.Lerp(dyingColor, healthyColor, normalizedHealth);
        healthbar.fillAmount = normalizedHealth;
    }

    //tracks damage that WILL arrive so that towers dont keep shooting something that is about to be dead
    public void onExpectedDamage(ref DamageEventData e)
    {
        //deal with effects that need to happen when we expect damage
        if (effectData != null)
            foreach (IEffect i in effectData.effects)
                if (i.effectType == EffectType.enemyDamaged)
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

        //deal with effects that need to happen when we actually take damage
        if (effectData != null)
            foreach (IEffect i in effectData.effects)
                if (i.effectType == EffectType.enemyDamaged)
                    ((IEffectEnemyDamaged)i).actualDamage(ref e);

        //take damage
        int damage = Mathf.CeilToInt(e.rawDamage);
        damage = System.Math.Min(damage, curHealth);
        curHealth -= damage;
        LevelManagerScript.instance.WaveTotalRemainingHealth -= damage;

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
            Destroy(gameObject);
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
        damage         = d.attack;
        maxHealth      = d.maxHealth;
        curHealth      = d.maxHealth;
        expectedHealth = d.maxHealth;
        unitSpeed      = d.unitSpeed;

        if (d.effectData != null)
            effectData = d.effectData.clone();
        else
            effectData = null;

        this.GetComponent<SpriteRenderer>().color = d.unitColor.toColor();

        //yes, I know its awkward, but we're setting the sprite with WWW.
        WWW www = new WWW ("file:///" + Application.dataPath + "/StreamingAssets/Art/Sprites/" + d.spriteName + ".png");
        yield return www;
        enemyImage.sprite = Sprite.Create(www.texture, new Rect(0, 0, www.texture.width, www.texture.height), new Vector2(0.5f, 0.5f));
    }

    //returns the distance from this enemy's current position to the goal, following its current path
    public float distanceToGoal()
    {
        float result = Vector2.Distance(transform.position, path[currentDestination]); //start with distance to the current destination...

        //..and add the length of each subsequent segment
        for (int segment = currentDestination + 1; segment < path.Count; segment++)
            result += Vector2.Distance(path[segment - 1], path[segment]);

        return result;
    }
}
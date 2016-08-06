using UnityEngine;
using System.Collections;
using Vexe.Runtime.Types;
using System.Collections.Generic;

//contains all the data this object needs to receive from the firing tower
public struct BurstShotData
{
    public DamageEventData  damageEvent; //damage event provided by the tower.  dest is ignored.
    public List<GameObject> targetList;  //enemies to be hit
    public float            burstRange;  //max size of the burst animation
}

//round burst attack used by towers with TargetAll.  expands to the towers range and attacks enemies as it reaches them.
public class BurstShotScript : BaseBehaviour
{
    public float speed; //speed of the attack wave
    public Color color; //default color to use for the burst

    public SpriteRenderer spriteRenderer; //component reference

    private bool                  initialized;  //whether or not this shot has been initialized
    private List<DamageEventData> damageEvents; //damage events to be used for each enemy hit
    private float                 curScale;     //current scale of this attack
    private float                 maxScale;     //maximum scale this attack should reach

    //set default color when spawned
    private void Start() { spriteRenderer.color = color; }

    //overrides the default color
    public void SetColor(Color newColor)
    {
        color = newColor;
        spriteRenderer.color = newColor;
    }

    //init attack
    void SetData (BurstShotData d)
    {
        //only allow this to be setup once
        if (initialized)
        {
            Debug.LogWarning("duplicate burst init ignored!");
            return;
        }

        curScale = 0.0f; //start at size 0
        maxScale = d.burstRange; //save range

        //populate event list
        damageEvents = new List<DamageEventData>(d.targetList.Count);
        foreach (GameObject curTarget in d.targetList)
        {
            //each target gets its own copy of the damage event with a different value for dest
            //this way changes from one enemy (i. e, armor) dont propagate to all enemies attacked by the same shot
            DamageEventData curEvent = new DamageEventData();
            curEvent.rawDamage = d.damageEvent.rawDamage;
            curEvent.source = d.damageEvent.source;
            curEvent.dest = curTarget;
            curEvent.effects = d.damageEvent.effects;

            //also perform the onDamageExpected calls
            if (curEvent.effects != null)
                foreach (IEffect effect in curEvent.effects.effects)
                    if (effect.effectType == EffectType.enemyDamaged)
                        ((IEffectEnemyDamaged)effect).expectedDamage(ref curEvent);
            curEvent.dest.GetComponent<EnemyScript>().onExpectedDamage(ref curEvent);

            damageEvents.Add(curEvent); //and add it to the list
        }
        //flag ourselves as set up
        initialized = true;
    }
	
	// Update is called once per frame
	void Update ()
    {
        if (initialized == false)
            return; //only update once we've been initialized

        curScale += speed * Time.deltaTime;                        //expand the burst
        curScale = Mathf.Min(curScale, maxScale);                  //enforce max size
        transform.localScale = new Vector3(curScale, curScale, 1); //update transform

        //deal damage to enemies that are now within range and remove them from the list
        //we cant modify the list while iterating on it, so we have to do removals in a second pass
        List<DamageEventData> eventsToRemove = new List<DamageEventData>(); 
        for (int i = 0; i < damageEvents.Count; i++)
        {
            DamageEventData curEvent = damageEvents[i];

            //calculate distance
            float dist;
            if (curScale == maxScale)
                dist = 0.0f; //if the explosion is over, hit everything still on the list
            else
                dist = Vector2.Distance(transform.position, curEvent.dest.transform.position);
            
            // if the enemy is in range
            if ( dist < curScale )
            {
                //damage effects
                if (curEvent.effects != null)
                    foreach (IEffect effect in curEvent.effects.effects)
                        if (effect.effectType == EffectType.enemyDamaged)
                            ((IEffectEnemyDamaged)effect).actualDamage(ref curEvent);

                //damage
                curEvent.dest.GetComponent<EnemyScript>().onDamage(curEvent);

                eventsToRemove.Add(curEvent); //mark for removal
            }
        }

        foreach (DamageEventData curEvent in eventsToRemove)
            damageEvents.Remove(curEvent);

        //if we are at max range, destroy this shot
        if (curScale == maxScale)
            Destroy(gameObject);
    }
}

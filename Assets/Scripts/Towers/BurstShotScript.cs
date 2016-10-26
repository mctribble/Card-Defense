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
    public float speed;     //speed of the attack wave
    public Color color;     //default color to use for the burst
    public float lookAhead; //seconds to look ahead when warning enemies about oncoming damage

    public SpriteRenderer spriteRenderer; //component reference

    private bool                  initialized;     //whether or not this shot has been initialized
    private List<DamageEventData> expectedToHit;   //list of enemies that we told to expect damage and the events associated with those hits
    private List<GameObject>      alreadyHit;      //list of enemies we already dealt damage
    private DamageEventData       baseDamageEvent; //damage event to base all the others on
    private float                 curScale;        //current scale of this attack
    private float                 maxScale;        //maximum scale this attack should reach

    //init
    private void Awake()
    {
        spriteRenderer.color = color;
        curScale = 0.0f;
        expectedToHit = new List<DamageEventData>();
        alreadyHit = new List<GameObject>();
    }

    //init attack
    void SetData (BurstShotData data)
    {
        maxScale = data.burstRange;
        baseDamageEvent = data.damageEvent;
        initialized = true; //flag ready
    }

    //overrides the default color
    public void SetColor(Color newColor)
    {
        color = newColor;
        spriteRenderer.color = newColor;
    }
	
	// Update is called once per frame
	void Update ()
    {
        if (initialized)
        {
            //update scale
            curScale += speed * Time.deltaTime;
            if (curScale > maxScale)
                curScale = maxScale;

            transform.localScale = new Vector3(curScale, curScale, 1);

            float lookAheadDist = (Mathf.Max(lookAhead, Time.deltaTime) * speed) + curScale; //expand the ring to include where we expect to be in lookAhead seconds, or in Time.deltaTime seconds, whichever is larger
            lookAheadDist = Mathf.Min(lookAheadDist, maxScale); //don't look further ahead than we will actually travel

            //find any enemies we are about to hit that dont already know its coming, and warn them
            List<GameObject> toWarnThisFrame = new List<GameObject>();
            foreach (GameObject enemy in EnemyManagerScript.instance.activeEnemies)
            {
                float enemyDist = Vector2.Distance (enemy.transform.position, transform.position);
                if (enemyDist <= lookAheadDist) 
                    if (expectedToHit.Exists(ded => ded.dest == enemy) == false)  //(if there is not already a damage event with enemy as the destination) (https://msdn.microsoft.com/en-us/library/bb397687.aspx)
                        if (alreadyHit.Contains(enemy) == false)
                            toWarnThisFrame.Add(enemy);
            }

            foreach (GameObject enemy in toWarnThisFrame)
            {
                DamageEventData ded = new DamageEventData();
                ded.source = baseDamageEvent.source;
                ded.dest = enemy;
                ded.rawDamage = baseDamageEvent.rawDamage;
                ded.effects = baseDamageEvent.effects;

                //trigger effects
                if (ded.effects != null)
                    foreach (IEffect i in ded.effects.effects)
                        if (i.triggersAs(EffectType.enemyDamaged))
                            ((IEffectEnemyDamaged)i).expectedDamage(ref ded);

                enemy.SendMessage("onExpectedDamage", ded);
                expectedToHit.Add(ded);
            }

            //figure out which enemies we need to attack this frame
            List<DamageEventData> toHitThisFrame = new List<DamageEventData>();

            if (curScale != maxScale)
            {
                //normal check: attack everything inside the burst
                foreach (DamageEventData ded in expectedToHit)
                    if (Vector2.Distance(ded.dest.transform.position, transform.position) <= curScale)
                        toHitThisFrame.Add(ded);
            }
            else
            {
                //if we are dying this frame, attack everything on our expected list.  This way we still hit things that very narrowly avoided the attack, even if they technically should have "escaped".
                toHitThisFrame = new List<DamageEventData>( expectedToHit );
            }

            //attack them
            for (int e = 0; e < toHitThisFrame.Count; e++)
            {
                DamageEventData ded = toHitThisFrame[e];

                //trigger effects
                if (ded.effects != null)
                {
                    foreach (IEffect i in ded.effects.effects)
                    {
                        if (i.triggersAs(EffectType.enemyDamaged))
                        {
                            float damageBefore = ded.rawDamage;
                            ((IEffectEnemyDamaged)i).actualDamage(ref ded);

                            //warn if damage amount changed in .actualDamage(), as this causes hard-to-find bugs.  anything that changes amount of damage done should happen in expectedDamage()
                            if (damageBefore != ded.rawDamage)
                                Debug.LogWarning("damage amount altered in .actualDamage() call of " + i.XMLName + "!");
                        }
                    }
                }

                //deal the damage.  We dont mind if there is no receiver, since that just means the enemy is no longer a valid target for whatever reason
                ded.dest.SendMessage("onDamage", ded, SendMessageOptions.DontRequireReceiver);

                expectedToHit.Remove(ded);
                alreadyHit.Add(ded.dest);
            }

            //if we are at max scale, we are done.  destroy self
            if (curScale == maxScale)
                Destroy(gameObject);
        }
    }
}

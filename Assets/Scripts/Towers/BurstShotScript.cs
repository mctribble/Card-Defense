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

        //put the initial target list on the expected list and inform those enemies
        //foreach (GameObject t in data.targetList)
        //{
        //    DamageEventData ded = new DamageEventData();
        //    ded.source = baseDamageEvent.source;
        //    ded.rawDamage = baseDamageEvent.rawDamage;
        //    ded.effects = baseDamageEvent.effects;
        //    ded.dest = t;
        //
        //    t.GetComponent<EnemyScript>().onExpectedDamage(ref ded);
        //    expectedToHit.Add(ded);
        //}

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
            //update scale and destroy if dead
            curScale += speed * Time.deltaTime;
            transform.localScale = new Vector3(curScale, curScale, 1);
            if (curScale > maxScale)
            {
                Destroy(gameObject);
                return;
            }

            float lookAheadDist = (Mathf.Max(lookAhead, Time.deltaTime) * speed) + curScale; //expand the box to include where we expect to be in lookAhead seconds, or in Time.deltaTime seconds, whichever is larger

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
                enemy.SendMessage("onExpectedDamage", ded);
                expectedToHit.Add(ded);
            }

            //figure out which enemies we need to attack this frame
            List<DamageEventData> toHitThisFrame = new List<DamageEventData>();
            foreach (DamageEventData ded in expectedToHit)
                if ( Vector2.Distance(ded.dest.transform.position, transform.position) <= curScale )
                    toHitThisFrame.Add(ded);

            //attack them
            foreach (DamageEventData ded in toHitThisFrame)
            {
                ded.dest.SendMessage("onDamage", ded);
                expectedToHit.Remove(ded);
                alreadyHit.Add(ded.dest);
            }
        }
    }
}

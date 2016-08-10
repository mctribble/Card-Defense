using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System.Collections.Generic;

//data required to initialize one of these projectiles
public struct DirectionalShotData
{
    public DamageEventData  damageEvent; //damage event provided by the tower.  dest is ignored.
    public List<GameObject> targetList;  //enemies to be hit
    public Vector2          attackDir;   //max size of the burst animation
}

//projectile intended to hit everything along a straight line
public class DirectionalShotScript : MonoBehaviour
{
    public SpriteRenderer sprite; //reference to the sprite
    public ParticleSystem trail; //reference to the particle trail

    public float speed;      //projectile speed
    public float timeToLive; //max lifetime of this projectile
    public float lookAhead;  //how far ahead, in seconds, to look for future targets

    private bool             initialized;   //whether or not this object is ready for action
    private Vector3          attackDir;     //direction the attack is moving
    private List<GameObject> expectedToHit; //list of enemies that we told to expect damage
    private List<GameObject> alreadyHit;    //list of enemies we already dealt damage
    private DamageEventData  baseDamageEvent; //damage event to base all the others on

	// Use this for initialization
	void Awake ()
    {
        trail.startColor = sprite.color;
        initialized = false;
        expectedToHit = new List<GameObject>();
        alreadyHit = new List<GameObject>();
	}
	
    //initializes the attack
    void SetData (DirectionalShotData data)
    {
        attackDir = data.attackDir;
        transform.rotation = Quaternion.FromToRotation(Vector2.up, attackDir);
        baseDamageEvent = data.damageEvent;

        //put the initial target list on the expected list and inform those enemies
        foreach (GameObject t in data.targetList)
        {
            expectedToHit.Add(t);

            DamageEventData ded = new DamageEventData();
            ded.source = baseDamageEvent.source;
            ded.rawDamage = baseDamageEvent.rawDamage;
            ded.effects = baseDamageEvent.effects;
            ded.dest = t;

            t.SendMessage("onExpectedDamage", ded);
        }

        initialized = true; //flag ready
    }

	// Update is called once per frame
	void Update ()
    {
	    if (initialized)
        {
            //update lifespan and destroy if dead
            timeToLive -= Time.deltaTime;                                 
            if (timeToLive <= 0)
            {
                Destroy(gameObject);
                return;
            }

            transform.position += (attackDir * (speed * Time.deltaTime)); //move

            //find the region in which we want to search for enemies to warn about incoming damage
            Rect lookAheadRegion = new Rect(transform.position.x - 0.25f, transform.position.y - 0.25f, 0.5f, 0.5f); //start with a small box at the same place as the projectile
            float lookAheadDist = Mathf.Max(lookAhead, Time.deltaTime) * speed; //expand the box to include where we expect to be in lookAhead seconds, or in Time.deltaTime seconds, whichever is larger
            if (attackDir.x != 0)
            {
                if (attackDir.x < 0)
                    lookAheadRegion.x -= lookAheadDist;
                lookAheadRegion.width += lookAheadDist;
            }
            if (attackDir.y != 0)
            {
                if (attackDir.y < 0)
                    lookAheadRegion.y -= lookAheadDist;
                lookAheadRegion.height += lookAheadDist;
            }

            //find any enemies we are about to hit that dont already know its coming, and warn them
            List<GameObject> toWarnThisFrame = new List<GameObject>();
            foreach (GameObject enemy in EnemyManagerScript.instance.activeEnemies)
                if (lookAheadRegion.Contains(enemy.transform.position))
                    if (expectedToHit.Contains(enemy) == false)
                        if (alreadyHit.Contains(enemy) == false)
                            toWarnThisFrame.Add(enemy);

            foreach (GameObject enemy in toWarnThisFrame)
            {
                expectedToHit.Add(enemy);
                DamageEventData ded = new DamageEventData();
                ded.source = baseDamageEvent.source;
                ded.dest = enemy;
                ded.rawDamage = baseDamageEvent.rawDamage;
                ded.effects = baseDamageEvent.effects;
                enemy.SendMessage("onExpectedDamage", ded);
            }

            //figure out which enemies we need to attack this frame
            Plane plane = new Plane(attackDir, transform.position);
            List<GameObject> toHitThisFrame = new List<GameObject>();
            foreach (GameObject enemy in expectedToHit)
                if (plane.GetSide(enemy.transform.position) == false)
                    toHitThisFrame.Add(enemy);
             
            //attack them
            foreach(GameObject enemy in toHitThisFrame)
            {
                DamageEventData ded = new DamageEventData();
                ded.source = baseDamageEvent.source;
                ded.dest = enemy;
                ded.rawDamage = baseDamageEvent.rawDamage;
                ded.effects = baseDamageEvent.effects;
                enemy.SendMessage("onDamage", ded);
                
                expectedToHit.Remove(enemy);
                alreadyHit.Add(enemy);
            }
        }
	}

}


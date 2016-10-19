using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

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
    public SpriteRenderer sprite;       //reference to the sprite
    public ParticleSystem trail;        //reference to the particle trail
    public Color          defaultColor; //default color

    public float speed;      //projectile speed
    public float timeToLive; //max lifetime of this projectile
    public float lookAhead;  //how far ahead, in seconds, to look for future targets

    private bool                  initialized;     //whether or not this object is ready for action
    private Vector3               attackDir;       //direction the attack is moving
    private List<DamageEventData> expectedToHit;   //list of enemies that we told to expect damage and the events associated with those hits
    private List<GameObject>      alreadyHit;      //list of enemies we already dealt damage
    private DamageEventData       baseDamageEvent; //damage event to base all the others on

	// Use this for initialization
	void Awake ()
    {
        trail.startColor = defaultColor;
        sprite.color = defaultColor;
        initialized = false;
        expectedToHit = new List<DamageEventData>();
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
            DamageEventData ded = new DamageEventData();
            ded.source = baseDamageEvent.source;
            ded.rawDamage = baseDamageEvent.rawDamage;
            ded.effects = baseDamageEvent.effects;
            ded.dest = t;

            t.GetComponent<EnemyScript>().onExpectedDamage(ref ded);
            expectedToHit.Add(ded);
        }

        initialized = true; //flag ready
    }

    //changes the color
    void SetColor (Color c)
    {
        trail.startColor = c;
        sprite.color = c;
    }

	// Update is called once per frame
	void Update ()
    {
	    if (initialized)
        {
            //update lifespan and destroy if dead
            timeToLive -= Time.deltaTime;                                 

            transform.position += (attackDir * (speed * Time.deltaTime)); //move

            //find the region in which we want to search for enemies to warn about incoming damage
            Rect lookAheadRegion = new Rect(transform.position.x - 0.25f, transform.position.y - 0.25f, 0.5f, 0.5f); //start with a small box at the same place as the projectile
            float lookAheadDist = Mathf.Max(lookAhead, Time.deltaTime) * speed; //expand the box to include where we expect to be in lookAhead seconds, or in Time.deltaTime seconds, whichever is larger
            lookAheadDist = Mathf.Min(lookAheadDist, (timeToLive * speed)); //dont look further ahead than where we will be when we die

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
                    if (expectedToHit.Exists(ded => ded.dest == enemy) == false)  //(if there is not already a damage event with enemy as the destination) (https://msdn.microsoft.com/en-us/library/bb397687.aspx)
                        if (alreadyHit.Contains(enemy) == false)
                            toWarnThisFrame.Add(enemy);

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
            Plane plane = new Plane(attackDir, transform.position);
            List<DamageEventData> toHitThisFrame = new List<DamageEventData>();

            if (timeToLive > 0)
            {
                //normal check: hit everything behind the plane
                foreach (DamageEventData ded in expectedToHit)
                    if (ded.dest != null) //null dest events can happen if the enemy dies at just the wrong time
                        if (plane.GetSide(ded.dest.transform.position) == false)
                            toHitThisFrame.Add(ded);
            }
            else
            {
                //if we are dying this frame, attack everything on our expected list.  Works around a bug where enemies can stop being targeted because they think they're going to die but are not
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

                ded.dest.SendMessage("onDamage", ded);
                
                expectedToHit.Remove(ded);
                alreadyHit.Add(ded.dest);
            }

            if (timeToLive <= 0)
            {
                Destroy(gameObject);
                return;
            }
        }
	}

}


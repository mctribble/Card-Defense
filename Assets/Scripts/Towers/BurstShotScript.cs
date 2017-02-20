using UnityEngine;
using Vexe.Runtime.Types;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

/// <summary>
/// contains all the data needed to initialize a burst shot:
/// damageEvent: details of the attack.  dest is ignored
/// targetList: initial list of enemies to be hit (the attack will still find enemies not on this list.  it is just a performance save)
/// burstRange; max size of the burst
/// </summary>
public struct BurstShotData
{
    public DamageEventData   damageEvent; 
    public List<EnemyScript> targetList;  
    public float             burstRange;  
}

//round burst attack used by towers with TargetAll.  expands to the towers range and attacks enemies as it reaches them.
public class BurstShotScript : BaseBehaviour
{
    public float speed;     //speed of the attack wave
    public Color color;     //default color to use for the burst
    public float lookAhead; //seconds to look ahead when warning enemies about oncoming damage

    public SpriteRenderer spriteRenderer; //component reference

    //sound data
    public AudioClip[] burstSounds;
    public AudioSource audioSource;
    public int         maxSoundsAtOnce;
    private static int curSoundsAtOnce;

    private bool                  initialized;     //whether or not this shot has been initialized
    private List<DamageEventData> expectedToHit;   //list of enemies that we told to expect damage and the events associated with those hits
    private float                 curScale;        //current scale of this attack
    private float                 maxScale;        //maximum scale this attack should reach

    //init
    private void Awake()
    {
        spriteRenderer.color = color;
        curScale = 0.0f;
        expectedToHit = new List<DamageEventData>();
        LevelManagerScript.instance.RoundOverEvent += roundOverHandler; //register event so we can destroy ourselves when the round ends
    }

    //we are done if the round ends
    private void roundOverHandler()
    {
        StartCoroutine(onDone());
    }

    // Update is called once per frame
    private void Update()
    {
        if (initialized)
        {
            //update scale
            curScale += speed * Time.deltaTime;
            if (curScale > maxScale)
                curScale = maxScale;

            transform.localScale = new Vector3(curScale, curScale, 1);

            //figure out which enemies we need to attack this frame and attack them
            //we use removeAll() and a helper function instead of looping here for performance reasons.  
            //Short explanation: removeAll() can modify in-place while the work happens, so there is a lot less looping
            //Long explanation: see http://stackoverflow.com/questions/32351499/how-is-it-possible-that-removeall-in-linq-is-much-faster-than-iteration
            expectedToHit.RemoveAll(ded => (ded.dest == null) || (hitIfInRange(ded)) ); //removes all enemies that are already dead or got attacked this frame

            //if we are at max scale, we are done.  
            if (curScale == maxScale)
                StartCoroutine(onDone());
            
        }
    }

    //helper function for Update().  If the given enemy needs to be attacked, attack it and return true.  otherwise, return false.
    private bool hitIfInRange(DamageEventData ded)
    {
        if ((curScale == maxScale) || //if this is the last frame the attack is alive
             (Vector2.Distance(ded.dest.transform.position, transform.position) <= curScale)) //or the enemy is within the burst

        {
            //then this enemy needs to be hit.  Trigger effects
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

            //deal the damage.  We dont mind if the reference is null now, since that just means it is no longer a valid target for some reason
            if (ded.dest != null)
                ded.dest.onDamage(ded);


            //return true to indicate we made an attack
            return true;
        }
        else
            return false; //we did not make an attack

    }

    //hides visuals, waits for sound to finish, then destroys self
    private IEnumerator onDone()
    {
        initialized = false; //stop updating the burst so we dont attack things
        LevelManagerScript.instance.RoundOverEvent -= roundOverHandler; //unregister ourselves for the event

        spriteRenderer.enabled = false;

        while (audioSource.isPlaying)
            yield return null;

        Destroy(gameObject);
    }

    //init attack
    public void SetData (BurstShotData data)
    {
        maxScale = data.burstRange; //store attack range

        //warn all enemies that will be hit
        foreach(EnemyScript e in EnemyManagerScript.instance.enemiesInRange(data.damageEvent.source.transform.position, maxScale))
        {
            DamageEventData ded = new DamageEventData();
            ded.source = data.damageEvent.source;
            ded.dest = e;
            ded.effects = data.damageEvent.effects;
            ded.rawDamage = data.damageEvent.rawDamage;

            e.onExpectedDamage(ref ded);
            expectedToHit.Add(ded);
        }        

        //play one of the sounds at random
        int soundToPlay = Random.Range(0, burstSounds.Length);
        audioSource.clip = burstSounds[soundToPlay];
        if (isActiveAndEnabled)
            StartCoroutine(playRespectLimit(audioSource)); //plays the sound, if we are not at the sound cap and the object is not disabled

        initialized = true; //flag ready
    }

    //overrides the default color
    public void SetColor(Color newColor)
    {
        color = newColor;
        spriteRenderer.color = newColor;
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


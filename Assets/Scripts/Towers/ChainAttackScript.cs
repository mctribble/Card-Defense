using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Vexe.Runtime.Types;

public class ChainAttackScript : BaseBehaviour
{
    public LineRenderer lineRenderer; //component used to render the line
    public float        displayTime;  //how long the effect should stay on screen after the attack is made

    private List<DamageEventData> attacksWaiting;
    private Vector3               firstTargetPos;
    private bool                  alreadyTriggered;

    // Use this for initialization
    private void Awake ()
    {
        attacksWaiting = new List<DamageEventData>();
        firstTargetPos = Vector3.zero;
        alreadyTriggered = false;
	}
	
    /// <summary>
    /// uses baseEVent to warn about attacks against all enemies within chainRange of firstTarget, and all enemies within chainRange of any other target it has chained to, up to targetCap enemies.
    /// </summary>
    /// <param name="baseEvent">DamageEvent to use as a base for all attacks</param>
    /// <param name="firstTarget">First enemy to attack.  It is assumed this enemy has already been attacked</param>
    /// <param name="chainRange">max distance between any two targets</param>
    /// <param name="targetCap">max number of enemies to hit</param>
    public void ChainAttackWarn(DamageEventData baseEvent, EnemyScript firstTarget, float chainRange, int targetCap)
    {
        //when a tower attacks, it clones the effectData.  If an attack hits multiple targets, they all share the same effects
        //therefore, by only allowing ourselves to trigger once, we ensure the attack only chains once per attack fired from the originating tower
        //regardless of how many enemies were hit by that attack
        if (alreadyTriggered)
            return;
        else
            alreadyTriggered = true;

        //abort if the effects we were given include chainHit: this causes issues so severe it crashes the editor
        if (baseEvent.effects != null)
        {
            if (baseEvent.effects.containsEffect("chainHit"))
            {
                throw new System.Exception("chainHit effect should never be present in the baseEvent passed to the ChainAttackScript.  This can cause severe overflows");
            }
        }

        //setup
        List<EnemyScript> enemiesToWarn = new List<EnemyScript>();
        enemiesToWarn.Add(firstTarget); //we put firstTarget in this list to simplify the target search, but we will remove it later before creating the actual warnings because it already got one

        //find everything that should be hit
        bool newTargetsFound = true;
        while (newTargetsFound && enemiesToWarn.Count < targetCap)
        {
            newTargetsFound = false;

            foreach(EnemyScript e in EnemyManagerScript.instance.activeEnemies.Except(enemiesToWarn).ToList()) //all active enemies that are not already on the warn list
            {
                if (enemiesToWarn.Any(etw => Vector2.Distance(etw.transform.position, e.transform.position) <= chainRange)) //if the enemy is within chainRange of an enemy already on the warn list
                {
                    enemiesToWarn.Add(e); //then we will want to warn it about the attack
                    newTargetsFound = true;

                    //bail if we hit the target cap
                    if (enemiesToWarn.Count == targetCap)
                        break;
                }
            }
        }

        //warn everything else on the list
        foreach(EnemyScript e in enemiesToWarn)
        {
            //we don't need to warn firstTarget because it was already warned when the initial attack was made
            if (e == firstTarget)
                continue;

            //create the damage event by cloning the base event and changing the target
            DamageEventData ded = new DamageEventData();
            ded.source = baseEvent.source;
            ded.effects = baseEvent.effects;
            ded.rawDamage = baseEvent.rawDamage;
            ded.dest = e;

            //trigger effects
            if (ded.effects != null)
                foreach (IEffect ie in ded.effects.effects)
                    if (ie.triggersAs(EffectType.enemyDamaged))
                        ((IEffectEnemyDamaged)ie).expectedDamage(ref ded);

            //send the warning
            e.onExpectedDamage(ref ded);

            //put it in the list to finish the attack later
            attacksWaiting.Add(ded);
        }

        //retain position of the initial target so we can add it to the position list when we draw the line
        firstTargetPos = firstTarget.transform.position;
    }

    /// <summary>
    /// hits all enemies previously warned about this attack
    /// </summary>
    public void ChainAttackHit()
    {
        //the line should have the points of all attacked enemies, including the first, but we need to drop their Z coordinates a little to make sure they show up
        Vector3 offset = new Vector3(0, 0, -5);
        List<Vector3> enemyPositions = attacksWaiting.Select(ded => ded.dest.transform.position + offset).ToList();
        enemyPositions.Insert(0, firstTargetPos);

        //pass them to the renderer
        lineRenderer.numPositions = enemyPositions.Count;
        lineRenderer.SetPositions(enemyPositions.ToArray());

        //make the attacks
        for(int i = 0; i < attacksWaiting.Count; i++)
        {
            DamageEventData ded = attacksWaiting[i];

            //trigger effects
            if (ded.effects != null)
                foreach (IEffect ie in ded.effects.effects)
                    if (ie.triggersAs(EffectType.enemyDamaged))
                        ((IEffectEnemyDamaged)ie).actualDamage(ref ded);

            //deal damage
            ded.dest.onDamage(ded);
        }

        //start onDone coroutine
        StartCoroutine(onDone());
    }

    /// <summary>
    /// run when the object is done to let it stay on screen for a while before going away
    /// </summary>
    private IEnumerator onDone()
    {
        //the line renderer misbehaves if we let it render on the frame we set it up (https://forum.unity3d.com/threads/problems-about-line-renderer.31005/)
        //as such, we wait a frame and then enable it after the fact
        yield return null;
        lineRenderer.enabled = true;

        float t = 0.0f;
        float maxWidth = lineRenderer.widthMultiplier;

        //line spawns at full size, shrinks to nothing, then is destroyed
        while (t < displayTime)
        {
            lineRenderer.widthMultiplier = Mathf.Lerp(maxWidth, 0.0f, t);
            t += Time.deltaTime;
            yield return null;
        }

        //then get rid of it
        Destroy(gameObject);
    }

}

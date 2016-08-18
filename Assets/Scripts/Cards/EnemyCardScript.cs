using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Vexe.Runtime.Types;

public class EnemyCardScript : BaseBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    //references
    public Text       description;
    public GameObject art;
    public Text       title;
    public Image      cardBack; 

    //animation settings
    public float motionSpeed;
    public float rotationSpeed;
    public float scaleSpeed;
    public Vector2 discardLocation;

    //wave stats
    [Hide] public int spawnCount;           //number of enemies that still need spawning in this wave
    [Hide] public int totalRemainingHealth; //total health of all enemies that still need spawning
    public WaveData wave;                   //wave associated with this card

    //private info
    private State      state;          //state of the FSM
    private Vector2    idleLocation;   //where the card should be when idle
    private Vector2    targetLocation; //where the card currently wants to be
    private GameObject hand;           //reference to parent hand
    private bool       hidden;         //whether or not the card should be hidden offscreen
    private bool       faceDown;       //whether or not the card is face down
    private int        siblingIndex;   //temp storage of this cards proper place in the sibling list, used to restore proper draw order after a card is no longer being moused over
    private string     enemyType;      //name of the enemy type currently depicted.  Cached to detect enemy type changes

    //init
    private void Awake()
    {
        state = State.idle;
        idleLocation = transform.position;
        targetLocation = idleLocation;
        hidden = false;
        faceDown = true;
        cardBack.enabled = true;
    }
  
    //sets the wave
    public void SetWave(WaveData w)
    {
        wave = w;
        description.text = w.enemyData.getDescription();
        foreach (Image i in art.GetComponentsInChildren<Image>())
            i.color = w.enemyData.unitColor.toColor();
        enemyType = w.enemyData.name;
    }

    //simple FSM
    private enum State
    {
        idle,
        moving,
        attacking,
        discarding
    }

    //tells the card where it should be idling
    private void SetIdleLocation(Vector2 newIdle)
    {
        idleLocation = newIdle; //update location

        //if card is not hidden or dying, tell it to relocate itself
        if ((hidden == false) && (state != State.discarding))
        {
            state = State.moving;
            targetLocation = idleLocation;
        }
    }

    // Update is called once per frame
    private void Update()
    {
        //update title text (???????x????)
        title.text = "<color=#" + wave.enemyData.unitColor.toHex() + ">" + wave.enemyData.name + "</color>x" + (wave.spawnCount - wave.spawnedThisWave);

        //if the enemy type changed, update description and art as well
        if (enemyType != wave.enemyData.name)
        {
            description.text = wave.enemyData.getDescription();
            foreach (Image i in art.GetComponentsInChildren<Image>())
                i.color = wave.enemyData.unitColor.toColor();
            enemyType = wave.enemyData.name;
        }

        //bail early if idle
        if (state == State.idle)
            return;

        //calculate new position
        Vector2 newPosition = Vector2.MoveTowards(transform.localPosition,
                                                  targetLocation,
                                                  motionSpeed * Time.deltaTime);
        //move there
        transform.localPosition = newPosition;

        //go idle or die if reached target
        if (newPosition == targetLocation)
        {
            if (state == State.discarding)
            {
                Destroy(gameObject);
            }
            else
            {
                state = State.idle;
            }
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        //ignore this event if hidden or discarding
        if (hidden || (state == State.discarding))
            return;

        siblingIndex = transform.GetSiblingIndex(); //save the current index for later
        transform.SetAsLastSibling(); //move to front

        //tell card to move up when moused over
        targetLocation = idleLocation;
        targetLocation.y -= 200;
        state = State.moving;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        //ignore this event if hidden or discarding
        if (hidden || (state == State.discarding))
            return;

        transform.SetSiblingIndex(siblingIndex); //restore to old position in the draw order

        //tell card to reset when no longer moused over
        targetLocation = idleLocation;
        state = State.moving;
    }

    //called by the hand to pass a reference to said hand
    private void SetHand(GameObject go)
    {
        hand = go;
    }

    //helper coroutine that simply waits until this card is idle (initial delay of one frame in case the card starts moving in the same frame as this is called)
    public IEnumerator waitForIdle() { yield return null; while (state != State.idle) yield return null; }

    //turns the card to the given quaternion at rotationSpeed degrees/second
    public IEnumerator turnToQuaternion(Quaternion target)
    {
        while (transform.rotation != target)
        {
            transform.rotation = Quaternion.RotateTowards(transform.rotation, target, rotationSpeed * Time.deltaTime);
            yield return null;
        }
    }

    //scales the card to the given size over time
    public IEnumerator scaleToVector(Vector3 targetSize)
    {
        while (transform.localScale != targetSize)
        {
            transform.localScale = Vector3.MoveTowards(transform.localScale, targetSize, scaleSpeed * Time.deltaTime);
            yield return null;
        }
    }

    private void Hide()
    {
        //ignore if discarding
        if (state == State.discarding)
            return;

        //cards hide just underneath the center of the screen
        targetLocation.x = 0;
        targetLocation.y = transform.root.GetComponent<RectTransform>().rect.yMax + 200;

        state = State.moving;       //mark this card as in motion
        hidden = true;              //mark this card as hidden
    }

    private void Show()
    {
        //ignore if not hidden
        if (hidden == false)
            return;

        //ignore if discarding
        if (state == State.discarding)
            return;

        //go back to where it was spawned
        targetLocation = idleLocation;
        state = State.moving;

        hidden = false;//clear hidden flag
    }

    //discards this card
    private void Discard()
    {
        state = State.discarding;
        targetLocation = discardLocation;
        hand.SendMessage("Discard", gameObject);
    }

    //card flip helpers
    public void flipOver() { StartCoroutine(flipCoroutine()); } //returns immediately
    public void flipFaceUp() { if (faceDown) flipOver(); } //calls flipOver only if the card is currently face down
    public IEnumerator flipWhenIdle() { yield return waitForIdle(); yield return flipCoroutine(); }

    //main card flip coroutine
    public IEnumerator flipCoroutine()
    {
        Quaternion flipQuaternion = Quaternion.AngleAxis(90, Vector3.up); //rotation to move towards to flip the card at
        faceDown = !faceDown; //flag the flip as complete before it technically even starts to make sure it isn't erroneously triggered again
        yield return StartCoroutine(turnToQuaternion(flipQuaternion)); //turn to the flip position the player doesnt see the back blink in or out of existence
        cardBack.enabled = faceDown; //flip the card
        yield return StartCoroutine(turnToQuaternion(Quaternion.identity)); //turn back to the baseline
        yield break; //done
    }

    //update stats for this wave
    public void updateWaveStats()
    {
        //show the wave message, if there is one, and then blank it out so it only shows once
        if (wave.message != null)
        {
            MessageHandlerScript.ShowNoYield(wave.message);
            wave.message = null;
        }

        if (wave.forcedSpawnCount > 0)
            spawnCount = wave.forcedSpawnCount;
        else
            spawnCount = Mathf.RoundToInt(((float)wave.budget / (float)wave.enemyData.spawnCost));

        totalRemainingHealth = spawnCount * wave.enemyData.maxHealth;
    }

    public void applyWaveEffect(IEffectWave e) { wave = e.alteredWaveData(wave); } //applies the given effect to the wave
}
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Vexe.Runtime.Types;

/// <summary>
/// how this card is classified in game
/// not to be confused with PlayerCardData, which is a complete card definition
/// </summary>
/// <seealso cref="CardData"/>
public enum PlayerCardType
{
    tower,      //summons a tower with the given stats
    upgrade,    //increases/decreases the target towers stats
    spell		//other effects
}

/// <summary>
/// represents a card being shown on the screen
/// </summary>
public abstract class CardScript : BaseBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    //used to hide many variables at runtime
    protected bool shouldShowRefs() { return !Application.isPlaying; }

    //component references
    [VisibleWhen("shouldShowRefs")] public Text  title;       //reference to card name text
    [VisibleWhen("shouldShowRefs")] public Text  description; //reference to card description text
    [VisibleWhen("shouldShowRefs")] public Image cardFront;   //reference to card front image
    [VisibleWhen("shouldShowRefs")] public Image cardBack;    //reference to card back image

    //behavior data
    [VisibleWhen("shouldShowRefs")] public float   mouseOverMod;  //amount the card should move up when moused over, expressed as a multiplier to card height
    [VisibleWhen("shouldShowRefs")] public float   motionSpeed;   //speed in pixels/second this card can move
    [VisibleWhen("shouldShowRefs")] public float   rotationSpeed; //speed in degrees/second this card can rotate
    [VisibleWhen("shouldShowRefs")] public float   scaleSpeed;    //speed in points/second this card can scale
    [VisibleWhen("shouldShowRefs")] public Color   tokenColor;    //color to tint tokens

    public bool faceDown; //whether or not the card is face down

    //sound data
    [VisibleWhen("shouldShowRefs")] public AudioClip[] drawSounds;  //sounds to use when drawn
    [VisibleWhen("shouldShowRefs")] public AudioSource audioSource; //source to play said sounds from

    //private data
    protected GameObject hand;            //reference to the hand object managing this card
    protected Vector2    idleLocation;    //location this card sits when it is resting
    protected Vector2    targetLocation;  //location this card will move towards if it is not already there
    protected bool       hidden;          //whether or not the card is hiding off screen
    protected int        siblingIndex;    //used to put card back where it belongs in the sibling list after it is brought to front for readability

    /// <summary>
    /// returns, in world space, where floating combat text related to this card should spawn
    /// </summary>
    public Vector2 combatTextPosition { get { return Camera.main.ScreenToWorldPoint ( idleLocation + new Vector2( (Screen.width / 2), (Screen.height - (cardFront.rectTransform.rect.height / 4) ) ) ); } }

    public virtual bool    discardable { get { return true; } } //returns whether or not this card can be discarded.  Almost all can.
    public abstract string cardName    { get; }                 //returns the name of the card

    //simple FSM
    protected enum State
    {
        idle,
        moving,
        casting,
        discarding
    }

    [Show] protected State state;

    // Use this for initialization
    protected virtual void Awake()
    {
        //start with the target being the location it was spawned at
        idleLocation = transform.localPosition;
        targetLocation = idleLocation;

        //start idle and face down
        state = State.idle;
        faceDown = true;
        cardBack.enabled = true;

        //play the sound (not using the playOnAwake setting since we are choosing a sound at random)
        if (drawSounds.Length > 0)
        {
            int soundToPlay = Random.Range(0, drawSounds.Length);
            audioSource.clip = drawSounds[soundToPlay];
            audioSource.Play();
        }
    }

    public void SetHand(GameObject go)
    {
        hand = go;
    } //called by the hand to pass a reference to said hand

    // Update is called once per frame
    protected virtual void Update()
    {
        //if idle, there is nothing to do.  If discarding, then DiscardCoroutine is doing the work
        if (state == State.idle || state == State.discarding)
            return;

        //calculate new position
        Vector2 newPosition = Vector2.MoveTowards(transform.localPosition,
                                                  targetLocation,
                                                  motionSpeed * Time.deltaTime);
        //move there
        transform.localPosition = newPosition;

        //go idle if reached target
        if (newPosition == targetLocation)
            state = State.idle;
    }

    /// <summary>
    /// [COROUTINE] waits until this card is idle (initial delay of one frame in case the card starts moving in the same frame as this is called)
    /// </summary>
    /// <returns></returns>
    public IEnumerator waitForIdle()
    {
        yield return null;
        while (state != State.idle)
            yield return null;
    }

    /// <summary>
    /// [COROUTINE] waits until this card is idle or being discarded (initial delay of one frame in case the card starts moving in the same frame as this is called)
    /// </summary>
    public IEnumerator waitForIdleOrDiscarding()
    {
        yield return null;
        while ((state != State.idle) && (state != State.discarding))
            yield return null;
    }

    /// <summary>
    /// [COROUTINE] waits until this card is either being discarded or is ready for further movement
    /// A card is deemed ready for movement if it is idle and not already undergoing some form of rotation/scaling.
    /// </summary>
    public IEnumerator waitForReady()
    {
        bool isReady = false;
        while (isReady == false)
        {
            yield return null;

            if (state == State.discarding)
                isReady = true;

            if (state == State.idle)
                if (isTurning == false)
                    if (isScaling == false)
                        isReady = true;
        }
    }

    //card flip helpers
    public void flipOver()
    {
        StartCoroutine(flipCoroutine());
    } 
    public void flipFaceUp() 
    {
        if (faceDown)
            flipOver();
    } 
    public IEnumerator flipWhenIdle()
    {
        yield return waitForIdle();
        yield return StartCoroutine(flipCoroutine());
    }
    public IEnumerator flipFaceUpWhenIdle()
    {
        yield return waitForIdle();
        if (faceDown)
            StartCoroutine(flipCoroutine());
    }

    /// <summary>
    /// [COROUTINE] flips the card over
    /// </summary>
    public IEnumerator flipCoroutine()
    {
        faceDown = !faceDown; //flag the flip as complete before it technically even starts to make sure it isn't erroneously triggered again
        Quaternion flipQuaternion = Quaternion.AngleAxis(90, Vector3.up); //rotation to move towards to flip the card at
        yield return StartCoroutine(turnToQuaternion(flipQuaternion)); //turn to the flip position the player doest see the back blink in or out of existence
        cardBack.enabled = faceDown; //flip the card
        yield return StartCoroutine(turnToQuaternion(Quaternion.identity)); //turn back to the baseline
        yield break; //done
    }

    private bool isTurning; //flag used to prevent simultaneous rotations

    /// <summary>
    /// [COROUTINE] turns the card to the given quaternion at rotationSpeed degrees/second
    /// </summary>
    public IEnumerator turnToQuaternion(Quaternion target)
    {
        while (isTurning)
            yield return null;

        isTurning = true;

        while (transform.localRotation != target)
        {
            transform.localRotation = Quaternion.RotateTowards(transform.localRotation, target, rotationSpeed * Time.deltaTime);
            yield return null;
        }

        isTurning = false;
    }

    private bool isScaling; //flag used to prevent simultaneous scales

    /// <summary>
    /// [COROUTINE] scales the card to the given size over time
    /// </summary>
    public IEnumerator scaleToVector(Vector3 targetSize)
    {
        while (isScaling)
            yield return null;

        isScaling = true;

        //error catch: this function causes an infinite loop with targetSize of 0,0,0 so use a very small value instead
        if (targetSize == Vector3.zero)
            targetSize.Set(0.001f, 0.001f, 0.001f);

        while (transform.localScale != targetSize)
        {
            transform.localScale = Vector3.MoveTowards(transform.localScale, targetSize, scaleSpeed * Time.deltaTime);
            yield return null;
        }

        isScaling = false;
    }

    /// <summary>
    /// handles the mouse moving onto the card
    /// </summary>
    public virtual void OnPointerEnter(PointerEventData eventData)
    {
        //ignore this event if hidden, discarding, or if a message is being shown to the player
        if (hidden || (state == State.discarding) || MessageHandlerScript.instance.messageBeingShown)
            return;

        siblingIndex = transform.GetSiblingIndex(); //save the current index for later
        transform.SetAsLastSibling(); //move to front

        //tell card to move when moused over
        targetLocation = idleLocation;
        targetLocation.y += GetComponent<RectTransform>().rect.height * mouseOverMod;
        state = State.moving;
    }

    /// <summary>
    /// handles the mouse moving off of the card
    /// </summary>
    public virtual void OnPointerExit(PointerEventData eventData)
    {
        //ignore this event if hidden or discarding
        if (hidden || (state == State.discarding))
            return;

        transform.SetSiblingIndex(siblingIndex); //restore to old position in the draw order

        //tell card to reset when no longer moused over
        targetLocation = idleLocation;
        state = State.moving;
    }

    public abstract void Hide();

    public virtual void Show()
    {
        //ignore if not hidden
        if (hidden == false)
            return;

        //ignore if discarding
        if (state == State.discarding)
            return;

        //go back to the idle location
        targetLocation = idleLocation;
        state = State.moving;

        hidden = false;//clear hidden flag
    }

    /// <summary>
    /// [COROUTINE] discards this card
    /// </summary>
    public abstract IEnumerator Discard();

    /// <summary>
    /// updates where this card should be if it isnt doing anything.  If the card is idle when the idle location changes, it moves to the new location
    /// </summary>
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

    /// <summary>
    /// updates the card description text.
    /// </summary>
    public abstract void updateDescriptionText();

    /// <summary>
    /// triggers all effects on this card that are meant to fire when the card is drawn
    /// </summary>
    public abstract void triggerOnDrawnEffects();
}
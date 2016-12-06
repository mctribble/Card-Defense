using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using UnityEngine;
using UnityEngine.UI;
using Vexe.Runtime.Types;

/// <summary>
/// used to represent card(s) in XMLDeck.
/// name: card name
/// count: number to put in the deck
/// </summary>
/// <seealso cref="XMLDeck"/>
[System.Serializable]
public class XMLDeckEntry
{
    //provides inspector popup menu for card name
    private string[] getCardNames() { return CardTypeManagerScript.instance.getCardNames(); }
    [Popup("getCardNames",CaseSensitive = true,Filter = true,HideUpdate = true,TextField = true)][XmlAttribute] public string name;  

    [XmlAttribute] public int    count;

    //default constructor
    public XMLDeckEntry()
    {
        name = "UNDEFINED";
        count = 0;
    }

    //explicit constructor
    public XMLDeckEntry(string newName, int newCount)
    {
        name = newName;
        count = newCount;
    }

    public override string ToString() { return name + "x" + count; }
};

/// <summary>
/// represents a deck of cards in XML
/// </summary>
/// <seealso cref="XMLDeckEntry"/>
[System.Serializable]
public class XMLDeck
{
    [XmlAttribute] public string name; //name of this deck

    //list of cards and how many instances of them exist
    [XmlArray("Cards")]
    [XmlArrayItem("Card")]
    [Display(Seq.GuiBox | Seq.PerItemDuplicate | Seq.PerItemRemove)]
    public List<XMLDeckEntry> contents;

    //default constructor
    public XMLDeck()
    {
        name = "New Deck";
        contents = new List<XMLDeckEntry>();
    }

    /// <summary>
    /// returns whether or not this deck conforms to the rules set in DeckRules
    /// </summary>
    /// <seealso cref="DeckRules"/>
    public bool isValid()
    {
        //tracking variables
        int cardCount = 0;
        int maxCardsOfOneType = 0;

        //deck scan
        foreach (XMLDeckEntry entry in contents)
        {
            //counting
            cardCount += entry.count;
            if (entry.count > maxCardsOfOneType)
                maxCardsOfOneType = entry.count;

            //no tokens
            if (CardTypeManagerScript.instance.getCardByName(entry.name).isToken)
                return false;
        }

        //compare stats to the deck rules
        if (cardCount         < DeckRules.MIN_CARDS_IN_DECK)      return false;
        if (cardCount         > DeckRules.MAX_CARDS_IN_DECK)      return false;
        if (maxCardsOfOneType > DeckRules.MAX_CARDS_OF_SAME_TYPE) return false;

        //all tests passed.  deck is valid.
        return true;
    }

    /// <summary>
    /// returns whether or not this deck contains modded cards
    /// </summary>
    public bool isModded()
    {
        foreach (XMLDeckEntry entry in contents)
            if (CardTypeManagerScript.instance.getCardByName(entry.name).isModded == true)
                return true;

        return false;
    }

    //returns total card count in this deck
    public int cardCount { get { return contents.Sum(x => x.count); } }

    public override string ToString() { return name + "(" + cardCount + " cards"; }

    /// <summary>
    /// Sorts the contents of this XMLDeck by name
    /// </summary>
    [Show] public void sort()
    {
        contents.Sort(new CardNameComparer());
    }
};

/// <summary>
/// maintains a collection of decks, including saving/loading to XML
/// </summary>
[XmlRoot("PlayerDecks")]
[System.Serializable]
public class DeckCollection
{
    //import the (teeny tiny) javascript lib being used to make sure saving persists on webGL builds
    [DllImport("__Internal")]
    private static extern void SyncFiles();

    /// <summary>
    /// used to specify the proper .xsd file in the serialized xml
    /// </summary>
    [Hide]
    [XmlAttribute("noNamespaceSchemaLocation", Namespace = System.Xml.Schema.XmlSchema.InstanceNamespace)]
    public readonly string schema = "../XML/Decks.xsd";

    //list of decks
    [XmlArray("Decks")]
    [XmlArrayItem("Deck")]
    [Display(Seq.GuiBox | Seq.PerItemDuplicate | Seq.PerItemRemove)]
    public List<XMLDeck> decks;

    /// <summary>
    /// default constructor.  creates an empty collection.
    /// </summary>
    public DeckCollection()
    {
        decks = new List<XMLDeck>();
    }

    //DEV: saves changes to this deck collection, with a confirmation box
    private string curPath;
    [Show] private IEnumerator saveDecks()
    {
        yield return DeckManagerScript.instance.StartCoroutine(MessageHandlerScript.PromptYesNo("Are you sure you want to save these decks?"));
        if (MessageHandlerScript.responseToLastPrompt == "Yes")
        {
            Save(curPath);
            Debug.Log("Decks Saved.");
        }
    }

    /// <summary>
    /// saves this collection to the given file
    /// </summary>
    public void Save(string path)
    {
        XmlSerializer serializer = new XmlSerializer(typeof(DeckCollection));

        using (StreamWriter stream = new StreamWriter(path, false, Encoding.GetEncoding("UTF-8")))
        {
            serializer.Serialize(stream, this);
        }

        //on web builds, javascript call to try and make sure the changes persist (see http://answers.unity3d.com/questions/1095407/saving-webgl.html and HandleIO.jslib)
        if (Application.platform == RuntimePlatform.WebGLPlayer)
            SyncFiles();
    }

    /// <summary>
    /// returns a new DeckCollection loaded from the given file
    /// </summary>
    public static DeckCollection Load(Stream stream, string filePath)
    {
        XmlSerializer serializer = new XmlSerializer(typeof(DeckCollection));
        DeckCollection result = serializer.Deserialize(stream) as DeckCollection;
        result.curPath = filePath;
        return result;
    }

    /// <summary>
    /// returns the deck of the given name, if it exists
    /// if it does not exist, a default is returned and produces a warning
    /// </summary>
    /// <param name="targetDeck"></param>
    public XMLDeck getDeckByName(string targetDeck)
    {
        //find the deck
        XMLDeck result = null;
        foreach (XMLDeck xDeck in decks)
        {
            if (xDeck.name == targetDeck)
            {
                result = xDeck;
                break;
            }
        }

        //if the deck did not exist, use a default deck and print a warning
        if (result == null)
        {
            MessageHandlerScript.Warning("Deck " + targetDeck + " could not be found.  Using a default deck instead");
            result = new XMLDeck();
            result.contents = new List<XMLDeckEntry>();
            result.contents.Add(new XMLDeckEntry("Basic Tower", 60));
        }

        //return the result
        return result;
    }

    /// <summary>
    /// returns an array of deck names present in this collection
    /// </summary>
    public string[] getNames()
    {
        List<string> names = new List<string>();

        foreach (XMLDeck xd in decks)
            names.Add(xd.name);

        names.Sort();
        return names.ToArray();
    }
}

/// <summary>
/// represents a player card that is in the deck but not on screen
/// data: the card type
/// charges: how many charges are remaining
/// </summary>
public struct PlayerCard
{
    public PlayerCardData data; //defines the card type
    public int charges; //number of remaining charges

    //overridden for better display in inspector
    public override string ToString()
    {
        if (data == null)
            return "null";
        else
            return data.cardName + "(" + charges + "/" + data.cardMaxCharges + ")";
    } 
}

/// <summary>
/// handles loading, saving, and tracking the contents of the player's current deck.  
/// </summary>
public class DeckManagerScript : BaseBehaviour
{
    //manager settings (only shown in editor)
    private bool shouldShowSettings() { return !Application.isPlaying; }
    [VisibleWhen("shouldShowSettings")] public static     DeckManagerScript instance; //singleton instance
    [VisibleWhen("shouldShowSettings")] public string     premadeDeckPath;            //location of premade deck file
    [VisibleWhen("shouldShowSettings")] public HandScript playerHand;                 //reference to the player's hand, if present

    //casualty report settings: a new card is spawned, given these properties, then immediately discarded to report a card death to the player
    [VisibleWhen("shouldShowSettings")] public GameObject playerCardPrefab; //prefab to use when spawning
    [VisibleWhen("shouldShowSettings")] public Image      deckImage;        //image to line up with as the spawn location
    [VisibleWhen("shouldShowSettings")] public float      discardDelay;     //minimum time between discards when multiple cards are destroyed at once
    [Hide] private bool waitToDiscard; //used to enforce discardDelay across multiple instances of DamageCoroutine().  Declared up here because C# wont let me declare a static inside the function itself

    //sound settings
    [VisibleWhen("shouldShowSettings")] public AudioClip[] playerDamageSounds; //sounds to use when player is hit
    [VisibleWhen("shouldShowSettings")] public AudioSource audioSource; //source to play them from

    //deck lists (only shown if loaded)
    private bool deckListsLoaded() { return (premadeDecks != null) && (premadeDecks.decks.Count > 0) && (playerDecks != null) && (playerDecks.decks.Count > 0); }
    [VisibleWhen("deckListsLoaded")] public DeckCollection premadeDecks; //stores premade decks
    [VisibleWhen("deckListsLoaded")] public DeckCollection playerDecks;  //stores player decks

    //number of charges in the deck
    [Hide] public int curDeckCharges; //remaining
    [Hide] public int maxDeckCharges; //max

    [Show][VisibleWhen("deckListsLoaded")] public string currentDeckName; //name of the current deck
    [Show][VisibleWhen("deckListsLoaded")] private List<PlayerCard> currentDeck; //actual current deck

    private const int SHUFFLE_ITERATIONS = 5; //number of times to shuffle the deck

    public int cardsLeft
    { //returns number of cards left in the deck
        get
        {
            return currentDeck.Count;
        }
    }

    public int deckSize { get; set; } //total number of cards in the deck

    // Use this for initialization
    private void Awake()
    {
        instance = this;

        //premade decks
        if (Application.platform == RuntimePlatform.WebGLPlayer)
        {
            StartCoroutine(loadPremadeDecksWeb()); //web player has to use a coroutine for this because it waits for a web request
        }
        else
        {
            //PC build, however, can load them right here
            string filePath = Path.Combine(Application.streamingAssetsPath, premadeDeckPath);
            using (FileStream stream = new FileStream(filePath, FileMode.Open))
                premadeDecks = DeckCollection.Load(stream, filePath);
        }

        //load player decks if the file exists, or create an empty collection if not.  
        //This file is local even on web builds, so it doesn't need special handling
        try
        {
            string filePath = Path.Combine(Application.persistentDataPath, "playerDecks.xml");
            using (FileStream stream = new FileStream(filePath, FileMode.Open)) 
                playerDecks  = DeckCollection.Load(stream, filePath);
        }
        catch (Exception e)
        {
            Debug.Log("no deck save file found. (" + e.Message + ")");
            playerDecks = new DeckCollection();
        }

        currentDeck = new List<PlayerCard>();
        deckSize = 0;
        curDeckCharges = 0;
        maxDeckCharges = 0;
    }

    /// <summary>
    /// [COROUTINE] loads premade decks on web builds.  PC builds do it in Awake().
    /// </summary>
    /// <returns></returns>
    private IEnumerator loadPremadeDecksWeb()
    {
        //form the web request
        string filePath = Application.streamingAssetsPath + '/' + premadeDeckPath;
        //while (filePath.StartsWith("/")) filePath = filePath.Substring(1); //remove any leading /'s
        WWW request = new WWW(filePath);

        //wait for the request to load
        yield return request;

        //show error if there was one
        if (request.error != null)
        {
            MessageHandlerScript.Error("Error loading premade decks:\n" + request.error);
            yield break;
        }

        //or, if we were successful, create a new stream and fill it with the contents of the web request:
        using (MemoryStream premadeDecksStream = new MemoryStream())    //create the stream
        {
            StreamWriter writer = new StreamWriter(premadeDecksStream); //used to write to it
            writer.Write(request.text);                               //write contents of the request
            writer.Flush();                                           //make sure it gets processed
            premadeDecksStream.Position = 0;                            //send the stream back to the start

            //now we can finally load the decks
            premadeDecks = DeckCollection.Load(premadeDecksStream, filePath);
        }
    }

    /// <summary>
    /// reloads the deck lists, empties out the deck, and then reloads it
    /// </summary>
    private void Reset()
    {
        //premade decks
        if (Application.platform == RuntimePlatform.WebGLPlayer)
        {
            StartCoroutine(loadPremadeDecksWeb()); //web player has to use a coroutine for this because it waits for a web request
        }
        else
        {
            //PC build, however, can load them right here
            string filePath = Path.Combine(Application.streamingAssetsPath, premadeDeckPath);
            using (FileStream stream = new FileStream(filePath, FileMode.Open))
                premadeDecks = DeckCollection.Load(stream, filePath);
        }

        //load player decks if the file exists, or create an empty collection if not
        try
        {
            string filePath = Path.Combine(Application.persistentDataPath, "playerDecks.xml");
            using (FileStream stream = new FileStream(filePath, FileMode.Open)) 
                playerDecks  = DeckCollection.Load(stream, filePath);
        }
        catch (Exception e)
        {
            Debug.Log("no deck save file found. (" + e.Message + ")");
            playerDecks = new DeckCollection();
        }

        //clear out the current deck
        currentDeck.Clear();

        if (LevelManagerScript.instance.data.usingLevelDeck)
        {
            //if we were using the level deck, reload that
            LevelManagerScript.instance.loadLevelDeck(); //this delegates to level manager so that it can retain control over the level data
        }
        else
        {
            //otherwise, the player picked a deck.  Search the deck lists for a deck of the same name as what we're using and load that.

            XMLDeck target = null; //deck to reload

            if (premadeDecks.getNames().Contains(currentDeckName))
                target = premadeDecks.getDeckByName(currentDeckName);

            if (playerDecks.getNames().Contains(currentDeckName))
            {
                if (target != null)
                    Debug.LogError("both deck lists contain a deck of the same name!  Unsure which to reload.");
                else
                    target = playerDecks.getDeckByName(currentDeckName);
            }

            if (target == null)
                Debug.LogError("could not find the deck to reload it!");

            SetDeck(target);
            Shuffle();
        }
    }

    /// <summary>
    /// saves the player decks back to the file
    /// </summary>
    public void savePlayerDecks()
    {
        playerDecks.Save(Path.Combine(Application.persistentDataPath, "playerDecks.xml")); //TODO: make save file per-user if/when user accounts exist
    }

    /// <summary>
    /// Sets the currentDeck based on the XMLDeck
    /// note that this does NOT shuffle the deck, since there are (rare) instances where this should not happen
    /// </summary>
    /// <seealso cref="Shuffle"/>
    private void SetDeck(XMLDeck newDeck)
    {
        //init
        currentDeckName = newDeck.name;
        maxDeckCharges = 0;

        //for each card on the list of cards in the deck...
        foreach (XMLDeckEntry xde in newDeck.contents)
        {
            PlayerCardData type = CardTypeManagerScript.instance.getCardByName(xde.name);
            //for each individual card to be added...
            for (int i = 0; i < xde.count; i++)
            {
                //setup the data
                PlayerCard c;
                c.data = type;
                c.charges = type.cardMaxCharges;

                //count the charges
                maxDeckCharges += type.cardMaxCharges;

                //and put it in
                currentDeck.Add(c);
            }
        }

        curDeckCharges = maxDeckCharges; //the deck starts full, so all charges are present

        deckSize = cardsLeft;
    }

    /// <summary>
    /// Shuffles the current deck
    /// </summary>
    public void Shuffle()
    {
        //shuffles by swapping every item with another random item. This is performed multiple times
        for (int iteration = 0; iteration < SHUFFLE_ITERATIONS; iteration++)
        {
            for (int i = 0; i < currentDeck.Count; i++)
            {
                int swapTarget = UnityEngine.Random.Range(0, currentDeck.Count);
                PlayerCard temp = currentDeck[i];
                currentDeck[i] = currentDeck[swapTarget];
                currentDeck[swapTarget] = temp;
            }
        }
    }

    /// <summary>
    /// Returns the top PlayerCard in the deck and removes it
    /// </summary>
    public PlayerCard Draw()
    {
        PlayerCard drawnCard = currentDeck [0];   //retrieve card
        currentDeck.RemoveAt(0);           //remove card
        curDeckCharges -= drawnCard.charges;//track charges
        return drawnCard;                   //return it
    }

    /// <summary>
    /// adds card c to the bottom of the deck
    /// </summary>
    public void addCardAtBottom(PlayerCard c)
    {
        curDeckCharges += c.charges; //track charges
        currentDeck.Add(c);          //and add the card
    }

    /// <summary>
    /// adds card c to the top of the deck
    /// </summary>
    public void addCardAtTop(PlayerCard c)
    {
        curDeckCharges += c.charges; //track charges
        currentDeck.Insert(0, c);    //and add the card
    }

    /// <summary>
    /// removes d charges from cards in the deck, starting at the top.  cards that hit zero charges in this way are removed
    /// </summary>
    public void Damage(int d) { StartCoroutine(DamageCoroutine(d)); } //hide coroutine-ness since calling code has no reason to care
    private IEnumerator DamageCoroutine(int d)
    {
        //skip if no damage
        if (d == 0)
            yield break;

        //sound
        int soundToPlay = UnityEngine.Random.Range(0, playerDamageSounds.Length);
        audioSource.clip = playerDamageSounds[soundToPlay];
        audioSource.Play();

        List<CardScript> deadCards = new List<CardScript>();

        while (d > 0)
        {
            if (currentDeck.Count == 0)
            {
                StartCoroutine(playerDead());
                yield break;
            }

            PlayerCard topCard = currentDeck[0];
            topCard.charges--;
            curDeckCharges--;
            d--;
            if (topCard.charges == 0)
            {
                currentDeck.RemoveAt(0);

                //spawn a card and kill it immediately to report ch destruction to the player
                CardScript deadCard = Instantiate(playerCardPrefab).GetComponent<CardScript>(); //create the card

               deadCard.transform.SetParent(HandScript.playerHand.transform.root); //declare the new card a child of the UI Canvas, which can be found at the root of the tree containing the player hand

                //position card to match up with the deck image we are spawning at
                RectTransform spawnT = deckImage.rectTransform;
                deadCard.GetComponent<RectTransform>().position = spawnT.position;
                deadCard.GetComponent<RectTransform>().rotation = spawnT.rotation;
                deadCard.GetComponent<RectTransform>().localScale = spawnT.localScale;

                deadCard.SendMessage("SetCard", topCard); //set the card

                //add it to a list.  After all the damage has been dealt and we know the player isn't dead, we'll send them off to be discarded
                deadCards.Add(deadCard);
            }
            else
            {
                currentDeck[0] = topCard;
            }
        }

        //damage has been dealt, and the player inst dead.  Animate the discards, with a slight delay between them (even across separate attacks)
        foreach (CardScript deadCard in deadCards)
        {
            //this is a loop, using the waitToDiscard flag, to enforce discardDelay across multiple instances of this coroutine.  If waitToDiscard is true, another instance has already discarded and we have to wait
            bool discarded = false;
            while (discarded == false)
            {
                if (waitToDiscard == false)
                {
                    waitToDiscard = true;
                    deadCard.SendMessage("scaleToVector", Vector3.one); //scale it up
                    deadCard.SendMessage("flipFaceUp"); //flip it over
                    deadCard.SendMessage("Discard"); //discard it
                    yield return new WaitForSeconds(discardDelay);
                    waitToDiscard = false;
                    discarded = true;
                }
                else
                {
                    yield return null; //waiting on another instance of this coroutine
                }
            }
        }
    }
    
    /// <summary>
    /// returns a new XMLDeck from randomly chosen cards
    /// the deck will contain from 30-60 cards, and each card type can appear 1-5 times.
    /// will not include modded cards.
    /// </summary>
    /// <returns>the new deck</returns>
    public XMLDeck generateRandomDeck()
    {
        int targetCardCount = UnityEngine.Random.Range(30,61); //.Range() excludes upper bound, so this is actually 30-60 inclusive.
        XMLDeck randomDeck = new XMLDeck();

        //while there is still room in the deck, add more cards.
        while (randomDeck.cardCount < targetCardCount)
        {
            //start by picking a card type at random
            PlayerCardData card = CardTypeManagerScript.instance.getRandomCardType();

            //skip tokens
            if (card.isToken)
                continue;

            //skip modded cards
            if (card.isModded)
                continue;

            //skip it if it is already in the deck
            if (randomDeck.contents.Any(xde => xde.name == card.cardName))
                continue;

            //it is not in the deck, so add 1-5 of it
            int amount = UnityEngine.Random.Range(1, 6); //.Range() excludes upper bound, so this is actually 1-5 inclusive
            amount = Math.Min(amount, (targetCardCount - randomDeck.cardCount) ); //do not exceed target card count
            randomDeck.contents.Add(new XMLDeckEntry(card.cardName, amount)); 
        }

        //throw error if the deck is invalid or the wrong size
        Debug.Assert(randomDeck.isValid(), "Generated deck invalid");
        Debug.Assert(randomDeck.cardCount == targetCardCount, "Generated deck wrong size" ); 

        return randomDeck; //done
    }

    /// <summary>
    /// removes d charges from cards in the hand, starting at the top.  
    /// cards that hit zero charges in this way are discarded.  
    /// If the hand is empty, damage is redirected to the deck
    /// </summary>
    /// <param name="d">how much damage to deal</param>
    /// <param name="sourcePosition">where to spawn the damage text</param>
    public void DamageHand(int d, Vector2 sourcePosition)
    {
        //if the player doesnt have a hand, something is seriously wrong
        if (playerHand == null)
        {
            MessageHandlerScript.Error("The player doesnt have a hand to damage!");
            return;
        }

        //delegate to the hand object to deal damage
        int damageDealt = playerHand.Damage(d);

        if (damageDealt < d)
        {
            //if the damage wasn't all dealt, forward the rest to the deck
            Damage(d - damageDealt);
            MessageHandlerScript.instance.spawnPlayerDamageText(sourcePosition, (d - damageDealt));
        }
        else
        {
            //if it was all dealt, then we'll play the sound here
            int soundToPlay = UnityEngine.Random.Range(0, playerDamageSounds.Length);
            audioSource.clip = playerDamageSounds[soundToPlay];
            audioSource.Play();
        }
    }

    /// <summary>
    /// [COROUTINE] handles player death
    /// </summary>
    public IEnumerator playerDead()
    {
        //stop music
        LevelManagerScript.instance.musicSource.Stop();

        //score report
        yield return MessageHandlerScript.ShowAndYield("GAME OVER!\n" + ScoreManagerScript.instance.report(false));

        //reload
        UnityEngine.SceneManagement.SceneManager.LoadScene("Game");
        yield break;
    }

    /// <summary>
    /// attempts to draw a card of the given type.  If this is possible, then that card is removed from the deck and returned as though it were drawn normally
    /// if such a card does not exist, does nothing and returns null
    /// </summary>
    /// <param name="cardType">the type of PlayerCard to draw</param>
    /// <returns>the PlayerCard drawn, if found, or null if not</returns>
    public PlayerCard? DrawCardType(PlayerCardType cardType)
    {
        //finds the first card of the correct type
        foreach (PlayerCard c in currentDeck)
        {
            if (c.data.cardType != cardType)
                continue;

            //card found.  remove it from the list and return it
            currentDeck.Remove(c);
            return c;
        }

        //card was not found.
        return null;
    }
}
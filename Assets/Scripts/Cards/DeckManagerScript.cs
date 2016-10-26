//based on tutorial found here: http://wiki.unity3d.com/index.php?title=Saving_and_Loading_Data:_XmlSerializer

using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using UnityEngine;
using UnityEngine.UI;
using Vexe.Runtime.Types;

//represents a card name and number, used to represent decks in xml
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

//represents a deck of cards in XMLM
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

    //returns whether or not this deck conforms to the rules set in DeckRules
    public bool isValid()
    {
        //tracking variables
        int cardCount = 0;
        int maxCardsOfOneType = 0;

        //deck scan
        foreach (XMLDeckEntry entry in contents)
        {
            cardCount += entry.count;
            if (entry.count > maxCardsOfOneType)
                maxCardsOfOneType = entry.count;
        }

        //compare stats to the deck rules
        if (cardCount         < DeckRules.MIN_CARDS_IN_DECK)      return false;
        if (cardCount         > DeckRules.MAX_CARDS_IN_DECK)      return false;
        if (maxCardsOfOneType > DeckRules.MAX_CARDS_OF_SAME_TYPE) return false;

        //all tests passed.  deck is valid.
        return true;
    }

    //returns whether or not this deck contains modded cards
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

    //[DEV]: provides a button in the inspector to sort the list by card names
    [Show] public void sort()
    {
        contents.Sort(new CardNameComparer());
    }
};

//maintains the collection of card types, including saving/loading to XML
[XmlRoot("PlayerDecks")]
[System.Serializable]
public class DeckCollection
{
    //list of decks
    [XmlArray("Decks")]
    [XmlArrayItem("Deck")]
    [Display(Seq.GuiBox | Seq.PerItemDuplicate | Seq.PerItemRemove)]
    public List<XMLDeck> decks;

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

    public void Save(string path)
    {
        XmlSerializer serializer = new XmlSerializer(typeof(DeckCollection));

        using (StreamWriter stream = new StreamWriter(path, false, Encoding.GetEncoding("UTF-8")))
        {
            serializer.Serialize(stream, this);
        }
    }

    public static DeckCollection Load(string path)
    {
        XmlSerializer serializer = new XmlSerializer(typeof(DeckCollection));
        using (var stream = new FileStream(path, FileMode.Open))
        {
            DeckCollection result = serializer.Deserialize(stream) as DeckCollection;
            result.curPath = path;
            return result;
        }
    }

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

    //returns a list of valid deck names
    public string[] getNames()
    {
        List<string> names = new List<string>();

        foreach (XMLDeck xd in decks)
            names.Add(xd.name);

        names.Sort();
        return names.ToArray();
    }
}

public struct Card
{
    public CardData data; //defines the card type
    public int charges; //number of remaining charges

    public override string ToString() { return data.cardName + "(" + charges + "/" + data.cardMaxCharges + ")"; } //overridden for better display in inspector
}

public class DeckManagerScript : BaseBehaviour
{
    //manager settings (only shown in editor)
    private bool shouldShowSettings() { return !Application.isPlaying; }
    [VisibleWhen("shouldShowSettings")] public static     DeckManagerScript instance; //singleton instance
    [VisibleWhen("shouldShowSettings")] public string     premadeDeckPath;            //location of premade deck file
    [VisibleWhen("shouldShowSettings")] public string     playerDeckPath;             //location of player deck file
    [VisibleWhen("shouldShowSettings")] public HandScript playerHand;                 //reference to the player's hand, if present

    //casualty report settings: a new card is spawned, given these properties, then immediately discarded to report a card death to the player
    [VisibleWhen("shouldShowSettings")] public GameObject playerCardPrefab; //prefab to use when spawning
    [VisibleWhen("shouldShowSettings")] public Image      deckImage;        //image to line up with as the spawn location
    [VisibleWhen("shouldShowSettings")] public float      discardDelay;     //minimum time between discards when multiple cards are destroyed at once
    [Hide] private bool waitToDiscard; //used to enforce discardDelay across multiple instances of DamageCoroutine().  Declared up here because C# wont let me declare a static inside the function itself

    //deck lists (only shown if loaded)
    private bool deckListsLoaded() { return (premadeDecks != null) && (premadeDecks.decks.Count > 0) && (playerDecks != null) && (playerDecks.decks.Count > 0); }
    [VisibleWhen("deckListsLoaded")] public DeckCollection premadeDecks; //stores premade decks
    [VisibleWhen("deckListsLoaded")] public DeckCollection playerDecks;  //stores player decks

    //number of charges in the deck
    [Hide] public int curDeckCharges; //remaining
    [Hide] public int maxDeckCharges; //max

    [Show][VisibleWhen("deckListsLoaded")] public string currentDeckName; //name of the current deck
    [Show][VisibleWhen("deckListsLoaded")] private List<Card> currentDeck; //actual current deck

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
        premadeDecks = DeckCollection.Load(Path.Combine(Application.dataPath, premadeDeckPath));
        playerDecks  = DeckCollection.Load(Path.Combine(Application.dataPath, playerDeckPath));
        currentDeck = new List<Card>();
        deckSize = 0;
        curDeckCharges = 0;
        maxDeckCharges = 0;
    }

    //called to reset the manager
    private void Reset()
    {
        //reload definitions
        premadeDecks = DeckCollection.Load(Path.Combine(Application.dataPath, premadeDeckPath));
        playerDecks = DeckCollection.Load(Path.Combine(Application.dataPath, playerDeckPath));

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

    //saves the player decks back to the file
    public void savePlayerDecks()
    {
        playerDecks.Save(Path.Combine(Application.dataPath, playerDeckPath));
    }

    // Sets the currentDeck based on the XMLDeck
    // note that this does NOT shuffle the deck, since there are (rare) instances where this should not happen
    private void SetDeck(XMLDeck newDeck)
    {
        //init
        currentDeckName = newDeck.name;
        maxDeckCharges = 0;

        //for each card on the list of cards in the deck...
        foreach (XMLDeckEntry xde in newDeck.contents)
        {
            CardData type = CardTypeManagerScript.instance.getCardByName(xde.name);
            //for each individual card to be added...
            for (int i = 0; i < xde.count; i++)
            {
                //setup the data
                Card c;
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

    // Shuffles the current deck by swapping every item with another random item.  This is performed multiple times
    public void Shuffle()
    {
        for (int iteration = 0; iteration < SHUFFLE_ITERATIONS; iteration++)
        {
            for (int i = 0; i < currentDeck.Count; i++)
            {
                int swapTarget = UnityEngine.Random.Range(0, currentDeck.Count);
                Card temp = currentDeck[i];
                currentDeck[i] = currentDeck[swapTarget];
                currentDeck[swapTarget] = temp;
            }
        }
    }

    // Returns the top card in the deck and removes it
    public Card Draw()
    {
        Card drawnCard = currentDeck [0];   //retrieve card
        currentDeck.RemoveAt(0);           //remove card
        curDeckCharges -= drawnCard.charges;//track charges
        return drawnCard;                   //return it
    }

    // adds card c to the bottom of the deck
    public void addCardAtBottom(Card c)
    {
        curDeckCharges += c.charges; //track charges
        currentDeck.Add(c);          //and add the card
    }

    // adds card c to the top of the deck
    public void addCardAtTop(Card c)
    {
        curDeckCharges += c.charges; //track charges
        currentDeck.Insert(0, c);    //and add the card
    }

    //removes d charges from cards in the deck, starting at the top.  cards that hit zero charges in this way are removed
    public void Damage(int d) { StartCoroutine(DamageCoroutine(d)); } //hide coroutine-ness since calling code has no reason to care
    private IEnumerator DamageCoroutine(int d)
    {
        //skip if no damage
        if (d == 0)
            yield break;

        List<CardScript> deadCards = new List<CardScript>();

        while (d > 0)
        {
            if (currentDeck.Count == 0)
            {
                StartCoroutine(playerDead());
                yield break;
            }

            Card topCard = currentDeck[0];
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

    //removes d charges from cards in the hand, starting at the top.  cards that hit zero charges in this way are removed.  If the hand is empty, damage is redirected to the deck
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

        //if the damage wasn't all dealt, forward the rest to the deck
        if (damageDealt < d)
        {
            Damage(d - damageDealt);
            MessageHandlerScript.instance.spawnPlayerDamageText(sourcePosition, (d - damageDealt));
        }
    }

    //handles player death
    public IEnumerator playerDead()
    {
        yield return MessageHandlerScript.ShowAndYield("GAME OVER!\n" + ScoreManagerScript.instance.report(false));
        UnityEngine.SceneManagement.SceneManager.LoadScene("Game");
        yield break;
    }
}
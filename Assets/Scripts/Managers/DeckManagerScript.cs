//based on tutorial found here: http://wiki.unity3d.com/index.php?title=Saving_and_Loading_Data:_XmlSerializer

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using UnityEngine;
using Vexe.Runtime.Types;

//represents a card name and number, used to represent decks in xml
[System.Serializable]
public class XMLDeckEntry
{
    [XmlAttribute] public string name;
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
};

//represents a deck of cards in XMLM
[System.Serializable]
public class XMLDeck
{
    [XmlAttribute] public string name; //name of this deck

    //list of cards and how many instances of them exist
    [XmlArray("Cards")]
    [XmlArrayItem("Card")]
    public List<XMLDeckEntry> contents;
};

//maintains the collection of card types, including saving/loading to XML
[XmlRoot("PlayerDecks")]
[System.Serializable]
public class DeckCollection
{
    //list of decks
    [XmlArray("Decks")]
    [XmlArrayItem("Deck")]
    public List<XMLDeck> decks;

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
            return serializer.Deserialize(stream) as DeckCollection;
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
            Debug.LogWarning("Deck " + targetDeck + " could not be found.  Using a default deck instead");
            result = new XMLDeck();
            result.contents = new List<XMLDeckEntry>();
            result.contents.Add(new XMLDeckEntry("Basic Tower", 60));
        }

        //return the result
        return result;
    }
}

public struct Card
{
    public CardData data; //defines the card type
    public int charges; //number of remaining charges
}

public class DeckManagerScript : BaseBehaviour
{
    //singleton instance
    public static DeckManagerScript instance;

    public string premadeDeckPath;      //location of premade deck file
    public DeckCollection premadeDecks; //stores premade decks

    public string playerDeckPath;       //location of player deck file
    public DeckCollection playerDecks;  //stores player decks

    //number of charges in the deck
    public int curDeckCharges; //remaining

    public int maxDeckCharges; //max

    private List<Card> currentDeck; //current deck

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
    }

    // Sets the currentDeck based on the XMLDeck
    // note that this does NOT shuffle the deck, since there are (rare) instances where this should not happen
    private void SetDeck(XMLDeck newDeck)
    {
        //init
        maxDeckCharges = 0;

        //for each card ont he list of cards in the deck...
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

    // removes d charges from cards, starting at the top.  cards that hit zero charges in this way are removed
    public void Damage(int d)
    {
        Debug.Log("The enemy dealt " + d + " damage!");
        while (d > 0)
        {
            if (currentDeck.Count == 0)
            {
                Debug.Log("GAME OVER!"); //todo: properly handle game over
                Time.timeScale = 0.0f;
                return;
            }

            Card topCard = currentDeck[0];
            topCard.charges--;
            curDeckCharges--;
            d--;
            if (topCard.charges == 0)
            {
                currentDeck.RemoveAt(0);
                Debug.Log(topCard.data.cardName + " was destroyed by the enemy!");
            }
            else
            {
                currentDeck[0] = topCard;
            }
        }
    }
}
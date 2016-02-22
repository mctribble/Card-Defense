//based on tutorial found here: http://wiki.unity3d.com/index.php?title=Saving_and_Loading_Data:_XmlSerializer

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;
using System.IO;
using System.Text;

//represents a card name and number, used to represent decks in xml
[System.Serializable]
public class XMLDeckEntry {
	[XmlAttribute] public string name;
	[XmlAttribute] public int 	 count;
};

//represents a deck of cards in XMLM
[System.Serializable]
public class XMLDeck {

	[XmlAttribute] public string name; //name of this deck

	//list of cards and how many instances of them exist
	[XmlArray("Cards")]
	[XmlArrayItem("Card")]
	public List<XMLDeckEntry> contents; 
};

//maintains the collection of card types, including saving/loading to XML
[XmlRoot("PlayerDecks")]
[System.Serializable]
public class DeckCollection {

	//list of decks
	[XmlArray("Decks")]
	[XmlArrayItem("Deck")]
	public List<XMLDeck> playerDecks;

	public void Save(string path)
	{
		XmlSerializer serializer = new XmlSerializer(typeof(DeckCollection));
		
		using(StreamWriter stream = new StreamWriter( path, false, Encoding.GetEncoding("UTF-8")))
		{
			serializer.Serialize(stream, this);
		}
	}
	
	public static DeckCollection Load(string path)
	{
		XmlSerializer serializer = new XmlSerializer(typeof(DeckCollection));
		using(var stream = new FileStream(path, FileMode.Open))
		{
			return serializer.Deserialize(stream) as DeckCollection;
		}
	}
	
}

public class DeckManagerScript : MonoBehaviour {

	//singleton instance
	public static DeckManagerScript instance;
	
	public string path;					//location of player deck file
	public DeckCollection playerDecks;	//stores player decks
	
	private List<CardData> currentDeck;	//current deck

	private const int SHUFFLE_ITERATIONS = 5; //number of times to shuffle the deck

	public int cardsLeft { //returns number of cards left in the deck
		get {
			return currentDeck.Count;
		}
	}

	public int deckSize { get; set; } //total number of cards in the deck

	// Use this for initialization
	void Awake () {
		instance = this;
		playerDecks = DeckCollection.Load (Path.Combine (Application.dataPath, path));
		currentDeck = new List<CardData> ();
	}
	
	// Sets the currentDeck based on the XMLDeck
	void SetDeck (XMLDeck newDeck) {
		foreach (XMLDeckEntry xde in newDeck.contents) {
			CardData type = CardTypeManagerScript.instance.getCardByName(xde.name);
			for (int i = 0; i < xde.count; i++) {
				currentDeck.Add(type);
			}
		}
		Shuffle ();
		deckSize = cardsLeft;
	}
	
	// Shuffles the current deck by swapping every item with another random item.  This is performed multiple times
	void Shuffle() {
		for (int iteration = 0; iteration < SHUFFLE_ITERATIONS; iteration++) {
			for (int i = 0; i < currentDeck.Count; i++) {
				int swapTarget = Random.Range(0, currentDeck.Count);
				CardData temp = currentDeck[i];
				currentDeck[i] = currentDeck[swapTarget];
				currentDeck[swapTarget] = temp;
			}
		}
	}

	// Returns the top card in the deck and removes it
	public CardData Draw () {
		CardData drawnCard = currentDeck [0];
		currentDeck.RemoveAt (0);
		return drawnCard;
	}
}
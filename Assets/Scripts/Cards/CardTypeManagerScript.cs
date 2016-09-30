﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using UnityEngine;
using Vexe.Runtime.Types;

//maintains the collection of card types, including saving/loading to XML
[XmlRoot("CardTypes")]
[System.Serializable]
public class CardTypeCollection
{
    //list of different card types
    [XmlArray("Cards")]
    [XmlArrayItem("Card")]
    [Display(Seq.GuiBox | Seq.PerItemDuplicate | Seq.PerItemRemove | Seq.Filter)]
    public List<CardData> cardTypes = new List<CardData>();

    public void Save(string path)
    {
        //temporarily remove all modded cards
        List<CardData> temp = new List<CardData>(cardTypes);
        cardTypes.RemoveAll(ct => ct.isModded);

        //save normally
        XmlSerializer serializer = new XmlSerializer(typeof(CardTypeCollection));

        using (StreamWriter stream = new StreamWriter(path, false, Encoding.GetEncoding("UTF-8")))
        {
            serializer.Serialize(stream, this);
        }

        //put the list back
        cardTypes = temp;
    }

    public static CardTypeCollection Load(string path)
    {
        XmlSerializer serializer = new XmlSerializer(typeof(CardTypeCollection));
        using (var stream = new FileStream(path, FileMode.Open))
        {
            return serializer.Deserialize(stream) as CardTypeCollection;
        }
    }

    //DEV: gives a button in the editor to sort the type list
    [Hide] public bool cardTypesLoaded() { return (cardTypes != null) && (cardTypes.Count > 0); }
    [Show][VisibleWhen("cardTypesLoaded")] private void sortCardTypes ()
    {
        cardTypes.Sort(new CardTypeComparer());
    }
}

public class CardTypeManagerScript : BaseBehaviour
{
    //manager settings: only shown outside of gameplay
    [Hide] private bool shouldShowSettings() { return !Application.isPlaying; }
    [VisibleWhen("shouldShowSettings")] public static CardTypeManagerScript instance; //singleton instance
    [VisibleWhen("shouldShowSettings")] public string path;                           //path of base game cards
    [VisibleWhen("shouldShowSettings")] public string modPath;                        //path of modded cards

    //contains all card types.  Only shown if it has data
    [Hide] public bool areTypesLoaded() { return (types != null) && (types.cardTypes != null) && (types.cardTypes.Count > 0); }
    [VisibleWhen("areTypesLoaded")] public CardTypeCollection types; 

    // Use this for initialization
    private void Awake()
    {
        instance = this;

        //load base game cards
        types = CardTypeCollection.Load(Path.Combine(Application.dataPath, path));
        foreach (CardData baseCard in types.cardTypes)
            baseCard.isModded = false; //flag base game cards as being from the base game

        //integrate mod files
        CardTypeCollection modTypes;                                                                //temp storage of mod cards
        DirectoryInfo modDir =  new DirectoryInfo (Path.Combine (Application.dataPath, modPath));   //mod folder
        FileInfo[] modFiles = modDir.GetFiles ("*.xml");                                            //file list

        foreach (FileInfo f in modFiles)
        {
            modTypes = CardTypeCollection.Load(f.FullName); //load file
            foreach (CardData moddedCard in modTypes.cardTypes)
            {
                moddedCard.isModded = true; //flag it as from a mod

                //find the existing version of this card
                CardData existingCard = null;
                foreach (CardData baseCard in types.cardTypes)
                {
                    if (baseCard.cardName == moddedCard.cardName)
                    {
                        existingCard = baseCard;
                        break;
                    }
                }

                //replace the card if it exists already, and add it if it doesnt
                if (existingCard != null)
                {
                    types.cardTypes.Remove(existingCard);
                    types.cardTypes.Add(moddedCard);
                    Debug.Log("Overwriting card: " + existingCard.cardName);
                }
                else
                {
                    types.cardTypes.Add(moddedCard);
                }
            }
        }
    }

    //Dev: shows a button in the inspector to save the card types, provided there are some loaded
    [Show][VisibleWhen("areTypesLoaded")] private System.Collections.IEnumerator saveCardChanges()
    {
        yield return StartCoroutine(MessageHandlerScript.PromptYesNo("Are you sure you want to overwrite the card definitions?"));
        if (MessageHandlerScript.responseToLastPrompt == "Yes")
        {
            types.Save(Path.Combine(Application.dataPath, path));
            Debug.Log("Card changes saved. <UNMODDED CARDS ONLY!>");
        }
    }

    //returns a random card type from the database
    public CardData getRandomCardType()
    {
        //get random index
        int index = Mathf.RoundToInt (Random.Range (0.0f, types.cardTypes.Count-1));

        //return card at that index
        return types.cardTypes[index];
    }

    //returns the card from the database with the given name
    public CardData getCardByName(string name)
    {
        foreach (CardData cd in types.cardTypes)
        {
            if (cd.cardName.Equals(name))
            {
                return cd;
            }
        }

        MessageHandlerScript.Error("Could not find card type " + name + ".");
        return null;
    }

    //returns the names of all available cards
    public string[] getCardNames()
    {
        List<string> names = new List<string>();

        foreach (CardData cd in types.cardTypes)
            names.Add(cd.cardName);

        names.Sort();
        return names.ToArray();
    }

    //returns the names of all available tower cards
    public string[] getTowerNames()
    {
        List<string> names = new List<string>();

        foreach (CardData cd in types.cardTypes)
            if (cd.cardType == CardType.tower)
                names.Add(cd.cardName);

        names.Sort();
        return names.ToArray();
    }

    //returns the names of all available upgrade cards
    public string[] getUpgradeNames()
    {
        List<string> names = new List<string>();

        foreach (CardData cd in types.cardTypes)
            if (cd.cardType == CardType.upgrade)
                names.Add(cd.cardName);

        names.Sort();
        return names.ToArray();
    }

    //returns the names of all available spell cards
    public string[] getSpellNames()
    {
        List<string> names = new List<string>();

        foreach (CardData cd in types.cardTypes)
            if (cd.cardType == CardType.spell)
                names.Add(cd.cardName);

        names.Sort();
        return names.ToArray();
    }
}
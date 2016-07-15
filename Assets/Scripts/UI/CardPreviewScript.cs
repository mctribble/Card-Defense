using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class CardPreviewScript : MonoBehaviour
{
    //data
    public CardData data; //card type being previewed

    //object references
    public Image art;         //reference to card art image
    public Text  title;       //reference to card name text
    public Text  description; //reference to card description text

    //saves card definition data and updates components as necessarry
    private IEnumerator SetCard(CardData c)
    {
        //save the data
        data = c;

        //update card text
        title.text = data.cardName + "\n" + data.cardMaxCharges + "/" + data.cardMaxCharges;
        updateDescriptionText();

        //load art with WWW (yes, really!  I couldn't find an easier way to do this and still let the user access the image files)
        WWW www = new WWW ("file:///" + Application.dataPath + "/StreamingAssets/Art/Card Art/" + data.cardArtName + ".png"); //load file
        yield return www; //wait for it to load
        art.sprite = Sprite.Create(www.texture, new Rect(0, 0, www.texture.width, www.texture.height), new Vector2(0.5f, 0.5f));
    }

    //helper function.  updates the card description text.
    private void updateDescriptionText()
    {
        //init
        description.text = "";

        //add info depending on card type
        switch (data.cardType)
        {
            case CardType.spell:
                break;

            case CardType.tower:
                //present tower stats
                description.text += "Damage: " + data.towerData.attackPower + '\n' +
                                    "Range: " + data.towerData.range + '\n' +
                                    "Fires in: " + data.towerData.rechargeTime + "s\n" +
                                    "Lifespan: " + data.towerData.lifespan + '\n';
                break;

            case CardType.upgrade:
                //present upgrade stats
                if (data.upgradeData.waveBonus          != 0) { description.text += "lifespan: +"  + data.upgradeData.waveBonus                               + '\n'; }
                if (data.upgradeData.attackMultiplier   != 1) { description.text += "damage: +"    + (data.upgradeData.attackMultiplier - 1).ToString("P1")   + '\n'; }
                if (data.upgradeData.rangeMultiplier    != 1) { description.text += "range: +"     + (data.upgradeData.rangeMultiplier - 1).ToString("P1")    + '\n'; }
                if (data.upgradeData.rechargeMultiplier != 1) { description.text += "recharge: +"  + (data.upgradeData.rechargeMultiplier - 1).ToString("P1") + '\n'; }
                if (data.upgradeData.attackModifier     != 0) { description.text += "damage: +"    + data.upgradeData.attackModifier.ToString()               + '\n'; }
                if (data.upgradeData.rangeModifier      != 0) { description.text += "range: +"     + data.upgradeData.rangeModifier.ToString()                + '\n'; }
                if (data.upgradeData.rechargeModifier   != 0) { description.text += "recharge: +"  + data.upgradeData.rechargeModifier.ToString()             + "s\n"; }
                if (data.upgradeData.waveBonus          != 0) { description.text += "lifespan: -"  + data.upgradeData.waveBonus                               + '\n'; }
                if (data.upgradeData.attackMultiplier   != 1) { description.text += "damage: -"    + (1 - data.upgradeData.attackMultiplier).ToString("P1")   + '\n'; }
                if (data.upgradeData.rangeMultiplier    != 1) { description.text += "range: -"     + (1 - data.upgradeData.rangeMultiplier).ToString("P1")    + '\n'; }
                if (data.upgradeData.rechargeMultiplier != 1) { description.text += "recharge: -"  + (1 - data.upgradeData.rechargeMultiplier).ToString("P1") + '\n'; }
                if (data.upgradeData.attackModifier     != 0) { description.text += "damage: -"    + data.upgradeData.attackModifier.ToString()               + '\n'; }
                if (data.upgradeData.rangeModifier      != 0) { description.text += "range: -"     + data.upgradeData.rangeModifier.ToString()                + '\n'; }
                if (data.upgradeData.rechargeModifier   != 0) { description.text += "recharge: -"  + data.upgradeData.rechargeModifier.ToString()             + "s\n"; }
                break;
        }

        //if there are effects, add them to the description
        if (data.effectData != null)
        {
            //make sure the effects have been parsed
            if (data.effectData.effects.Count == 0) { data.effectData.parseEffects(); }

            //add a line of text to the description for each
            foreach (IEffect e in data.effectData.effects)
            {
                description.text += "\n<Color=#" + e.effectType.ToString("X") + ">" + e.Name + "</Color>";
            }
        }
        //end with the flavor text found in the card file
        description.text += "\n<i>" + data.cardDescription + "</i>";
    }
}

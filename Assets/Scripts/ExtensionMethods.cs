using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

/// <summary>
/// container class for all the extension methods used in this project
/// </summary>
public static class ExtensionMethods
{
    /// <summary>
    /// returns the int as a roman numeral string.  Supports 0-3999, inclusive. For numbers outside that range, the regular number.toString() is returned instead.
    /// </summary>
    public static string ToRomanNumeral(this int number)
    {
        //very slight modification of this answer on stack overflow: http://stackoverflow.com/a/11749642

        //special cases
        if ((number < 0) || (number > 3999)) return number.ToString(); //out of range
        if (number == 0) return "";                                    //0 is an empty string

        //general case: find the find the beginning of the number and recurse to get the rest
        if (number >= 1000) return "M"  + ToRomanNumeral(number - 1000);
        if (number >= 900)  return "CM" + ToRomanNumeral(number - 900); 
        if (number >= 500)  return "D"  + ToRomanNumeral(number - 500);
        if (number >= 400)  return "CD" + ToRomanNumeral(number - 400);
        if (number >= 100)  return "C"  + ToRomanNumeral(number - 100);
        if (number >= 90)   return "XC" + ToRomanNumeral(number - 90);
        if (number >= 50)   return "L"  + ToRomanNumeral(number - 50);
        if (number >= 40)   return "XL" + ToRomanNumeral(number - 40);
        if (number >= 10)   return "X"  + ToRomanNumeral(number - 10);
        if (number >= 9)    return "IX" + ToRomanNumeral(number - 9);
        if (number >= 5)    return "V"  + ToRomanNumeral(number - 5);
        if (number >= 4)    return "IV" + ToRomanNumeral(number - 4);
        if (number >= 1)    return "I"  + ToRomanNumeral(number - 1);

        //we shouldn't be able to get here
        Debug.LogWarning("something went wrong in extension function ToRoman().");
        return "???";
    }

    //for caching which effects are forbidden where
    private struct Contexts { public bool playerCard; public bool tower; public bool enemyCard; public bool enemyUnit; }
    private static Dictionary<Type, Contexts> contextForbidDict;

    //calculates the contextForbidDict by examining all effects
    private static void buildContextForbidDict()
    {
        contextForbidDict = new Dictionary<Type, Contexts>(); //create the dict

        IEnumerable<Type> effectTypes = EffectTypeManagerScript.IEffectTypes(); //gets all classes that implement IEffect

        foreach(Type T in effectTypes)
        {
            //create a Contexts struct for each one
            Contexts C = new Contexts();
            C.playerCard = false;
            C.tower      = false;
            C.enemyCard  = false;
            C.enemyUnit  = false;

            //search for [ForbidEffectContext] on the type and set the appropriate flag if it is found
            foreach (System.Object attribute in T.GetCustomAttributes(true))
            {
                ForbidEffectContextAttribute fec = attribute as ForbidEffectContextAttribute;
                if (fec != null)
                {
                    switch (fec.forbiddenContext)
                    {
                        case EffectContext.playerCard: C.playerCard = true; break;
                        case EffectContext.tower:      C.tower      = true; break;
                        case EffectContext.enemyCard:  C.enemyCard  = true; break;
                        case EffectContext.enemyUnit:  C.enemyUnit  = true; break;
                        default: Debug.LogWarning("Unknown Context"); break;
                    }
                }
            }

            //add it to the dict
            contextForbidDict.Add(T, C);
        }
    }

    public static bool forbiddenInContext(this IEffect effect, EffectContext context)
    {
        //if the Dictionary doesnt exist yet, build it
        if (contextForbidDict == null)
            buildContextForbidDict();

        //look up the effect
        Contexts C;
        if (contextForbidDict.TryGetValue(effect.GetType(), out C))
        {
            switch (context)
            {
                case EffectContext.playerCard: return C.playerCard;
                case EffectContext.tower:      return C.tower;
                case EffectContext.enemyCard:  return C.enemyCard;
                case EffectContext.enemyUnit:  return C.enemyUnit;
                default: Debug.LogWarning("Unknown Context"); return true;
            }   
        }       
        else    
        {
            Debug.LogError("Could not find " + effect.XMLName + " in the forbidden context dict!");
            return true;
        }
    }
}

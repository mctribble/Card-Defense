using UnityEngine;
using UnityEngine.UI;
using Vexe.Runtime.Types;

public class CastingTooltipScript : BaseBehaviour
{
    public Color CastableColor;             //color of tooltip when cast is OK
    public Color UncastableColor;           //color of tooltip when cast is forbidden
    public Image rangeImage;                //reference to image for the range overlay

    private const float GRID_SCALE = 0.5f;  //size of grid to snap to.
    private CardScript parentCardScript;    //card that produced this tooltip
    private bool castable;                  //whether or not the spell can be cast here
    private GameObject targetTower;         //the tower this card is targeting.  Applies only to upgrades

    // Use this for initialization
    private void Start()
    {
        castable = false;
        targetTower = null;
    }

    // Update is called once per frame
    private void Update()
    {
        //skip if we dont know what the parent is yet
        if (parentCardScript == null)
            return;

        //get position of cursor in world space
        Vector2 mousePositionWorld = Camera.main.ScreenToWorldPoint (Input.mousePosition);

        //move to the mouse position, but snap to a grid of size GRID_SCALE
        transform.position = new Vector3(Mathf.Round(mousePositionWorld.x / GRID_SCALE) * GRID_SCALE,
                                         Mathf.Round(mousePositionWorld.y / GRID_SCALE) * GRID_SCALE,
                                         -3.0f);

        //check if this spot is free
        Collider2D collision = Physics2D.OverlapPoint (transform.position, LayerMask.GetMask("Obstacle"));

        //determine if the card can be cast
        if (parentCardScript.card.data.cardType == CardType.tower)
        {
            //towers only castable if unobstructed
            if (collision)
                castable = false;
            else
                castable = true;
        }
        else if (parentCardScript.card.data.cardType == CardType.upgrade)
        {
            //only castable if there is a tower here
            if (collision)
            {
                if (collision.GetComponent<Collider2D>().gameObject.tag.Equals("TowerImage")) //test for TowerImage to only collide with the tower itself and not its range
                {
                    //get the tower under the cursor
                    GameObject newTargetTower = collision.GetComponent<Collider2D>().gameObject.transform.root.gameObject;

                    //if this is not same tower as before, change our target
                    if (newTargetTower != targetTower)
                    {
                        //tell old tower to revert to the normal tooltip
                        if (targetTower != null)
                            targetTower.SendMessage("UpdateTooltipText");

                        targetTower = newTargetTower; //change target
                        targetTower.SendMessage("UpgradeTooltip", parentCardScript.card.data.upgradeData); //tell new target to use the upgrade tooltip
                        if ( (parentCardScript.card.data.effectData != null) && (parentCardScript.card.data.effectData.effects.Count > 0) )
                            targetTower.SendMessage("NewEffectTooltip", parentCardScript.card.data.effectData); //if there are new effects, show those
                        castable = true;
                    }
                }
                else
                {
                    //there is no tower underneath.  clear the target and tell the tower it can stop showing upgrade data
                    if (targetTower != null)
                    {
                        targetTower.SendMessage("UpdateTooltipText");
                        targetTower = null;
                    }
                    castable = false;
                }
            }
            else
            {
                castable = false;
            }
        }

        //colorize accordingly
        if (castable)
        {
            GetComponentInChildren<Image>().color = CastableColor;
            rangeImage.enabled = true;

            if (Input.GetMouseButtonUp(0))
                Cast();
        }
        else
        {
            GetComponentInChildren<Image>().color = UncastableColor;
            rangeImage.enabled = false;
        }
    }

    //called when a cast is attempted
    private void Cast()
    {
        if (castable)
        {
            //error prevention: cancel cast if card no longer exists
            if (parentCardScript != null)
            {
                if (parentCardScript.card.data.cardType == CardType.tower)
                    parentCardScript.SendMessage("SummonTower", transform.position);
                else if (parentCardScript.card.data.cardType == CardType.upgrade)
                    parentCardScript.SendMessage("UpgradeTower", targetTower);
            }
            Destroy(gameObject);
        }
    }

    //stores a reference to the card that created this tooltip
    private void SetParent(CardScript parent)
    {
        parentCardScript = parent;
    }

    //sets the size of the range overlay
    private void SetRange(float r)
    {
        rangeImage.gameObject.GetComponent<RectTransform>().localScale = new Vector3(r, r, 1.0f);
    }
}
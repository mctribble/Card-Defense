using UnityEngine;
using System.Collections;
using Vexe.Runtime.Types;
using UnityEngine.EventSystems;
using System;

/// <summary>
/// used by level manager to provide an interface for laying down new paths
/// </summary>
public class PathTooltipScript : BaseBehaviour, IPointerClickHandler
{
    public float   gridScale;    //size of the grid to snap to

    private Vector2? lastClickPos; //position of the last click, if there was one

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            //find location of this click in world space
            Vector2 thisClickPos = Camera.main.ScreenToWorldPoint(transform.position);

            //round to two decimal places since the conversion can add some error
            thisClickPos.x = Mathf.Round(thisClickPos.x * 100.0f) / 100.0f;
            thisClickPos.y = Mathf.Round(thisClickPos.y * 100.0f) / 100.0f;

            //if this is not the first click, tell the level manager to spawn a path
            if (lastClickPos != null)
            {
                LevelManagerScript.instance.addPathSegment(lastClickPos.Value, thisClickPos);
            }

            //save the position for use in the next click
            lastClickPos = thisClickPos;
        }
        else if (eventData.button == PointerEventData.InputButton.Right)
        {
            //on right click, clear the last click position so we can start a new path
            lastClickPos = null;
        }
    }

    //on spawn, scale to be a little larger than a path grid square
    private void Start()
    {
        //the scale we want varies on resolution, so we find it by calculating the screen space distance between two points in world space
        transform.localScale = Camera.main.WorldToScreenPoint(new Vector3(0.75f, 0.75f)) - Camera.main.WorldToScreenPoint(Vector3.zero);
    }

    // Update is called once per frame
    private void Update()
    {
        //get position of cursor in world space
        Vector2 mousePositionWorld = Camera.main.ScreenToWorldPoint (Input.mousePosition);

        //find the desired position in world space
        Vector3 worldPosition = new Vector3(Mathf.Round(mousePositionWorld.x / gridScale) * gridScale,
                                            Mathf.Round(mousePositionWorld.y / gridScale) * gridScale,
                                            -3.0f);

        //go there in screen space
        transform.position = Camera.main.WorldToScreenPoint(worldPosition);
    }
}

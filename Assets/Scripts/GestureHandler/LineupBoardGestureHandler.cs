﻿using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class LineupBoardGestureHandler : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public static string GRIDSLOTPANEL = "GridSlotPanel";
    public static bool dragBegins = false;

    public GameObject collectionPanel, createLineupPanel, infoCard, cantSwitch;

    private static float offsetLeft, offsetRight, offsetDown, offsetUp;

    private GameObject selectedObject;
    private Transform parent;
    private CollectionManager collectionManager;
    private BoardInfo boardInfo;
    private Image cardImage;
    private Color tmpColor;

    private void Start()
    {
        collectionManager = collectionPanel.GetComponent<CollectionManager>();
        Rect boardRect = transform.Find("LineupBoard(Clone)").GetComponent<RectTransform>().rect,
             parentRect = transform.parent.GetComponent<RectTransform>().rect;
        offsetLeft = boardRect.x + transform.localPosition.x - parentRect.x;
        offsetRight = boardRect.width + offsetLeft;
        offsetDown = boardRect.y - parentRect.y;
        offsetUp = parentRect.height - offsetDown;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        selectedObject = eventData.pointerCurrentRaycast.gameObject;
        parent = selectedObject.transform.parent;
        cardImage = parent.Find("CardImage").GetComponent<Image>();
        if (selectedObject.name == GRIDSLOTPANEL && cardImage.sprite!=null && cardImage.sprite.name == "Image")
        {
            dragBegins = true;            
            EnableImage(cardImage, false);
            infoCard.SetActive(true);
            infoCard.transform.position = Input.mousePosition;
            infoCard.GetComponent<CardInfo>().SetAttributes(boardInfo.cardLocations[new Location(parent.name)]);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!dragBegins) return;
        infoCard.transform.position = Input.mousePosition;
    }

    public static Location FindLoc(Vector3 loc)
    {
        int x = (int)Mathf.Floor((loc.x - offsetLeft) / 100); // 100 is grid size
        int y = (int)Mathf.Floor((loc.y - offsetDown) / 100);
        return new Location(x, y);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!dragBegins) return;
        dragBegins = false;
        if (!infoCard.GetComponent<CardInfo>().IsStandard())
        {
            cardImage.GetComponent<Image>().sprite = null;
            CardInfo newCard = infoCard.GetComponent<CardInfo>();
            GameObject find = GameObject.Find(FindLoc(Input.mousePosition).ToString());
            if (find != null)
            {
                Transform oldObject = find.transform;
                Image oldCardImage = oldObject.Find("CardImage").GetComponent<Image>();
                if (oldCardImage.sprite != null)
                {
                    // switch
                    PieceAttributes attributes = boardInfo.attributesDict[oldObject.name];                    
                    if (attributes.type == newCard.GetCardType())
                    {
                        boardInfo.SetCard(attributes, new Location(parent.name));
                        cardImage.GetComponent<Image>().sprite = attributes.image;
                        parent = oldObject;
                        boardInfo.SetCard(newCard.piece, new Location(parent.name));
                        oldCardImage.sprite = newCard.piece.image;
                    }
                    else
                    {
                        // Show Animation: Can't Switch
                        StartCoroutine(ShowCantSwitch());
                        cardImage.GetComponent<Image>().sprite = infoCard.GetComponent<CardInfo>().piece.image;
                    }
                }
                else
                {
                    // Drag to an empty spot and resume.
                    cardImage.GetComponent<Image>().sprite = infoCard.GetComponent<CardInfo>().piece.image;
                }
            }
            else
            {
                // Drag outside the board.
                string cardType = newCard.GetCardType();
                collectionManager.AddCollection(new Collection(newCard.piece));
                boardInfo.SetStandardCard(cardType, new Location(parent.name));
                collectionManager.RemoveCollection(Collection.StandardCollection(cardType));
                collectionManager.ShowCurrentPage();
                cardImage.sprite = Database.standardAttributes["Standard " + cardType].image;
            }
        }
        EnableImage(cardImage);
        infoCard.SetActive(false);
    }

    private IEnumerator ShowCantSwitch()
    {
        cantSwitch.SetActive(true);
        yield return new WaitForSeconds(1.5f);
        cantSwitch.SetActive(false);
    }

    private void EnableImage(Image image, bool enable = true)
    {
        tmpColor = image.color;
        if (enable) tmpColor.a = 255;
        else tmpColor.a = 0;
        image.color = tmpColor;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        GameObject clickedObject = eventData.pointerCurrentRaycast.gameObject;
        string cardType, parentName = clickedObject.transform.parent.name;
        if (clickedObject.name == GRIDSLOTPANEL && boardInfo.locationType.TryGetValue(parentName, out cardType))
            collectionManager.ClickTab(cardType);
    }

    public void SetBoardInfo(BoardInfo info) { boardInfo = info; }

    public static bool InBoardRegion(Vector2 pos) { return offsetLeft <= pos.x && pos.x <= offsetRight && offsetDown <= pos.y && pos.y <=  offsetUp; }
}

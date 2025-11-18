using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class HandUIController : MonoBehaviour
{
    public GameObject cardPrefab;
    public Transform cardArea;
    public PlayerState localPlayer;
    public NetworkTurnHandler net;

    List<UICard> cards = new List<UICard>();

    void Start()
    {
        GameEventBus.OnRequestUIRefresh += RefreshUI;
    }

    public void RefreshUI()
    {
        if (localPlayer == null)
        {
            Debug.LogError("HandUIController: localPlayer is NULL");
            return;
        }

        if (cardArea == null)
        {
            Debug.LogError("HandUIController: cardArea is NULL");
            return;
        }

        if (cardPrefab == null)
        {
            Debug.LogError("HandUIController: cardPrefab is NULL");
            return;
        }

        foreach (Transform t in cardArea)
            Destroy(t.gameObject);

        cards.Clear();

        foreach (int cardId in localPlayer.hand)
        {
            var data = GameManager.Instance.cardDatabase.GetById(cardId);
            if (data == null) continue;

            var go = Instantiate(cardPrefab, cardArea);
            var ui = go.GetComponent<UICard>();
            ui.Init(data);
            cards.Add(ui);
        }
    }


    public void OnPressPlay()
    {
        List<int> selected = new List<int>();
        foreach (var c in cards)
            if (c.selected) selected.Add(c.cardId);

        net.SendPlayCards(localPlayer.actorNumber, selected.ToArray());
    }
}

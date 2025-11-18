using Photon.Pun;
using TMPro;
using UnityEngine;

public class WaitingOverlayUI : MonoBehaviour
{
    [Header("Overlay References")]
    [SerializeField] private GameObject overlayRoot;
    [SerializeField] private TextMeshProUGUI messageLabel;
    [TextArea]
    [SerializeField] private string waitingMessage = "Waiting for other player to make a move";

    private void Awake()
    {
        HideOverlay();
    }

    private void OnEnable()
    {
        GameEventBus.OnCardsPlayed += HandleCardsPlayed;
        GameEventBus.OnTurnResolved += HandleTurnResolved;
    }

    private void OnDisable()
    {
        GameEventBus.OnCardsPlayed -= HandleCardsPlayed;
        GameEventBus.OnTurnResolved -= HandleTurnResolved;
    }

    private void HandleCardsPlayed(int actorNumber, int[] _)
    {
        if (PhotonNetwork.LocalPlayer == null)
            return;

        if (actorNumber == PhotonNetwork.LocalPlayer.ActorNumber)
        {
            ShowOverlay();
        }
    }

    private void HandleTurnResolved(int _)
    {
        HideOverlay();
    }

    private void ShowOverlay()
    {
        if (messageLabel != null)
        {
            messageLabel.text = waitingMessage;
        }

        if (overlayRoot != null && !overlayRoot.activeSelf)
        {
            overlayRoot.SetActive(true);
        }
    }

    private void HideOverlay()
    {
        if (overlayRoot != null && overlayRoot.activeSelf)
        {
            overlayRoot.SetActive(false);
        }
    }
}


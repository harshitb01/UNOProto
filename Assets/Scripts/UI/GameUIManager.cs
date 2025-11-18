using UnityEngine;
using TMPro;
using Photon.Pun;

public class GameUIManager : MonoBehaviour
{
    public static GameUIManager Instance;

    [Header("Player UI")]
    public TextMeshProUGUI playerScoreText;
    public TextMeshProUGUI playerEnergyText;
    public TextMeshProUGUI playerDeckCountText;

    [Header("Opponent UI")]
    public TextMeshProUGUI opponentScoreText;
    public TextMeshProUGUI opponentEnergyText;
    public TextMeshProUGUI opponentDeckCountText;

    [Header("Timer")]
    public TextMeshProUGUI timerText;

    [Header("Round")]
    public TextMeshProUGUI roundText;

    private void Awake()
    {
        Instance = this;
    }

    private void OnEnable()
    {
        GameEventBus.OnRequestUIRefresh += RefreshUI;
    }

    private void OnDisable()
    {
        GameEventBus.OnRequestUIRefresh -= RefreshUI;
    }

    public void RefreshUI()
    {
        if (GameManager.Instance == null || GameManager.Instance.players == null)
            return;

        int localActor = PhotonNetwork.LocalPlayer.ActorNumber;

        foreach (var kv in GameManager.Instance.players)
        {
            int actor = kv.Key;
            var ps = kv.Value;

            if (actor == localActor)
            {
                playerScoreText.text = $"Score: {ps.score}";
                playerEnergyText.text = $"Energy: {ps.energy}";
                playerDeckCountText.text = $"Remaining Cards: {ps.deck.Count}";
            }
            else
            {
                opponentScoreText.text = $"Enemy Score: {ps.score}";
                opponentEnergyText.text = $"Enemy Energy: {ps.energy}";
                opponentDeckCountText.text = $"Enemy Remaining Cards: {ps.deck.Count}";
            }
        }

        SetRound(GameManager.Instance.currentTurn, GameManager.Instance.totalTurns);
    }

    public void SetTimer(int seconds)
    {
        timerText.text = seconds.ToString();
    }

    public void SetRound(int current, int total)
    {
        int safeTurn = Mathf.Clamp(current, 0, total);
        roundText.text = $"Turn {safeTurn} / {total}";
    }
}

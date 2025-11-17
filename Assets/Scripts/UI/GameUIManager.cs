using UnityEngine;
using TMPro;
using Photon.Pun;

public class GameUIManager : MonoBehaviour
{
    public static GameUIManager Instance;

    [Header("Player UI")]
    public TextMeshProUGUI playerScoreText;
    public TextMeshProUGUI playerEnergyText;

    [Header("Opponent UI")]
    public TextMeshProUGUI opponentScoreText;
    public TextMeshProUGUI opponentEnergyText;

    [Header("Timer")]
    public TextMeshProUGUI timerText;

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

        int local = PhotonNetwork.LocalPlayer.ActorNumber;

        foreach (var kv in GameManager.Instance.players)
        {
            if (kv.Key == local)
            {
                // our score
                playerScoreText.text = "My Score: " + kv.Value.score;
                playerEnergyText.text = "My Energy: " + kv.Value.energy;
            }
            else
            {
                // opponent score
                opponentScoreText.text = "P2 Score: " + kv.Value.score;
                opponentEnergyText.text = "P2 Energy: " + kv.Value.energy;
            }
        }

    }

    public void SetTimer(int seconds)
    {
        timerText.text = seconds.ToString();
    }
}

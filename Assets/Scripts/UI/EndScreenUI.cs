using UnityEngine;
using TMPro;

public class EndScreenUI : MonoBehaviour
{
    public static EndScreenUI Instance;
    public GameObject endScreenRoot;
    public TextMeshProUGUI winnerText;
    public TextMeshProUGUI finalScoreText;
    public TextMeshProUGUI opponentScoreText;

    private void Awake()
    {
        Instance = this;
        endScreenRoot.SetActive(false);
    }

    public void ShowEndScreen(bool youWin, int yourScore, int oppScore)
    {
        endScreenRoot.SetActive(true);

        if (youWin)
            winnerText.text = "YOU WIN!";
        else if (yourScore == oppScore)
            winnerText.text = "DRAW!";
        else
            winnerText.text = "YOU LOSE!";

        finalScoreText.text = "Your Score: " + yourScore;
        opponentScoreText.text = "Opponent Score: " + oppScore;
    }

    public void OnPlayAgain()
    {
        Photon.Pun.PhotonNetwork.LeaveRoom();
        UnityEngine.SceneManagement.SceneManager.LoadScene("Lobby");
    }

    public void OnQuit()
    {
        Application.Quit();
    }
}

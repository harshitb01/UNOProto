using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;

public class PhotonNetworkManager : MonoBehaviourPunCallbacks
{
    public static PhotonNetworkManager Instance;
    public TextMeshProUGUI statusText;
    public TMP_InputField inputCode;
    public TextMeshProUGUI roomCodeText;
    string generatedCode;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        if (!PhotonNetwork.IsConnected)
        {
            PhotonNetwork.ConnectUsingSettings();
        }
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("Connected to Photon Master");
        PhotonNetwork.AutomaticallySyncScene = false;
    }

    public void CreateRoomWithCode(string code)
    {
        if (!PhotonNetwork.IsConnectedAndReady)
        {
            Debug.LogError("Not connected to Photon yet");
            return;
        }

        RoomOptions options = new RoomOptions
        {
            MaxPlayers = 2,
            IsVisible = false,
            IsOpen = true
        };

        PhotonNetwork.CreateRoom(code, options);
    }
    public void JoinRoomWithCode(string code)
    {
        if (!PhotonNetwork.IsConnectedAndReady)
        {
            Debug.LogError("Not connected to Photon yet");
            return;
        }

        PhotonNetwork.JoinRoom(code);
    }

    public void CreateRoom()
    {
        generatedCode = RoomCodeGenerator.Generate();
        roomCodeText.text = "Room Code: " + generatedCode;
        CreateRoomWithCode(generatedCode);
    }

    public void JoinRoom()
    {
        string code = inputCode.text.ToUpper().Trim();

        if (string.IsNullOrEmpty(code))
        {
            Debug.LogError("Code empty!");
            return;
        }

        JoinRoomWithCode(code);
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.LogError("Create room failed: " + message);
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.LogError("Join room failed: " + message);
    }

    public override void OnCreatedRoom()
    {
        statusText.text = "Created room. Waiting for opponent...";
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("Joined room. Players: " + PhotonNetwork.CurrentRoom.PlayerCount);
        // load game scene when 2 players present
        if (PhotonNetwork.CurrentRoom.PlayerCount == 2)
        {
            StartCoroutine(LoadGameScene());
        }
        else
        {
            statusText.text = "Waiting for 2nd player...";
        }
        // StartCoroutine(LoadGameScene());
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        if (PhotonNetwork.CurrentRoom.PlayerCount == 2 && PhotonNetwork.IsMasterClient)
        {
            StartCoroutine(LoadGameScene());
        }
    }

    IEnumerator LoadGameScene()
    {
        yield return null; // let UI update
        PhotonNetwork.LoadLevel("Game"); // requires Photon PUN scene sync if enabled
    }

    // Reconnect handling: attempt to ReconnectAndRejoin if disconnected
    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogWarning("Disconnected: " + cause);
        // Try to reconnect automatically
        StartCoroutine(AttemptReconnect());
    }

    IEnumerator AttemptReconnect()
    {
        yield return new WaitForSeconds(1f);
        PhotonNetwork.ReconnectAndRejoin();
    }

    public override void OnJoinedLobby() { }
}

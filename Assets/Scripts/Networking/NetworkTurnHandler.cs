using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using UnityEngine;
using System.Collections.Generic;

public class NetworkTurnHandler : MonoBehaviour, IOnEventCallback
{
    public const byte EVT_PLAY_CARDS = 1;
    public const byte EVT_TURN_RESULT = 2;
    public const byte EVT_REQUEST_STATE = 3;
    public const byte EVT_FULL_STATE = 4;
    public const byte EVT_END_GAME = 5;

    void OnEnable() { PhotonNetwork.AddCallbackTarget(this); }
    void OnDisable() { PhotonNetwork.RemoveCallbackTarget(this); }

    public void SendPlayCards(int actorNumber, int[] cardIds)
    {
        var payload = new Dictionary<string, object> {
            { "player", actorNumber },
            { "cardIds", cardIds }
        };
        var content = NewtonsoftJsonWrapper.Serialize(payload);
        var options = new RaiseEventOptions { Receivers = ReceiverGroup.All };
        PhotonNetwork.RaiseEvent(EVT_PLAY_CARDS, content, options, SendOptions.SendReliable);
    }

    public void BroadcastTurnResult(object resultObject)
    {
        var content = NewtonsoftJsonWrapper.Serialize(resultObject);
        PhotonNetwork.RaiseEvent(EVT_TURN_RESULT, content, RaiseEventOptions.Default, SendOptions.SendReliable);
    }

    public void BroadcastEndGame(object state)
    {
        var json = NewtonsoftJsonWrapper.Serialize(state);
        PhotonNetwork.RaiseEvent(EVT_END_GAME, json,
            new RaiseEventOptions { Receivers = ReceiverGroup.All },
            ExitGames.Client.Photon.SendOptions.SendReliable);
    }


    public void OnEvent(EventData photonEvent)
    {
        byte ev = photonEvent.Code;
        if (ev == EVT_PLAY_CARDS)
        {
            string json = (string)photonEvent.CustomData;
            var dict = NewtonsoftJsonWrapper.Deserialize<Dictionary<string, object>>(json);
            int player = (int)(long)dict["player"];
            var cardIds = NewtonsoftJsonWrapper.ConvertToIntArray(dict["cardIds"]);
            GameEventBus.OnCardsPlayed?.Invoke(player, cardIds);
        }
        else if (ev == EVT_TURN_RESULT)
        {
            string json = (string)photonEvent.CustomData;
            GameManager.Instance.HandleTurnResultFromNetwork(json);
        }
        else if (ev == EVT_REQUEST_STATE)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                string req = (string)photonEvent.CustomData;
                var reqDict = NewtonsoftJsonWrapper.Deserialize<Dictionary<string, object>>(req);
                int requester = (int)(long)reqDict["requester"];
                var fullState = GameManager.Instance.GetFullState();
                var payload = new Dictionary<string, object> {
                    { "to", requester },
                    { "state", fullState }
                };
                PhotonNetwork.RaiseEvent(EVT_FULL_STATE, NewtonsoftJsonWrapper.Serialize(payload), new RaiseEventOptions { Receivers = ReceiverGroup.Others }, SendOptions.SendReliable);
            }
        }
        else if (ev == EVT_FULL_STATE)
        {
            string json = (string)photonEvent.CustomData;
            var payload = NewtonsoftJsonWrapper.Deserialize<Dictionary<string, object>>(json);
            var state = payload["state"];
            GameManager.Instance.RestoreFullStateFromNetwork(state);
        }
        else if (ev == 99) // timer update event
        {
            int time = (int)photonEvent.CustomData;
            GameUIManager.Instance?.SetTimer(time);
        }
        else if (ev == EVT_END_GAME)
        {
            string json = (string)photonEvent.CustomData;
            GameManager.Instance.OnReceiveEndGame(json);
        }

    }
}

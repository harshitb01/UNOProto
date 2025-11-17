using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

[System.Serializable]
public class TurnSubmission
{
    public int[] playedCardIds;
    public int basePower;
    public List<string> abilities = new List<string>();
}

[System.Serializable]
public class TurnResultBroadcast
{
    public int turnIndex;
    public Dictionary<int, PlayerTurnResult> players = new Dictionary<int, PlayerTurnResult>();
}

[System.Serializable]
public class PlayerTurnResult
{
    public int score;
    public int energy;
    public int finalPower;
    public int[] played;
    public string[] abilities;
}


public class GameManager : MonoBehaviourPunCallbacks
{
    public static GameManager Instance;

    public int totalTurns = 6;
    public int currentTurn = 0;
    public int turnTimer = 30;

    public CardDatabase cardDatabase;
    public Dictionary<int, PlayerState> players = new Dictionary<int, PlayerState>(); // key: actorNumber

    private Dictionary<int, int[]> submittedThisTurn = new Dictionary<int, int[]>();
    private NetworkTurnHandler net;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        net = FindObjectOfType<NetworkTurnHandler>();
        cardDatabase = CardDatabase.LoadFromResources();
        GameEventBus.OnCardsPlayed += OnCardsPlayed;
        InitializePlayers();
        var handUI = FindObjectOfType<HandUIController>();
        if (handUI != null)
        {
            handUI.localPlayer = players[PhotonNetwork.LocalPlayer.ActorNumber];
            handUI.net = net;
        }
        StartCoroutine(StartMatchRoutine());
    }

    void InitializePlayers()
    {
        foreach (var p in PhotonNetwork.PlayerList)
        {
            var ps = new PlayerState(p.ActorNumber);
            // simple deck: fill with sample ids, shuffle
            ps.deck = new List<int>();
            foreach (var c in cardDatabase.cards) ps.deck.Add(c.id);
            Shuffle(ps.deck);
            DrawInitialHand(ps);
            players[p.ActorNumber] = ps;
        }
    }

    void DrawInitialHand(PlayerState ps)
    {
        for (int i = 0; i < 3; i++) DrawFromDeck(ps);
    }

    void DrawFromDeck(PlayerState ps)
    {
        if (ps.deck.Count == 0) return;
        int id = ps.deck[0];
        ps.deck.RemoveAt(0);
        ps.hand.Add(id);
    }

    IEnumerator StartMatchRoutine()
    {
        yield return new WaitForSeconds(0.5f);
        NextTurn();
    }

    void NextTurn()
    {
        currentTurn++;
        submittedThisTurn.Clear();
        foreach (var p in players.Values)
        {
            p.energy = Mathf.Min(6, p.energy + 1); // increase energy each turn; initial energy should be set beforehand
            DrawFromDeck(p);
            p.blockedThisTurn = false;
        }
        // start timer coroutine (local to master)
        if (PhotonNetwork.IsMasterClient) StartCoroutine(TurnTimerCoroutine());
        // notify UI
        GameEventBus.OnRequestUIRefresh?.Invoke();
    }

    IEnumerator TurnTimerCoroutine()
    {
        int t = turnTimer;

        while (t > 0)
        {
            // master updates UI locally
            GameUIManager.Instance?.SetTimer(t);

            // master sends timer to all clients
            PhotonNetwork.RaiseEvent(
                99, // custom event code for timer
                t,
                new RaiseEventOptions { Receivers = ReceiverGroup.All },
                ExitGames.Client.Photon.SendOptions.SendUnreliable
            );

            yield return new WaitForSeconds(1f);
            t--;
        }

        // 0 on timeout
        PhotonNetwork.RaiseEvent(
            99,
            0,
            new RaiseEventOptions { Receivers = ReceiverGroup.All },
            ExitGames.Client.Photon.SendOptions.SendUnreliable
        );

        ResolveTurn();
    }



    void OnCardsPlayed(int playerActor, int[] cardIds)
    {
        // store latest submission from player for this turn
        submittedThisTurn[playerActor] = cardIds;
        // optional: if both players submitted early, resolve immediately
        if (submittedThisTurn.Count >= players.Count && PhotonNetwork.IsMasterClient)
        {
            ResolveTurn();
        }
    }

    void ResolveTurn()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        Dictionary<int, TurnSubmission> submissions = new Dictionary<int, TurnSubmission>();

        // Build turn submissions
        foreach (var kv in players)
        {
            int actor = kv.Key;
            var ps = kv.Value;

            int[] submitted = submittedThisTurn.ContainsKey(actor)
                ? submittedThisTurn[actor]
                : new int[0];

            TurnSubmission ts = new TurnSubmission();
            ts.playedCardIds = submitted;
            int totalCost = 0;
            int totalPower = 0;

            // Process each card
            foreach (int cid in submitted)
            {
                var c = cardDatabase.GetById(cid);
                if (c != null)
                {
                    totalCost += c.cost;
                    totalPower += c.power;
                    ts.abilities.AddRange(c.abilities);
                    ps.hand.Remove(cid);
                }
            }

            // Cost reduction if over limit
            List<int> mutable = new List<int>(submitted);
            while (totalCost > ps.energy && mutable.Count > 0)
            {
                int last = mutable[mutable.Count - 1];
                var c = cardDatabase.GetById(last);
                if (c != null)
                {
                    totalCost -= c.cost;
                    totalPower -= c.power;
                    foreach (string ab in c.abilities)
                        ts.abilities.Remove(ab);
                }
                mutable.RemoveAt(mutable.Count - 1);
            }

            ts.playedCardIds = mutable.ToArray();
            ts.basePower = totalPower;
            submissions[actor] = ts;

            ps.energy -= totalCost;
        }

        // Compute final powers
        Dictionary<int, int> finalPower = new Dictionary<int, int>();
        foreach (var kv in submissions)
        {
            int actor = kv.Key;
            var sub = kv.Value;

            int p = sub.basePower;

            if (sub.abilities.Contains("DoublePower"))
                p *= 2;

            finalPower[actor] = p;
        }

        // BlockNextAttack
        foreach (var kv in submissions)
        {
            int actor = kv.Key;
            var sub = kv.Value;

            if (sub.abilities.Contains("BlockNextAttack"))
            {
                foreach (var opp in players.Keys)
                {
                    if (opp != actor)
                        finalPower[opp] = 0;
                }
            }
        }

        // Apply power to score
        foreach (var kv in finalPower)
            players[kv.Key].score += kv.Value;

        // Other abilities
        foreach (var kv in submissions)
        {
            int actor = kv.Key;
            var sub = kv.Value;

            foreach (string ab in sub.abilities)
            {
                if (ab == "GainPoints")
                    players[actor].score += 2;
                else if (ab == "StealPoints")
                {
                    foreach (var opp in players.Values)
                    {
                        if (opp.actorNumber != actor && opp.score > 0)
                        {
                            opp.score--;
                            players[actor].score++;
                            break;
                        }
                    }
                }
                else if (ab == "DrawExtraCard")
                    DrawFromDeck(players[actor]);
            }
        }

        // Build broadcast payload
        TurnResultBroadcast br = new TurnResultBroadcast();
        br.turnIndex = currentTurn;

        foreach (var kv in players)
        {
            int actor = kv.Key;
            var ps = kv.Value;
            var sub = submissions[actor];

            PlayerTurnResult ptr = new PlayerTurnResult();
            ptr.energy = ps.energy;
            ptr.score = ps.score;
            ptr.finalPower = finalPower[actor];
            ptr.played = sub.playedCardIds;
            ptr.abilities = sub.abilities.ToArray();
            br.players[actor] = ptr;
        }

        net.BroadcastTurnResult(br);

        // Next turn or end match
        if (currentTurn >= totalTurns)
            EndMatch();
        else
            NextTurn();

        GameEventBus.OnTurnResolved?.Invoke(currentTurn);
    }


    void EndMatch()
    {
        Debug.Log("END MATCH CALLED (MASTER)");

        // Build final score data to broadcast
        Dictionary<int, int> scoreMap = new Dictionary<int, int>();

        foreach (var kv in players)
        {
            scoreMap[kv.Key] = kv.Value.score;
        }

        // Wrap it in an object for serialization
        var payload = new Dictionary<string, object>()
    {
        { "players", scoreMap }
    };

        // 🔥 Send END GAME to ALL clients (including master)
        net.BroadcastEndGame(payload);

        Debug.Log("EndGame event sent to all clients.");
    }



    void Shuffle<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int r = Random.Range(i, list.Count);
            var tmp = list[i];
            list[i] = list[r];
            list[r] = tmp;
        }
    }

    // Called by NetworkTurnHandler when clients receive TurnResult
    public void HandleTurnResultFromNetwork(string json)
    {
        var payload = NewtonsoftJsonWrapper.Deserialize<Dictionary<string, object>>(json);

        var playersDict = (Newtonsoft.Json.Linq.JObject)payload["players"];

        foreach (var kv in playersDict)
        {
            int actor = int.Parse(kv.Key);
            var result = kv.Value.ToObject<PlayerTurnResult>();

            // IMPORTANT: update local game state
            var ps = players[actor];
            ps.score = result.score;
            ps.energy = result.energy;
        }

        GameEventBus.OnRequestUIRefresh?.Invoke();
    }


    public object GetFullState()
    {
        // Return a serializable object representing full state (players, turn, decksize etc)
        var payload = new Dictionary<string, object>();
        payload["turn"] = currentTurn;
        var pld = new Dictionary<int, object>();
        foreach (var kv in players)
        {
            pld[kv.Key] = new
            {
                score = kv.Value.score,
                energy = kv.Value.energy,
                hand = kv.Value.hand,
                deck = kv.Value.deck
            };
        }
        payload["players"] = pld;
        return payload;
    }

    public void OnReceiveEndGame(string json)
    {
        Debug.Log("Received END GAME EVENT");

        var payload = NewtonsoftJsonWrapper.Deserialize<Dictionary<string, object>>(json);
        var playersObj = (Newtonsoft.Json.Linq.JObject)payload["players"];

        // Convert JSON -> Dictionary<int,int>
        Dictionary<int, int> scores = new Dictionary<int, int>();
        foreach (var kv in playersObj)
        {
            scores[int.Parse(kv.Key)] = (int)(long)kv.Value;
        }

        // Local and opponent score extraction
        int localActor = Photon.Pun.PhotonNetwork.LocalPlayer.ActorNumber;

        int myScore = scores[localActor];

        int oppScore = 0;
        foreach (var kv in scores)
            if (kv.Key != localActor)
                oppScore = kv.Value;

        bool draw = (myScore == oppScore);
        bool iWin = (myScore > oppScore);

        // Draw overrides win flag
        if (draw) iWin = false;

        // SHOW UI
        EndScreenUI.Instance.ShowEndScreen(iWin, myScore, oppScore);
    }



    public void RestoreFullStateFromNetwork(object stateObj)
    {
        // parse stateObj and set local players accordingly. Implementation left brief
        GameEventBus.OnRequestUIRefresh?.Invoke();
    }
}

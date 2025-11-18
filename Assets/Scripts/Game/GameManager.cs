using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using Newtonsoft.Json.Linq;

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
    public int handCount;
    public int deckCount;
    public int[] hand;
    public int[] deck;
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
    private Coroutine turnTimerRoutine;
    private bool turnResolving;

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
            if (players.ContainsKey(PhotonNetwork.LocalPlayer.ActorNumber))
            {
                handUI.localPlayer = players[PhotonNetwork.LocalPlayer.ActorNumber];
                handUI.net = net;
            }
            else
            {
                Debug.LogError("Local player state not found when linking HandUIController");
            }
        }

        StartCoroutine(StartMatchRoutine());
    }

    void InitializePlayers()
    {
        foreach (var p in PhotonNetwork.PlayerList)
        {
            var ps = new PlayerState(p.ActorNumber);
            ps.energy = 0;
            ps.score = 0;
            ps.blockedThisTurn = false;
            ps.hand = new List<int>();
            ps.deck = new List<int>();

            List<int> pool = new List<int>();
            foreach (var c in cardDatabase.cards) pool.Add(c.id);

            if (pool.Count == 0)
            {
                Debug.LogError("CardDatabase is empty!");
            }
            else
            {
                List<int> shuffled = new List<int>(pool);
                Shuffle(shuffled);

                if (shuffled.Count >= 12)
                {
                    for (int i = 0; i < 12; i++) ps.deck.Add(shuffled[i]);
                }
                else
                {
                    int idx = 0;
                    while (ps.deck.Count < 12)
                    {
                        ps.deck.Add(shuffled[idx % shuffled.Count]);
                        idx++;
                    }
                }
            }

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
        if (ps.deck == null) ps.deck = new List<int>();
        if (ps.hand == null) ps.hand = new List<int>();

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
            p.energy = Mathf.Min(6, p.energy + 1);
            DrawFromDeck(p);

            p.blockedThisTurn = false;
        }

        turnResolving = false;
        if (turnTimerRoutine != null)
        {
            StopCoroutine(turnTimerRoutine);
            turnTimerRoutine = null;
        }
        if (PhotonNetwork.IsMasterClient) turnTimerRoutine = StartCoroutine(TurnTimerCoroutine());
        GameEventBus.OnRequestUIRefresh?.Invoke();
    }

    IEnumerator TurnTimerCoroutine()
    {
        int t = turnTimer;

        while (t > 0 && !turnResolving)
        {
            BroadcastTimerValue(t);
            yield return new WaitForSeconds(1f);
            t--;
        }

        if (!turnResolving)
        {
            BroadcastTimerValue(0);
            ResolveTurn();
        }
    }

    private void BroadcastTimerValue(int value)
    {
        GameUIManager.Instance?.SetTimer(value);
        PhotonNetwork.RaiseEvent(
            99,
            value,
            new RaiseEventOptions { Receivers = ReceiverGroup.All },
            ExitGames.Client.Photon.SendOptions.SendUnreliable
        );
    }

    void OnCardsPlayed(int playerActor, int[] cardIds)
    {
        submittedThisTurn[playerActor] = cardIds;
        if (submittedThisTurn.Count >= players.Count && PhotonNetwork.IsMasterClient)
        {
            ResolveTurn();
        }
    }

    void ResolveTurn()
    {
        if (!PhotonNetwork.IsMasterClient || turnResolving) return;
        turnResolving = true;

        if (turnTimerRoutine != null)
        {
            StopCoroutine(turnTimerRoutine);
            turnTimerRoutine = null;
        }
        BroadcastTimerValue(0);

        Dictionary<int, TurnSubmission> submissions = new Dictionary<int, TurnSubmission>();

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

            foreach (int cid in submitted)
            {
                var c = cardDatabase.GetById(cid);
                if (c != null)
                {
                    totalCost += c.cost;
                    totalPower += c.power;
                    ts.abilities.AddRange(c.abilities);
                    if (ps.hand.Contains(cid)) ps.hand.Remove(cid); // Remove card from hand (if present)
                }
            }

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
            if (ps.energy < 0) ps.energy = 0;
        }

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

        foreach (var kv in finalPower)
            players[kv.Key].score += kv.Value;


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
                {
                    DrawFromDeck(players[actor]);
                }
            }
        }

        int resolvedTurnIndex = currentTurn;
        bool isLastTurn = currentTurn >= totalTurns;
        if (!isLastTurn)
        {
            NextTurn();
        }

        TurnResultBroadcast br = new TurnResultBroadcast();
        br.turnIndex = resolvedTurnIndex;

        foreach (var kv in players)
        {
            int actor = kv.Key;
            var ps = kv.Value;

            var sub = submissions.ContainsKey(actor) ? submissions[actor] : new TurnSubmission();

            PlayerTurnResult ptr = new PlayerTurnResult();
            ptr.energy = ps.energy;
            ptr.score = ps.score;
            ptr.finalPower = finalPower.ContainsKey(actor) ? finalPower[actor] : 0;
            ptr.played = sub.playedCardIds ?? new int[0];
            ptr.abilities = sub.abilities != null ? sub.abilities.ToArray() : new string[0];
            ptr.handCount = ps.hand != null ? ps.hand.Count : 0;
            ptr.deckCount = ps.deck != null ? ps.deck.Count : 0;
            ptr.hand = ps.hand != null ? ps.hand.ToArray() : new int[0];
            ptr.deck = ps.deck != null ? ps.deck.ToArray() : new int[0];

            br.players[actor] = ptr;
        }

        net.BroadcastTurnResult(br);

        if (isLastTurn)
        {
            EndMatch();
        }

        GameEventBus.OnTurnResolved?.Invoke(currentTurn);
    }

    void EndMatch()
    {
        Debug.Log("END MATCH CALLED (MASTER)");
        Dictionary<int, int> scoreMap = new Dictionary<int, int>();

        foreach (var kv in players)
        {
            scoreMap[kv.Key] = kv.Value.score;
        }
        var payload = new Dictionary<string, object>()
        {
            { "players", scoreMap }
        };
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

    // called by NetworkTurnHandler when clients receive turn result
    public void HandleTurnResultFromNetwork(string json)
    {
        var payload = NewtonsoftJsonWrapper.Deserialize<Dictionary<string, object>>(json);
        if (payload.ContainsKey("turnIndex"))
        {
            int resolvedTurnIndex = (int)(long)payload["turnIndex"];
            if (resolvedTurnIndex < totalTurns)
            {
                currentTurn = resolvedTurnIndex + 1;
            }
            else
            {
                currentTurn = resolvedTurnIndex;
            }
        }

        var playersDict = (JObject)payload["players"];

        foreach (var kv in playersDict)
        {
            int actor = int.Parse(kv.Key);
            var result = kv.Value.ToObject<PlayerTurnResult>();

            if (!players.ContainsKey(actor))
            {
                var ps = new PlayerState(actor);
                ps.score = result.score;
                ps.energy = result.energy;
                ps.hand = new List<int>();
                ps.deck = new List<int>();
                players[actor] = ps;
            }
            else
            {
                var ps = players[actor];
                ps.score = result.score;
                ps.energy = result.energy;
                ps.hand = result.hand != null ? new List<int>(result.hand) : new List<int>();
                ps.deck = result.deck != null ? new List<int>(result.deck) : new List<int>();
            }
        }

        GameEventBus.OnRequestUIRefresh?.Invoke();
        GameUIManager.Instance?.SetRound(currentTurn, totalTurns);
        if (!PhotonNetwork.IsMasterClient)
        {
            GameEventBus.OnTurnResolved?.Invoke(currentTurn);
        }
    }

    public object GetFullState()
    {
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
        var playersObj = (JObject)payload["players"];

        Dictionary<int, int> scores = new Dictionary<int, int>();
        foreach (var kv in playersObj)
        {
            scores[int.Parse(kv.Key)] = (int)(long)kv.Value;
        }

        int localActor = Photon.Pun.PhotonNetwork.LocalPlayer.ActorNumber;
        int myScore = scores[localActor];
        int oppScore = 0;

        foreach (var kv in scores)
            if (kv.Key != localActor)
                oppScore = kv.Value;

        bool draw = (myScore == oppScore);
        bool iWin = (myScore > oppScore);
        if (draw) iWin = false;
        EndScreenUI.Instance.ShowEndScreen(iWin, myScore, oppScore);
    }

    public void RestoreFullStateFromNetwork(object stateObj)
    {
        GameEventBus.OnRequestUIRefresh?.Invoke();
    }
}

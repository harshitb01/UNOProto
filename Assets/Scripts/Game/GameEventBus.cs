using System;
public static class GameEventBus {
    public static Action<int, int[]> OnCardsPlayed; // (playerId, cardIds)
    public static Action<int> OnTurnResolved; // turn index
    public static Action OnRequestUIRefresh;
}

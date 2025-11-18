using System;
public static class GameEventBus
{
    public static Action<int, int[]> OnCardsPlayed;
    public static Action<int> OnTurnResolved;
    public static Action OnRequestUIRefresh;
}

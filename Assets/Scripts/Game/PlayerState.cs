using System.Collections.Generic;

public class PlayerState
{
    public int actorNumber;
    public List<int> deck = new List<int>();
    public List<int> hand = new List<int>();
    public int energy = 1;
    public int score = 0;
    public bool blockedThisTurn = false;

    public PlayerState(int actorNumber)
    {
        this.actorNumber = actorNumber;
    }
}

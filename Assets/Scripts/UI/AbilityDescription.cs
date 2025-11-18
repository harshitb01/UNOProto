using System.Collections.Generic;

public static class AbilityDescriptions
{
    public static readonly Dictionary<string, string> Description = new Dictionary<string, string>()
    {
        { "GainPoints",      "Gain +2 score" },
        { "StealPoints",     "Steal 1 point from opponent" },
        { "BlockNextAttack", "Block opponent's attack this turn" },
        { "DoublePower",     "Double this card's power" },
        { "DrawExtraCard",   "Draw +1 card immediately" }
    };

    public static string Get(string ability)
    {
        if (Description.TryGetValue(ability, out string desc))
            return desc;

        return ability.Replace("_", " ");
    }
}

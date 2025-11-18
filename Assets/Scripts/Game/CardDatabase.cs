using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Card/CardDatabase")]
public class CardDatabase : ScriptableObject
{
    public List<CardData> cards = new List<CardData>();
    public static CardDatabase LoadFromResources()
    {
        var ta = Resources.Load<TextAsset>("cards");
        var db = ScriptableObject.CreateInstance<CardDatabase>();
        if (ta != null)
        {
            db.cards = JsonUtilityWrapper.FromJsonList<CardData>(ta.text);
        }
        return db;
    }

    public CardData GetById(int id)
    {
        return cards.Find(c => c.id == id);
    }
}

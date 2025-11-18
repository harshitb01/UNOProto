using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Text;

public class UICard : MonoBehaviour
{
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI costText;
    public TextMeshProUGUI powerText;
    public TextMeshProUGUI abilityText;
    public int cardId;
    public bool selected = false;

    public void Init(CardData data)
    {
        cardId = data.id;
        nameText.text = data.name;
        costText.text = "Cost: " + data.cost;
        powerText.text = "Power: " + data.power;

        if (data.abilities != null && data.abilities.Count > 0)
            abilityText.text = FormatAbilitiesReadable(data.abilities.ToArray());
        else
            abilityText.text = "No Ability";
    }

    string FormatAbilitiesReadable(string[] abilities)
    {
        StringBuilder sb = new StringBuilder();
        foreach (var ab in abilities)
        {
            sb.Append(AbilityDescriptions.Get(ab)).Append("\n");
        }
        return sb.ToString().TrimEnd();
    }

    public void ToggleSelect()
    {
        selected = !selected;
        GetComponentInChildren<Image>().color = selected ? new Color(0.49f, 0.89f, 0.82f) : new Color(0.20f, 0.60f, 0.54f);
    }
}

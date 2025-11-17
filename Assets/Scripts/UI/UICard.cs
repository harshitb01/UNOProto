using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UICard : MonoBehaviour
{
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI costText;
    public TextMeshProUGUI powerText;
    public int cardId;

    public bool selected = false;

    public void Init(CardData data)
    {
        cardId = data.id;
        nameText.text = data.name;
        costText.text = "Cost: " + data.cost;
        powerText.text = "Power: " + data.power;
    }

    public void ToggleSelect()
    {
        selected = !selected;
        GetComponentInChildren<Image>().color = selected ? Color.green : Color.white;
    }
}

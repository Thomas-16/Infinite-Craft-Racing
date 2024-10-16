using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

public abstract class ElementStation : MonoBehaviour
{
    protected ChatGPTClient chatGPTClient;

    protected void Awake() {
        chatGPTClient = FindObjectOfType<ChatGPTClient>();
    }
    protected void Start() {
        this.name = GetStationText();
        GetComponentInChildren<TextMeshProUGUI>().text = GetStationText();
    }
    public abstract string GetStationText();

    public virtual async Task<string[]> GetStationResult(string input) {
        return null;
    }
}

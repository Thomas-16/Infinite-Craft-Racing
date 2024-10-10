using LLMUnity;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

public class ChangerStation : ElementStation
{
    [SerializeField] private string changerName;

    public override async Task<string[]> GetStationResult(string input) {
        string[] result = new string[1];
        result[0] = await chatGPTClient.SendChatRequest("what does " + input + " become after it goes through a " + changerName + "? (please ONLY respond with the word it becomes(no full sentence))");
        return result;
    }

    public override string GetStationText() {
        return changerName;
    }
    
}

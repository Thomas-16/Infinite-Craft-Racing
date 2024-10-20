using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class AssociatorStation : ElementStation
{
    [SerializeField] private string target;

    public override async Task<string[]> GetStationResult(string input) {
        string[] result = new string[1];
        result[0] = await chatGPTClient.SendChatRequest("give me one phrase or thing that associates " +input+ " with " +target+
            " (please ONLY respond with the one word(no full sentence))");
        return result;
    }

    public override string GetStationText() {
        return "Associator: " + target;
    }
}

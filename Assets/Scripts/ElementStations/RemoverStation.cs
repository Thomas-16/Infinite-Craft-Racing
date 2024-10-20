using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class RemoverStation : ElementStation
{
    [SerializeField] private string target;

    public override async Task<string[]> GetStationResult(string input) {
        string[] result = new string[1];
        result[0] = await chatGPTClient.SendChatRequest("what does " + input + " become without " + target + "? " +
            "(please ONLY respond with the ONE word it becomes(no full sentence))");
        return result;
    }

    public override string GetStationText() {
        return "Remover: " + target;
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class SeperatorStation : ElementStation
{
    public override async Task<string[]> GetStationResult(string input) {
        string response = await chatGPTClient.SendChatRequest("you are a \"seperator\", give me two phrases or things that you can seperate " + input + " into" +
            " (please ONLY respond with the two phrases seperated by a + (no full sentence))");

        return SplitString(response);
    }
    public string[] SplitString(string input) {
        // Split by " + " with optional trimming of spaces around the plus sign
        return input.Split(new[] { " + " }, StringSplitOptions.None);
    }

    public override string GetStationText() {
        return "Seperator";
    }
}

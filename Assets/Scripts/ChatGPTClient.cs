using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;

public class ChatGPTClient : MonoBehaviour
{
    public string apiKey = "your_openai_api_key_here";
    private string apiUrl = "https://api.openai.com/v1/chat/completions";

    [System.Serializable]
    public class Message
    {
        public string role;
        public string content;
    }

    [System.Serializable]
    public class ChatRequest
    {
        public string model = "gpt-4o-mini"; // Using GPT-4 model
        public List<Message> messages;
        public float temperature = 1f;
    }

    [System.Serializable]
    public class Choice
    {
        public Message message;
    }

    [System.Serializable]
    public class ChatResponse
    {
        public List<Choice> choices;
    }

    // Asynchronous method to send a message to the ChatGPT API and await the response
    public async Task<string> SendChatRequest(string userMessage)
    {
        // Construct the message list
        List<Message> messages = new List<Message>
        {
            new Message { role = "user", content = userMessage }
        };

        // Create a ChatRequest object
        ChatRequest chatRequest = new ChatRequest
        {
            messages = messages
        };

        string jsonData = JsonConvert.SerializeObject(chatRequest);

        // Create a UnityWebRequest
        using (UnityWebRequest request = new UnityWebRequest(apiUrl, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", $"Bearer {apiKey}");

            // Send the request and wait for it to complete
            var operation = request.SendWebRequest();

            while (!operation.isDone)
            {
                await Task.Yield(); // Wait for the request to complete
            }

            // Check for errors
            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Error: {request.error}");
                return $"Error: {request.error}";
            }
            else
            {
                // Parse the response
                ChatResponse response = JsonConvert.DeserializeObject<ChatResponse>(request.downloadHandler.text);
                string chatResponse = response.choices[0].message.content;
                return chatResponse;
            }
        }
    }
}

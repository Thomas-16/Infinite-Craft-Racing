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
        public string model = "gpt-4o-mini"; // Correct GPT-4 model name (or "gpt-3.5-turbo" if needed)
        public List<Message> messages;
        public float temperature = 1f;      // Adjust this to add more randomness
        public float top_p = 1f;            // Adjust this for more diverse output (try between 0.8 and 1)
        public float presence_penalty = 0.5f; // Encourages the model to bring up new topics (values between 0 and 1)
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
        ChatRequest chatRequest = new ChatRequest {
            messages = messages,
            temperature = 1.5f,         // Increase temperature for more creative responses
            top_p = 0.8f,               // Use top_p to introduce sampling diversity
            presence_penalty = 0.6f     // Encourage new topics or less repetitive answers
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

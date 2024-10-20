using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TMPro;
using UnityEngine.Windows;

[System.Serializable]
public class Config
{
    public string openai_api_key;
}

[System.Serializable]
public class ElementData
{
    public string word;
    public string color;

    public Color Colour
    {
        get { return HexToColor(color); }
    }

    // Convert hex color string to Unity's Color
    private Color HexToColor(string hex)
    {
        Color color;
        if (ColorUtility.TryParseHtmlString(hex, out color))
        {
            return color;
        }
        return Color.black; // Fallback color
    }
}

public class NewGameManager : MonoBehaviourPunCallbacks
{

    public static NewGameManager Instance { get; private set; }
    public ChatGPTClient ChatGPTClient;
    public Transform CanvasTransform;
    public Transform ElementParentTransform;

    [SerializeField] private GameObject cursorPrefab;
    [SerializeField] private GameObject elementPrefab;
    [SerializeField] private TextAsset allItemsTxt;

    // This dictionary will hold references to each player's cursor by their Actor Number
    private Dictionary<int, GameObject> playerCursors = new Dictionary<int, GameObject>();

    [SerializeField] private Button randomElementButton;

    private Dictionary<(string, string), (string, string)> recipes = new Dictionary<(string, string), (string, string)>();


    public static Config config;

    // Unique colors for each player
    private List<Color> availableColors = new List<Color> {
        Color.red, Color.blue, Color.green, Color.yellow, Color.cyan, Color.magenta, Color.white, Color.black
    };

    private void Awake()
    {
        // Implementing Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public override void OnEnable() {
        base.OnEnable();
        randomElementButton.onClick.AddListener(GetRandomElement);
    }

    public override void OnDisable() {
        base.OnDisable();
        randomElementButton.onClick.RemoveListener(GetRandomElement);
    }

    private void Start()
    {
        string apiKey = ExtractAPIKeyConfig();
        if (apiKey != "") {
            ChatGPTClient.apiKey = apiKey;
        }

        // Check if already in a room
        if (PhotonNetwork.InRoom)
        {
            SpawnLocalCursor();
        }
    }

    private string ExtractAPIKeyConfig() {
        TextAsset configFile = Resources.Load<TextAsset>("config");
        if (configFile != null)
        {
            config = JsonUtility.FromJson<Config>(configFile.text);
            Debug.Log("API Key Loaded: " + config.openai_api_key);
            return config.openai_api_key;
        }
        else
        {
            Debug.LogWarning("Config file not found.");
            return "";
        }
    }

    public async void GetRandomElement()
    {
        SFXManager.Instance.PlayCombineSFX();

        // Define the range for random positioning around the button
        float range = 300f; // Adjust this value as necessary
        float minDistance = 100f; // Minimum distance from the button

        Vector2 localPos;

        do
        {
            // Generate a random position offset from the button's position
            float offsetX = Random.Range(-range, range);
            float offsetY = Random.Range(-range, range);

            // Calculate the random position relative to the button's position
            localPos = (Vector2)randomElementButton.transform.position + new Vector2(offsetX, offsetY);

            // Loop until the position is at least `minDistance` away from the button
        } while (Vector2.Distance(localPos, randomElementButton.transform.position) < minDistance);

        // Spawn the element at the calculated position
        LLement newElement = SpawnLLement("...", localPos);


        //ElementData element = JsonUtility.FromJson<ElementData>(response);
        string randomElement = await GetRandomLineAsync();
        ElementData element = new ElementData() {
            word = randomElement,
            color = await ChatGPTClient.SendChatRequest("Give me ONLY the a HEX code for the colour that represents " + randomElement + " with the # at the start")
        };
        newElement.SetElementData(element);
        newElement.SetPreoccupied(false);

    }

    public override void OnJoinedRoom()
    {
        SpawnLocalCursor();
        Debug.Log("Joined room with " + PhotonNetwork.CurrentRoom.PlayerCount + " players.");
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.LogFormat("OnPlayerEnteredRoom() {0}", newPlayer.NickName);
        Debug.Log(PhotonNetwork.CurrentRoom.PlayerCount + " players are in the room");
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        // Remove the cursor for the player that left
        if (playerCursors.TryGetValue(otherPlayer.ActorNumber, out GameObject cursor))
        {
            PhotonNetwork.Destroy(cursor);
            playerCursors.Remove(otherPlayer.ActorNumber);
        }

        Debug.Log("MasterClient is transferring ownership of disconnected player's objects.");

        foreach (PhotonView photonView in FindObjectsOfType<PhotonView>()) {
            if (photonView.GetComponent<NewPlayerCursor>() != null) { continue; }
            // Check if the object belongs to the leaving player
            if (photonView.Owner == otherPlayer) {
                Debug.LogFormat("Transferring ownership of {0} to the MasterClient", photonView.name);

                // Transfer ownership to the MasterClient
                photonView.TransferOwnership(PhotonNetwork.MasterClient);
            }
        }
    }

    private void SpawnLocalCursor()
    {
        // Instantiate the cursor locally on this player
        GameObject cursorGO = PhotonNetwork.Instantiate(cursorPrefab.name, Vector3.zero, Quaternion.identity);
        
        // Parent the cursor to the canvas transform
        if (CanvasTransform != null)
        {
            cursorGO.transform.SetParent(CanvasTransform, false);
        }
        else
        {
            Debug.LogWarning("Canvas Transform is not assigned in GameManager!");
        }

        // Set up the cursor with a unique color
        NewPlayerCursor playerCursor = cursorGO.GetComponent<NewPlayerCursor>();
        Color playerColor = GetUniqueColor();
        playerCursor.SetColor(playerColor);

        // Add the cursor to the dictionary to keep track of it by actor number
        playerCursors[PhotonNetwork.LocalPlayer.ActorNumber] = cursorGO;
    }

    private Color GetUniqueColor()
    {
        int playerIndex = PhotonNetwork.LocalPlayer.ActorNumber % availableColors.Count;
        Color playerColor = new Color(availableColors[playerIndex].r, availableColors[playerIndex].g, availableColors[playerIndex].b);
        return playerColor;
    }

    public async void CombineElements(LLement element1, LLement element2) {
        while (!element1.photonView.IsMine) {
            element1.photonView.RequestOwnership();
            await Task.Yield(); 
        }
        while (!element2.photonView.IsMine) {
            element2.photonView.RequestOwnership();
            await Task.Yield(); 
        }
        string elementName1 = element1.ElementName;
        string elementName2 = element2.ElementName;
        Debug.Log($"Combining elements: {elementName1} and {elementName2}!");
        SFXManager.Instance.PlayCombineSFX();

        LLement element = SpawnLLement($"{elementName1} + {elementName2}", FindMidpoint(element1.gameObject, element2.gameObject));
        PhotonNetwork.Destroy(element1.gameObject);
        PhotonNetwork.Destroy(element2.gameObject);//element.SetPreoccupied(true);

        string response, colourResponse;
        if(TryGetRecipeResult(elementName1, elementName2, out response, out colourResponse)) {
            Debug.Log($"saved recipe found for {elementName1} + {elementName2}");
        } else {
            response = await ChatGPTClient.SendChatRequest("What object or concept comes to mind when I combine " + elementName1 + " with " + elementName2 + "? Say ONLY one simple word that represents an object or concept. Keep it simple, creative and engaging for a game where players combine elements. Do not make up words.");
            response = response.Replace(".", "");
            colourResponse = await ChatGPTClient.SendChatRequest("Give me ONLY the a HEX code for the colour that represents " + response + " with the # at the start");

            photonView.RPC(nameof(AddRecipeRPC), RpcTarget.All, elementName1, elementName2, response, colourResponse);
        }


        element.SetPreoccupied(false);
        ElementData elementData = new ElementData() {
            word = response,
            color = colourResponse
        };
        element.SetElementData(elementData);
        SFXManager.Instance.PlayCombineSFX();
    }


    public LLement SpawnLLement(string elementName, Vector3 pos) {
        GameObject elementGO = PhotonNetwork.Instantiate(elementPrefab.name, pos, Quaternion.identity);
        LLement element = elementGO.GetComponent<LLement>();
        element.ElementName = elementName;
        element.elementData = new ElementData { word = elementName, color = "#FFFFFF" };
        return element;
    }

    #region Utils
    // Add a new recipe
    [PunRPC]
    public void AddRecipeRPC(string element1, string element2, string result, string colourResult) {
        // Use the ordered tuple as the key
        var key = GetOrderedKey(element1, element2);
        recipes[key] = (result, colourResult);

        Debug.Log($"Saved recipe: {element1} + {element2} = {result} with colour: {colourResult}");
    }
    // Retrieve a recipe result
    private bool TryGetRecipeResult(string element1, string element2, out string result, out string colourResult) {
        // Use the ordered tuple as the key for lookup
        (string, string) key = GetOrderedKey(element1, element2);
        if (recipes.TryGetValue(key, out var tupleResult)) {
            result = tupleResult.Item1;
            colourResult = tupleResult.Item2;
            return true;
        }
        result = string.Empty;
        colourResult = string.Empty;
        return false; // No recipe found
    }
    // Helper function to ensure elements are always ordered the same way
    private (string, string) GetOrderedKey(string element1, string element2) {
        // Ensure the two elements are stored in lexicographical order
        return string.Compare(element1, element2, System.StringComparison.Ordinal) < 0 ?
            (element1, element2) : (element2, element1);
    }
    
    public Vector3 FindMidpoint(GameObject obj1, GameObject obj2)
    {
        Vector3 position1 = obj1.transform.position;
        Vector3 position2 = obj2.transform.position;

        // Calculate the midpoint
        Vector3 midpoint = (position1 + position2) / 2;
        return midpoint;
    }
    private string JsonResponseCleanup(string response) {
        int startIndex = response.IndexOf('{');
        int endIndex = response.IndexOf('}');

        // Check if both braces exist
        if (startIndex != -1 && endIndex != -1 && endIndex > startIndex) {
            // Return the substring including the curly braces
            return response.Substring(startIndex, endIndex - startIndex + 1);
        }

        // Return an empty string if curly braces are not found
        return string.Empty;
    }
    // Async method to return a random line from the TextAsset
    public async Task<string> GetRandomLineAsync() {
        // Simulate an asynchronous operation (e.g., waiting for I/O, computation)
        await Task.Yield(); // Yields control back to the caller and waits for the next frame

        // Split the text into an array of lines
        string[] lines = allItemsTxt.text.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);

        // Check if there are any lines in the file
        if (lines.Length == 0) {
            Debug.LogWarning("Text file is empty or has no valid lines.");
            return string.Empty;
        }

        // Get a random index and return the corresponding line
        int randomIndex = Random.Range(0, lines.Length);
        return lines[randomIndex];
    }

    public string GetFirstWord(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        // Split the string by spaces and take the first part
        string firstWord = input.Split(' ')[0];

        // Trim any trailing punctuation from the first word
        firstWord = firstWord.TrimEnd('.', ',', '!', '?', ':', ';', '-', '\"', '\'');

        return firstWord;
    }
    #endregion
}


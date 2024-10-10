using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using LLMUnity;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

public class NewGameManager : MonoBehaviourPunCallbacks
{
    public enum AIModel
    {
        ChatGPT,
        LLMUnity
    }

    public AIModel aiModel = AIModel.ChatGPT;
    public static NewGameManager Instance { get; private set; }
    [SerializeField] private LLMCharacter llmCharacter;
    [SerializeField] private ChatGPTClient chatGPTClient;
    public Transform CanvasTransform;
    public Transform ElementParentTransform;

    [SerializeField] private GameObject cursorPrefab;
    [SerializeField] private GameObject elementPrefab;

    // This dictionary will hold references to each player's cursor by their Actor Number
    private Dictionary<int, GameObject> playerCursors = new Dictionary<int, GameObject>();

    [SerializeField] private Button randomElementButton;

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
        // Check if already in a room
        if (PhotonNetwork.InRoom)
        {
            SpawnLocalCursor();
        }
    }

    public async void GetRandomElement() {
        // Define the range for random positioning around the center
        float range = 200f; // Adjust this value as necessary

        // Generate a random position offset from the center
        float offsetX = Random.Range(-range, range);
        float offsetY = Random.Range(-range, range);

        // Calculate the random position in canvas local space
        Vector2 localPos = randomElementButton.transform.position + new Vector3(offsetX, offsetY, 0);
        LLement newElement = SpawnLLement("...", localPos);
        string response = "";
        if (aiModel == AIModel.ChatGPT) {
            response = await chatGPTClient.SendChatRequest("Say ONLY one simple word, a random object or element or concept.");
        }
        else if (aiModel == AIModel.LLMUnity) {
            response = await llmCharacter.Chat("Say ONLY one simple word, a random object or element or concept.");
        }

        // Split the string by spaces and take the first part
        newElement.SetName(GetFirstWord(response));
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
            Destroy(cursor);
            playerCursors.Remove(otherPlayer.ActorNumber);
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
        LLement element = SpawnLLement($"{elementName1} + {elementName2}", FindMidpoint(element1.gameObject, element2.gameObject));
        PhotonNetwork.Destroy(element1.gameObject);
        PhotonNetwork.Destroy(element2.gameObject);
        //element.SetPreoccupied(true);

        string response = "";
        if (aiModel == AIModel.ChatGPT) {
            response = await chatGPTClient.SendChatRequest($"What do you get when you combine {elementName1} with {elementName2}? Respond with a single word only. It must be an existing word.");
        }
        else if (aiModel == AIModel.LLMUnity) {
            response = await llmCharacter.Chat($"What do you get when you combine {elementName1} with {elementName2}? Respond with a single word only. It must be an existing word.");
        }


        // Split the string by spaces and take the first part
        element.SetName(GetFirstWord(response));
        element.SetPreoccupied(false);
        //sfxManager.PlayCombineSFX();
    }


    public LLement SpawnLLement(string elementName, Vector3 pos) {
        GameObject elementGO = PhotonNetwork.Instantiate(elementPrefab.name, pos, Quaternion.identity);
        LLement element = elementGO.GetComponent<LLement>();
        element.ElementName = elementName;
        return element;
    }

    #region Utils
    public Vector3 FindMidpoint(GameObject obj1, GameObject obj2)
    {
        Vector3 position1 = obj1.transform.position;
        Vector3 position2 = obj2.transform.position;

        // Calculate the midpoint
        Vector3 midpoint = (position1 + position2) / 2;
        return midpoint;
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

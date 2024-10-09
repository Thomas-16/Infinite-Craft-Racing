using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class NewGameManager : MonoBehaviourPunCallbacks
{
    public static NewGameManager Instance { get; private set; }

    public Transform CanvasTransform;
    [SerializeField] private GameObject cursorPrefab;

    // This dictionary will hold references to each player's cursor by their Actor Number
    private Dictionary<int, GameObject> playerCursors = new Dictionary<int, GameObject>();

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

    private void Start()
    {
        // Check if already in a room
        if (PhotonNetwork.InRoom)
        {
            SpawnLocalCursor();
        }
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
}

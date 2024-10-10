using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameConnectInfo : MonoBehaviour
{
    // Singleton instance
    public static GameConnectInfo Instance { get; private set; }

    public bool isJoiningAsPlayer;

    public string apiKey = "";

    public TMP_InputField apiInputField;
    public Toggle isPlayerToggle;


    private void Awake()
    {
        // Check if an instance already exists
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // Destroy this instance if another already exists
            return;
        }

        // Assign this instance to the static Instance field
        Instance = this;

        // Make this instance persistent across scenes
        DontDestroyOnLoad(gameObject);
    }

    public void OnEnable() {
        apiInputField.onValueChanged.AddListener(SetAPIKey);
        isPlayerToggle.onValueChanged.AddListener(SetIsPlayer);
    }

    public void OnDisable() {
        apiInputField.onValueChanged.RemoveListener(SetAPIKey);
        isPlayerToggle.onValueChanged.RemoveListener(SetIsPlayer);
    }

    private void SetAPIKey(string newKey) {
        apiKey = newKey;
    }

    private void SetIsPlayer(bool val) {
        isJoiningAsPlayer = val;
    }
}

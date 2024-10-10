using UnityEngine;
using UnityEngine.UI;

using Photon.Pun;
using Photon.Realtime;

using System.Collections;
using TMPro;

/// <summary>
/// Player name input field. Let the user input his name, will appear above the player in the game.
/// </summary>
[RequireComponent(typeof(TMP_InputField))]
public class PlayerNameInputField : MonoBehaviour
{
    // Store the PlayerPref Key to avoid typos
    const string playerNamePrefKey = "PlayerName";
    string[] defaultNames = new string[]
    {
        "Shadow",
        "Lunar",
        "Nova",
        "Crimson",
        "Echo",
        "Blaze",
        "Thunder",
        "Aqua",
        "Star",
        "Iron",
        "Mystic",
        "Storm",
        "Crystal",
        "Solar",
        "Vortex",
        "Dark",
        "Phoenix",
        "Silver",
        "Frost",
        "Night",
        "Celestial",
        "Nebula",
        "Electro",
        "Sky",
        "Galactic",
        "Tempest",
        "Zephyr",
        "Moon",
        "Flame",
        "Wraith",
        "Raven",
        "Steel",
        "Sun",
        "Hawk",
        "Astra",
        "Comet",
        "Blizzard",
        "Sonic",
        "Luna",
        "Bringer",
        "Ice",
        "Serpent",
        "Walker",
        "Ashen",
        "Knight",
        "Eclipse",
        "Tide"
    };


    /// <summary>
    /// MonoBehaviour method called on GameObject by Unity during initialization phase.
    /// </summary>
    void Start() {

        string defaultName = defaultNames[Random.Range(0, defaultNames.Length - 1)];
        TMP_InputField _inputField = this.GetComponent<TMP_InputField>();
        if (_inputField != null) {
            _inputField.text = defaultName;
            if (PlayerPrefs.HasKey(playerNamePrefKey)) {
                defaultName = PlayerPrefs.GetString(playerNamePrefKey);
                _inputField.text = defaultName;
            }
        }

        PhotonNetwork.LocalPlayer.NickName = defaultName;
    }

    /// <summary>
    /// Sets the name of the player, and save it in the PlayerPrefs for future sessions.
    /// </summary>
    /// <param name="value">The name of the Player</param>
    public void SetPlayerName(string value) {
        // #Important
        if (string.IsNullOrEmpty(value)) {
            Debug.LogError("Player Name is null or empty");
            return;
        }
        PhotonNetwork.LocalPlayer.NickName = value;

        PlayerPrefs.SetString(playerNamePrefKey, value);
    }
}
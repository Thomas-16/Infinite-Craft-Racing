using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Changer : MonoBehaviour
{
    public string ChangerName;

    private void Start() {
        this.name = ChangerName;
        GetComponentInChildren<TextMeshProUGUI>().text = ChangerName;
    }
}

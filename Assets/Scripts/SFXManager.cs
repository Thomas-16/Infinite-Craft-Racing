using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SFXManager : MonoBehaviour
{
    public static SFXManager Instance {  get; private set; }

    [SerializeField] private AudioClip selectSFX;
    [SerializeField] private AudioClip combineSFX;
    private AudioSource audioSource;

    private void Awake() {
        audioSource = GetComponent<AudioSource>();
        Instance = this;
    }

    public void PlaySelectSFX() {
        audioSource.PlayOneShot(selectSFX, .8f);
    }
    public void PlayCombineSFX() {
        audioSource.PlayOneShot(combineSFX, 1f);
    }
}

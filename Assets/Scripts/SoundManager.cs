
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("Audio Source para Efeitos (SFX)")]
    [SerializeField] private AudioSource sfxAudioSource;

    [Header("Banco de Sons (SFX)")]
    [SerializeField]
    private List<SoundData> soundList =
        new List<SoundData>();

    [System.Serializable]
    public struct SoundData
    {
        public string soundName;
        public AudioClip clip;

        [Range(0f, 1f)]
        public float volume;
    }

    private Dictionary<string, SoundData> soundDictionary =
        new Dictionary<string, SoundData>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (sfxAudioSource == null)
        {
            sfxAudioSource = GetComponent<AudioSource>();

            if (sfxAudioSource == null)
                sfxAudioSource = gameObject.AddComponent<AudioSource>();
        }

        soundDictionary.Clear();

        foreach (SoundData sound in soundList)
        {
            if (!soundDictionary.ContainsKey(sound.soundName))
            {
                soundDictionary.Add(sound.soundName, sound);
            }
        }
    }

    public void PlaySound(string soundName)
    {
        if (soundDictionary.TryGetValue(soundName, out SoundData sound))
        {
            if (sound.clip != null && sfxAudioSource != null)
            {
                sfxAudioSource.PlayOneShot(sound.clip, sound.volume);
            }
        }
        else
        {
            Debug.LogWarning(
                "SoundManager: Som com o nome '" +
                soundName +
                "' não foi encontrado!");
        }
    }
}

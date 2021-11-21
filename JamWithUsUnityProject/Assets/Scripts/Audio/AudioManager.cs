using UnityEngine;

public class AudioManager : MonoBehaviour
{
    [SerializeField]
    private UnityEngine.Audio.AudioMixer audioMixer;

    private void Start()
    {
        this.MainVolumeChanged(Settings.MainVolume);
        this.MusicVolumeChanged(Settings.MusicVolume);

        Settings.MainVolumeChanged += this.MainVolumeChanged;
        Settings.MusicVolumeChanged += this.MusicVolumeChanged;
    }

    private void MainVolumeChanged(float value)
    {
        this.audioMixer.SetFloat("MasterVolume", Mathf.Log10(Mathf.Max(value, .001f)) * 20);
    }

    private void MusicVolumeChanged(float value)
    {
        this.audioMixer.SetFloat("MusicVolume", Mathf.Log10(Mathf.Max(value, .001f)) * 20);
    }
}

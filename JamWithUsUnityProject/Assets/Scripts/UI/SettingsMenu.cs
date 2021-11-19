using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SettingsMenu : MonoBehaviour
{
	public Slider MusicSlider, SFXSlider;

	public TMPro.TMP_InputField WidthField, HeightField;
	public Toggle FullScreenToggle;

	public Animator Curtain;
	private string pendingSceneName;

	private void OnEnable()
	{
		this.WidthField.text = Screen.width.ToString();
		this.HeightField.text = Screen.height.ToString();
		this.FullScreenToggle.SetIsOnWithoutNotify(Screen.fullScreen);

		this.SFXSlider.SetValueWithoutNotify(Settings.SFXVolume);
		this.MusicSlider.SetValueWithoutNotify(Settings.MusicVolume);

		Time.timeScale = 0f;
	}

	private void OnDisable()
	{
		Time.timeScale = 1f;
	}

	public void SetMusicVolume(float value)
	{
		Settings.MusicVolume = value;
	}

	public void SetSFXVolume(float value)
	{
		Settings.SFXVolume = value;
	}

	public void ToggleFullScreen(bool newValue)
	{
		Screen.fullScreen = newValue;
	}

	public void ApplyResolution()
	{
		Screen.SetResolution(int.Parse(this.WidthField.text), int.Parse(this.HeightField.text), Screen.fullScreen);
	}

	public void LoadScene(string sceneName)
	{
		this.Curtain.Play("FadeIn");

		this.pendingSceneName = sceneName;
		this.Invoke(nameof(LoadPendingScene), 0.5f);
		Time.timeScale = 1f;
	}

	private void LoadPendingScene()
	{
		UnityEngine.SceneManagement.SceneManager.LoadScene(this.pendingSceneName);
	}
}
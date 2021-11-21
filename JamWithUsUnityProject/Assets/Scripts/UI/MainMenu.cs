using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
	public static GameObject StartupMusic;
	public GameObject Music;

	public Animator Curtain;

	public Button LevelSelectionButton;
	public LevelButton[] LevelButtons;

	private string pendingSceneName;

	private void Start()
	{
		if (MainMenu.StartupMusic)
		{
			GameObject.Destroy(this.Music);
		}

		for (int i = 0; i < this.LevelButtons.Length; i++)
		{
			int highScore = PlayerPrefs.GetInt($"JamWithUs_HighscoreLevel{i}", -1);

			if (i == 0)
			{
				this.LevelSelectionButton.interactable = highScore >= 0;
			}

			for (int j = 0; j < this.LevelButtons[i].Stars.Length; j++)
			{
				this.LevelButtons[i].Stars[j].SetActive(highScore > j);
			}

			if (i < this.LevelButtons.Length - 1)
			{
				this.LevelButtons[i + 1].Button.interactable = highScore >= 0;
			}
		}
	}

	public void OnClickPlay(string sceneName)
	{
		this.Curtain.Play("FadeIn");

		this.pendingSceneName = sceneName;
		this.Invoke(nameof(LoadGameScene), 0.5f);
	}

	public void OnClickQuit()
	{
		this.Curtain.Play("FadeIn");

		this.Invoke(nameof(Quit), 0.5f);
	}

	public void OnClickURL(string url)
	{
		Application.OpenURL(url);
	}

	private void Quit()
	{
#if UNITY_EDITOR
		UnityEditor.EditorApplication.ExitPlaymode();
#else
		Application.Quit();
#endif
	}

	private void LoadGameScene()
	{
		GameObject.Destroy(MainMenu.StartupMusic);
		MainMenu.StartupMusic = null;

		GameCameraController.PlayIntroduction = true;
		SceneManager.LoadScene(this.pendingSceneName);
	}
}
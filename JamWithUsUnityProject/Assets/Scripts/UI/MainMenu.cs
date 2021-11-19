using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
	public Animator Curtain;

	public void OnClickPlay()
	{
		this.Curtain.Play("FadeIn");

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
		SceneManager.LoadScene(1);
	}
}
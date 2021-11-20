using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StartupScreen : MonoBehaviour
{
	public Animator Curtain;

	private void Update()
	{
		if (Input.anyKeyDown)
		{
			this.Curtain.Play("FadeIn");
			this.Invoke(nameof(LoadMenu), 0.5f);
		}
	}

	private void LoadMenu()
	{
		SceneManager.LoadScene("MainMenu");
	}
}
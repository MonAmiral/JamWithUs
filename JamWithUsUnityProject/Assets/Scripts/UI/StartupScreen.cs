using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StartupScreen : MonoBehaviour
{
	public Animator Curtain;

	public GameObject Music;

	private void Start()
	{
		GameObject.DontDestroyOnLoad(this.Music);
		MainMenu.StartupMusic = this.Music;
	}

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
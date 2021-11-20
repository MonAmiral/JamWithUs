using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameUI : MonoBehaviour
{
	[Header("Game")]
	public GameObject GameInterface;
	public Image CorruptionBar;
	public TextMeshProUGUI CorruptionLabel;
	public Animator CorruptionAnimator;

	[Header("Pause")]
	public SettingsMenu SettingsMenu;
	public GameObject ResumeButton;

	[Header("Game over")]
	public GameObject GameOverScreen;
	public GameObject RestartButton;
	
	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.JoystickButton7))
		{
			this.SettingsMenu.gameObject.SetActive(!this.SettingsMenu.gameObject.activeSelf);
			UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(this.ResumeButton);
		}
	}
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameUI : MonoBehaviour
{
	public SettingsMenu SettingsMenu;

	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.JoystickButton7))
		{
			this.SettingsMenu.gameObject.SetActive(!this.SettingsMenu.gameObject.activeSelf);
		}
	}
}
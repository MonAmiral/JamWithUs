using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Settings
{
	private static bool initialized;

	public static System.Action<float> MainVolumeChanged;
	public static System.Action<float> MusicVolumeChanged;

	private static float mainVolume = 1f;
	public static float MainVolume
	{
		get
		{
			if (!initialized)
			{
				Initialize();
			}

			return mainVolume;
		}

		set
		{
			mainVolume = value;
			PlayerPrefs.SetFloat("MainVolume", value);
			PlayerPrefs.Save();

			MainVolumeChanged?.Invoke(value);
		}
	}

	private static float musicVolume = 1f;
	public static float MusicVolume
	{
		get
		{
			if (!initialized)
			{
				Initialize();
			}

			return musicVolume;
		}

		set
		{
			musicVolume = value;
			PlayerPrefs.SetFloat("MusicVolume", value);
			PlayerPrefs.Save();

			MusicVolumeChanged?.Invoke(value);
		}
	}

	private static void Initialize()
	{
		initialized = true;

		mainVolume = PlayerPrefs.GetFloat("MainVolume", 1f);
		musicVolume = PlayerPrefs.GetFloat("MusicVolume", 1f);
	}
}
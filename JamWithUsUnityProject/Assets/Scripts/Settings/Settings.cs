using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Settings
{
	private static bool initialized;

	public static System.Action<float> SFXVolumeChanged;
	public static System.Action<float> MusicVolumeChanged;

	private static float sfxVolume = 1f;
	public static float SFXVolume
	{
		get
		{
			if (!initialized)
			{
				Initialize();
			}

			return sfxVolume;
		}

		set
		{
			sfxVolume = value;
			PlayerPrefs.SetFloat("HoloWEEN_SFXVolume", value);
			PlayerPrefs.Save();

			SFXVolumeChanged?.Invoke(value);
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
			PlayerPrefs.SetFloat("HoloWEEN_MusicVolume", value);
			PlayerPrefs.Save();

			MusicVolumeChanged?.Invoke(value);
		}
	}

	private static void Initialize()
	{
		initialized = true;

		sfxVolume = PlayerPrefs.GetFloat("HoloWEEN_SFXVolume", 1f);
		musicVolume = PlayerPrefs.GetFloat("HoloWEEN_MusicVolume", 1f);
	}
}
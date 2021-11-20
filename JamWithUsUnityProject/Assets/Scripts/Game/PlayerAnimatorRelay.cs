using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAnimatorRelay : MonoBehaviour
{
	public PlayerController PlayerController;

	public void Step()
	{
		this.PlayerController.Step();
	}
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestructiblePlatform : MonoBehaviour
{
	public float DestructionDelay;

	public void Destroy()
	{
		this.GetComponent<Animator>().Play("Destroy");

		GameObject.Destroy(this);
		GameObject.Destroy(this.gameObject, this.DestructionDelay + 1f);
	}
}
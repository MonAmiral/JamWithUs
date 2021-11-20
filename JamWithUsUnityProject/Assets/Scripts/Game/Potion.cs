using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Potion : MonoBehaviour
{
	public int Corruption;

	[Space]
	public bool InvertControls;
	public bool DrunkEffect;
	public bool Adrenaline;
	public bool ReduceVision;
	public bool Shadow;
	public bool Antidote;

	public void Collect()
	{
		this.GetComponentInChildren<Animator>().Play("Collect");
		GameObject.Destroy(this.gameObject, 1f);
		this.GetComponent<Collider2D>().enabled = false;
	}
}
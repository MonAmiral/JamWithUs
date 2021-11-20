using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Potion : MonoBehaviour
{
	public int Corruption;

	[Space]
	public bool InvertControls;
	public float DrunkPercentage;
	public bool CantStopMoving;
	public bool ReduceVision;
	public bool SpawnFollowingDanger;
	public float FartScale;
	public bool Antidote;

	public void Collect()
	{
		this.GetComponentInChildren<Animator>().Play("Collect");
		GameObject.Destroy(this.gameObject, 1f);
		this.GetComponent<Collider2D>().enabled = false;
	}
}
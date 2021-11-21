using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameCameraController : MonoBehaviour
{
	private static GameCameraController Instance;

	public Rect MovementArea;
	public Vector3 Offset;

	[Space]
	public Transform Target;
	public Transform IntroductionTarget;
	public static bool PlayIntroduction = true;

	[Space]
	public float MovementSpeed;
	private Vector3 velocity;

	private void Start()
	{
		GameCameraController.Instance = this;
	}

	public void LateUpdate()
	{
		if (GameCameraController.PlayIntroduction)
		{
			this.transform.position = Vector3.SmoothDamp(this.transform.position, this.IntroductionTarget.position + this.Offset, ref this.velocity, this.MovementSpeed);
		}
		else
		{
			Vector3 goalPosition = new Vector3(Mathf.Clamp(this.Target.position.x, this.MovementArea.xMin, this.MovementArea.xMax), Mathf.Clamp(this.Target.position.y, this.MovementArea.yMin, this.MovementArea.yMax), this.Target.position.z);
			goalPosition += this.Offset;

			this.transform.position = Vector3.SmoothDamp(this.transform.position, goalPosition, ref this.velocity, this.MovementSpeed);
		}
	}

	public void ReEnableIntroduction()
	{
		if (GameCameraController.Instance)
		{
			GameCameraController.Instance.enabled = false;
		}

		GameCameraController.PlayIntroduction = true;
	}

#if UNITY_EDITOR
	private void OnDrawGizmosSelected()
	{
		UnityEditor.Handles.DrawWireCube(this.MovementArea.center, this.MovementArea.size);
	}
#endif
}

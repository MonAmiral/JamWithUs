using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameCameraController : MonoBehaviour
{
	public Rect MovementArea;
	public Transform Target;
	public Vector3 Offset;

	public float MovementSpeed;
	private Vector3 velocity;

	public void LateUpdate()
	{
		Vector3 goalPosition = new Vector3(Mathf.Clamp(this.Target.position.x, this.MovementArea.xMin, this.MovementArea.xMax), Mathf.Clamp(this.Target.position.y, this.MovementArea.yMin, this.MovementArea.yMax), this.Target.position.z);
		goalPosition += this.Offset;

		this.transform.position = Vector3.SmoothDamp(this.transform.position, goalPosition, ref this.velocity, this.MovementSpeed);
	}

#if UNITY_EDITOR
	private void OnDrawGizmosSelected()
	{
		UnityEditor.Handles.DrawWireCube(this.MovementArea.center, this.MovementArea.size);
	}
#endif
}

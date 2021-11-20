using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
	[Header("Mouvement")]
	[Tooltip("La vitesse de déplacement maximale du personnage, exprimée en m/s.")]
	public float MovementSpeed;
	[Tooltip("L'augmentation de la vitesse du personnage quand le joueur donne un input gauche/droite, exprimée en m/s/s.")]
	public float Acceleration;
	[Tooltip("La réduction de la vitesse du personnage quand le joueur ne donne pas d'input gauche/droite, exprimée en m/s/s.")]
	public float Deceleration;
	[Tooltip("La vitesse de rotation du modèle. N'affecte pas le gameplay.")]
	public float RotationSpeed;

	[Header("Saut/chute")]
	[Tooltip("La vitesse verticale lors d'un saut.\n-X = temps en secondes, Y = vitesse verticale.\nSi le temps passé en l'air dépasse la position du dernier point de la courbe, la valeur de la courbe à ce temps est tout de même utilisée.")]
	public AnimationCurve JumpVerticalSpeed;
	[Tooltip("Le multiplicateur sur la vitesse horizontale lors d'un saut.\n-X = temps en secondes, Y = multiplicateur de la vitesse de déplacement.\nSi le temps passé en l'air dépasse la position du dernier point de la courbe, la valeur de la courbe à ce temps est tout de même utilisée.")]
	public AnimationCurve JumpHorizontalSpeedMultiplier;
	[Tooltip("Le multiplicateur d'accélération/décélération lors d'un saut.\n-X = temps en secondes, Y = multiplicateur d'accélération/décélération.\nSi le temps passé en l'air dépasse la position du dernier point de la courbe, la valeur de la courbe à ce temps est tout de même utilisée.")]
	public AnimationCurve JumpAirControl;
	[Tooltip("Le temps en secondes pendant lequel le joueur peut sauter alors qu'il vient de commencer à chuter d'une plateforme.")]
	public float CoyoteTime;
	[Tooltip("La vitesse verticale lors d'une chute.\n-X = temps en secondes, Y = vitesse verticale.\nSi le temps passé en l'air dépasse la position du dernier point de la courbe, la valeur de la courbe à ce temps est tout de même utilisée.")]
	public AnimationCurve Gravity;
	[Tooltip("Les layers de colliders qui seront considérés comme du sol.")]
	public LayerMask FloorLayer;

	private new Rigidbody2D rigidbody;

	private float horizontalInput;
	private float horizontalSpeedRatio;

	private float airTime;
	private bool isJumping;
	private float dashTime;
	private bool hasLandedAfterDash;
	private float verticalSpeed;
	private RaycastHit2D[] movementHits = new RaycastHit2D[1];

	private int facing = 2;

	[Header("Dash")]
	[Tooltip("La vitesse horizontale lors d'un dash.\n-X = temps en secondes, Y = multiplicateur de la vitesse de déplacement.\nLe dash se termine lorsque le temps dépasse la position du dernier point de la courbe.")]
	public AnimationCurve DashHorizontalSpeed;
	private float dashDuration;
	[Tooltip("Le temps d'attente en secondes entre le contact avec le sol après un dash et le moment où le joueur peut de nouveau dash.")]
	public float DashCooldown;

	[Header("References")]
	public Transform Model;
	public Animator Animator;
	public AudioSource AudioSource;

	[Header("Audio")]
	public AudioClip[] Footsteps;
	public AudioClip[] JumpStart;
	public AudioClip[] Dash;

	private void Start()
	{
		this.rigidbody = this.GetComponent<Rigidbody2D>();
		this.dashDuration = this.DashHorizontalSpeed.keys[this.DashHorizontalSpeed.keys.Length - 1].time;
	}

	private void Update()
	{
		this.horizontalInput = Input.GetAxis("Horizontal");

		this.Model.localRotation = Quaternion.Lerp(this.Model.localRotation, Quaternion.Euler(Vector3.up * 90 * this.facing), Time.deltaTime * this.RotationSpeed);

		if (this.dashTime <= 0f)
		{
			// Handle Jump when in coyote time.
			if (this.hasLandedAfterDash && !this.isJumping && this.airTime < this.CoyoteTime)
			{
				if (Input.GetButtonDown("Jump"))
				{
					this.StartJump();
				}
			}

			// Handle Dash when ready.
			if (this.hasLandedAfterDash)
			{
				if (this.dashTime == 0f)
				{
					if (Input.GetButtonDown("Dash"))
					{
						this.StartDash();
					}
				}
				else
				{
					this.dashTime = Mathf.Min(this.dashTime + Time.deltaTime, 0f);
				}
			}
		}

		this.Animator.SetFloat("SpeedRatio", Mathf.Abs(this.horizontalSpeedRatio));
		this.Animator.SetBool("IsFalling", this.verticalSpeed < 0f);
		this.Animator.SetBool("IsOnGround", this.airTime == 0f && this.hasLandedAfterDash);
	}

	private void FixedUpdate()
	{
		this.rigidbody.velocity *= 0.8f;

		Vector2 position = this.transform.position;
		float deltaTime = Time.fixedDeltaTime;

		if (this.dashTime > 0f)
		{
			this.UpdateDashVelocityAndMove(position, deltaTime);
		}
		else
		{
			this.UpdateNormalVelocityAndMove(position, deltaTime);
		}
	}

	private void UpdateDashVelocityAndMove(Vector2 position, float deltaTime)
	{
		this.horizontalSpeedRatio = this.facing * this.DashHorizontalSpeed.Evaluate(this.dashTime);
		this.dashTime += deltaTime;
		this.rigidbody.MovePosition(position + Vector2.right * this.horizontalSpeedRatio * this.MovementSpeed * deltaTime);

		if (this.dashTime > this.dashDuration)
		{
			this.EndDash();
		}
	}

	private void UpdateNormalVelocityAndMove(Vector2 position, float deltaTime)
	{
		float acceleration = Mathf.Abs(this.horizontalInput) > 0.05f ? this.Acceleration : this.Deceleration;

		if (this.isJumping)
		{
			acceleration *= this.JumpAirControl.Evaluate(this.airTime);
			this.verticalSpeed = this.JumpVerticalSpeed.Evaluate(this.airTime);
		}
		else
		{
			this.verticalSpeed = this.Gravity.Evaluate(this.airTime);
		}

		this.horizontalSpeedRatio = Mathf.MoveTowards(this.horizontalSpeedRatio, this.horizontalInput, acceleration * deltaTime);

		// Compute movement vector.
		Vector2 movement = new Vector2(this.horizontalSpeedRatio * this.MovementSpeed, this.verticalSpeed) * deltaTime;
		if (this.isJumping)
		{
			movement.x *= this.JumpHorizontalSpeedMultiplier.Evaluate(this.airTime);
		}

		if (this.horizontalSpeedRatio != 0)
		{
			this.facing = (int)Mathf.Sign(this.horizontalSpeedRatio);
		}

		// Check for floor.
		float floorCheckDistance = this.airTime < this.CoyoteTime ? 0.1f : Mathf.Max(0f, -this.verticalSpeed * deltaTime);
		Vector2 goalPosition;
		RaycastHit2D hit = Physics2D.Raycast(position + movement + Vector2.up * 0.1f, Vector2.down, 0.1f + floorCheckDistance, this.FloorLayer);
		if (hit.collider)
		{
			goalPosition = hit.point;

			if (this.verticalSpeed <= 0f && hit.collider.tag == "Destructible")
			{
				DestructiblePlatform platform = hit.collider.GetComponent<DestructiblePlatform>();
				if (platform)
				{
					platform.Destroy();
				}
			}
		}
		else
		{
			goalPosition = position + movement;
		}

		if (this.isJumping && this.verticalSpeed > 0 || hit.collider == null)
		{
			// Can't land when ascending.
			this.airTime += deltaTime;
		}
		else
		{
			this.airTime = 0;
			this.hasLandedAfterDash = true;

			if (this.isJumping)
			{
				this.EndJump();
			}
		}

		// Move.
		if (this.rigidbody.Cast(goalPosition - position, this.movementHits) > 0 && this.movementHits[0].normal.y < 0.5f)
		{
			float distance = Vector2.Distance(hit.point, this.rigidbody.ClosestPoint(hit.point)) - 0.05f;
			goalPosition = Vector2.MoveTowards(position, goalPosition, distance);

			Debug.DrawRay(hit.point, hit.normal, Color.red);
		}

		this.rigidbody.MovePosition(goalPosition);
		
	}

	private void StartJump()
	{
		this.isJumping = true;
		this.airTime = 0f;

		this.Animator.SetBool("IsJumping", true);
		this.PlaySound(this.JumpStart);
	}

	private void EndJump()
	{
		this.isJumping = false;

		this.Animator.SetBool("IsJumping", false);
	}

	private void StartDash()
	{
		if (this.horizontalInput != 0)
		{
			this.facing = (int)Mathf.Sign(this.horizontalInput);
		}

		if (this.isJumping)
		{
			this.EndJump();
		}

		this.dashTime = float.Epsilon;
		this.Animator.SetTrigger("DashStart");
		this.PlaySound(this.Dash);
	}

	private void EndDash()
	{
		this.dashTime = -this.DashCooldown;
		this.hasLandedAfterDash = false;
		this.airTime = 0;

		this.Animator.SetTrigger("DashEnd");
	}

	public void Step()
	{
		this.PlaySound(this.Footsteps, 0.3f);
	}

	private void PlaySound(AudioClip[] clips, float volume = 1f)
	{
		if (clips == null || clips.Length <= 0)
		{
			return;
		}

		this.AudioSource.PlayOneShot(clips[Random.Range(0, clips.Length)], volume);
	}
}

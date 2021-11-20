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
	[Tooltip("Si le joueur descend plus bas que cette altitude, c'est game over.\nCe n'est pas censé arriver, c'est pour si jamais y'a un bug et qu'une plateforme est traversée.")]
	public float KillAltitude = -10;

	private new Rigidbody2D rigidbody;

	private float horizontalInput;
	private float horizontalSpeedRatio;

	private float airTime;
	private bool isJumping;
	private float dashTime;
	private bool hasLandedAfterDash;
	private float verticalSpeed;
	private Vector3 lastVelocity;

	private int facing = 2;
	private RaycastHit2D[] movementHits = new RaycastHit2D[1];

	private bool gameHasStarted;
	private bool gameIsOver;

	[Header("Dash")]
	[Tooltip("La vitesse horizontale lors d'un dash.\n-X = temps en secondes, Y = multiplicateur de la vitesse de déplacement.\nLe dash se termine lorsque le temps dépasse la position du dernier point de la courbe.")]
	public AnimationCurve DashHorizontalSpeed;
	private float dashDuration;
	[Tooltip("Le temps d'attente en secondes entre le contact avec le sol après un dash et le moment où le joueur peut de nouveau dash.")]
	public float DashCooldown;

	[Header("Corruption")]
	public float MaximumCorruption;
	public float StartingCorruption;
	public float CorruptionPerSecond;
	private float currentCorruption;

	[Header("Potion effects")]
	public GameObject PotionDangerPrefab;
	public float DangerStartDelay;
	public float DangerLoopDelay;
	public GameObject FartPrefab;

	[Header("References")]
	public Transform Model;
	public Animator Animator;
	public AudioSource AudioSource;
	public GameUI GameUI;
	public Animator CameraAnimator;

	[Header("Audio")]
	public AudioClip[] Footsteps;
	public AudioClip[] JumpStart;
	public AudioClip[] Dash;
	public AudioClip[] Fart;
	public AudioClip[] Potion;

	private bool controlsInverted;
	private float desiredDrunkLevel;
	private float currentDrunkLevel;
	private bool cannotStopMoving;
	private float fartScale;

	private void Start()
	{
		this.rigidbody = this.GetComponent<Rigidbody2D>();
		this.dashDuration = this.DashHorizontalSpeed.keys[this.DashHorizontalSpeed.keys.Length - 1].time;
	}

	private void Update()
	{
		if (this.gameIsOver)
		{
			return;
		}

		if (Time.timeScale < 0.1f)
		{
			return;
		}

		this.UpdateInput();
		this.UpdateCorruption();
		this.UpdateAnimator();

		if (this.transform.position.y < this.KillAltitude)
		{
			this.GameOver();
		}
	}

	private void UpdateInput()
	{
		this.horizontalInput = Input.GetAxis("Horizontal");
		if (this.controlsInverted)
		{
			this.horizontalInput *= -1;
		}

		if (this.cannotStopMoving)
		{
			if (this.horizontalInput == 0f)
			{
				this.horizontalInput = this.facing;
			}
			else
			{
				this.horizontalInput = Mathf.Sign(this.horizontalInput);
			}
		}

		this.Model.localRotation = Quaternion.Lerp(this.Model.localRotation, Quaternion.Euler(Vector3.up * 90 * this.facing), Time.deltaTime * this.RotationSpeed);

		this.gameHasStarted |= this.horizontalInput != 0f;

		if (this.dashTime <= 0f)
		{
			// Handle Jump when in coyote time.
			if (this.hasLandedAfterDash && !this.isJumping && this.airTime < this.CoyoteTime)
			{
				if (Input.GetButtonDown("Jump") && !this.controlsInverted || Input.GetButtonDown("Dash") && this.controlsInverted)
				{
					this.gameHasStarted = true;
					this.StartJump();
				}
			}

			// Handle Dash when ready.
			if (this.hasLandedAfterDash)
			{
				if (this.dashTime == 0f)
				{
					if (Input.GetButtonDown("Dash") && !this.controlsInverted || Input.GetButtonDown("Jump") && this.controlsInverted)
					{
						this.gameHasStarted = true;
						this.StartDash();
					}
				}
				else
				{
					this.dashTime = Mathf.Min(this.dashTime + Time.deltaTime, 0f);
				}
			}
		}
	}

	private void UpdateCorruption()
	{
		if (this.gameHasStarted)
		{
			this.currentCorruption = Mathf.Min(this.currentCorruption + this.CorruptionPerSecond * Time.deltaTime, this.MaximumCorruption);
		}

		this.GameUI.CorruptionBar.fillAmount = this.currentCorruption / this.MaximumCorruption;
		this.GameUI.CorruptionAnimator.SetFloat("CorruptionRatio", this.currentCorruption / this.MaximumCorruption);
		this.Animator.SetFloat("CorruptionRatio", this.currentCorruption / this.MaximumCorruption);

		int percentage = (int)((this.currentCorruption / this.MaximumCorruption) * 100f);
		if (percentage > 8)
		{
			this.GameUI.CorruptionLabel.text = $"{percentage}%";
		}
		else
		{
			this.GameUI.CorruptionLabel.text = $"0{percentage}%";
		}

		if (this.currentCorruption >= this.MaximumCorruption)
		{
			this.GameOver();
		}
	}

	private void UpdateAnimator()
	{
		this.Animator.SetFloat("SpeedRatio", Mathf.Abs(this.horizontalSpeedRatio));
		this.Animator.SetBool("IsFalling", this.verticalSpeed < 0f);
		this.Animator.SetBool("IsOnGround", this.airTime == 0f && this.hasLandedAfterDash);
	}

	private void FixedUpdate()
	{
		this.rigidbody.velocity *= 0.8f;

		if (this.gameIsOver)
		{
			this.lastVelocity *= 0.9f;
			this.transform.position += this.lastVelocity;

			return;
		}

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

		this.lastVelocity = this.transform.position - (Vector3)position;
	}

	private void UpdateDashVelocityAndMove(Vector2 position, float deltaTime)
	{
		this.horizontalSpeedRatio = this.facing * this.DashHorizontalSpeed.Evaluate(this.dashTime);
		this.dashTime += deltaTime;

		if (this.fartScale != 0f)
		{
			this.horizontalSpeedRatio *= this.fartScale;
		}

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

			if (this.fartScale != 0f)
			{
				if (this.verticalSpeed < 0f)
				{
					this.fartScale = 0f;
				}
				else
				{
					this.verticalSpeed *= this.fartScale;
				}
			}
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
		if (this.rigidbody.Cast(goalPosition - position, this.movementHits) > 0 && !this.movementHits[0].collider.isTrigger && this.movementHits[0].normal.y < 0.5f)
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

		if (this.fartScale != 0f)
		{
			this.Animator.SetBool("EffectFartReady", false);
			this.PlaySound(this.Fart);

			GameObject fartInstance = GameObject.Instantiate(this.FartPrefab, this.transform.position, Quaternion.identity);
			GameObject.Destroy(fartInstance, 1f);
		}
		else
		{
			this.PlaySound(this.JumpStart);
		}
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
		else if (this.facing == 2)
		{
			this.facing = 1;
		}

		if (this.isJumping)
		{
			this.EndJump();
		}

		this.dashTime = float.Epsilon;
		this.Animator.SetTrigger("DashStart");

		if (this.fartScale != 0f)
		{
			this.Animator.SetBool("EffectFartReady", false);
			this.PlaySound(this.Fart);

			GameObject fartInstance = GameObject.Instantiate(this.FartPrefab, this.transform.position, Quaternion.identity);
			GameObject.Destroy(fartInstance, 1f);
		}
		else
		{
			this.PlaySound(this.Dash);
		}
	}

	private void EndDash()
	{
		this.dashTime = -this.DashCooldown;
		this.hasLandedAfterDash = false;
		this.airTime = 0;
		this.fartScale = 0f;

		this.Animator.SetTrigger("DashEnd");
	}

	private void GameOver()
	{
		this.gameIsOver = true;

		this.GameUI.CorruptionBar.fillAmount = 1f;
		this.GameUI.CorruptionAnimator.SetFloat("CorruptionRatio", 1f);
		this.Animator.SetFloat("CorruptionRatio", 1f);
		this.Animator.SetTrigger("CorruptionComplete");

		this.GameUI.enabled = false;
		this.GameUI.GameInterface.SetActive(false);
		this.GameUI.GameOverScreen.SetActive(true);

		UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(this.GameUI.RestartButton);

		this.CancelInvoke(nameof(SpawnFollowingDanger));
	}

	private void OnTriggerEnter2D(Collider2D collision)
	{
		if (this.gameIsOver)
		{
			return;
		}

		if (collision.tag == "Potion")
		{
			Potion potion = collision.GetComponentInParent<Potion>();

			this.ApplyPotionEffects(potion);
			potion.Collect();
		}

		if (collision.tag == "Danger")
		{
			this.GameOver();
		}
	}

	private void ApplyPotionEffects(Potion potion)
	{
		if (potion.InvertControls)
		{
			this.controlsInverted = true;
			this.Animator.SetBool("EffectControlsInverted", this.controlsInverted);
		}

		if (potion.DrunkPercentage != 0f)
		{
			this.desiredDrunkLevel += potion.DrunkPercentage;
			this.CameraAnimator.SetFloat("EffectDrunkLevel", this.desiredDrunkLevel);
		}

		if (potion.CantStopMoving)
		{
			this.cannotStopMoving = true;
			this.Animator.SetBool("EffectCantStopMoving", this.cannotStopMoving);
		}

		if (potion.ReduceVision)
		{
			this.CameraAnimator.SetBool("EffectVisionReduced", true);
		}

		if (potion.SpawnFollowingDanger)
		{
			this.InvokeRepeating(nameof(SpawnFollowingDanger), this.DangerStartDelay, this.DangerLoopDelay);
		}

		if (potion.Antidote)
		{
			this.controlsInverted = false;
			this.Animator.SetBool("EffectControlsInverted", this.controlsInverted);

			this.desiredDrunkLevel = 0f;
			this.CameraAnimator.SetFloat("EffectDrunkLevel", this.desiredDrunkLevel);

			this.cannotStopMoving = false;
			this.Animator.SetBool("EffectCantStopMoving", this.cannotStopMoving);

			this.CameraAnimator.SetBool("EffectVisionReduced", false);

			this.fartScale = 0f;
			this.Animator.SetBool("EffectFartReady", false);

			this.CancelInvoke(nameof(SpawnFollowingDanger));
		}

		if (potion.FartScale != 0f)
		{
			this.fartScale = potion.FartScale;
			this.Animator.SetBool("EffectFartReady", true);
		}

		this.currentCorruption = Mathf.Clamp(this.currentCorruption + potion.Corruption, 0f, this.MaximumCorruption);
		this.PlaySound(this.Potion);
	}

	private void SpawnFollowingDanger()
	{
		GameObject danger = GameObject.Instantiate(this.PotionDangerPrefab, this.transform.position, Quaternion.identity);
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

	private void OnDrawGizmos()
	{
		Debug.DrawRay(Vector3.up * this.KillAltitude + Vector3.left * 100, Vector3.right * 200, Color.red);
	}
}
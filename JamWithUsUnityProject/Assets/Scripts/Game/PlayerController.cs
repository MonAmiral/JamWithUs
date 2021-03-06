using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
	[Header("Mouvement")]
	[Tooltip("La vitesse de d?placement maximale du personnage, exprim?e en m/s.")]
	public float MovementSpeed;
	[Tooltip("L'augmentation de la vitesse du personnage quand le joueur donne un input gauche/droite, exprim?e en m/s/s.")]
	public float Acceleration;
	[Tooltip("La r?duction de la vitesse du personnage quand le joueur ne donne pas d'input gauche/droite, exprim?e en m/s/s.")]
	public float Deceleration;
	[Tooltip("La vitesse de rotation du mod?le. N'affecte pas le gameplay.")]
	public float RotationSpeed;

	[Header("Saut/chute")]
	[Tooltip("La vitesse verticale lors d'un saut.\n-X = temps en secondes, Y = vitesse verticale.\nSi le temps pass? en l'air d?passe la position du dernier point de la courbe, la valeur de la courbe ? ce temps est tout de m?me utilis?e.")]
	public AnimationCurve JumpVerticalSpeed;
	[Tooltip("Le multiplicateur sur la vitesse horizontale lors d'un saut.\n-X = temps en secondes, Y = multiplicateur de la vitesse de d?placement.\nSi le temps pass? en l'air d?passe la position du dernier point de la courbe, la valeur de la courbe ? ce temps est tout de m?me utilis?e.")]
	public AnimationCurve JumpHorizontalSpeedMultiplier;
	[Tooltip("Le multiplicateur d'acc?l?ration/d?c?l?ration lors d'un saut.\n-X = temps en secondes, Y = multiplicateur d'acc?l?ration/d?c?l?ration.\nSi le temps pass? en l'air d?passe la position du dernier point de la courbe, la valeur de la courbe ? ce temps est tout de m?me utilis?e.")]
	public AnimationCurve JumpAirControl;
	[Tooltip("Le temps en secondes pendant lequel le joueur peut sauter alors qu'il vient de commencer ? chuter d'une plateforme.")]
	public float CoyoteTime;
	[Tooltip("La vitesse verticale lors d'une chute.\n-X = temps en secondes, Y = vitesse verticale.\nSi le temps pass? en l'air d?passe la position du dernier point de la courbe, la valeur de la courbe ? ce temps est tout de m?me utilis?e.")]
	public AnimationCurve Gravity;
	[Tooltip("Les layers de colliders qui seront consid?r?s comme du sol.")]
	public LayerMask FloorLayer;
	[Tooltip("Si le joueur descend plus bas que cette altitude, c'est game over.\nCe n'est pas cens? arriver, c'est pour si jamais y'a un bug et qu'une plateforme est travers?e.")]
	public float KillAltitude = -10;

	[Space]
	[Tooltip("Le prefab spawn? lorsqu'un saut est ex?cut?.")]
	public GameObject JumpPrefab;
	[Tooltip("Le prefab spawn? lorsqu'un dash est ex?cut?.")]
	public GameObject DashPrefab;
	[Tooltip("Le prefab spawn? lorsqu'un dash est pr?t.")]
	public GameObject DashReadyPrefab;

	[Header("Dash")]
	[Tooltip("La vitesse horizontale lors d'un dash.\n-X = temps en secondes, Y = multiplicateur de la vitesse de d?placement.\nLe dash se termine lorsque le temps d?passe la position du dernier point de la courbe.")]
	public AnimationCurve DashHorizontalSpeed;
	private float dashDuration;
	[Tooltip("Le temps d'attente en secondes entre le contact avec le sol apr?s un dash et le moment o? le joueur peut de nouveau dash.")]
	public float DashCooldown;

	[Header("Corruption")]
	[Tooltip("La valeur de corruption de d?part.")]
	public float StartingCorruption;
	[Tooltip("Lorsque la corruption atteint cette valeur, la partie est perdue.")]
	public float MaximumCorruption;
	[Tooltip("La corruption est augment?e de cette valeur par seconde.")]
	public float CorruptionPerSecond;
	private float currentCorruption;

	[Header("Effets de potions")]
	[Tooltip("Le prefab spawn? r?guli?rement quand une potion 'Danger' a ?t? bue.")]
	public GameObject PotionDangerPrefab;
	[Tooltip("Le d?lai entre le moment o? le joueur boit la potion 'Danger' et l'apparition du premier Danger.")]
	public float DangerStartDelay;
	[Tooltip("Le d?lai entre deux apparitions de dangers.")]
	public float DangerLoopDelay;
	[Tooltip("Le prefab spawn? lorsqu'un saut ou dash am?lior? est ex?cut?.")]
	public GameObject FartPrefab;

	[Header("R?f?rences")]
	public Transform Model;
	public Animator Animator;
	public AudioSource AudioSource;
	public GameUI GameUI;
	public Animator CameraAnimator;

	[Header("Victoire")]
	[Tooltip("Le num?ro du niveau. Commence ? 0, et est utilis? par les high scores et tout.")]
	public int LevelIndex;

	[Space]
	[Tooltip("Le nombre d'objets avec le tag 'Trigger' qui doivent ?tre activ?s pour gagner.")]
	public int RequiredTriggers;
	[Tooltip("Cocher cette case pour faire que les objets 'Trigger' ne sont activ?s que lorsque le joueur dash dedans.")]
	public bool TriggersRequireDash;

	[Space]
	[Tooltip("Cocher cette case pour faire que la partie est gagn?e quand la corruption atteint 0.")]
	public bool WinWhenCorruptionReachesZero;

	[Space]
	[Tooltip("Le nombre d'ingr?dients qui doivent ?tre amen?s ? un chaudron pour gagner la partie.")]
	public int RequiredIngredients;
	[Tooltip("La position o? les ingr?dients sont plac?s quand ils sont transport?s par le joueur.")]
	public Transform IngredientAnchor;
	[Tooltip("La courbe de mouvement vertical de l'ingr?dient quand il est d?plac? vers le joueur ou le chaudron.")]
	public AnimationCurve IngredientVerticalCurve;
	[Tooltip("La courbe de mouvement horizontal de l'ingr?dient quand il est d?plac? vers le joueur ou le chaudron.")]
	public AnimationCurve IngredientHorizontalCurve;
	private Transform activeIngredient;

	[Space]
	[Tooltip("La liste des murs destructibles qui, lorsqu'ils sont tous d?truits, font gagner la partie.")]
	public List<Collider2D> RequiredDestructions;

	[Header("Audio")]
	public AudioClip[] Footsteps;
	public AudioClip[] JumpStart;
	public AudioClip[] Dash;
	public AudioClip[] Fart;
	public AudioClip[] Potion;
	public AudioSource Music;

	private bool musicStarted = false;

	private new Rigidbody2D rigidbody;

	private float horizontalInput;
	private float horizontalSpeedRatio;

	private float airTime;
	private bool isJumping;
	private float dashTime;
	private bool hasLandedAfterDash;
	private float verticalSpeed;
	private Vector3 lastVelocity;
	private float climbRatio;

	private int facing = 2;
	private RaycastHit2D[] movementHits = new RaycastHit2D[1];

	private bool introInProgress;
	private bool gameHasStarted;
	private bool gameIsOver;

	private bool controlsInverted;
	private float desiredDrunkLevel;
	private float currentDrunkLevel;
	private bool cannotStopMoving;
	private float activeFartScale;
	private float pendingFartScale;

	private int score;

	private void Start()
	{
		currentCorruption = StartingCorruption;
		this.rigidbody = this.GetComponent<Rigidbody2D>();
		this.dashDuration = this.DashHorizontalSpeed.keys[this.DashHorizontalSpeed.keys.Length - 1].time;

		this.introInProgress = GameCameraController.PlayIntroduction;
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

		this.UpdateCorruption();
		this.UpdateAnimator();
		this.UpdateMusic();
		this.UpdateModelRotation();

		if (this.introInProgress)
		{
			if (GameCameraController.PlayIntroduction && Input.anyKeyDown)
			{
				GameCameraController.PlayIntroduction = false;
				this.Invoke(nameof(FinishIntro), 1f);
			}

			return;
		}

		this.UpdateInput();

		if (this.transform.position.y < this.KillAltitude)
		{
			this.GameOver();
		}
	}

	private void FinishIntro()
	{
		this.introInProgress = false;
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
					this.GameUI.DashBar.fillAmount = 1 + this.dashTime / this.DashCooldown;

					if (this.dashTime == 0f)
					{
						this.GameUI.DashBar.fillAmount = 1f;
						this.GameUI.DashAnimator.SetBool("Ready", true);

						this.SpawnVFX(this.DashReadyPrefab);
					}
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
		if (percentage >= 10)
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

	private void UpdateMusic()
	{
		if (this.Music == null)
		{
			return;
		}

		// Wait until the player moves to start the music.
		if (this.gameHasStarted && !this.gameIsOver && !this.musicStarted)
		{
			this.Music.Play();
			this.musicStarted = true;
		}
	}

	private void UpdateModelRotation()
	{
		Vector3 desiredEuler = Vector3.up * 90 * this.facing;
		RaycastHit2D hit = Physics2D.Raycast((Vector2)this.transform.position + Vector2.up * 0.05f, Vector2.down, 0.1f, this.FloorLayer);
		if (hit.collider)
		{
			Quaternion normalLook = Quaternion.LookRotation(Vector3.forward, hit.normal);
			desiredEuler.x = normalLook.eulerAngles.z * this.facing * -1f;
		}

		this.Model.localRotation = Quaternion.Lerp(this.Model.localRotation, Quaternion.Euler(desiredEuler), Time.deltaTime * this.RotationSpeed);
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

		this.lastVelocity = (Vector3)((Vector2)this.transform.position - position);
	}

	private void UpdateDashVelocityAndMove(Vector2 position, float deltaTime)
	{
		this.horizontalSpeedRatio = this.facing * this.DashHorizontalSpeed.Evaluate(this.dashTime);
		this.dashTime += deltaTime;

		if (this.activeFartScale != 0f)
		{
			this.horizontalSpeedRatio *= this.activeFartScale;
		}

		this.rigidbody.MovePosition(position + Vector2.right * this.horizontalSpeedRatio * this.MovementSpeed * deltaTime);

		this.GameUI.DashBar.fillAmount = 1 - (this.dashTime / this.dashDuration);
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

			if (this.activeFartScale != 0f)
			{
				if (this.verticalSpeed < 0f)
				{
					this.activeFartScale = 0f;
				}
				else
				{
					this.verticalSpeed *= this.activeFartScale;
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

		// Update air time.
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

		// Climb up ledges.
		if (this.ShouldClimb(position))
		{
			this.climbRatio = Mathf.MoveTowards(this.climbRatio, 1f, deltaTime * 5f);
			this.airTime -= deltaTime * 1.1f;
			goalPosition.y = Mathf.Max(goalPosition.y, position.y);
		}
		else
		{
			this.climbRatio = Mathf.MoveTowards(this.climbRatio, 0f, deltaTime * 3f);
		}

		goalPosition += this.climbRatio * Vector2.up * deltaTime * 5f;

		// Move.
		if (this.airTime == 0f)
		{
			position += Vector2.up * 0.1f;
		}

		float goalDistance = Vector2.Distance(position, goalPosition);
		if (this.rigidbody.Cast(goalPosition - position, this.movementHits, goalDistance) > 0
		 && !this.movementHits[0].collider.isTrigger
		 && this.movementHits[0].normal.y < 0.5f)
		{
			float distance = Vector2.Distance(hit.point, this.rigidbody.ClosestPoint(hit.point)) - 0.15f;
			goalPosition = Vector2.MoveTowards(position, goalPosition, distance);

			Debug.DrawRay(hit.point, hit.normal, Color.red);
		}

		this.rigidbody.MovePosition(goalPosition);
	}

	private bool ShouldClimb(Vector2 position)
	{
		if (this.airTime == 0f || this.horizontalSpeedRatio == 0f)
		{
			return false;
		}

		Vector2 centerCheckStart = position + Vector2.up * 0.7f;
		RaycastHit2D centerHit = Physics2D.Raycast(centerCheckStart, Vector2.down, 0.7f, this.FloorLayer);
		if (centerHit.collider != null)
		{
			return false;
		}

		Vector2 ledgeTopCheckStart = position + new Vector2(this.horizontalSpeedRatio * 0.7f, 0.7f);
		RaycastHit2D ledgeTopHit = Physics2D.Raycast(ledgeTopCheckStart, Vector2.down, 0.7f, this.FloorLayer);
		if (ledgeTopHit.collider == null)
		{
			return false;
		}

		return true;
	}

	private void StartJump()
	{
		this.isJumping = true;
		this.airTime = 0f;

		this.Animator.SetBool("IsJumping", true);

		if (this.pendingFartScale != 0f)
		{
			this.activeFartScale = this.pendingFartScale;
			this.pendingFartScale = 0f;
			this.GameUI.DashAnimator.SetBool("PoweredUp", false);

			this.Animator.SetBool("EffectFartReady", false);
			this.PlaySound(this.Fart);

			this.SpawnVFX(this.FartPrefab);
		}
		else
		{
			this.activeFartScale = 0f;
			this.PlaySound(this.JumpStart);

			this.SpawnVFX(this.JumpPrefab);
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
		this.GameUI.DashAnimator.SetBool("Ready", false);

		if (this.pendingFartScale != 0f)
		{
			this.activeFartScale = this.pendingFartScale;
			this.pendingFartScale = 0f;
			this.GameUI.DashAnimator.SetBool("PoweredUp", false);

			this.Animator.SetBool("EffectFartReady", false);
			this.PlaySound(this.Fart);

			this.SpawnVFX(this.FartPrefab);
		}
		else
		{
			this.activeFartScale = 0f;
			this.PlaySound(this.Dash);

			this.SpawnVFX(this.DashPrefab, true);
		}
	}

	private void EndDash()
	{
		this.dashTime = -this.DashCooldown;
		this.hasLandedAfterDash = false;
		this.airTime = 0;
		this.activeFartScale = 0f;

		this.Animator.SetTrigger("DashEnd");
	}

	public void Victory()
	{
		this.gameIsOver = true;

		this.GameUI.enabled = false;
		this.GameUI.GameInterface.SetActive(false);

		this.GameUI.VictoryScreen.SetActive(true);
		UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(this.GameUI.NextLevelButton);

		for (int i = 0; i < this.GameUI.Stars.Length; i++)
		{
			this.GameUI.Stars[i].SetActive(this.score > i);
		}

		this.CancelInvoke(nameof(SpawnFollowingDanger));

		int currentHighScore = PlayerPrefs.GetInt($"JamWithUs_HighscoreLevel{this.LevelIndex}", -1);
		if (this.score > currentHighScore)
		{
			PlayerPrefs.SetInt($"JamWithUs_HighscoreLevel{this.LevelIndex}", this.score);
			PlayerPrefs.Save();
			this.GameUI.NewHighscore.SetActive(true);
		}
		else
		{
			this.GameUI.NewHighscore.SetActive(false);
		}

		this.Music.Stop();
		this.musicStarted = false;
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

		this.Music.Stop();
		this.musicStarted = false;
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

		if (collision.tag == "Finish")
		{
			this.Victory();
		}

		if (collision.tag == "Ingredient")
		{
			if (this.activeIngredient == null)
			{
				collision.enabled = false;
				this.activeIngredient = collision.transform;
				this.activeIngredient.parent = this.IngredientAnchor;
				this.StartCoroutine(this.MoveIngredient(collision.transform, this.lastVelocity));
			}
		}

		if (collision.tag == "Cauldron")
		{
			if (this.activeIngredient != null)
			{
				this.activeIngredient.parent = collision.transform;
				this.StartCoroutine(this.MoveIngredient(this.activeIngredient, this.lastVelocity));
				this.activeIngredient = null;
				this.RequiredIngredients--;

				if (this.RequiredIngredients == 0)
				{
					this.Victory();
				}
			}
		}

		if (collision.tag == "Score")
		{
			Animator animator = collision.GetComponent<Animator>();
			if (animator)
			{
				animator.Play("Interact");
			}

			collision.enabled = false;
			GameObject.Destroy(collision.gameObject, 1f);
			this.score++;
		}
	}

	private void OnTriggerStay2D(Collider2D collision)
	{
		if (this.gameIsOver)
		{
			return;
		}

		if (collision.tag == "Trigger")
		{
			if (this.dashTime > 0f || !this.TriggersRequireDash)
			{
				collision.enabled = false;
				this.RequiredTriggers--;

				Animator animator = collision.GetComponent<Animator>();
				if (animator)
				{
					animator.Play("Interact");
				}

				if (this.RequiredTriggers == 0)
				{
					this.Victory();
				}
			}
		}
	}

	private void OnCollisionStay2D(Collision2D collision)
	{
		if (this.dashTime > 0f && collision.collider.tag == "DashDestructible")
		{
			collision.collider.enabled = false;
			collision.collider.GetComponent<Animator>().Play("Destroy");

			if (this.RequiredDestructions.Contains(collision.collider))
			{
				this.RequiredDestructions.Remove(collision.collider);
				if (this.RequiredDestructions.Count == 0)
				{
					this.Victory();
				}
			}
		}
	}

	private IEnumerator MoveIngredient(Transform ingredient, Vector3 initialVelocity)
	{
		Vector3 initialPosition = ingredient.localPosition;

		float elapsedTime = 0f;
		while (elapsedTime < 0.5f)
		{
			yield return null;

			elapsedTime += Time.deltaTime;
			float ratio = elapsedTime / 0.5f;

			Vector3 interpolatedPosition = initialPosition * (1 - this.IngredientHorizontalCurve.Evaluate(ratio));
			Vector3 verticalOffset = Vector3.up * this.IngredientVerticalCurve.Evaluate(ratio);

			ingredient.localPosition = interpolatedPosition + verticalOffset;
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

			this.pendingFartScale = 0f;
			this.Animator.SetBool("EffectFartReady", false);
			this.GameUI.DashAnimator.SetBool("PoweredUp", false);

			this.CancelInvoke(nameof(SpawnFollowingDanger));
		}

		if (potion.FartScale != 0f)
		{
			this.pendingFartScale = potion.FartScale;
			this.Animator.SetBool("EffectFartReady", true);
			this.GameUI.DashAnimator.SetBool("PoweredUp", true);
		}

		this.currentCorruption = Mathf.Clamp(this.currentCorruption + potion.Corruption, 0f, this.MaximumCorruption);
		this.PlaySound(this.Potion);
		this.Animator.Play("Eat", 1, 0f);

		if (this.WinWhenCorruptionReachesZero && this.currentCorruption == 0f)
		{
			this.Victory();
		}

		this.SpawnVFX(potion.VFXPrefab);
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

	private void SpawnVFX(GameObject prefab, bool dontLookDown = false)
	{
		if (prefab == null)
		{
			return;
		}

		GameObject dashInstance = GameObject.Instantiate(prefab, this.Model.position, Quaternion.LookRotation(Vector3.forward, this.Model.up), this.transform);

		if (dontLookDown && this.Model.forward.y < 0)
		{
			dashInstance.transform.rotation = Quaternion.identity;
		}

		if (this.Model.forward.x < 0)
		{
			dashInstance.transform.localScale = new Vector3(-1f, 1f, 1f);
		}

		GameObject.Destroy(dashInstance, 1f);
	}

	private void OnDrawGizmos()
	{
		Debug.DrawRay(Vector3.up * this.KillAltitude + Vector3.left * 100, Vector3.right * 200, Color.red);
	}
}
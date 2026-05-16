using Godot;

public partial class GuardInteraction : CharacterBody2D, IDamageable
{
	[Export] public string PromptText = "Press E to talk";
	[Export(PropertyHint.MultilineText)] public string Dialogue = "";
	[Export] public bool DisableCollisionAfterDialogue = false;
	[Export] public bool KillableAfterDialogue = false;
	[Export] public int MaxHealth = 30;
	[Export] public int AttackDamage = 15;
	[Export] public float AttackRange = 34.0f;
	[Export] public float AttackCooldown = 1.2f;
	[Export] public float AttackDamageDelay = 0.24f;

	private const float WinterZoneStartX = 3456.0f;
	private static readonly Color WinterTextColor = new(1.0f, 0.94f, 0.58f, 1.0f);

	private Area2D _interactionArea;
	private CollisionShape2D _bodyCollider;
	private AnimationPlayer _animationPlayer;
	private Label _promptLabel;
	private Label _dialogueLabel;
	private HealthBar _healthBar;
	private PlayerController _player;
	private string[] _dialogueLines = System.Array.Empty<string>();
	private int _dialogueIndex = -1;
	private int _currentHealth;
	private bool _dialogueComplete;
	private bool _wasInteractPressed;
	private bool _isAttacking;
	private bool _isDead;
	private float _attackCooldownTimer;
	private float _attackDamageTimer = -1.0f;

	public override void _Ready()
	{
		_interactionArea = GetNode<Area2D>("InteractionArea");
		_bodyCollider = GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
		_animationPlayer = GetNodeOrNull<AnimationPlayer>("AnimationPlayer");
		_promptLabel = GetNode<Label>("PromptLabel");
		_dialogueLabel = GetNode<Label>("DialogueLabel");
		_healthBar = GetNodeOrNull<HealthBar>("HealthBar");

		_currentHealth = MaxHealth;
		_dialogueLines = Dialogue.Split('\n', System.StringSplitOptions.RemoveEmptyEntries | System.StringSplitOptions.TrimEntries);

		LayoutLabels();
		ApplyBiomeTextStyle();
		_promptLabel.Text = PromptText;
		_promptLabel.Visible = false;
		_dialogueLabel.Visible = false;
		if (_healthBar != null)
		{
			_healthBar.SetHealth(_currentHealth, MaxHealth);
			_healthBar.Visible = false;
		}

		_interactionArea.BodyEntered += OnBodyEntered;
		_interactionArea.BodyExited += OnBodyExited;
		if (_animationPlayer != null)
			_animationPlayer.AnimationFinished += OnAnimationFinished;
	}

	public override void _Process(double delta)
	{
		if (_isDead)
			return;

		if (_player != null && _player.IsDead)
		{
			ClearCombatTarget();
			return;
		}

		if (_attackCooldownTimer > 0.0f)
			_attackCooldownTimer -= (float)delta;

		if (_attackDamageTimer > 0.0f)
		{
			_attackDamageTimer -= (float)delta;
			if (_attackDamageTimer <= 0.0f)
			{
				_attackDamageTimer = -1.0f;
				DealAttackDamage();
			}
		}

		bool interactPressed = Input.IsKeyPressed(Key.E);
		if (_player != null && interactPressed && !_wasInteractPressed)
			AdvanceDialogue();

		if (KillableAfterDialogue && _dialogueComplete)
			UpdateCombat();

		_wasInteractPressed = interactPressed;
	}

	public void TakeDamage(int damage)
	{
		if (_isDead || !KillableAfterDialogue || !_dialogueComplete)
			return;

		_currentHealth = Mathf.Max(0, _currentHealth - damage);
		_healthBar?.SetHealth(_currentHealth, MaxHealth);
		GD.Print($"Guard HP: {_currentHealth}/{MaxHealth}");

		if (_currentHealth <= 0)
			Die();
	}

	private void Die()
	{
		_isDead = true;
		_isAttacking = false;
		_attackDamageTimer = -1.0f;
		_promptLabel.Visible = false;
		_dialogueLabel.Visible = false;
		if (_healthBar != null)
			_healthBar.Visible = false;

		CollisionLayer = 0;
		CollisionMask = 0;
		if (_bodyCollider != null)
			_bodyCollider.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);

		if (_animationPlayer != null && _animationPlayer.HasAnimation("death"))
			_animationPlayer.Play("death");
		else
			QueueFree();
	}

	private void UpdateCombat()
	{
		if (_player == null || _isAttacking || _attackCooldownTimer > 0.0f)
			return;

		if (!IsPlayerInAttackReach())
			return;

		_isAttacking = true;
		_attackCooldownTimer = AttackCooldown;
		_attackDamageTimer = AttackDamageDelay;

		if (_animationPlayer != null && _animationPlayer.HasAnimation("attack"))
			_animationPlayer.Play("attack");
		else
		{
			DealAttackDamage();
			_isAttacking = false;
		}
	}

	private void DealAttackDamage()
	{
		if (_player == null || !GodotObject.IsInstanceValid(_player) || _player.IsDead || !IsPlayerInAttackReach())
			return;

		_player.TakeDamageFrom(this, AttackDamage);
	}

	private void ClearCombatTarget()
	{
		_player = null;
		_isAttacking = false;
		_attackDamageTimer = -1.0f;
		_attackCooldownTimer = 0.0f;
		_promptLabel.Visible = false;
		_dialogueLabel.Visible = false;

		if (_animationPlayer != null && _animationPlayer.HasAnimation("idle"))
			_animationPlayer.Play("idle");
	}

	private void AdvanceDialogue()
	{
		if (_dialogueComplete)
			return;

		_promptLabel.Visible = false;
		_dialogueIndex++;

		if (_dialogueIndex < _dialogueLines.Length)
		{
			_dialogueLabel.Text = _dialogueLines[_dialogueIndex];
			_dialogueLabel.Visible = true;
			return;
		}

		_dialogueComplete = true;
		_dialogueLabel.Visible = false;

		if (DisableCollisionAfterDialogue)
			DisableBodyCollision();
		else if (KillableAfterDialogue)
		{
			_promptLabel.Text = "Fight me, if you dare.";
			if (_healthBar != null)
			{
				_healthBar.Visible = true;
				_healthBar.SetHealth(_currentHealth, MaxHealth);
			}
		}

		if (_player != null && !_dialogueComplete)
			_promptLabel.Visible = true;
	}

	private void DisableBodyCollision()
	{
		CollisionLayer = 0;
		CollisionMask = 0;
		if (_bodyCollider != null)
			_bodyCollider.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);
	}

	private void LayoutLabels()
	{
		_promptLabel.Position = new Vector2(-10.0f, -58.0f);
		_promptLabel.Size = new Vector2(42.0f, 12.0f);
		_promptLabel.Scale = new Vector2(0.45f, 0.45f);
		_promptLabel.HorizontalAlignment = HorizontalAlignment.Center;
		_promptLabel.VerticalAlignment = VerticalAlignment.Center;
		_promptLabel.ZIndex = 50;

		_dialogueLabel.Position = new Vector2(-20.0f, -76.0f);
		_dialogueLabel.Size = new Vector2(68.0f, 20.0f);
		_dialogueLabel.Scale = new Vector2(0.30f, 0.30f);
		_dialogueLabel.HorizontalAlignment = HorizontalAlignment.Center;
		_dialogueLabel.VerticalAlignment = VerticalAlignment.Center;
		_dialogueLabel.AutowrapMode = TextServer.AutowrapMode.Off;
		_dialogueLabel.ZIndex = 50;
	}

	private void ApplyBiomeTextStyle()
	{
		if (GlobalPosition.X < WinterZoneStartX)
			return;

		ApplyTextColor(_promptLabel, WinterTextColor);
		ApplyTextColor(_dialogueLabel, WinterTextColor);
	}

	private static void ApplyTextColor(Label label, Color color)
	{
		label.AddThemeColorOverride("font_color", color);
		label.AddThemeColorOverride("font_shadow_color", Colors.Black);
		label.AddThemeConstantOverride("shadow_offset_x", 1);
		label.AddThemeConstantOverride("shadow_offset_y", 1);
	}

	private bool IsPlayerInAttackReach()
	{
		if (_player == null)
			return false;

		Vector2 delta = _player.GlobalPosition - GlobalPosition;
		return Mathf.Abs(delta.X) <= AttackRange && Mathf.Abs(delta.Y) <= 48.0f;
	}

	private void OnAnimationFinished(StringName animName)
	{
		if (animName == "death")
		{
			QueueFree();
			return;
		}

		if (_isDead)
			return;

		if (animName != "attack")
			return;

		_isAttacking = false;
		if (_animationPlayer != null && _animationPlayer.HasAnimation("idle"))
			_animationPlayer.Play("idle");
	}

	private void OnBodyEntered(Node2D body)
	{
		if (body is not PlayerController player)
			return;

		_player = player;
		if (!_dialogueComplete)
			_promptLabel.Visible = true;
	}

	private void OnBodyExited(Node2D body)
	{
		if (body != _player)
			return;

		_player = null;
		_promptLabel.Visible = false;
		_dialogueLabel.Visible = false;
	}
}

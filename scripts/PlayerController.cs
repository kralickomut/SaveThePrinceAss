using Godot;

public partial class PlayerController : CharacterBody2D, IDamageable
{
	public const float Speed = 100.0f;
	public const float JumpVelocity = -250.0f;

	[Export] public int MaxHealth = 100;
	[Export] public int AttackDamage = 10;

	// ── Состояния ───────────────────────────────────────────────────────────
	private enum State { Idle, Move, Jump, Attack, Hurt, Dead }
	private State _state = State.Idle;

	// ── Ноды ────────────────────────────────────────────────────────────────
	private AnimationPlayer _animationPlayer;
	private Sprite2D _sprite2D;
	private Area2D _attackHitbox;
	private CollisionShape2D _hitboxShape;

	// ── Данные ──────────────────────────────────────────────────────────────
	private int _currentHealth;
	private bool _facingLeft = false;
	private bool _hasDealtDamageThisAttack = false;
	private float _invincibleTimer = 0f;
	private const float InvincibleDuration = 1.2f;

	private Texture2D _knightTexture;
	private Texture2D _attackTexture;

	// ── Инициализация ───────────────────────────────────────────────────────
	public override void _Ready()
	{
		_animationPlayer = GetNode<AnimationPlayer>("PlayerAnimator/AnimationPlayer");
		_sprite2D        = GetNode<Sprite2D>("PlayerAnimator/Sprite2D");
		_attackHitbox    = GetNode<Area2D>("AttackHitbox");
		_hitboxShape     = GetNode<CollisionShape2D>("AttackHitbox/CollisionShape2D");

		_knightTexture = GD.Load<Texture2D>("res://sprites/knight.png");
		_attackTexture = GD.Load<Texture2D>("res://sprites/attack.png");

		_currentHealth        = MaxHealth;
		_hitboxShape.Disabled = true;

		_animationPlayer.AnimationFinished += OnAnimationFinished;
		AddToGroup("player");

		SetState(State.Idle);
	}

	// ── Физика (каждый кадр) ────────────────────────────────────────────────
	public override void _PhysicsProcess(double delta)
	{
		if (_state == State.Dead) return;

		Vector2 velocity = Velocity;

		if (_invincibleTimer > 0f)
			_invincibleTimer -= (float)delta;

		// Гравитация — ВСЕГДА
		if (!IsOnFloor())
			velocity += GetGravity() * (float)delta;

		// Ввод — только в незаблокированных состояниях
		if (_state == State.Idle || _state == State.Move || _state == State.Jump)
		{
			// Прыжок
			if (Input.IsActionJustPressed("ui_accept") && IsOnFloor())
				velocity.Y = JumpVelocity;

			// Атака
			if (Input.IsActionJustPressed("attack"))
			{
				SetState(State.Attack);
			}
			else
			{
				// Движение
				Vector2 dir = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
				if (dir != Vector2.Zero)
				{
					velocity.X  = dir.X * Speed;
					_facingLeft = dir.X < 0;
					_sprite2D.FlipH          = _facingLeft;
					_attackHitbox.Scale = new Vector2(_facingLeft ? -1f : 1f, 1f);
				}
				else
				{
					velocity.X = Mathf.MoveToward(velocity.X, 0, Speed);
				}
			}
		}
		else
		{
			// Во время атаки/получения урона — тормозим
			velocity.X = Mathf.MoveToward(velocity.X, 0, Speed);
		}

		// Проверка попадания — пока хитбокс активен, один раз за атаку
		if (_state == State.Attack && !_hitboxShape.Disabled && !_hasDealtDamageThisAttack)
		{
			foreach (var body in _attackHitbox.GetOverlappingBodies())
			{
				if (!GodotObject.IsInstanceValid(body)) continue;
				if (body is IDamageable damageable)
				{
					damageable.TakeDamage(AttackDamage);
					_hasDealtDamageThisAttack = true;
					break;
				}
			}
		}

		Velocity = velocity;
		MoveAndSlide();
		UpdateMovementState();
	}

	// ── Переключение состояния ──────────────────────────────────────────────
	private void SetState(State next)
	{
		if (_state == next) return;
		_state = next;

		// Хитбокс выключаем ВСЕГДА кроме входа в атаку (им управляет анимация)
		if (next != State.Attack)
			_hitboxShape.Disabled = true;

		switch (next)
		{
			case State.Idle:
				_sprite2D.Texture = _knightTexture;
				_animationPlayer.Play("idle");
				break;

			case State.Move:
				_sprite2D.Texture = _knightTexture;
				_animationPlayer.Play("move");
				break;

			case State.Jump:
				_sprite2D.Texture = _knightTexture;
				_animationPlayer.Play("jump");
				break;

			case State.Attack:
				_hasDealtDamageThisAttack = false;
				_sprite2D.Texture = _attackTexture;
				_animationPlayer.Play("attack");
				break;

			case State.Hurt:
				_sprite2D.Texture = _knightTexture;
				_animationPlayer.Play("hit");
				break;

			case State.Dead:
				_sprite2D.Texture = _knightTexture;
				_animationPlayer.Play("death");
				break;
		}
	}

	// Обновляет движение/прыжок/idle — НЕ трогает Attack/Hurt/Dead
	private void UpdateMovementState()
	{
		if (_state == State.Attack || _state == State.Hurt || _state == State.Dead)
			return;

		State next;
		if (!IsOnFloor())
			next = State.Jump;
		else if (Mathf.Abs(Velocity.X) > 1f)
			next = State.Move;
		else
			next = State.Idle;

		SetState(next); // SetState сам проверит — если то же состояние, ничего не произойдёт
	}

	// ── Получение урона (IDamageable) ───────────────────────────────────────
	public void TakeDamage(int damage)
	{
		if (_state == State.Dead || _invincibleTimer > 0f) return;

		_currentHealth = Mathf.Max(0, _currentHealth - damage);
		_invincibleTimer = InvincibleDuration;
		GD.Print($"Player HP: {_currentHealth}/{MaxHealth}");

		if (_currentHealth <= 0)
		{
			SetState(State.Dead);
			GD.Print("Player died!");
			return;
		}

		SetState(State.Hurt);
	}

	// ── Конец анимации ──────────────────────────────────────────────────────
	private void OnAnimationFinished(StringName animName)
	{
		if (_state == State.Dead) return;

		if (animName == "attack" && _state == State.Attack)
		{
			_hitboxShape.Disabled = true; // гарантированно выключаем хитбокс
			SetState(State.Idle);
		}

		if (animName == "hit" && _state == State.Hurt)
		{
			SetState(State.Idle);
		}
	}
}

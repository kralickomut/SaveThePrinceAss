using Godot;
using System.Collections.Generic;

public partial class PlayerController : CharacterBody2D, IDamageable
{
	public const float Speed = 100.0f;
	public const float RunSpeed = 160.0f;
	public const float JumpVelocity = -250.0f;

	[Export] public int MaxHealth = 100;
	[Export] public int AttackDamage = 10;

	// ── Состояния ───────────────────────────────────────────────────────────
	private enum State { Idle, Move, Run, Jump, Fall, Attack, Hurt, Dead }
	private State _state = State.Idle;

	// ── Ноды ────────────────────────────────────────────────────────────────
	private AnimationPlayer _animationPlayer;
	private Sprite2D _sprite2D;
	private Area2D _attackHitbox;
	private CollisionShape2D _hitboxShape;
	private HealthBar _healthBar;

	// ── Данные ──────────────────────────────────────────────────────────────
	private int _currentHealth;
	private bool _facingLeft = false;
	private bool _hasDealtDamageThisAttack = false;
	private float _invincibleTimer = 0f;
	private readonly Dictionary<ulong, float> _damageCooldownByAttacker = new();
	private readonly List<ulong> _damageCooldownKeys = new();
	private const float InvincibleDuration = 1.2f;
	private const ulong GlobalDamageSourceId = 0;
	private static readonly Vector2 AttackHitboxRightPosition = new(40f, -9f);
	private static readonly Vector2 AttackHitboxLeftPosition = new(-40f, -9f);

	// ── Инициализация ───────────────────────────────────────────────────────
	public override void _Ready()
	{
		_animationPlayer = GetNode<AnimationPlayer>("PlayerAnimator/AnimationPlayer");
		_sprite2D        = GetNode<Sprite2D>("PlayerAnimator/Sprite2D");
		_attackHitbox    = GetNode<Area2D>("AttackHitbox");
		_hitboxShape     = GetNode<CollisionShape2D>("AttackHitbox/CollisionShape2D");
		_healthBar       = GetNodeOrNull<HealthBar>("PlayerHud/HealthBar");

		_currentHealth        = MaxHealth;
		_hitboxShape.Disabled = true;
		_healthBar?.SetHealth(_currentHealth, MaxHealth);

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
		UpdateDamageCooldowns((float)delta);

		// Гравитация — ВСЕГДА
		if (!IsOnFloor())
			velocity += GetGravity() * (float)delta;

		// Ввод — только в незаблокированных состояниях
		if (_state == State.Idle || _state == State.Move || _state == State.Run || _state == State.Jump || _state == State.Fall)
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
					float speed = IsRunHeld() ? RunSpeed : Speed;
					velocity.X  = dir.X * speed;
					_facingLeft = dir.X < 0;
					UpdateFacingDirection();
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
				_animationPlayer.Play("idle");
				break;

			case State.Move:
				_animationPlayer.Play("move");
				break;

			case State.Run:
				_animationPlayer.Play("run");
				break;

			case State.Jump:
				_animationPlayer.Play("jump");
				break;

			case State.Fall:
				_animationPlayer.Play("fall");
				break;

			case State.Attack:
				_hasDealtDamageThisAttack = false;
				UpdateFacingDirection();
				_animationPlayer.Play("attack");
				break;

			case State.Hurt:
				_animationPlayer.Play("hit");
				break;

			case State.Dead:
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
			next = Velocity.Y > 0f ? State.Fall : State.Jump;
		else if (Mathf.Abs(Velocity.X) > 1f)
			next = IsRunHeld() ? State.Run : State.Move;
		else
			next = State.Idle;

		SetState(next); // SetState сам проверит — если то же состояние, ничего не произойдёт
	}

	private void UpdateFacingDirection()
	{
		// Warrior sheet faces left by default, so flip only when the player faces right.
		_sprite2D.FlipH = !_facingLeft;
		_attackHitbox.Position = _facingLeft ? AttackHitboxLeftPosition : AttackHitboxRightPosition;
		_attackHitbox.Scale = Vector2.One;
	}

	private static bool IsRunHeld()
	{
		return Input.IsKeyPressed(Key.Shift);
	}

	// ── Получение урона (IDamageable) ───────────────────────────────────────
	public void TakeDamage(int damage)
	{
		ApplyDamage(damage, GlobalDamageSourceId);
	}

	public void TakeDamageFrom(GodotObject attacker, int damage)
	{
		if (attacker == null || !GodotObject.IsInstanceValid(attacker))
		{
			TakeDamage(damage);
			return;
		}

		ApplyDamage(damage, attacker.GetInstanceId());
	}

	private void ApplyDamage(int damage, ulong damageSourceId)
	{
		if (_state == State.Dead) return;

		if (damageSourceId == GlobalDamageSourceId)
		{
			if (_invincibleTimer > 0f)
				return;

			_invincibleTimer = InvincibleDuration;
		}
		else
		{
			if (_damageCooldownByAttacker.TryGetValue(damageSourceId, out float cooldown) && cooldown > 0f)
				return;

			_damageCooldownByAttacker[damageSourceId] = InvincibleDuration;
			_invincibleTimer = InvincibleDuration;
		}

		_currentHealth = Mathf.Max(0, _currentHealth - damage);
		_healthBar?.SetHealth(_currentHealth, MaxHealth);
		GD.Print($"Player HP: {_currentHealth}/{MaxHealth}");

		if (_currentHealth <= 0)
		{
			SetState(State.Dead);
			GD.Print("Player died!");
			return;
		}

		SetState(State.Hurt);
	}

	private void UpdateDamageCooldowns(float delta)
	{
		_damageCooldownKeys.Clear();
		foreach (ulong sourceId in _damageCooldownByAttacker.Keys)
			_damageCooldownKeys.Add(sourceId);

		foreach (ulong sourceId in _damageCooldownKeys)
		{
			float remaining = _damageCooldownByAttacker[sourceId] - delta;
			if (remaining <= 0f)
				_damageCooldownByAttacker.Remove(sourceId);
			else
				_damageCooldownByAttacker[sourceId] = remaining;
		}
	}

	public bool Heal(int amount)
	{
		if (_state == State.Dead || _currentHealth >= MaxHealth)
			return false;

		_currentHealth = Mathf.Min(MaxHealth, _currentHealth + amount);
		_healthBar?.SetHealth(_currentHealth, MaxHealth);
		GD.Print($"Player HP: {_currentHealth}/{MaxHealth}");
		return true;
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

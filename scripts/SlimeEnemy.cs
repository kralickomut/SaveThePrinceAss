using Godot;

public partial class SlimeEnemy : CharacterBody2D, IDamageable
{
	[Export] public int MaxHealth     = 30;
	[Export] public int AttackDamage  = 5;
	[Export] public float MoveSpeed   = 40.0f;
	[Export] public float DetectRange = 120.0f;
	[Export] public float AttackRange = 52.0f;

	// ── Состояния ───────────────────────────────────────────────────────────
	private enum State { Patrol, Chase, Attack, Hurt, Dead }
	private State _state = State.Patrol;

	// ── Ноды ────────────────────────────────────────────────────────────────
	private AnimationPlayer _animationPlayer;
	private Sprite2D _sprite;
	private HealthBar _healthBar;

	// ── Данные ──────────────────────────────────────────────────────────────
	private int _currentHealth;
	private float _patrolDirection = 1f;
	private float _patrolTimer     = 0f;
	private const float PatrolTime = 2.5f;
	private const float AttackVerticalTolerance = 48.0f;
	private const float FacingDeadZone = 2.0f;

	// Урон наносится через задержку (в середине анимации атаки)
	private float _attackDamageTimer = -1f;
	private const float AttackDamageDelay = 0.24f;

	// ── Инициализация ───────────────────────────────────────────────────────
	public override void _Ready()
	{
		_currentHealth  = MaxHealth;
		_animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
		_sprite         = GetNode<Sprite2D>("Sprite2D");
		_healthBar      = GetNodeOrNull<HealthBar>("HealthBar");
		_healthBar?.SetHealth(_currentHealth, MaxHealth);

		_animationPlayer.AnimationFinished += OnAnimationFinished;
		SetState(State.Patrol);
	}

	// ── Физика (каждый кадр) ────────────────────────────────────────────────
	public override void _PhysicsProcess(double delta)
	{
		if (_state == State.Dead) return;

		Vector2 velocity = Velocity;

		// Гравитация — ВСЕГДА
		if (!IsOnFloor())
			velocity += GetGravity() * (float)delta;

		// Таймер нанесения урона при атаке
		if (_attackDamageTimer > 0)
		{
			_attackDamageTimer -= (float)delta;
			if (_attackDamageTimer <= 0)
			{
				_attackDamageTimer = -1f;
				DealAttackDamage();
			}
		}

		// Движение — только в Patrol / Chase
		if (_state == State.Patrol || _state == State.Chase)
		{
			var player        = GetTree().GetFirstNodeInGroup("player") as CharacterBody2D;
			float distToPlayer = player != null
				? GlobalPosition.DistanceTo(player.GlobalPosition)
				: 9999f;

			if (player != null && IsPlayerInAttackReach(player, AttackRange))
			{
				// Атакуем
				velocity.X = 0;
				UpdateFacing(player);
				SetState(State.Attack);
			}
			else if (player != null && distToPlayer < DetectRange)
			{
				// Гонимся
				float xDiff = player.GlobalPosition.X - GlobalPosition.X;
				float dir  = xDiff > 0 ? 1f : -1f;
				velocity.X = dir * MoveSpeed;
				if (Mathf.Abs(xDiff) > FacingDeadZone)
					_sprite.FlipH = dir < 0;
				if (_state != State.Chase) SetState(State.Chase);
			}
			else
			{
				// Патруль
				_patrolTimer += (float)delta;
				if (_patrolTimer >= PatrolTime || IsOnWall())
				{
					_patrolDirection *= -1f;
					_patrolTimer      = 0f;
				}
				velocity.X    = _patrolDirection * MoveSpeed * 0.5f;
				_sprite.FlipH = _patrolDirection < 0;
				if (_state != State.Patrol) SetState(State.Patrol);
			}
		}
		else
		{
			// Атака / получение урона — плавно тормозим
			velocity.X = Mathf.MoveToward(velocity.X, 0, MoveSpeed);
		}

		Velocity = velocity;
		MoveAndSlide();
	}

	// ── Переключение состояния ──────────────────────────────────────────────
	private void SetState(State next)
	{
		if (_state == next) return;
		_state = next;

		switch (next)
		{
			case State.Patrol:
			case State.Chase:
				_animationPlayer.Play("move");
				break;

			case State.Attack:
				_attackDamageTimer = AttackDamageDelay;
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

	// Наносим урон игроку в середине анимации атаки
	private void DealAttackDamage()
	{
		if (_state != State.Attack) return;

		var player = GetTree().GetFirstNodeInGroup("player") as CharacterBody2D;
		if (player == null) return;
		if (!GodotObject.IsInstanceValid(player)) return;

		if (!IsPlayerInAttackReach(player, AttackRange + 10f))
			return;

		if (player is PlayerController playerController)
			playerController.TakeDamageFrom(this, AttackDamage);
		else if (player is IDamageable damageable)
			damageable.TakeDamage(AttackDamage);
	}

	// ── Получение урона (IDamageable) ───────────────────────────────────────
	public void TakeDamage(int damage)
	{
		if (_state == State.Dead) return;

		_currentHealth = Mathf.Max(0, _currentHealth - damage);
		_healthBar?.SetHealth(_currentHealth, MaxHealth);
		GD.Print($"Slime HP: {_currentHealth}/{MaxHealth}");

		if (_currentHealth <= 0)
		{
			GD.Print("Slime died!");
			SetState(State.Dead);
			return;
		}

		SetState(State.Hurt);
	}

	// ── Конец анимации ──────────────────────────────────────────────────────
	private void OnAnimationFinished(StringName animName)
	{
		if (animName == "death")
		{
			QueueFree();
			return;
		}

		if (_state == State.Dead) return;

		if (animName == "attack" || animName == "hit")
		{
			// Напрямую устанавливаем состояние без проверки на равенство
			var player        = GetTree().GetFirstNodeInGroup("player") as CharacterBody2D;
			float distToPlayer = player != null
				? GlobalPosition.DistanceTo(player.GlobalPosition)
				: 9999f;

			_state = distToPlayer < DetectRange ? State.Chase : State.Patrol;
			_animationPlayer.Play("move");
		}
	}

	private bool IsPlayerInAttackReach(CharacterBody2D player, float horizontalRange)
	{
		Vector2 delta = player.GlobalPosition - GlobalPosition;
		return Mathf.Abs(delta.X) <= horizontalRange && Mathf.Abs(delta.Y) <= AttackVerticalTolerance;
	}

	private void UpdateFacing(CharacterBody2D player)
	{
		float xDiff = player.GlobalPosition.X - GlobalPosition.X;
		if (Mathf.Abs(xDiff) > FacingDeadZone)
			_sprite.FlipH = xDiff < 0f;
	}
}

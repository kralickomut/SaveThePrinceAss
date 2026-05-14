using Godot;

public partial class HealthBar : Node2D
{
	[Export] public float Width = 28.0f;
	[Export] public float Height = 4.0f;
	[Export] public bool Centered = true;
	[Export] public bool UseGandalfStyle = true;
	[Export] public bool UseLargeFrame = false;
	[Export] public float SecondaryWidth = 60.0f;
	[Export] public float SecondaryHeight = 8.0f;
	[Export] public Color HighColor = new Color(0.18f, 0.92f, 0.28f, 1.0f);
	[Export] public Color MidColor = new Color(0.96f, 0.78f, 0.16f, 1.0f);
	[Export] public Color LowColor = new Color(0.9f, 0.16f, 0.12f, 1.0f);
	[Export] public Color PlayerHealthColor = new Color(0.88f, 0.05f, 0.03f, 1.0f);

	private ColorRect _border;
	private ColorRect _background;
	private ColorRect _fill;
	private Sprite2D _frameSprite;
	private Sprite2D _redOrbSprite;
	private ColorRect _playerHealthFill;
	private Sprite2D _sprintFill;
	private int _maxHealth = 1;
	private bool _createdRedOrbSprite = false;
	private bool _createdFrameSprite = false;
	private bool _createdPlayerHealthFill = false;
	private bool _createdSprintFill = false;

	private static readonly Texture2D FrameTexture = GD.Load<Texture2D>("res://GandalfHardcore Hp bar/Hp bar.png");
	private static readonly Texture2D RedOrbTexture = GD.Load<Texture2D>("res://GandalfHardcore Hp bar/red bar.png");
	private static readonly Texture2D YellowBarTexture = GD.Load<Texture2D>("res://GandalfHardcore Hp bar/yellow bar.png");

	public override void _Ready()
	{
		_border = GetNodeOrNull<ColorRect>("Border");
		_background = GetNodeOrNull<ColorRect>("Background");
		_fill = GetNodeOrNull<ColorRect>("Fill");
		if (UseGandalfStyle)
			BuildGandalfStyle();

		Layout();
	}

	public void SetHealth(int currentHealth, int maxHealth)
	{
		_maxHealth = Mathf.Max(1, maxHealth);
		float ratio = Mathf.Clamp((float)currentHealth / _maxHealth, 0.0f, 1.0f);

		Color fillColor = GetFillColor(ratio);

		if (_playerHealthFill != null)
		{
			_playerHealthFill.Size = new Vector2(Width * ratio, Height);
			_playerHealthFill.Color = PlayerHealthColor;
		}

		if (_fill != null)
		{
			_fill.Size = new Vector2(Width * ratio, Height);
			_fill.Color = fillColor;
		}

		Visible = currentHealth > 0;
	}

	public void SetSecondary(float ratio)
	{
		ratio = Mathf.Clamp(ratio, 0.0f, 1.0f);
		if (_sprintFill == null)
			return;

		float fillWidth = Mathf.Max(0.0f, YellowBarTexture.GetWidth() * ratio);
		_sprintFill.RegionRect = new Rect2(0, 0, fillWidth, YellowBarTexture.GetHeight());
	}

	private void Layout()
	{
		Vector2 origin = Centered
			? new Vector2(-Width * 0.5f, -Height * 0.5f)
			: Vector2.Zero;

		if (_frameSprite != null)
		{
			if (_createdFrameSprite)
			{
				_frameSprite.Position = origin;
				_frameSprite.Scale = new Vector2(2.0f, 2.0f);
			}
		}

		if (_redOrbSprite != null)
		{
			if (_createdRedOrbSprite)
			{
				_redOrbSprite.Position = origin + new Vector2(0.0f, 3.0f);
				_redOrbSprite.Scale = new Vector2(2.0f, 2.0f);
			}
		}

		if (_border != null)
		{
			_border.Position = origin - Vector2.One;
			_border.Size = new Vector2(Width + 2.0f, Height + 2.0f);
		}

		if (_background != null)
		{
			_background.Position = origin;
			_background.Size = new Vector2(Width, Height);
		}

		if (_playerHealthFill != null)
		{
			if (_createdPlayerHealthFill)
				_playerHealthFill.Position = origin + new Vector2(126.0f, 106.0f);

			_playerHealthFill.Size = new Vector2(Width, Height);
		}

		if (_sprintFill != null)
		{
			if (_createdSprintFill)
			{
				_sprintFill.Position = origin + new Vector2(132.0f, 94.0f);
				_sprintFill.Scale = new Vector2(SecondaryWidth / YellowBarTexture.GetWidth(), SecondaryHeight / YellowBarTexture.GetHeight());
			}

			_sprintFill.RegionRect = new Rect2(0, 0, YellowBarTexture.GetWidth(), YellowBarTexture.GetHeight());
		}

		if (_fill != null)
		{
			_fill.Position = origin;
			_fill.Size = new Vector2(Width, Height);
		}
	}

	private void BuildGandalfStyle()
	{
		if (UseLargeFrame)
		{
			if (_border != null)
				_border.Visible = false;
			if (_background != null)
				_background.Visible = false;
			if (_fill != null)
				_fill.Visible = false;

			_redOrbSprite = GetNodeOrNull<Sprite2D>("RedOrbFill");
			if (_redOrbSprite == null)
			{
				_redOrbSprite = new Sprite2D { Name = "RedOrbFill" };
				AddChild(_redOrbSprite);
				MoveChild(_redOrbSprite, 0);
				_createdRedOrbSprite = true;
			}

			_redOrbSprite.Texture = RedOrbTexture;
			_redOrbSprite.Centered = false;
			_redOrbSprite.ZIndex = 0;

			_frameSprite = GetNodeOrNull<Sprite2D>("Frame");
			if (_frameSprite == null)
			{
				_frameSprite = new Sprite2D { Name = "Frame" };
				AddChild(_frameSprite);
				MoveChild(_frameSprite, 0);
				_createdFrameSprite = true;
			}

			_frameSprite.Texture = FrameTexture;
			_frameSprite.Centered = false;
			_frameSprite.ZIndex = 10;

			_playerHealthFill = GetNodeOrNull<ColorRect>("PlayerHealthFill");
			if (_playerHealthFill == null)
			{
				_playerHealthFill = new ColorRect { Name = "PlayerHealthFill" };
				AddChild(_playerHealthFill);
				_createdPlayerHealthFill = true;
			}

			_playerHealthFill.Color = PlayerHealthColor;
			_playerHealthFill.ZIndex = 1;

			_sprintFill = GetNodeOrNull<Sprite2D>("SprintFill");
			if (_sprintFill == null)
			{
				_sprintFill = new Sprite2D { Name = "SprintFill" };
				AddChild(_sprintFill);
				_createdSprintFill = true;
			}

			_sprintFill.Texture = YellowBarTexture;
			_sprintFill.Centered = false;
			_sprintFill.RegionEnabled = true;
			_sprintFill.ZIndex = 1;
		}
		else
		{
			if (_border != null)
				_border.Visible = true;
			if (_background != null)
				_background.Visible = true;
			if (_fill != null)
				_fill.Visible = true;
		}
	}

	private Color GetFillColor(float ratio)
	{
		if (ratio > 0.5f)
			return MidColor.Lerp(HighColor, (ratio - 0.5f) * 2.0f);

		return LowColor.Lerp(MidColor, ratio * 2.0f);
	}
}

using Godot;

public partial class HealthBar : Node2D
{
	[Export] public float Width = 28.0f;
	[Export] public float Height = 4.0f;
	[Export] public bool Centered = true;
	[Export] public Color HighColor = new Color(0.18f, 0.92f, 0.28f, 1.0f);
	[Export] public Color MidColor = new Color(0.96f, 0.78f, 0.16f, 1.0f);
	[Export] public Color LowColor = new Color(0.9f, 0.16f, 0.12f, 1.0f);

	private ColorRect _border;
	private ColorRect _background;
	private ColorRect _fill;
	private int _maxHealth = 1;

	public override void _Ready()
	{
		_border = GetNodeOrNull<ColorRect>("Border");
		_background = GetNode<ColorRect>("Background");
		_fill = GetNode<ColorRect>("Fill");
		Layout();
	}

	public void SetHealth(int currentHealth, int maxHealth)
	{
		_maxHealth = Mathf.Max(1, maxHealth);
		float ratio = Mathf.Clamp((float)currentHealth / _maxHealth, 0.0f, 1.0f);

		if (_fill == null)
			return;

		_fill.Size = new Vector2(Width * ratio, Height);
		_fill.Color = GetFillColor(ratio);
		Visible = currentHealth > 0;
	}

	private void Layout()
	{
		Vector2 origin = Centered
			? new Vector2(-Width * 0.5f, -Height * 0.5f)
			: Vector2.Zero;

		if (_border != null)
		{
			_border.Position = origin - Vector2.One;
			_border.Size = new Vector2(Width + 2.0f, Height + 2.0f);
		}

		_background.Position = origin;
		_background.Size = new Vector2(Width, Height);

		_fill.Position = origin;
		_fill.Size = new Vector2(Width, Height);
	}

	private Color GetFillColor(float ratio)
	{
		if (ratio > 0.5f)
			return MidColor.Lerp(HighColor, (ratio - 0.5f) * 2.0f);

		return LowColor.Lerp(MidColor, ratio * 2.0f);
	}
}

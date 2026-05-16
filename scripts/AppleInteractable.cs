using Godot;

public partial class AppleInteractable : Sprite2D
{
	[Export] public int HealAmount = 25;
	[Export] public Vector2 PromptOffset = new(0.0f, -56.0f);

	private const float WinterZoneStartX = 3456.0f;
	private static readonly Color WinterTextColor = new(1.0f, 0.94f, 0.58f, 1.0f);

	private Area2D _interactionArea;
	private Label _promptLabel;
	private PlayerController _player;
	private bool _wasInteractPressed;

	public override void _Ready()
	{
		_interactionArea = GetNode<Area2D>("InteractionArea");
		_promptLabel = GetNode<Label>("PromptLabel");
		LayoutPromptLabel();
		_promptLabel.Visible = false;

		_interactionArea.BodyEntered += OnBodyEntered;
		_interactionArea.BodyExited += OnBodyExited;
	}

	public override void _Process(double delta)
	{
		bool interactPressed = Input.IsKeyPressed(Key.E);

		if (_player != null && interactPressed && !_wasInteractPressed)
		{
			if (_player.Heal(HealAmount))
				QueueFree();
		}

		_wasInteractPressed = interactPressed;
	}

	private void LayoutPromptLabel()
	{
		Vector2 labelSize = new(180.0f, 24.0f);
		Vector2 labelScale = new(0.45f, 0.45f);
		_promptLabel.Position = PromptOffset - new Vector2(labelSize.X * labelScale.X * 0.5f, 0.0f);
		_promptLabel.Size = labelSize;
		_promptLabel.Scale = labelScale;
		_promptLabel.Text = "Press E to eat";
		_promptLabel.HorizontalAlignment = HorizontalAlignment.Center;
		_promptLabel.VerticalAlignment = VerticalAlignment.Center;
		_promptLabel.AutowrapMode = TextServer.AutowrapMode.Off;
		_promptLabel.ZIndex = 50;

		if (GlobalPosition.X >= WinterZoneStartX)
		{
			_promptLabel.AddThemeColorOverride("font_color", WinterTextColor);
			_promptLabel.AddThemeColorOverride("font_shadow_color", Colors.Black);
			_promptLabel.AddThemeConstantOverride("shadow_offset_x", 1);
			_promptLabel.AddThemeConstantOverride("shadow_offset_y", 1);
		}
	}

	private void OnBodyEntered(Node2D body)
	{
		if (body is not PlayerController player)
			return;

		_player = player;
		_promptLabel.Visible = true;
	}

	private void OnBodyExited(Node2D body)
	{
		if (body != _player)
			return;

		_player = null;
		_promptLabel.Visible = false;
	}
}

using Godot;

public partial class AppleInteractable : Sprite2D
{
	[Export] public int HealAmount = 25;

	private Area2D _interactionArea;
	private Label _promptLabel;
	private PlayerController _player;
	private bool _wasInteractPressed;

	public override void _Ready()
	{
		_interactionArea = GetNode<Area2D>("InteractionArea");
		_promptLabel = GetNode<Label>("PromptLabel");
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

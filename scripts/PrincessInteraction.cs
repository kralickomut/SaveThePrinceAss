using Godot;

public partial class PrincessInteraction : CharacterBody2D
{
	[Export] public string PromptText = "Press E to talk";
	[Export(PropertyHint.MultilineText)] public string BoundDialogue = "You came for me...\nPlease, cut these ropes.\nThe guards said no one would make it this far.";
	[Export(PropertyHint.MultilineText)] public string FreedDialogue = "I can move again.\nThank you, brave prince.\nLet us leave this frozen camp behind.";
	[Export] public string EndText = "THE END";
	[Export] public float TentShakeSeconds = 3.0f;
	[Export] public float TentShakeDistance = 8.0f;
	[Export] public Vector2 WinterDialoguePosition = new(-155.0f, -88.0f);

	private const float WinterZoneStartX = 3456.0f;
	private static readonly Color WinterTextColor = new(1.0f, 0.94f, 0.58f, 1.0f);

	private Area2D _interactionArea;
	private CollisionShape2D _bodyCollider;
	private Sprite2D _tiedLayer;
	private Label _promptLabel;
	private Label _dialogueLabel;
	private PlayerController _player;
	private string[] _boundLines = System.Array.Empty<string>();
	private string[] _freedLines = System.Array.Empty<string>();
	private int _dialogueIndex = -1;
	private bool _isFreed;
	private bool _endingStarted;
	private bool _wasInteractPressed;

	public override void _Ready()
	{
		_interactionArea = GetNode<Area2D>("InteractionArea");
		_bodyCollider = GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
		_tiedLayer = GetNodeOrNull<Sprite2D>("tied");
		_promptLabel = GetNode<Label>("PromptLabel");
		_dialogueLabel = GetNode<Label>("DialogueLabel");

		_boundLines = SplitLines(BoundDialogue);
		_freedLines = SplitLines(FreedDialogue);

		LayoutLabels();
		ApplyBiomeTextStyle();
		_promptLabel.Text = PromptText;
		_promptLabel.Visible = false;
		_dialogueLabel.Visible = false;

		_interactionArea.BodyEntered += OnBodyEntered;
		_interactionArea.BodyExited += OnBodyExited;
	}

	public override void _Process(double delta)
	{
		bool interactPressed = Input.IsKeyPressed(Key.E);
		if (_player != null && interactPressed && !_wasInteractPressed && !_endingStarted)
			AdvanceDialogue();

		_wasInteractPressed = interactPressed;
	}

	private void AdvanceDialogue()
	{
		_promptLabel.Visible = false;
		_dialogueIndex++;

		string[] activeLines = _isFreed ? _freedLines : _boundLines;
		if (_dialogueIndex < activeLines.Length)
		{
			_dialogueLabel.Text = activeLines[_dialogueIndex];
			_dialogueLabel.Visible = true;
			return;
		}

		if (!_isFreed)
		{
			UntiePrincess();
			_isFreed = true;
			_dialogueIndex = -1;
			AdvanceDialogue();
			return;
		}

		StartEnding();
	}

	private void UntiePrincess()
	{
		if (_tiedLayer != null)
			_tiedLayer.Visible = false;
	}

	private async void StartEnding()
	{
		_endingStarted = true;
		_promptLabel.Visible = false;
		_dialogueLabel.Visible = false;

		if (_bodyCollider != null)
			_bodyCollider.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);

		Visible = false;
		if (_player != null && GodotObject.IsInstanceValid(_player))
		{
			_player.Visible = false;
			_player.SetPhysicsProcess(false);
			_player.SetProcess(false);
		}

		Node2D tent = GetParent()?.GetNodeOrNull<Node2D>("Decor/LargeTent");
		if (tent != null)
		{
			Vector2 originalPosition = tent.Position;
			Tween tween = CreateTween();
			int loops = Mathf.Max(1, Mathf.RoundToInt(TentShakeSeconds / 0.16f));
			tween.SetLoops(loops);
			tween.TweenProperty(tent, "position:x", originalPosition.X - TentShakeDistance, 0.08f);
			tween.TweenProperty(tent, "position:x", originalPosition.X + TentShakeDistance, 0.08f);
			await ToSignal(tween, Tween.SignalName.Finished);
			tent.Position = originalPosition;
		}
		else
		{
			await ToSignal(GetTree().CreateTimer(TentShakeSeconds), SceneTreeTimer.SignalName.Timeout);
		}

		ShowEndText();
	}

	private void ShowEndText()
	{
		CanvasLayer layer = new CanvasLayer { Name = "EndingLayer", Layer = 200 };
		Label label = new Label { Name = "EndingText", Text = EndText };
		label.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		label.HorizontalAlignment = HorizontalAlignment.Center;
		label.VerticalAlignment = VerticalAlignment.Center;
		label.AddThemeFontSizeOverride("font_size", 72);
		label.AddThemeColorOverride("font_color", new Color(1.0f, 0.93f, 0.62f, 1.0f));
		label.AddThemeColorOverride("font_shadow_color", Colors.Black);
		label.AddThemeConstantOverride("shadow_offset_x", 4);
		label.AddThemeConstantOverride("shadow_offset_y", 4);

		layer.AddChild(label);
		GetTree().CurrentScene.AddChild(layer);
	}

	private void LayoutLabels()
	{
		_promptLabel.Position = new Vector2(-12.0f, -64.0f);
		_promptLabel.Size = new Vector2(52.0f, 12.0f);
		_promptLabel.Scale = new Vector2(0.45f, 0.45f);
		_promptLabel.HorizontalAlignment = HorizontalAlignment.Center;
		_promptLabel.VerticalAlignment = VerticalAlignment.Center;
		_promptLabel.ZIndex = 50;

		_dialogueLabel.Position = new Vector2(-24.0f, -84.0f);
		_dialogueLabel.Size = new Vector2(82.0f, 22.0f);
		_dialogueLabel.Scale = new Vector2(0.33f, 0.33f);
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
		_dialogueLabel.Position = WinterDialoguePosition;
	}

	private static void ApplyTextColor(Label label, Color color)
	{
		label.AddThemeColorOverride("font_color", color);
		label.AddThemeColorOverride("font_shadow_color", Colors.Black);
		label.AddThemeConstantOverride("shadow_offset_x", 1);
		label.AddThemeConstantOverride("shadow_offset_y", 1);
	}

	private static string[] SplitLines(string text)
	{
		return text.Split('\n', System.StringSplitOptions.RemoveEmptyEntries | System.StringSplitOptions.TrimEntries);
	}

	private void OnBodyEntered(Node2D body)
	{
		if (body is not PlayerController player)
			return;

		_player = player;
		if (!_endingStarted)
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

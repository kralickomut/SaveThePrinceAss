using Godot;

public partial class ZoneManager : Node
{
    private const float Zone1End = 576f;   // x where zone 1 → zone 2 crossfade is centred
    private const float Zone2End = 1152f;  // x where zone 2 → zone 3 crossfade is centred
    private const float FadeSize = 128f;   // total crossfade width in game pixels

    private Node2D _normalSprites;
    private Node2D _autumnSprites;
    private Node2D _winterSprites;

    public override void _Ready()
    {
        var root = GetParent();
        _normalSprites = root.GetNode<Node2D>("NormalBG/Sprites");
        _autumnSprites = root.GetNode<Node2D>("AutumnBG/Sprites");
        _winterSprites = root.GetNode<Node2D>("WinterBG/Sprites");

        SetAlpha(_autumnSprites, 0f);
        SetAlpha(_winterSprites, 0f);
    }

    public override void _Process(double delta)
    {
        var player = GetTree().GetFirstNodeInGroup("player") as Node2D;
        if (player == null) return;

        float px = player.GlobalPosition.X;

        // t12: 0 = fully zone 1, 1 = fully zone 2+
        float t12 = Mathf.Clamp((px - Zone1End + FadeSize * 0.5f) / FadeSize, 0f, 1f);
        // t23: 0 = fully zone 2, 1 = fully zone 3
        float t23 = Mathf.Clamp((px - Zone2End + FadeSize * 0.5f) / FadeSize, 0f, 1f);

        // NormalBG is always the base layer (alpha 1) so it fills the sky behind everything.
        // Autumn fades in over Normal, then Winter fades in over Autumn.
        SetAlpha(_autumnSprites, t12 * (1f - t23));
        SetAlpha(_winterSprites, t23);
    }

    private static void SetAlpha(Node2D node, float alpha)
    {
        var c = node.Modulate;
        c.A = alpha;
        node.Modulate = c;
    }
}

/// <summary>
/// Breakout
/// By Thomas Doyle
/// Portfolio: http://thomasdoyle11.github.io
/// </summary>

using Mirror;
using UnityEngine;

// Bricks are destructible objects that the player aims to destroy all of
// Once they are loaded into position with a given size and colour, they remain static until destroyed
public class Brick : NetworkBehaviour
{
    [SyncVar(hook = nameof(SetScale))]
    private Vector2 scale; // Width and height of the Brick
    private void SetScale(Vector2 oldScale, Vector2 newScale)
    {
        spriteRenderer.size = newScale;
        xBounds = new Vector2(position.x - scale.x / 2, position.x + scale.x / 2);
        yBounds = new Vector2(position.y - scale.y / 2, position.y + scale.y / 2);
    }

    [SyncVar(hook = nameof(SetPosition))]
    public Vector2 position; // Position of the Brick
    private void SetPosition(Vector2 oldPos, Vector2 newPos)
    {
        transform.position = newPos;
        xBounds = new Vector2(position.x - scale.x / 2, position.x + scale.x / 2);
        yBounds = new Vector2(position.y - scale.y / 2, position.y + scale.y / 2);
    }


    [SyncVar(hook = nameof(SetColour))]
    private Color colour; // Colour of the Brick
    private void SetColour(Color oldColour, Color newColour)
    {
        spriteRenderer.color = newColour;
    }

    public Vector2 xBounds; // Minimum and maximum x coordinates of the Brick
    public Vector2 yBounds; // Minimum and maximum y coordinates of the Brick

    public SpriteRenderer spriteRenderer; // Brick sprite

    void Awake()
    {
        spriteRenderer = transform.Find("BrickImage").GetComponent<SpriteRenderer>();
    }

    // Initialise Brick parameters
    public void SetParams(Vector2 position, Vector2 scale, Color colour)
    {
        this.position = position;
        this.colour = colour;
        this.scale = scale;
    }
}

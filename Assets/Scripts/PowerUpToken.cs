/// <summary>
/// Breakout
/// By Thomas Doyle
/// Portfolio: http://thomasdoyle11.github.io
/// </summary>

using Mirror;
using UnityEngine;

// PowerUpTokens are the visible manifestation of PowerUps. If they collide with the Paddle then the PowerUp 'Type' is activated
// These can affect one or both of the players, and can be negative or positive
// Possible future PowerUps: Slow Paddle, invert Paddle controls, oscillating Y Paddle, smaller Paddle.
public class PowerUpToken : NetworkBehaviour
{
    public float speed; // Speed at which token moves downwards

    private Vector2 _position; // Current position of the Token
    public Vector2 position
    {
        get
        {
            return _position;
        }
        set
        {
            _position = value;
            transform.position = new Vector3(_position.x, _position.y, 0);
        }
    }

    private float _radius; // Radius of the Token
    public float radius
    {
        get
        {
            return _radius;
        }
        set
        {
            _radius = value;
            transform.localScale = new Vector3(2 * radius, 2 * radius, 1);
        }
    }

    // Power-up types
    public enum Type
    {
        None, // Default type that shouldn't be used, used to force use of hook
        Barrier, // Creates a one-time barrier below the Paddle
        Multiball, // Splits the ball into multiple balls, only one of which needs to stay in
        Wider, // Widens only the Paddle this token collided with 
        Faster, // Increases Ball speed
        Hotball // Makes the ball pass through a single Brick instead of bouncing
    }

    [SyncVar(hook = nameof(SetType))]
    public Type type;
    private void SetType(Type oldType, Type newType)
    {
        if (newType == Type.Barrier)
        {
            spriteRenderer.sprite = GameController.instance.tokenCrossSprite;
            spriteRenderer.color = Color.red;
        }
        else if (newType == Type.Faster)
        {
            spriteRenderer.sprite = GameController.instance.tokenArrowsSprite;
            spriteRenderer.color = Color.green;
        }
        else if (newType == Type.Hotball)
        {
            spriteRenderer.sprite = GameController.instance.tokenFireSprite;
            spriteRenderer.color = new Color(1, 0.5f, 0);
        }
        else if (newType == Type.Multiball)
        {
            spriteRenderer.sprite = GameController.instance.tokenDotsSprite;
            spriteRenderer.color = Color.blue;
        }
        else if (newType == Type.Wider)
        {
            spriteRenderer.sprite = GameController.instance.tokenLineSprite;
            spriteRenderer.color = Color.cyan;
        }
    }

    public static Type[] types; // Array of all types, given its value in GameController
    public static Type RandomType()
    {
        int index = Random.Range(0, types.Length);
        return types[index];
    }

    SpriteRenderer spriteRenderer; // PowerUpToken sprite

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        speed = 3f;
        radius = 0.25f;
    }

    // Initialise PowerUpToken parameters
    public void SetParams(Brick brick, Type type)
    {
        position = brick.position;
        SetType(type, type);
        this.type = type;
    }
}

/// <summary>
/// Breakout
/// By Thomas Doyle
/// Portfolio: http://thomasdoyle11.github.io
/// </summary>

using Mirror;
using UnityEngine;

// The Ball is the entity that bounces around the play area off of walls, Bricks and Paddles.
// A Ball destroys a Brick if it collides with it.
// A Ball is destroyed if it goes beneath the play area
// There may be multiple Balls in play, and play is restarted when the last Ball is destroyed
public class Ball : NetworkBehaviour
{
    private void Awake()
    {
        speed = 6f;
        GameController.instance.standardBallSpeed = speed;
        direction = new Vector2(0, 1);
        radius = 0.1f;
        position = transform.position;
    }

    public float speed; // Speed of the Ball

    public Vector2 direction; // Direction of the Ball

    private Vector2 _position;
    public Vector2 position // Position of the Ball
    {
        get
        {
            return _position;
        }
        set
        {
            _position = value;
            transform.position = position;
        }
    }

    private float _radius; // Radius of the Ball
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
}

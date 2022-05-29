/// <summary>
/// Breakout
/// By Thomas Doyle
/// Portfolio: http://thomasdoyle11.github.io
/// </summary>

using Mirror;
using UnityEngine;

// The Paddle is what the player will control and thus there is just one per player
// The paddle is able to move left and right until its edges touch the edge of the play area
// The width of the Paddle may change during play
public class Paddle : NetworkBehaviour
{
    public override void OnStartClient()
    {
        base.OnStartClient();

        scale = new Vector2(1.6f, 0.1f);
        variableBounceWidth = 0.4f;
        speed = 7.5f;

        // Set the Paddle positions and names
        if (isLocalPlayer)
        {
            if (isServer)
            {
                SetPosition(new Vector2(0, -2.5f));
                playerName.GetComponent<TextMesh>().text = "Player 1";
            }
            else
            {
                SetPosition(new Vector2(0, -3.5f));
                playerName.GetComponent<TextMesh>().text = "Player 2";
            }
        }
        else
        {
            if (isServer)
            {
                playerName.GetComponent<TextMesh>().text = "Player 2";
            }
            else
            {
                playerName.GetComponent<TextMesh>().text = "Player 1";
            }
        }

        // Set the Fire Angles to show possible Ball launch trajectories
        fireAngle.Find("RightAngle").rotation = Quaternion.Euler(0, 0, 90 - GameController.instance.ballFireAngle * 180 / Mathf.PI);
        fireAngle.Find("LeftAngle").rotation = Quaternion.Euler(0, 0, -90 + GameController.instance.ballFireAngle * 180 / Mathf.PI);

        GameController.instance.standardPaddleWidth = scale.x;
        GameController.instance.paddles.Add(this);
        if (isServer && isLocalPlayer)
        {
            GameController.instance.AddBall(_position);
        }
    }

    public Transform paddleImage; // Visible part of the paddle
    public Transform playerName; // Displayed player name
    public Transform fireAngle; // Shows the min and max angle at which the Ball can be fired

    public float speed { get; private set; } // Movement speed of the Paddle
    public float minPosition { get; private set; } // Minimum X coordinate of Paddle against left edge
    public float maxPosition { get; private set; } // Maximum X coordinate of Paddle against right edge
    public float variableBounceWidth { get; private set; } // Width at each of the Paddle at which the Ball bounces differently. Only relevant if [variablePaddleEdgeBounces] is true in GameController

    // Main Ball in play
    public Ball mainBall
    {
        get
        {
            return GameController.instance.balls.Count > 0 ? GameController.instance.balls[0] : null;
        }
    }

    public Vector2 _scale; // Width of the Paddle
    public Vector2 scale
    {
        get { return _scale; }
        set
        {
            _scale = value;
            paddleImage.localScale = new Vector3(_scale.x, _scale.y, 1);
            maxPosition = (GameController.instance.gameAreaSize - _scale.x) / 2;
            minPosition = -maxPosition;
            if (!isLocalPlayer && isServer)
            {
                SendScaleToClient(_scale);
            }
        }
    }
    // Scale can be changed on the server by Powerups, so send this to the client
    [ClientRpc]
    public void SendScaleToClient(Vector2 scale)
    {
        if (isLocalPlayer)
        {
            this.scale = scale;
        }
    }

    private Vector2 _position; // Position of the Paddle in the gameplay area
    public Vector2 position { get { return _position; } }
    public void SetPosition(Vector2 position)
    {
        this._position = new Vector2(Mathf.Clamp(position.x, minPosition, maxPosition), position.y);
        transform.position = this._position;
        if (isServer && isLocalPlayer && !GameController.instance.inPlay && GameController.instance.balls.Count > 0)
        {
            mainBall.position = ballSittingPosition;
        }
        if (isLocalPlayer)
        {
            SendPositionToServer(_position);
        }
    }
    // Player client can use Input to change there position locally, and this must then be sent to the server
    // This must be sent to the server as the logic for Paddle collisions is performed there
    [Command]
    public void SendPositionToServer(Vector2 position)
    {
        _position = position;
    }

    public Vector2 ballSittingPosition // Position that ball sits on Paddle when not in play
    {
        get
        {
            return new Vector2(_position.x, _position.y + mainBall.radius);
        }
    }

    public Vector2 xBounds // Current left-most and right-most x coordinates of Paddle
    {
        get
        {
            return new Vector2(_position.x - scale.x / 2, _position.x + scale.x / 2);
        }
    }

    public Vector2 yBounds // Top-most and bottom-most x coordinates of Paddle
    {
        get
        {
            return new Vector2(_position.y - scale.y / 2, _position.y + scale.y / 2);
        }
    }
    
    private void Update()
    {
        // Listen for Input
        if (isLocalPlayer)
        {
            if (Input.GetKey(KeyCode.LeftArrow))
            {
                SetPosition(new Vector2(position.x - speed * Time.deltaTime, position.y));
            }
            else if (Input.GetKey(KeyCode.RightArrow))
            {
                SetPosition(new Vector2(position.x + speed * Time.deltaTime, position.y));
            }
            else if (Input.GetKey(KeyCode.Space))
            {
                // Let's only let the server serve
                if (isServer)
                {
                    if (!GameController.instance.inPlay)
                    {
                        GameController.instance.inPlay = true;
                    }
                }
            }
            if (Input.GetKey(KeyCode.Escape))
            {
                Application.Quit();
            }
        }
    }

    public override void OnStopClient()
    {
        base.OnStopClient();
        GameController.instance.paddles.Remove(this);
    }
}

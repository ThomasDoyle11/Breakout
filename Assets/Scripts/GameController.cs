/// <summary>
/// Breakout
/// By Thomas Doyle
/// Portfolio: http://thomasdoyle11.github.io
/// </summary>

using Mirror;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Linq;

// The GameController controls the logic for the game
// All clients will have a GameController to access values from it, but only the host will perform logic in the Update method
// The GameController could possibly be removed in later versions if data is distributed differently
public class GameController : NetworkBehaviour
{
    // Implement singleton pattern for GameController
    public static GameController instance;
    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            instance = this;
        }
    }

    public float gameAreaSize { get; private set; } // Height and width of the square gameplay area

    [SyncVar(hook = nameof(SetScore))]
    private int _score;  // Players current score
    public int score
    {
        get
        {
            return _score;
        }
        set
        {
            _score = value;
            scoreText.text = "Score: " + score;
        }
    }
    public void SetScore(int _, int score)
    {
        this.score = score;
    }

    [SyncVar(hook = nameof(SetDeaths))]
    private int _deaths; // Number of times player has died
    public int deaths
    {
        get
        {
            return _deaths;
        }
        set
        {
            _deaths = value;
            deathsText.text = "Deaths: " + deaths;
        }
    }
    public void SetDeaths(int _, int deaths)
    {
        this.deaths = deaths;
    }

    // Powerup-related variables
    [SyncVar(hook = nameof(SetHasBarrier))]
    private bool hasBarrier; // Allows the ball to hit the bottom once
    public void SetHasBarrier(bool oldValue, bool newValue)
    {
        barrierRef.SetActive(newValue);
    }
    public float standardPaddleWidth { get; set; }
    public float paddleWidthMultiplier { get; private set; } // Multiplies the Paddle width 
    public float standardBallSpeed { get; set; }
    public float ballSpeedMultiplier { get; private set; } // Multiplies the Ball speed
    public bool addExtraBalls { get; private set; }
    public int numExtraBalls { get; private set; } // Adds extra Balls
    public bool ballIsHot { get; private set; } // Ball will pass through a single brick

    // True when ball in motion, false when Ball is waiting on Paddle
    // [SyncVar]
    public bool _inPlay;
    public bool inPlay
    {
        get
        {
            return _inPlay;
        }
        set
        {
            if (!isServer)
            {
                Debug.Log("Don't touch this");
            }
            _inPlay = value;
            if (_inPlay)
            {
                LaunchBall(balls[0]);
            }
            else
            {
                ChangeFireAnglesVisibility(true);
                DestroyAllPowerUpTokens();
                ClearPowerUps();
                ballsToDestroy.Clear();
                balls[0].position = paddles[0].ballSittingPosition;
                PlaySound(deadSound);
            }
        }
    }

    // Gameplay options
    public float ballFireAngle { get; private set; } // Angle in radians measured against the vertical at which the ball can be fired from the start
    public float catchForgiveDistance { get; private set; } // Distance the Ball can travel past the Paddle and still be allowed to reflect
    public int scorePerBrick { get; private set; } // Score awarded for destroying a Brick
    public float edgeBuffer { get; private set; } // Distance between Bricks and the edge of the gameplay area
    public float brickBuffer { get; private set; } // Distance between adjacent Bricks on same row
    public float brickHeight { get; private set; } // Height of a Brick
    public float bottomBrickRowPosition { get; private set; } // Y position of the bottom row of Bricks
    public int bricksPerRow { get; private set; } // Number of Bricks in a row
    public int brickRowsPerGrid { get; private set; } // Number of Brick rows in the grid
    public float brickWidthRandomness { get; private set; } // Between 0 and 1, 0 meaning no randomness and 1 meaning up to 100% bigger or smaller
    public bool variablePaddleEdgeBounces { get; private set; } // If true, the Ball bounces off the edge of the Paddle in the direction of that Paddle edge. Options for this are set in the Paddle class
    public bool keepGeneratingBricks { get; private set; } // If true, generates a new grid of Bricks when all have been destroyed
    public float powerUpDropChance { get; private set; } // Chance of a Brick dropping a powerup when destroyed

    // Colours that Bricks will be
    public Color[] brickColours { get; private set; } 

    // Instantiable Prefabs
    public GameObject brickPrefab;
    public GameObject powerUpTokenPrefab;
    public GameObject ballPrefab;

    // Sprites
    public Sprite tokenDotsSprite;
    public Sprite tokenLineSprite;
    public Sprite tokenCrossSprite;
    public Sprite tokenFireSprite;
    public Sprite tokenArrowsSprite;

    // Sounds
    public AudioSource audioSource;
    public AudioClip bounceSound;
    public AudioClip brickSound;
    public AudioClip deadSound;

    // GameObject references
    public GameObject bricksRef;
    public GameObject powerUpTokensRef;
    public GameObject scoreRef;
    public GameObject deathsRef;
    public GameObject barrierRef;
    public GameObject ballsRef;

    // Script references
    public List<Paddle> paddles;
    public List<Ball> balls { get; private set; }
    public List<List<Brick>> bricks;
    public List<PowerUpToken> tokens { get; private set; }
    public TextMeshProUGUI scoreText { get; private set; }
    public TextMeshProUGUI deathsText { get; private set; }

    public List<Ball> ballsToDestroy; // Used to destroy Balls at the end of the Update method to prevent issues with for loop

    // Start is called before the first frame update
    public override void OnStartClient()
    {
        base.OnStartClient();

        audioSource = GetComponent<AudioSource>();
        balls = new List<Ball>();
        bricks = new List<List<Brick>>();
        tokens = new List<PowerUpToken>();
        scoreText = scoreRef.GetComponent<TextMeshProUGUI>();
        deathsText = deathsRef.GetComponent<TextMeshProUGUI>();
        ballsToDestroy = new List<Ball>();

        gameAreaSize = 2 * Camera.main.orthographicSize;
        score = 0;

        paddleWidthMultiplier = 1.5f;
        ballSpeedMultiplier = 1.5f;
        ballIsHot = false;
        addExtraBalls = false;
        numExtraBalls = 3;
        hasBarrier = false;
        // Hook workaround
        SetHasBarrier(false, false);

        ballFireAngle = Mathf.PI / 4;
        catchForgiveDistance = 0.1f;
        edgeBuffer = 1;
        scorePerBrick = 100;
        edgeBuffer = 0.5f;
        brickBuffer = 0.05f;
        brickHeight = 0.65f;
        bottomBrickRowPosition = -0.5f;
        bricksPerRow = 10;
        brickRowsPerGrid = 5;
        brickWidthRandomness = 0.8f;
        variablePaddleEdgeBounces = true;
        keepGeneratingBricks = true;
        powerUpDropChance = 0.15f;

        brickColours = new Color[] { Color.red, Color.yellow, Color.green, Color.blue, Color.magenta };
        // Don't include first PowerUpToken.Type enum, it's only used so that the value has to be set to a non-default value
        PowerUpToken.types = new List<PowerUpToken.Type>(System.Enum.GetValues(typeof(PowerUpToken.Type)).OfType<PowerUpToken.Type>()).Skip(1).ToArray();

        if (isServer)
        {
            AddBrickGrid();
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (inPlay)
        {
            if (isServer)
            {
                // Move tokens
                foreach (PowerUpToken token in tokens)
                {
                    token.position = new Vector2(token.position.x, token.position.y - token.speed * Time.deltaTime);
                    if (token.position.y < -gameAreaSize / 2)
                    {
                        Destroy(token.gameObject);
                        tokens.Remove(token);
                        break;
                    }
                }
                // Paddle-related checks for all Paddles
                foreach (Ball ball in balls)
                {
                    float speed = ball.speed;
                    Vector2 currentDirection = ball.direction;
                    Vector2 currentPosition = ball.position;
                    float radius = ball.radius;
                    float xIncrease = speed * currentDirection.x * Time.deltaTime;
                    float yIncrease = speed * currentDirection.y * Time.deltaTime;
                    float newX = currentPosition.x + xIncrease;
                    float newY = currentPosition.y + yIncrease;
                    // Check for contact between Ball and Paddles
                    foreach (Paddle paddle in paddles)
                    {
                        // Check for Ball hitting Paddle from above
                        if (newY - radius < paddle.position.y)
                        {
                            // Only let the Ball collide with the Paddle if it hasn't travelled too far past it
                            float distanceToPaddle = paddle.position.y - (currentPosition.y - radius);
                            if (distanceToPaddle < catchForgiveDistance && currentDirection.y < 0)
                            {
                                Vector2 paddleBounds = paddle.xBounds;
                                if (newX + radius >= paddleBounds.x && newX - radius <= paddleBounds.y)
                                {
                                        BounceBallOffLine(ball, paddle.position.y, false, currentPosition.y, radius, yIncrease);
                                    // Check for Variable Paddle Edge Bounces
                                    if (variablePaddleEdgeBounces)
                                    {
                                        float distanceToLeftPaddleEdge = newX - paddleBounds.x;
                                        float distanceToRightPaddleEdge = paddleBounds.y - newX;
                                        if (distanceToLeftPaddleEdge <= paddle.variableBounceWidth)
                                        {
                                            float ratio = Mathf.Clamp(distanceToLeftPaddleEdge / paddle.variableBounceWidth, 0, 1);
                                            float newAngle = Mathf.PI / 2 + ballFireAngle * (1 - ratio);
                                            ball.direction = new Vector2(Mathf.Cos(newAngle), Mathf.Sin(newAngle));
                                        }
                                        else if (distanceToRightPaddleEdge <= paddle.variableBounceWidth)
                                        {
                                            float ratio = Mathf.Clamp(distanceToRightPaddleEdge / paddle.variableBounceWidth, 0, 1);
                                            float newAngle = Mathf.PI / 2 - ballFireAngle * (1 - ratio);
                                            ball.direction = new Vector2(Mathf.Cos(newAngle), Mathf.Sin(newAngle));
                                        }
                                    }
                                    break;
                                }
                            }
                        }
                        // Check for Powerups hitting Paddle
                        foreach (PowerUpToken token in tokens)
                        {
                            if (token.position.x + token.radius > paddle.xBounds.x && token.position.x - token.radius < paddle.xBounds.y && token.position.y + token.radius > paddle.yBounds.x && token.position.y - token.radius < paddle.yBounds.y)
                            {
                                ActivatePowerUp(token, paddle);
                                break;
                            }
                        }
                    }

                    // Check for Ball hitting one of the 4 edges
                    if (newX + radius > gameAreaSize / 2)
                    {
                        newX = BounceBallOffLine(ball, gameAreaSize / 2, true, currentPosition.x, radius, xIncrease);
                    }
                    else if (newX - radius < -gameAreaSize / 2)
                    {
                        newX = BounceBallOffLine(ball, -gameAreaSize / 2, true, currentPosition.x, radius, xIncrease);
                    }
                    else if (newY + radius > gameAreaSize / 2)
                    {
                        newY = BounceBallOffLine(ball, gameAreaSize / 2, false, currentPosition.y, radius, yIncrease);
                    }
                    else if (newY - radius < -gameAreaSize / 2)
                    {
                        // Only allow the Ball to bounce off bottom edge if player has correct power-up
                        // ball.ballLost = false;
                        if (hasBarrier)
                        {
                            newY = BounceBallOffLine(ball, -gameAreaSize / 2, false, currentPosition.y, radius, yIncrease);
                            hasBarrier = false;
                        }
                        else if (balls.Count > 1)
                        {
                            ballsToDestroy.Add(ball);
                        }
                        else
                        {
                            deaths += 1;
                            inPlay = false;
                            return;
                        }
                    }
                    else
                    {
                        bool brickDestroyed = false;
                        // Check for Ball hitting any one Brick
                        foreach (List<Brick> brickRow in bricks)
                        {
                            foreach (Brick brick in brickRow)
                            {
                                // Check if ball will enter brick
                                if (newX + radius > brick.xBounds.x && newX - radius < brick.xBounds.y && newY + radius > brick.yBounds.x && newY - radius < brick.yBounds.y)
                                {
                                    if (!ballIsHot)
                                    {
                                        // Check the direction the ball entered from by checking the current position
                                        if (currentPosition.x + radius < brick.xBounds.x)
                                        {
                                            newX = BounceBallOffLine(ball, brick.xBounds.x, true, currentPosition.x, radius, xIncrease);
                                        }
                                        else if (currentPosition.x - radius > brick.xBounds.y)
                                        {
                                            newX = BounceBallOffLine(ball, brick.xBounds.y, true, currentPosition.x, radius, xIncrease);
                                        }
                                        // Check both X and Y (not or) to allow bouncing back from a corner
                                        if (currentPosition.y + radius < brick.yBounds.x)
                                        {
                                            newY = BounceBallOffLine(ball, brick.yBounds.x, false, currentPosition.y, radius, yIncrease);
                                        }
                                        else if (currentPosition.y - radius > brick.yBounds.y)
                                        {
                                            newY = BounceBallOffLine(ball, brick.yBounds.y, false, currentPosition.y, radius, yIncrease);
                                        }
                                        PlaySound(brickSound);
                                    }
                                    else
                                    {
                                        ballIsHot = false;
                                    }
                                    DestroyBrick(brick, brickRow);
                                    // If last Brick is destroyed, no longer in play, stop Update method
                                    if (!inPlay)
                                    {
                                        return;
                                    }
                                    brickDestroyed = true;
                                    break;
                                }
                            }
                            if (brickDestroyed)
                            {
                                break;
                            }
                        }
                    }
                    // Update ball position
                    ball.position = new Vector3(newX, newY, 0);
                }

                if (!inPlay)
                {
                    return;
                }

                if (ballsToDestroy.Count > 0)
                {
                    foreach (Ball ball in ballsToDestroy)
                    {
                        DestroyBall(ball);
                    }
                    ballsToDestroy.Clear();
                }

                if (addExtraBalls)
                {
                    for (int i = 0; i < numExtraBalls; i++)
                    {
                        AddAndLaunchBall(balls[0].position);
                    }
                    addExtraBalls = false;
                }
            }
        }

    }

    // Bounce the Ball off the given line on the given axis, effectively reflecting the vector that goes past the line
    private float BounceBallOffLine(Ball ball, float line, bool isXAxis, float position, float radius, float increase)
    {
        float directionSign;
        if (isXAxis)
        {
            directionSign = Mathf.Sign(ball.direction.x);
            ball.direction.x = -ball.direction.x;
        }
        else
        {
            directionSign = Mathf.Sign(ball.direction.y);
            ball.direction.y = -ball.direction.y;
        }
        float distanceToEdge = line - (position + directionSign * radius);
        float remainingDistance = increase - distanceToEdge;
        increase = distanceToEdge - remainingDistance;
        PlaySound(bounceSound);
        return position + increase;
    }

    // Place a Brick in the scene at the given position, with the given scale and colour
    private void AddBrick(Vector2 position, Vector2 scale, Color colour, List<Brick> brickRow)
    {
        GameObject brickObject = Instantiate(brickPrefab, bricksRef.transform);
        Brick brick = brickObject.GetComponent<Brick>();
        brick.SetParams(position, scale, colour);
        brickRow.Add(brick);
        NetworkServer.Spawn(brickObject);
    }

    // Add a row of Bricks at the given height in the scene, all of the given colour
    private void AddBrickRow(float height, Color colour)
    {
        float startOfBrick = edgeBuffer;
        float standardBrickWidth = (gameAreaSize - 2 * edgeBuffer) / bricksPerRow;
        List<Brick> brickRow = new List<Brick>();
        bricks.Add(brickRow);
        for (int i = 0; i < bricksPerRow; i++)
        {
            float endOfBrick;
            if (i == bricksPerRow - 1)
            {
                endOfBrick = gameAreaSize - edgeBuffer;
            }
            else
            {
                endOfBrick = edgeBuffer + standardBrickWidth * (i + 1 + brickWidthRandomness * (Random.value - 0.5f)) - brickBuffer / 2;
            }
            AddBrick(new Vector2((startOfBrick + endOfBrick) / 2 - gameAreaSize / 2, height), new Vector2(endOfBrick - startOfBrick, brickHeight), colour, brickRow);
            startOfBrick = endOfBrick + brickBuffer;
        }
    }

    // Add a grid of Bricks
    private void AddBrickGrid()
    {
        for (int i = 0; i < brickRowsPerGrid; i++)
        {
            AddBrickRow(bottomBrickRowPosition + i * (brickHeight + brickBuffer), brickColours[i % brickColours.Length]);
        }
    }

    // Remove a Brick from the game and add points
    private void DestroyBrick(Brick brick, List<Brick> brickRow)
    {
        score += scorePerBrick;
        brickRow.Remove(brick);
        Destroy(brick.gameObject);
        if (Random.value <= powerUpDropChance)
        {
            DropPowerUpToken(brick, PowerUpToken.RandomType());
        }
        if (brickRow.Count == 0)
        {
            bricks.Remove(brickRow);
            if (keepGeneratingBricks && bricks.Count == 0)
            {
                AddBrickGrid();
                inPlay = false;
            }
        }
    }

    // Drop a PowerUpToken from the given Brick, of the given Type
    private void DropPowerUpToken(Brick brick, PowerUpToken.Type type)
    {
        GameObject tokenObject = Instantiate(powerUpTokenPrefab, powerUpTokensRef.transform);
        PowerUpToken token = tokenObject.GetComponent<PowerUpToken>();
        token.SetParams(brick, type);
        tokens.Add(token);
        NetworkServer.Spawn(tokenObject);
    }

    // Destroy any PowerUpTokens in the scene
    private void DestroyAllPowerUpTokens()
    {
        foreach (PowerUpToken token in tokens)
        {
            Destroy(token.gameObject);
        }
        tokens.Clear();
    }

    // Add the effect of a PowerUp to the player
    private void ActivatePowerUp(PowerUpToken token, Paddle paddle)
    {
        Destroy(token.gameObject);
        tokens.Remove(token);
        PowerUpToken.Type type = token.type;
        if (type == PowerUpToken.Type.Barrier)
        {
            hasBarrier = true;
        }
        else if (type == PowerUpToken.Type.Faster)
        {
            foreach (Ball ball in balls)
            {
                ball.speed = standardBallSpeed * ballSpeedMultiplier;
            }
        }
        else if (type == PowerUpToken.Type.Hotball)
        {
            ballIsHot = true;
        }
        else if (type == PowerUpToken.Type.Multiball)
        {
            addExtraBalls = true;
        }
        else if (type == PowerUpToken.Type.Wider)
        {
            paddle.scale = new Vector3(standardPaddleWidth * paddleWidthMultiplier, paddle.scale.y, 1);
        }
    }

    // Clear any PowerUp effects on the players
    private void ClearPowerUps()
    {
        hasBarrier = false;
        while(balls.Count > 1)
        {
            DestroyBall(balls[0]);
        }
        balls[0].speed = standardBallSpeed;
        ballIsHot = false;
        foreach (Paddle paddle in paddles)
        {
            paddle.scale = new Vector3(standardPaddleWidth, paddle.scale.y, 1);
        }
    }

    // Add a Ball into the game at the given position
    public Ball AddBall(Vector2 position)
    {
        GameObject ballObject = Instantiate(ballPrefab, ballsRef.transform);
        Ball ball = ballObject.GetComponent<Ball>();
        ball.position = position;
        balls.Add(ball);
        NetworkServer.Spawn(ballObject);
        return ball;
    }

    // Launch the given Ball from its current position
    private void LaunchBall(Ball ball)
    {
        float angle = (Mathf.PI / 2 - ballFireAngle) + Random.value * 2 * ballFireAngle;
        ball.direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        ChangeFireAnglesVisibility(false);
    }

    // Show or hide the 'Fire Angles', which show the arc of possible Ball launch trajectories
    [ClientRpc]
    private void ChangeFireAnglesVisibility(bool visible)
    {
        foreach (Paddle paddle in paddles)
        {
            paddle.fireAngle.gameObject.SetActive(visible);
        }
    }

    // Add a Ball at the given position and instantly launch it
    private void AddAndLaunchBall(Vector2 position)
    {
        Ball ball = AddBall(position);
        LaunchBall(ball);
    }

    // Remove the given Ball from the game, as long as it isn't the last ball
    private void DestroyBall(Ball ball)
    {
        // Don't allow destruction of last Ball
        if (balls.Count > 1)
        {
            balls.Remove(ball);
            Destroy(ball.gameObject);
        }
    }

    // Play an audio clip
    private void PlaySound(AudioClip sound)
    {
        if (sound != null)
        {
            audioSource.clip = sound;
            audioSource.Play();
        }
    }
}

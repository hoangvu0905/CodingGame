using System.Text;
using System.Drawing;
using System.Diagnostics;
using System.Reflection.Metadata;

namespace CodingGame.Winter2026;

/**
 * Auto-generated code below aims at helping you parse
 * the standard input according to the problem statement.
 **/
public class Player
{
    private const int MAX_TURN = 200;
    private const int MAX_STEP_SIMULATE = 10;
    private const int MAX_CALCULATE_RESULT = 100;

    static void Main(string[] args)
    {
        var board = new Board();

        board.ParseInitialInput();

        // game loop
        for (int i = 0; i < MAX_TURN; i++)
        {
            board.ResetRound(i);

            // Write an action using Console.WriteLine()
            // To debug: Console.Error.WriteLine("Debug messages...");
            
            // TODO: Simulate step and choice the best one.

            board.DoAction();
        }
    }
}

public class Board
{
    #region Initial input (constant)

    public int MyId { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public int Turn { get; private set; }

    public int NumberSnake { get; set; }

    /// <summary>IDs registered during initialization for each side (stable across rounds).</summary>
    public HashSet<int> MySnakeIds { get; set; } = [];

    public HashSet<Point> Platforms { get; set; } = [];

    #endregion

    #region Dynamic state (updated each round)

    /// <summary>Active snakes this round: snakeId → body (head-first list of Points).</summary>
    /// <remarks>
    /// Map lookup uses <c>Point(row, col) = Point(Y, X)</c> game-coordinates. See <see cref="Map"/>.
    /// </remarks>
    public Dictionary<int, IList<Point>> MySnake { get; set; } = [];
    public Dictionary<int, IList<Point>> OppSnake { get; set; } = [];

    /// <summary>
    /// Passability of each grid cell. Key is <c>Point(row, col)</c> = <c>Point(Y, X)</c>
    /// in game coordinates where X=column and Y=row (top=0).
    /// Value is <c>true</c> for a free cell, <c>false</c> for a platform.
    /// </summary>

    public IList<Point> Power { get; set; } = [];

    #endregion

    private StringBuilder sbAction = new();
    private Stopwatch sw = new();

    public void ResetRound(int turn)
    {
        Turn = turn;
        Power.Clear();
        sbAction.Clear();
        sw.Restart();
        MySnake.Clear();
        OppSnake.Clear();

        ParseRoundInput();
    }

    public void AddAction(string action)
    {
        sbAction.Append(action);
    }

    public void DoAction()
    {
        if (sbAction.Length == 0)
        {
            Console.WriteLine("WAIT");
            return;
        }

        var strAction = sbAction.ToString();
        Console.WriteLine(strAction);
        Console.Error.WriteLine($"[{Turn}] {sw.Elapsed.TotalMilliseconds} {strAction}");
    }

    #region Input parsing (initialization)

    public void ParseInitialInput()
    {
        MyId = int.Parse(Console.ReadLine());
        Width = int.Parse(Console.ReadLine());
        Height = int.Parse(Console.ReadLine());
        for (int i = 0; i < Height; i++)
        {
            string row = Console.ReadLine();
            for (int j = 0; j < row.Length; j++)
            {
                if (row[j] == '#')
                {
                    Platforms.Add(new Point(i, j));
                }
            }
        }
        NumberSnake = int.Parse(Console.ReadLine());
        for (int i = 0; i < NumberSnake; i++)
        {
            int mySnakebotId = int.Parse(Console.ReadLine());
            MySnakeIds.Add(mySnakebotId);
        }
        // We don't need to know Opponent snake IDs. We can identify it by MySnakeIds
        for (int i = 0; i < NumberSnake; i++)
        {
            _ = Console.ReadLine();
        }
    }

    public void ParseRoundInput()
    {
        int powerSourceCount = int.Parse(Console.ReadLine());
        string[] inputs;
        for (int i = 0; i < powerSourceCount; i++)
        {
            inputs = Console.ReadLine().Split(' ');
            int x = int.Parse(inputs[0]);
            int y = int.Parse(inputs[1]);
            Power.Add(new Point(x, y));
        }
        int snakebotCount = int.Parse(Console.ReadLine());
        for (int i = 0; i < snakebotCount; i++)
        {
            inputs = Console.ReadLine().Split(' ');
            int snakebotId = int.Parse(inputs[0]);
            string bodyStr = inputs[1];

            var body = SnakeHelper.ParseBody(bodyStr);
            if (MySnakeIds.Contains(snakebotId))
                MySnake[snakebotId] = body;
            else
                OppSnake[snakebotId] = body;
        }
    }

    #endregion
}

public static class SnakeHelper
{
    /// <summary>Parses a colon-separated body string into a head-first list of Points.</summary>
    /// <example>"0,1:1,1:2,1" → [(0,1),(1,1),(2,1)]</example>
    public static IList<Point> ParseBody(string body) =>
        body.Split(':').Select(p =>
        {
            var nums = p.Split(',').Select(int.Parse).ToArray();
            return new Point(nums[0], nums[1]);
        }).ToList();


    /// <summary>Enumerates all directions the snake may turn (excludes immediate 180° reversal).</summary>
    public static IEnumerable<Point> GetNextDirectionAvailable(IList<Point> body)
    {
        foreach (var dir in Constants.AllPoints)
        {
            if (body[0] + (Size)dir != body[1]) yield return dir;
        }
    }

    private static string GoTo(int id, Point direction, string message = null)
    {
        return $"{id} {direction} {id}-{direction}{(!string.IsNullOrEmpty(message) ? ":" : "")}{message};";
    }
}

public class Constants
{
    public static readonly Point UP = new Point(0, -1);
    public static readonly Point DOWN = new Point(0, 1);
    public static readonly Point LEFT = new Point(-1, 0);
    public static readonly Point RIGHT = new Point(1, 0);

    public static readonly IList<Point> AllPoints = [UP, DOWN, LEFT, RIGHT];
}

// ---------------------------------------------------------------------------
// Move simulation
// ---------------------------------------------------------------------------

public static class MoveSimulator
{
    /// <summary>
    /// Returns the position the snake head would occupy after taking <paramref name="move"/>.
    /// Coordinate system: X = column (0 = left), Y = row (0 = top).
    /// </summary>
    public static Point SimulateNextPosition(Point head, Point move) => head + move;

    /// <summary>
    /// Returns all body positions after the snake takes one move step.
    /// The head advances and the rest of the body follows (tail is dropped).
    /// </summary>
    public static IList<Point> SimulateBodyAfterMove(IList<Point> body, Point move)
    {
        var result = new List<Point>(body.Count);
        result.Add(SimulateNextPosition(body[0], move));
        for (int i = 0; i < body.Count - 1; i++)
            result.Add(body[i]);
        return result;
    }

    /// <summary>
    /// Simulates gravity on a snake body: the whole body falls downward (Y+1 each step)
    /// until at least one body part has a solid cell directly below it.
    /// Returns the settled body positions, or <c>null</c> if the snake falls entirely
    /// off the bottom of the map and is removed.
    /// <para>
    /// Solid cells are: platforms, power sources, and other active snakes' body parts.
    /// The snake's own body does not support itself during a gravity fall.
    /// </para>
    /// </summary>
    public static IList<Point> ApplyGravity(IList<Point> body, Board board, int snakeId)
    {
        var otherBodies = new HashSet<Point>(
            board.MySnake
                .Concat(board.OppSnake)
                .Where(kvp => kvp.Key != snakeId && kvp.Value != null)
                .SelectMany(kvp => kvp.Value)
        );

        var current = body.ToList();
        // A snake can fall at most board.Height rows before leaving the map
        for (int step = 0; step < board.Height; step++)
        {
            if (IsGrounded(current, board, otherBodies))
                return current;

            var next = current.Select(p => new Point(p.X, p.Y + 1)).ToList();
            // If all body parts have left the map, the snake is removed
            if (next.All(p => p.Y >= board.Height))
                return null;

            current = next;
        }
        return null;
    }

    /// <summary>
    /// Convenience wrapper: simulates a single move then applies gravity.
    /// Returns the post-gravity head position, or <c>null</c> if the snake falls off the map.
    /// </summary>
    public static Point? SimulateHeadAfterGravity(Board board, int snakeId, IList<Point> body, Point move)
    {
        var bodyAfterMove = SimulateBodyAfterMove(body, move);
        var settled = ApplyGravity(bodyAfterMove, board, snakeId);
        return settled?[0];
    }

    private static bool IsGrounded(IList<Point> body, Board board, HashSet<Point> otherBodies)
    {
        foreach (var part in body)
        {
            var below = new Point(part.X, part.Y + 1);
            if (below.X < 0 || below.X >= board.Width) continue; // outside the map horizontally
            if (below.Y >= board.Height) continue;                // outside the map vertically

            // Platform – map is stored as Point(row, col) = Point(Y, X) in game coordinates
            var mapKey = new Point(below.Y, below.X);
            if (board.Platforms.Contains(mapKey))
                return true;

            // Power source (solid according to the problem rules)?
            if (board.Power.Contains(below))
                return true;

            // Other snake body part?
            if (otherBodies.Contains(below))
                return true;
        }
        return false;
    }
}
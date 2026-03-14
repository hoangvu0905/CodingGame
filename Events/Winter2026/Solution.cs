using System;
using System.Linq;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Diagnostics;

namespace CodingGame.Winter2026;

/**
 * Auto-generated code below aims at helping you parse
 * the standard input according to the problem statement.
 **/


class Player
{
    static void Main(string[] args)
    {
        var board = new Board();

        var scoringStrategy = new CompositeScoringStrategy(
        [
            new DangerPenaltyScore(),
            new DistanceToPowerScore(),
            new PowerEatBonusScore(),
        ]);

        string[] inputs;
        board.MyId = int.Parse(Console.ReadLine());
        board.Width = int.Parse(Console.ReadLine());
        board.Height = int.Parse(Console.ReadLine());
        for (int i = 0; i < board.Height; i++)
        {
            string row = Console.ReadLine();
            for (int j = 0; j < row.Length; j++)
            {
                board.Map.Add(new Point(i, j), row[j] == '.');
            }
        }
        board.NumberSnake = int.Parse(Console.ReadLine());
        for (int i = 0; i < board.NumberSnake; i++)
        {
            int mySnakebotId = int.Parse(Console.ReadLine());
            board.MySnakeIds.Add(mySnakebotId);
        }
        for (int i = 0; i < board.NumberSnake; i++)
        {
            int oppSnakebotId = int.Parse(Console.ReadLine());
            board.OppSnakeIds.Add(oppSnakebotId);
        }

        // game loop
        while (true)
        {
            board.ResetRound();

            int powerSourceCount = int.Parse(Console.ReadLine());
            for (int i = 0; i < powerSourceCount; i++)
            {
                inputs = Console.ReadLine().Split(' ');
                int x = int.Parse(inputs[0]);
                int y = int.Parse(inputs[1]);
                board.Power.Add(new Point(x, y));
            }
            int snakebotCount = int.Parse(Console.ReadLine());
            for (int i = 0; i < snakebotCount; i++)
            {
                inputs = Console.ReadLine().Split(' ');
                int snakebotId = int.Parse(inputs[0]);
                string bodyStr = inputs[1];

                var body = SnakeHelper.ParseBody(bodyStr);
                if (board.MySnakeIds.Contains(snakebotId))
                    board.MySnake[snakebotId] = body;
                else
                    board.OppSnake[snakebotId] = body;
            }

            // Write an action using Console.WriteLine()
            // To debug: Console.Error.WriteLine("Debug messages...");

            foreach (var (id, body) in board.MySnake)
            {
                board.AddAction(SnakeHelper.GetActions(id, body, board, scoringStrategy));
            }

            board.DoAction();
        }
    }
}

public class Board
{
    public int MyId { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public int Turn { get; set; }

    public int NumberSnake { get; set; }

    /// <summary>IDs registered during initialization for each side (stable across rounds).</summary>
    public HashSet<int> MySnakeIds { get; set; } = [];
    public HashSet<int> OppSnakeIds { get; set; } = [];

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
    public Dictionary<Point, bool> Map { get; set; } = [];

    public IList<Point> Power { get; set; } = [];

    private StringBuilder sbAction = new();
    private Stopwatch sw = new();

    public void ResetRound()
    {
        Turn++;
        Power.Clear();
        sbAction.Clear();
        sw.Restart();
        MySnake.Clear();
        OppSnake.Clear();
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

    /// <summary>Returns the direction the snake is currently facing based on head and next body part.</summary>
    public static Direction GetDirection(IList<Point> body)
    {
        if (body.Count < 2)
            return Direction.UP; // single-part snake — default to UP (will be removed by the game shortly)

        var first = body[0];
        var second = body[1];

        if (first.X < second.X) return Direction.LEFT;
        if (first.X > second.X) return Direction.RIGHT;

        return first.Y < second.Y ? Direction.UP : Direction.DOWN;
    }

    /// <summary>Enumerates all directions the snake may turn (excludes immediate 180° reversal).</summary>
    public static IEnumerable<Direction> GetNextDirectionAvailable(IList<Point> body)
    {
        foreach (var dir in Constants.AllDirections)
        {
            // Direction enum: UP=0, DOWN=1, LEFT=2, RIGHT=3.
            // XOR with 1 maps each direction to its opposite: UP↔DOWN (0↔1), LEFT↔RIGHT (2↔3).
            if (((int)dir ^ 1) == (int)GetDirection(body)) continue;
            yield return dir;
        }
    }

    /// <summary>Chooses and returns the best action string for this snake this turn.</summary>
    public static string GetActions(int snakeId, IList<Point> body, Board board, IMoveScoringStrategy scoringStrategy)
    {
        var dirs = GetNextDirectionAvailable(body).ToList();
        if (dirs.Count == 0) return "";

        var bestDir = dirs
            .OrderByDescending(d => scoringStrategy.Score(board, snakeId, body, d))
            .First();

        return GoTo(snakeId, bestDir);
    }

    private static string GoTo(int id, Direction direction, string message = null)
    {
        return $"{id} {direction.ToString()} {id}-{direction.ToString()}{(!string.IsNullOrEmpty(message) ? ":" : "")}{message};";
    }
}

public class Constants
{
    public static readonly IList<Direction> AllDirections = [Direction.DOWN, Direction.UP, Direction.LEFT, Direction.RIGHT];
}

public enum Direction
{
    UP = 0,
    DOWN = 1,
    LEFT = 2,
    RIGHT = 3
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
    public static Point SimulateNextPosition(Point head, Direction move) =>
        move switch
        {
            Direction.UP    => new Point(head.X, head.Y - 1),
            Direction.DOWN  => new Point(head.X, head.Y + 1),
            Direction.LEFT  => new Point(head.X - 1, head.Y),
            Direction.RIGHT => new Point(head.X + 1, head.Y),
            _               => head,
        };

    /// <summary>
    /// Returns all body positions after the snake takes one move step.
    /// The head advances and the rest of the body follows (tail is dropped).
    /// </summary>
    public static IList<Point> SimulateBodyAfterMove(IList<Point> body, Direction move)
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
    public static Point? SimulateHeadAfterGravity(Board board, int snakeId, IList<Point> body, Direction move)
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
            if (board.Map.TryGetValue(mapKey, out bool isFree) && !isFree)
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

// ---------------------------------------------------------------------------
// Scoring strategy interface & implementations
// ---------------------------------------------------------------------------

/// <summary>Evaluates how desirable a particular move is for a given snake.</summary>
public interface IMoveScoringStrategy
{
    double Score(Board state, int snakeId, IList<Point> body, Direction move);
}

/// <summary>
/// Applies a large penalty for moves that are immediately lethal:
/// out-of-bounds, into a platform, or into any snake body.
/// </summary>
public class DangerPenaltyScore : IMoveScoringStrategy
{
    private const double Penalty = -1000.0;

    public double Score(Board state, int snakeId, IList<Point> body, Direction move)
    {
        var next = MoveSimulator.SimulateNextPosition(body[0], move);

        // Out of bounds
        if (next.X < 0 || next.X >= state.Width || next.Y < 0 || next.Y >= state.Height)
            return Penalty;

        // Platform – map is stored as Point(row, col) = Point(Y, X) in game coordinates
        var mapKey = new Point(next.Y, next.X);
        if (state.Map.TryGetValue(mapKey, out bool isFree) && !isFree)
            return Penalty;

        // Collision with any active snake body
        foreach (var (id, otherBody) in state.MySnake.Concat(state.OppSnake))
        {
            if (otherBody == null) continue;

            // Own tail will move away this turn so is no longer a collision risk.
            // Growth (after eating power) adds a new tail segment that appears at the old tail
            // position, which is the segment we skip here — so the collision-free slot is correct
            // in both the normal and growth cases.
            var segments = id == snakeId ? otherBody.SkipLast(1) : otherBody.AsEnumerable();
            foreach (var segment in segments)
            {
                if (segment == next) return Penalty;
            }
        }

        // Gravity: would the snake fall entirely off the map after this move?
        var bodyAfterMove = MoveSimulator.SimulateBodyAfterMove(body, move);
        if (MoveSimulator.ApplyGravity(bodyAfterMove, state, snakeId) == null)
            return Penalty;

        return 0.0;
    }
}

/// <summary>
/// Rewards moves that bring the snake head closer to the nearest power source.
/// Uses the post-gravity head position so the distance reflects where the snake
/// actually lands after falling, not just the raw movement direction.
/// Returns the negative Manhattan distance so that a shorter distance = higher score.
/// </summary>
public class DistanceToPowerScore : IMoveScoringStrategy
{
    public double Score(Board state, int snakeId, IList<Point> body, Direction move)
    {
        if (state.Power.Count == 0) return 0.0;

        // Use the head position after gravity settles; if the snake falls off the map,
        // DangerPenaltyScore already applies a large penalty so we just return 0.
        var head = MoveSimulator.SimulateHeadAfterGravity(state, snakeId, body, move);
        if (head == null) return 0.0;

        double minDist = state.Power
            .Min(p => Math.Abs(p.X - head.Value.X) + Math.Abs(p.Y - head.Value.Y));

        return -minDist;
    }
}

/// <summary>
/// Gives a bonus when the snake head lands directly on a power source,
/// rewarding moves that immediately collect one.
/// </summary>
public class PowerEatBonusScore : IMoveScoringStrategy
{
    private const double Bonus = 500.0;

    public double Score(Board state, int snakeId, IList<Point> body, Direction move)
    {
        var next = MoveSimulator.SimulateNextPosition(body[0], move);
        return state.Power.Contains(next) ? Bonus : 0.0;
    }
}

/// <summary>
/// Aggregates multiple <see cref="IMoveScoringStrategy"/> implementations by summing
/// their individual scores, following the Composite / Strategy pattern.
/// </summary>
public class CompositeScoringStrategy : IMoveScoringStrategy
{
    private readonly IReadOnlyList<IMoveScoringStrategy> _strategies;

    public CompositeScoringStrategy(IReadOnlyList<IMoveScoringStrategy> strategies)
    {
        _strategies = strategies;
    }

    public double Score(Board state, int snakeId, IList<Point> body, Direction move) =>
        _strategies.Sum(s => s.Score(state, snakeId, body, move));
}
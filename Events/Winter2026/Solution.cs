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
            board.MySnakes.Add(mySnakebotId, new Snake() { Id = mySnakebotId });
        }
        for (int i = 0; i < board.NumberSnake; i++)
        {
            int oppSnakebotId = int.Parse(Console.ReadLine());
            board.OppSnakes.Add(oppSnakebotId, new Snake() { Id = oppSnakebotId });
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
                string body = inputs[1];

                var snake = board.MySnakes.GetValueOrDefault(snakebotId) ?? board.OppSnakes[snakebotId];
                snake.UpdateBody(body);
            }

            // Write an action using Console.WriteLine()
            // To debug: Console.Error.WriteLine("Debug messages...");

            foreach (var (id, snake) in board.MySnakes)
            {
                if (snake.IsActive)
                    board.AddAction(snake.GetActions(board, scoringStrategy));
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
    public Dictionary<int, Snake> MySnakes { get; set; } = [];
    public Dictionary<int, Snake> OppSnakes { get; set; } = [];

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
        foreach (var snake in MySnakes.Values.Concat(OppSnakes.Values))
            snake.IsActive = false;
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


public class Snake
{
    public int Id { get; set; }
    public bool IsActive { get; set; }
    public IList<Point> Body { get; set; }

    // 0,1:1,1:2,1
    public void UpdateBody(string body)
    {
        IsActive = true;
        Body = body.Split(':').Select(p =>
        {
            var nums = p.Split(',').Select(int.Parse).ToArray();
            return new Point(nums[0], nums[1]);
        }).ToList();
    }

    public string GetActions(Board board, IMoveScoringStrategy scoringStrategy)
    {
        var dirs = GetNextDirectionAvailable().ToList();
        if (dirs.Count == 0) return "";

        var bestDir = dirs
            .OrderByDescending(d => scoringStrategy.Score(board, this, d))
            .First();

        return GoTo(bestDir);
    }

    private string GoTo(Direction direction, string message = null)
    {
        return $"{Id} {direction.ToString()} {Id}-{direction.ToString()}{(!string.IsNullOrEmpty(message) ? ":": "")}{message};";
    }

    public Direction GetDirection()
    {
        var first = Body[0];
        var second = Body[1];

        if (first.X < second.X) return Direction.LEFT;
        if (first.X > second.X) return Direction.RIGHT;

        return first.Y < second.Y ? Direction.UP : Direction.DOWN;
    }

    public IEnumerable<Direction> GetNextDirectionAvailable()
    {
        foreach (var dir in Constants.AllDirections)
        {
            if (((int)dir ^ 1) == (int)GetDirection()) continue;
            yield return dir;
        }
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
            board.MySnakes.Values
                .Concat(board.OppSnakes.Values)
                .Where(s => s.IsActive && s.Id != snakeId && s.Body != null)
                .SelectMany(s => s.Body)
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
    public static Point? SimulateHeadAfterGravity(Board board, Snake snake, Direction move)
    {
        var bodyAfterMove = SimulateBodyAfterMove(snake.Body, move);
        var settled = ApplyGravity(bodyAfterMove, board, snake.Id);
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
    double Score(Board state, Snake snake, Direction move);
}

/// <summary>
/// Applies a large penalty for moves that are immediately lethal:
/// out-of-bounds, into a platform, or into any snake body.
/// </summary>
public class DangerPenaltyScore : IMoveScoringStrategy
{
    private const double Penalty = -1000.0;

    public double Score(Board state, Snake snake, Direction move)
    {
        var next = MoveSimulator.SimulateNextPosition(snake.Body[0], move);

        // Out of bounds
        if (next.X < 0 || next.X >= state.Width || next.Y < 0 || next.Y >= state.Height)
            return Penalty;

        // Platform – map is stored as Point(row, col) = Point(Y, X) in game coordinates
        var mapKey = new Point(next.Y, next.X);
        if (state.Map.TryGetValue(mapKey, out bool isFree) && !isFree)
            return Penalty;

        // Collision with any active snake body
        foreach (var s in state.MySnakes.Values.Concat(state.OppSnakes.Values))
        {
            if (!s.IsActive || s.Body == null) continue;

            // Own tail will move away this turn, so skip it; all other segments stay
            var segments = s.Id == snake.Id ? s.Body.SkipLast(1) : s.Body.AsEnumerable();
            foreach (var segment in segments)
            {
                if (segment == next) return Penalty;
            }
        }

        // Gravity: would the snake fall entirely off the map after this move?
        var bodyAfterMove = MoveSimulator.SimulateBodyAfterMove(snake.Body, move);
        if (MoveSimulator.ApplyGravity(bodyAfterMove, state, snake.Id) == null)
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
    public double Score(Board state, Snake snake, Direction move)
    {
        if (state.Power.Count == 0) return 0.0;

        // Use the head position after gravity settles; if the snake falls off the map,
        // DangerPenaltyScore already applies a large penalty so we just return 0.
        var head = MoveSimulator.SimulateHeadAfterGravity(state, snake, move);
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

    public double Score(Board state, Snake snake, Direction move)
    {
        var next = MoveSimulator.SimulateNextPosition(snake.Body[0], move);
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

    public double Score(Board state, Snake snake, Direction move) =>
        _strategies.Sum(s => s.Score(state, snake, move));
}
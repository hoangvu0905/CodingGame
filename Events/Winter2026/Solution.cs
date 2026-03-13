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
                board.AddAction(snake.GetActions());
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
    public IList<Point> Body { get; set; }

    // 0,1:1,1:2,1
    public void UpdateBody(string body)
    {
        Body = body.Split(':').Select(p =>
        {
            var nums = p.Split(',').Select(int.Parse).ToArray();
            return new Point(nums[0], nums[1]);
        }).ToList();
    }

    public string GetActions()
    {
        var dirs = GetNextDirectionAvailable().ToList();
        if (dirs.Count > 0)
        {
            return GoTo(dirs[0]);
        }

        return "";
    }

    private string GoTo(Direction direction, string messsage = null)
    {
        return $"{Id} {direction.ToString()} {Id}-{direction.ToString()}{(!string.IsNullOrEmpty(messsage) ? ":": "")}{messsage};";
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
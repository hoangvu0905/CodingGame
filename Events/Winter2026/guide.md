# Winter Challenge 2026 — AI Guide

## Problem Overview

This is a **2-player competitive snake game** played on a 2D grid viewed from the side.  
Each player controls a team of **snakebots** (snake-like robots). The goal is to have more total body parts across all your snakebots than your opponent when the game ends.

---

## Key Concepts

### Grid
- A rectangular grid with dimensions **15–45 wide × 10–30 tall**.
- Cells are either **platforms** (`#`) — impassable — or **free** (`.`).
- The grid is viewed from the **side**, so **gravity** applies.

### Snakebots
- Each snakebot is a sequence of body parts occupying adjacent cells; the first part is the **head**.
- A snakebot is affected by gravity: at least one body part must rest on something solid (platform, power source, or another snakebot), otherwise it falls.
- If a snakebot falls completely off the grid, it is removed.

### Power Sources
- Scattered across the grid.
- When a snakebot's head moves into a power source cell, the snake **eats** it: the snake grows by one part and the power source disappears (cell becomes free).
- Multiple snakebots can eat the same power source simultaneously.

### Movement Rules (per turn)
1. Each snakebot moves one cell in its current direction (default: **UP** on first turn).
2. **Collision with platform/body part**: the head is destroyed; the next part becomes the new head. If fewer than 3 parts remain, the entire snakebot is removed.
3. **Collision with power source**: the snake eats it (grows +1, cell becomes free).
4. After all moves and removals, all snakebots **fall** downward until resting on something solid.

### Actions (per turn output)
- `id UP` / `id DOWN` / `id LEFT` / `id RIGHT` — set direction for snake `id`.
- `MARK x y` — debug marker (up to 4 per turn).
- `WAIT` — do nothing.
- Multiple actions are separated by `;`.

### Game End
The game ends when any of these conditions are met:
- All of a player's snakebots are removed.
- No power sources remain.
- 200 turns have passed.

**Winner**: the player with more total body parts across all surviving snakebots.

---

## Input / Output Protocol

### Initialization (once)
```
myId          # 0 or 1
width
height
<height rows of the grid, each row has width chars: '#' or '.'>
snakebotsPerPlayer
<snakeBotsPerPlayer lines: one integer snakebotId each — YOUR snakes>
<snakeBotsPerPlayer lines: one integer snakebotId each — OPPONENT snakes>
```

### Each Turn
**Input:**
```
powerSourceCount
<powerSourceCount lines: x y>
snakebotCount
<snakebotCount lines: snakebotId body>
  # body format: "x,y:x,y:x,y" (head first)
```

**Output:** A single line with at least one action, e.g.:
```
1 LEFT;2 RIGHT;MARK 12 2
```

### Constraints
- Response time ≤ **50 ms** per turn (≤ **1000 ms** for turn 1).

---

## Solution Approach

The current solution uses a **simple greedy strategy**:

### Data Structures
| Class | Purpose |
|-------|---------|
| `Board` | Holds the full game state: grid map, snake dictionaries, power sources, turn counter, action builder |
| `Snake` | Represents one snakebot: id, body (list of `Point`), direction inference, action selection |
| `Direction` (enum) | UP=0, DOWN=1, LEFT=2, RIGHT=3 |
| `Constants` | Provides the ordered list of all directions |

### Turn Loop
1. **Parse** incoming power source positions and update all snake bodies.
2. For each **own snake**, call `GetActions()`:
   - Compute the current direction from the head and the second body part.
   - Enumerate all 4 directions and filter out the **opposite** direction (a snake cannot reverse).
   - Pick the **first available non-reverse direction** and emit the move command.
3. Output all collected actions joined together; if none, emit `WAIT`.

### Direction Logic
Direction is encoded as an integer (UP=0, DOWN=1, LEFT=2, RIGHT=3). The opposite of a direction `d` is `d ^ 1` (XOR with 1), which flips between UP↔DOWN and LEFT↔RIGHT.

```csharp
if (((int)dir ^ 1) == (int)GetDirection()) continue; // skip reverse direction
```

### Current Limitations / Ideas for Improvement
- **No pathfinding**: the bot does not navigate toward the nearest power source. Adding BFS/A* to target power sources would significantly improve performance.
- **No gravity simulation**: the bot does not predict where it will land after falling, which can lead to unexpected collisions.
- **No opponent awareness**: the bot ignores opponent snakebots. Predicting opponent moves and avoiding or blocking them would add a competitive edge.
- **No multi-snake coordination**: when controlling multiple snakebots, they could be directed toward different power sources to maximize coverage.
- **Scoring heuristic**: evaluating board states by total body-part count difference would help choose better moves.

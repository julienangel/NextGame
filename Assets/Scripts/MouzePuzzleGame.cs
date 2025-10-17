using Unity.Collections;
using Unity.Mathematics;

public class MousePuzzleGame
{
    private NativeArray<BurstSolver.BoardCell> board;
    private int2 currentPos;
    private int2 finishPos;
    private int boardSize;

    public void Initialize(Level level)
    {
        boardSize = level.BoardSize.x;
        board = new NativeArray<BurstSolver.BoardCell>(boardSize * boardSize, Allocator.Persistent);

        // A mesma lógica de inicialização do LevelSolver
        for (int i = 0; i < board.Length; i++)
            board[i] = new BurstSolver.BoardCell { Value = 0, AllowedIn = Direction.All, AllowedOut = Direction.All };
        int finishIdx = level.FinishPos.y * boardSize + level.FinishPos.x;
        board[finishIdx] = new BurstSolver.BoardCell
            { Value = -1, AllowedIn = Direction.All, AllowedOut = Direction.None };
        foreach (var p in level.PiecesInfo)
        {
            int idx = p.Position.y * boardSize + p.Position.x;
            board[idx] = new BurstSolver.BoardCell
                { Value = (sbyte)p.Value, AllowedIn = Direction.All, AllowedOut = Direction.All };
        }

        foreach (var p in level.DirectionalPiecesInfo)
        {
            int idx = p.Position.y * boardSize + p.Position.x;
            board[idx] = new BurstSolver.BoardCell
                { Value = (sbyte)p.Value, AllowedIn = p.AllowedIn, AllowedOut = p.AllowedOut };
        }

        currentPos = level.MousePos;
        IsFinished = false;
        finishPos = level.FinishPos;
    }

    public bool TryMove(MoveDirection direction)
    {
        if (IsFinished || direction == MoveDirection.None) return false;

        int2 targetPos = direction switch
        {
            MoveDirection.Up => currentPos + new int2(0, 1),
            MoveDirection.Down => currentPos + new int2(0, -1),
            MoveDirection.Left => currentPos + new int2(-1, 0),
            MoveDirection.Right => currentPos + new int2(1, 0),
            _ => currentPos
        };

        if (targetPos.x < 0 || targetPos.x >= boardSize || targetPos.y < 0 || targetPos.y >= boardSize) return false;

        int currentIdx = currentPos.y * boardSize + currentPos.x;
        int targetIdx = targetPos.y * boardSize + targetPos.x;

        var currentCell = board[currentIdx];
        var targetCell = board[targetIdx];

        var moveFlag = ToDirectionFlag(direction);
        var oppositeFlag = ToDirectionFlag(GetOpposite(direction));

        if ((currentCell.AllowedOut & moveFlag) == 0) return false;
        if ((targetCell.AllowedIn & oppositeFlag) == 0) return false;

        if (targetCell.Value == 0) return false;

        if (targetCell.Value == -1)
        {
            if (CanFinishNow())
            {
                var modifiedCell = board[currentIdx];
                modifiedCell.Value--;
                board[currentIdx] = modifiedCell;
                IsFinished = true;
                currentPos = targetPos;
                return true;
            }

            return false;
        }

        var newCurrentCell = board[currentIdx];
        newCurrentCell.Value--;
        board[currentIdx] = newCurrentCell;
        currentPos = targetPos;
        return true;
    }

    private bool CanFinishNow()
    {
        if (board[currentPos.y * boardSize + currentPos.x].Value != 1) return false;
        int positiveCount = 0;
        for (int i = 0; i < board.Length; i++)
        {
            if (board[i].Value > 0) positiveCount++;
        }

        return positiveCount == 1;
    }

    public BurstSolver.BoardCell GetCell(int2 pos) => board[pos.y * boardSize + pos.x];
    public bool IsFinished { get; private set; }
    public int2 CurrentPosition => currentPos;

    public void Dispose()
    {
        if (board.IsCreated) board.Dispose();
    }

    #region Helpers

    private Direction ToDirectionFlag(MoveDirection d) => d switch
    {
        MoveDirection.Up => Direction.Up, MoveDirection.Down => Direction.Down,
        MoveDirection.Left => Direction.Left, MoveDirection.Right => Direction.Right, _ => Direction.None
    };

    private MoveDirection GetOpposite(MoveDirection d) => d switch
    {
        MoveDirection.Up => MoveDirection.Down, MoveDirection.Down => MoveDirection.Up,
        MoveDirection.Left => MoveDirection.Right, MoveDirection.Right => MoveDirection.Left,
        _ => MoveDirection.None
    };

    #endregion
}
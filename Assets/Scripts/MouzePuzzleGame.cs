using Unity.Collections;
using Unity.Mathematics;
using System; // Necessário para EventArgs e Action
using System.Collections.Generic; // Necessário para Stack
using UnityEngine; // Necessário para Debug.Log

public class MousePuzzleGame : IDisposable
{
    // --- Campos de Estado ---
    private NativeArray<BurstSolver.BoardCell> board;
    private int2 currentPos;
    private int2 finishPos;
    private int boardSize;
    private bool isInitialized = false;

    // --- Campos para Reset ---
    private NativeList<PieceInfo> initialPiecesInfo;
    private NativeList<DirectionalPieceInfo> initialDirectionalPiecesInfo;
    private int2 initialMousePos;
    private int2 initialFinishPos; // Guarda também a posição final inicial

    // --- Campos para Undo ---
    private struct MoveRecord
    {
        public int changedCellIndex; // Índice da célula que foi decrementada
        public sbyte valueBeforeDecrement; // O valor dessa célula ANTES de ser decrementada
        public int2 previousCursorPos; // Onde o cursor estava ANTES do movimento
        public bool wasGameFinished; // O jogo terminou com este movimento?
    }

    private Stack<MoveRecord> moveHistory = new();

    // --- Eventos Públicos ---
    public event Action OnGameWon;
    public event Action OnLevelReset;

    public class UndoEventArgs : EventArgs
    {
        public int2 RestoredCursorPos { get; } // Nova posição do cursor após Undo
        public int2 CellPositionToUpdate { get; } // Posição da célula cujo valor foi restaurado
        public sbyte NewCellValue { get; } // Novo valor (valor original) dessa célula

        public UndoEventArgs(int2 restoredCursorPos, int2 cellPos, sbyte newValue)
        {
            RestoredCursorPos = restoredCursorPos;
            CellPositionToUpdate = cellPos;
            NewCellValue = newValue;
        }
    }

    public event Action<UndoEventArgs> OnMoveUndone;

    // --- Inicialização e Limpeza ---

    public void Initialize(Level level)
    {
        DisposePreviousState(); // Limpa estado anterior se houver

        boardSize = level.BoardSize.x;
        board = new NativeArray<BurstSolver.BoardCell>(boardSize * boardSize, Allocator.Persistent);

        // Guarda o estado inicial para o Reset
        initialMousePos = level.MousePos;
        initialFinishPos = level.FinishPos; // Guarda a posição final também
        initialPiecesInfo = new NativeList<PieceInfo>(level.PiecesInfo.Length, Allocator.Persistent);
        initialPiecesInfo.CopyFrom(level.PiecesInfo);
        initialDirectionalPiecesInfo =
            new NativeList<DirectionalPieceInfo>(level.DirectionalPiecesInfo.Length, Allocator.Persistent);
        initialDirectionalPiecesInfo.CopyFrom(level.DirectionalPiecesInfo);

        // Inicializa o tabuleiro com o estado inicial
        PopulateBoardFromState(initialPiecesInfo, initialDirectionalPiecesInfo, initialFinishPos);

        currentPos = initialMousePos;
        finishPos = initialFinishPos; // Define a posição final para o jogo atual
        IsFinished = false;
        moveHistory.Clear();
        isInitialized = true;

        Debug.Log($"Game Initialized: Size={boardSize}x{boardSize}, StartPos={currentPos}, FinishPos={finishPos}");
    }

    private void PopulateBoardFromState(NativeList<PieceInfo> pieces, NativeList<DirectionalPieceInfo> dirPieces,
        int2 finish)
    {
        if (!board.IsCreated) return; // Segurança extra

        for (int i = 0; i < board.Length; i++)
            board[i] = new BurstSolver.BoardCell { Value = 0, AllowedIn = Direction.All, AllowedOut = Direction.All };
        int finishIdx = finish.y * boardSize + finish.x;
        board[finishIdx] = new BurstSolver.BoardCell
            { Value = -1, AllowedIn = Direction.All, AllowedOut = Direction.None };
        foreach (var p in pieces)
        {
            int idx = p.Position.y * boardSize + p.Position.x;
            if (idx >= 0 && idx < board.Length)
                board[idx] = new BurstSolver.BoardCell
                    { Value = p.Value, AllowedIn = Direction.All, AllowedOut = Direction.All };
        }

        foreach (var p in dirPieces)
        {
            int idx = p.Position.y * boardSize + p.Position.x;
            if (idx >= 0 && idx < board.Length)
                board[idx] = new BurstSolver.BoardCell
                    { Value = p.Value, AllowedIn = p.AllowedIn, AllowedOut = p.AllowedOut };
        }
    }

    public void Dispose()
    {
        DisposePreviousState();
    }

    private void DisposePreviousState()
    {
        if (board.IsCreated) board.Dispose();
        if (initialPiecesInfo.IsCreated) initialPiecesInfo.Dispose();
        if (initialDirectionalPiecesInfo.IsCreated) initialDirectionalPiecesInfo.Dispose();
        isInitialized = false;
        moveHistory.Clear(); // Limpa histórico ao fazer dispose
    }

    // --- Lógica de Jogo ---

    public bool TryMove(MoveDirection direction)
    {
        if (IsFinished || direction == MoveDirection.None || !isInitialized) return false;

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

        // Verifica se os índices são válidos (segurança extra)
        if (currentIdx < 0 || currentIdx >= board.Length || targetIdx < 0 || targetIdx >= board.Length) return false;

        var currentCell = board[currentIdx];
        var targetCell = board[targetIdx];

        var moveFlag = ToDirectionFlag(direction);
        var oppositeFlag = ToDirectionFlag(GetOpposite(direction));

        if ((currentCell.AllowedOut & moveFlag) == 0) return false;
        if ((targetCell.AllowedIn & oppositeFlag) == 0) return false;

        if (targetCell.Value == 0) return false;

        // --- Movimento para o Finisher ---
        if (targetCell.Value == -1)
        {
            if (CanFinishNow())
            {
                var record = new MoveRecord
                {
                    changedCellIndex = currentIdx,
                    valueBeforeDecrement = board[currentIdx].Value,
                    previousCursorPos = currentPos,
                    wasGameFinished = true
                };
                moveHistory.Push(record);

                var modifiedCurrentCell = board[currentIdx];
                modifiedCurrentCell.Value--;
                board[currentIdx] = modifiedCurrentCell;

                currentPos = targetPos;
                IsFinished = true;
                OnGameWon?.Invoke();
                return true;
            }
            else
            {
                return false;
            }
        }

        // --- Movimento Normal ---
        var normalRecord = new MoveRecord
        {
            changedCellIndex = currentIdx,
            valueBeforeDecrement = currentCell.Value,
            previousCursorPos = currentPos,
            wasGameFinished = false
        };
        moveHistory.Push(normalRecord);

        var modifiedCellNormal = board[currentIdx];
        modifiedCellNormal.Value--;
        board[currentIdx] = modifiedCellNormal;

        currentPos = targetPos;
        return true;
    }

    private bool CanFinishNow()
    {
        int currentIdx = currentPos.y * boardSize + currentPos.x;
        if (currentIdx < 0 || currentIdx >= board.Length) return false; // Segurança

        if (board[currentIdx].Value != 1) return false;
        if (CountPositiveCellsWithValueOne() != 1) return false;

        // A verificação de adjacência já está implícita porque TryMove só chega aqui
        // se o targetPos for o finishPos (targetCell.Value == -1).

        return true;
    }

    private int CountPositiveCellsWithValueOne()
    {
        if (!board.IsCreated) return -1;
        int count = 0;
        for (int i = 0; i < board.Length; i++)
        {
            if (board[i].Value == 1) count++;
            else if (board[i].Value > 1) return -1; // Otimização: falha rápida
        }

        return count;
    }


    // --- Funcionalidades Adicionais ---

    public void ResetLevel()
    {
        if (!isInitialized) return;

        // Recria o tabuleiro a partir do estado inicial guardado
        PopulateBoardFromState(initialPiecesInfo, initialDirectionalPiecesInfo, initialFinishPos);

        currentPos = initialMousePos;
        finishPos = initialFinishPos; // Restaura finishPos também
        IsFinished = false;
        moveHistory.Clear();

        OnLevelReset?.Invoke();
        Debug.Log("Level Reset!");
    }

    public void UndoLastMove()
    {
        if (moveHistory.Count == 0 || !isInitialized) return;

        var lastMove = moveHistory.Pop();

        // Verifica se o índice é válido (segurança)
        if (lastMove.changedCellIndex < 0 || lastMove.changedCellIndex >= board.Length) return;

        var restoredCell = board[lastMove.changedCellIndex];
        restoredCell.Value = lastMove.valueBeforeDecrement;
        board[lastMove.changedCellIndex] = restoredCell;

        currentPos = lastMove.previousCursorPos;

        if (lastMove.wasGameFinished)
        {
            IsFinished = false;
        }

        var eventArgs = new UndoEventArgs(
            currentPos,
            IndexToPosition(lastMove.changedCellIndex),
            lastMove.valueBeforeDecrement
        );
        OnMoveUndone?.Invoke(eventArgs);
    }

    // --- Acesso ao Estado (para Hint e UI) ---

    public NativeArray<BurstSolver.BoardCell> GetCurrentBoardState(Allocator allocator)
    {
        if (!isInitialized || !board.IsCreated) return default;
        var boardCopy =
            new NativeArray<BurstSolver.BoardCell>(board.Length, allocator, NativeArrayOptions.UninitializedMemory);
        board.CopyTo(boardCopy);
        return boardCopy;
    }

    public BurstSolver.BoardCell GetCell(int2 pos)
    {
        if (!isInitialized || !IsInBounds(pos)) return default; // Segurança
        return board[pos.y * boardSize + pos.x];
    }

    public bool IsFinished { get; private set; }
    public int2 CurrentPosition => currentPos;
    public bool CanUndo => moveHistory.Count > 0;

    #region Helpers Internos

    private bool IsInBounds(int2 pos) => pos.x >= 0 && pos.x < boardSize && pos.y >= 0 && pos.y < boardSize;
    private int2 IndexToPosition(int index) => new int2(index % boardSize, index / boardSize);

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
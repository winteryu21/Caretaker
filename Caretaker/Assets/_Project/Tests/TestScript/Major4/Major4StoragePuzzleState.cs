using System;

using Unity.Netcode;
using UnityEngine;

namespace Caretaker.Presentation
{
    /// <summary>
    /// Stores Major 4 storage puzzle progress and synchronizes opened future cells.
    /// </summary>
    [AddComponentMenu("Caretaker/Puzzle/Major 4 Storage Puzzle State")]
    [DisallowMultipleComponent]
    public sealed class Major4StoragePuzzleState : NetworkBehaviour
    {
        public const int GRID_SIZE = 8;
        public const int CELL_COUNT = GRID_SIZE * GRID_SIZE;
        public const int REGION_COUNT = 4;
        public const int REGION_SIZE = 4;
        public const int TARGET_COUNT = 3;
        public const int MAX_ATTEMPTS = 10;

        [Header("Puzzle")]
        [SerializeField] private int _maxAttempts = MAX_ATTEMPTS;
        [SerializeField] private int[] _fixedTargetFutureCells = Array.Empty<int>();
        [SerializeField] private bool _randomizeTargetsOnFirstUse = true;
        [SerializeField] private bool _logAttempts = true;

        private readonly int[] _regionMap = new int[REGION_COUNT];
        private readonly int[] _rowMap = new int[REGION_SIZE];
        private readonly int[] _columnMap = new int[REGION_SIZE];

        private ulong _openedCellMask;
        private ulong _openedTargetMask;
        private ulong _targetMask;
        private int _attemptsUsed;
        private int _lastOpenedFutureCell = -1;
        private bool _lastOpenHitTarget;
        private bool _hasInitializedPuzzle;

        public event Action<Major4StoragePuzzleState> OnStateChanged;

        public int AttemptsUsed => _attemptsUsed;

        public int MaxAttempts => _maxAttempts;

        public int RemainingAttempts => Mathf.Max(0, _maxAttempts - _attemptsUsed);

        public int LastOpenedFutureCell => _lastOpenedFutureCell;

        public bool LastOpenHitTarget => _lastOpenHitTarget;

        public bool IsSolved { get; private set; }

        private void Awake()
        {
            _maxAttempts = Mathf.Max(TARGET_COUNT, _maxAttempts);
        }

        private void OnValidate()
        {
            _maxAttempts = Mathf.Max(TARGET_COUNT, _maxAttempts);
        }

        private void Start()
        {
            if (!IsNetworkSessionActive())
            {
                EnsurePuzzleInitialized();
                NotifyStateChanged();
            }
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                EnsurePuzzleInitialized();
                BroadcastStateClientRpc(_openedCellMask, _openedTargetMask, _attemptsUsed, IsSolved, -1, false);
            }
        }

        /// <summary>
        /// Requests opening the future cell mapped from the selected past computer cell.
        /// </summary>
        /// <param name="pastCellIndex">Past computer cell index in row-major 8x8 order.</param>
        public void RequestOpenFromPastCell(int pastCellIndex)
        {
            if (!IsValidCellIndex(pastCellIndex))
            {
                return;
            }

            if (IsNetworkSessionActive())
            {
                RequestOpenFromPastCellServerRpc(pastCellIndex);
                return;
            }

            ApplyOpenFromPastCell(pastCellIndex);
        }

        /// <summary>
        /// Returns true when the future cell is currently open.
        /// </summary>
        /// <param name="futureCellIndex">Future storage cell index in row-major 8x8 order.</param>
        public bool IsFutureCellOpen(int futureCellIndex)
        {
            return IsValidCellIndex(futureCellIndex) && HasBit(_openedCellMask, futureCellIndex);
        }

        /// <summary>
        /// Returns true when an opened future cell contained one of the target objects.
        /// </summary>
        /// <param name="futureCellIndex">Future storage cell index in row-major 8x8 order.</param>
        public bool IsOpenedTargetCell(int futureCellIndex)
        {
            return IsValidCellIndex(futureCellIndex) && HasBit(_openedTargetMask, futureCellIndex);
        }

        /// <summary>
        /// Returns true when the selected past cell maps to a currently opened future storage cell.
        /// </summary>
        /// <param name="pastCellIndex">Past computer cell index in row-major 8x8 order.</param>
        public bool IsPastCellOpened(int pastCellIndex)
        {
            int futureCellIndex = MapPastCellToFutureCell(pastCellIndex);
            return IsFutureCellOpen(futureCellIndex);
        }

        /// <summary>
        /// Maps a past computer cell to the future storage cell using the current region, row, and column mapping.
        /// </summary>
        /// <param name="pastCellIndex">Past computer cell index in row-major 8x8 order.</param>
        /// <returns>Mapped future storage cell index, or -1 if the index is invalid.</returns>
        public int MapPastCellToFutureCell(int pastCellIndex)
        {
            if (!IsValidCellIndex(pastCellIndex))
            {
                return -1;
            }

            EnsurePuzzleInitialized();

            int pastRow = pastCellIndex / GRID_SIZE;
            int pastColumn = pastCellIndex % GRID_SIZE;
            int pastRegion = GetRegionIndex(pastRow, pastColumn);
            int localRow = pastRow % REGION_SIZE;
            int localColumn = pastColumn % REGION_SIZE;

            int futureRegion = _regionMap[pastRegion];
            int futureRow = GetRegionBaseRow(futureRegion) + _rowMap[localRow];
            int futureColumn = GetRegionBaseColumn(futureRegion) + _columnMap[localColumn];

            return futureRow * GRID_SIZE + futureColumn;
        }

        [ServerRpc(RequireOwnership = false)]
        private void RequestOpenFromPastCellServerRpc(int pastCellIndex)
        {
            ApplyOpenFromPastCell(pastCellIndex);
        }

        [ClientRpc]
        private void BroadcastStateClientRpc(
            ulong openedCellMask,
            ulong openedTargetMask,
            int attemptsUsed,
            bool isSolved,
            int lastOpenedFutureCell,
            bool lastOpenHitTarget)
        {
            _openedCellMask = openedCellMask;
            _openedTargetMask = openedTargetMask;
            _attemptsUsed = attemptsUsed;
            IsSolved = isSolved;
            _lastOpenedFutureCell = lastOpenedFutureCell;
            _lastOpenHitTarget = lastOpenHitTarget;

            NotifyStateChanged();
        }

        private void ApplyOpenFromPastCell(int pastCellIndex)
        {
            EnsurePuzzleInitialized();

            if (IsSolved)
            {
                return;
            }

            int futureCell = MapPastCellToFutureCell(pastCellIndex);
            if (!IsValidCellIndex(futureCell) || HasBit(_openedCellMask, futureCell))
            {
                NotifyStateChanged();
                return;
            }

            _attemptsUsed++;
            _lastOpenedFutureCell = futureCell;
            _lastOpenHitTarget = HasBit(_targetMask, futureCell);
            _openedCellMask = SetBit(_openedCellMask, futureCell);

            if (_lastOpenHitTarget)
            {
                _openedTargetMask = SetBit(_openedTargetMask, futureCell);
            }

            if (CountBits(_openedTargetMask) >= TARGET_COUNT)
            {
                IsSolved = true;
            }
            else if (_attemptsUsed >= _maxAttempts)
            {
                ResetAttempts();
            }

            if (_logAttempts)
            {
                Debug.Log(
                    $"Major4 storage opened futureCell={futureCell}, hitTarget={_lastOpenHitTarget}, remaining={RemainingAttempts}.",
                    this);
            }

            BroadcastOrNotify();
        }

        private void ResetAttempts()
        {
            _openedCellMask = 0UL;
            _openedTargetMask = 0UL;
            _attemptsUsed = 0;
            _lastOpenedFutureCell = -1;
            _lastOpenHitTarget = false;
        }

        private void BroadcastOrNotify()
        {
            if (IsNetworkSessionActive() && IsServer)
            {
                BroadcastStateClientRpc(
                    _openedCellMask,
                    _openedTargetMask,
                    _attemptsUsed,
                    IsSolved,
                    _lastOpenedFutureCell,
                    _lastOpenHitTarget);
                return;
            }

            NotifyStateChanged();
        }

        private void EnsurePuzzleInitialized()
        {
            if (_hasInitializedPuzzle)
            {
                return;
            }

            InitializeMapping();
            InitializeTargets();
            _hasInitializedPuzzle = true;
        }

        private void InitializeMapping()
        {
            FillIdentity(_regionMap);
            FillIdentity(_rowMap);
            FillIdentity(_columnMap);

            Shuffle(_regionMap);
            Shuffle(_rowMap);
            Shuffle(_columnMap);
        }

        private void InitializeTargets()
        {
            _targetMask = 0UL;

            if (!_randomizeTargetsOnFirstUse && _fixedTargetFutureCells != null)
            {
                for (int i = 0; i < _fixedTargetFutureCells.Length && CountBits(_targetMask) < TARGET_COUNT; i++)
                {
                    if (IsValidCellIndex(_fixedTargetFutureCells[i]))
                    {
                        _targetMask = SetBit(_targetMask, _fixedTargetFutureCells[i]);
                    }
                }
            }

            while (CountBits(_targetMask) < TARGET_COUNT)
            {
                int targetCell = UnityEngine.Random.Range(0, CELL_COUNT);
                _targetMask = SetBit(_targetMask, targetCell);
            }
        }

        private void NotifyStateChanged()
        {
            OnStateChanged?.Invoke(this);
        }

        private bool IsNetworkSessionActive()
        {
            return NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening && IsSpawned;
        }

        private static int GetRegionIndex(int row, int column)
        {
            int regionRow = row / REGION_SIZE;
            int regionColumn = column / REGION_SIZE;
            return regionRow * 2 + regionColumn;
        }

        private static int GetRegionBaseRow(int regionIndex)
        {
            return regionIndex >= 2 ? REGION_SIZE : 0;
        }

        private static int GetRegionBaseColumn(int regionIndex)
        {
            return regionIndex % 2 == 1 ? REGION_SIZE : 0;
        }

        private static bool IsValidCellIndex(int cellIndex)
        {
            return cellIndex >= 0 && cellIndex < CELL_COUNT;
        }

        private static bool HasBit(ulong mask, int bitIndex)
        {
            return (mask & (1UL << bitIndex)) != 0UL;
        }

        private static ulong SetBit(ulong mask, int bitIndex)
        {
            return mask | (1UL << bitIndex);
        }

        private static int CountBits(ulong mask)
        {
            int count = 0;
            while (mask != 0UL)
            {
                mask &= mask - 1UL;
                count++;
            }

            return count;
        }

        private static void FillIdentity(int[] values)
        {
            for (int i = 0; i < values.Length; i++)
            {
                values[i] = i;
            }
        }

        private static void Shuffle(int[] values)
        {
            for (int i = values.Length - 1; i > 0; i--)
            {
                int swapIndex = UnityEngine.Random.Range(0, i + 1);
                (values[i], values[swapIndex]) = (values[swapIndex], values[i]);
            }
        }
    }
}

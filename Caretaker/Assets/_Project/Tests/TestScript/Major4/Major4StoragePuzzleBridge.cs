using System;

using Unity.Netcode;
using UnityEngine;

namespace Caretaker.Presentation
{
    /// <summary>
    /// Server-authoritative bridge for Major 4 storage puzzle communication between past and future UIs.
    /// </summary>
    [AddComponentMenu("Caretaker/Puzzle/Major 4 Storage Puzzle Bridge")]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NetworkObject))]
    public sealed class Major4StoragePuzzleBridge : NetworkBehaviour
    {
        public const int GRID_SIZE = 8;
        public const int CELL_COUNT = GRID_SIZE * GRID_SIZE;
        public const int REGION_COUNT = 4;
        public const int REGION_SIZE = 4;
        public const int TARGET_COUNT = 3;
        public const int MAX_ATTEMPTS = 10;

        [Header("Puzzle")]
        [SerializeField] private int _maxAttempts = MAX_ATTEMPTS;
        [SerializeField] private int[] _targetFutureCells = { 0, 10, 63 };
        [SerializeField] private bool _randomizeMappingOnSpawn = true;
        [SerializeField] private bool _logAttempts = true;

        [Header("Fixed Mapping")]
        [SerializeField] private int[] _fixedRegionMap = { 2, 3, 1, 0 };
        [SerializeField] private int[] _fixedRowMap = { 0, 1, 2, 3 };
        [SerializeField] private int[] _fixedColumnMap = { 0, 1, 2, 3 };

        private readonly int[] _regionMap = new int[REGION_COUNT];
        private readonly int[] _rowMap = new int[REGION_SIZE];
        private readonly int[] _columnMap = new int[REGION_SIZE];

        private ulong _attemptedPastMask;
        private ulong _openedFutureMask;
        private ulong _openedTargetMask;
        private ulong _targetFutureMask;
        private int _attemptsUsed;
        private int _lastOpenedFutureCell = -1;
        private bool _lastOpenHitTarget;
        private bool _hasInitializedPuzzle;

        public static Major4StoragePuzzleBridge ActiveBridge { get; private set; }

        public event Action<Major4StoragePuzzleBridge> OnStateChanged;

        public int AttemptsUsed => _attemptsUsed;

        public int MaxAttempts => _maxAttempts;

        public int RemainingAttempts => Mathf.Max(0, _maxAttempts - _attemptsUsed);

        public int LastOpenedFutureCell => _lastOpenedFutureCell;

        public bool LastOpenHitTarget => _lastOpenHitTarget;

        public bool IsSolved { get; private set; }

        private void Awake()
        {
            NormalizeConfiguration();
        }

        private void OnEnable()
        {
            ActiveBridge = this;
        }

        private void OnDisable()
        {
            if (ActiveBridge == this)
            {
                ActiveBridge = null;
            }
        }

        private void OnValidate()
        {
            NormalizeConfiguration();
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
            ActiveBridge = this;

            if (IsServer)
            {
                EnsurePuzzleInitialized();
                BroadcastStateClientRpc(
                    _attemptedPastMask,
                    _openedFutureMask,
                    _openedTargetMask,
                    _attemptsUsed,
                    IsSolved,
                    _lastOpenedFutureCell,
                    _lastOpenHitTarget);
            }
        }

        public override void OnNetworkDespawn()
        {
            if (ActiveBridge == this)
            {
                ActiveBridge = null;
            }

            base.OnNetworkDespawn();
        }

        /// <summary>
        /// Requests opening the future storage cell mapped from a past computer cell.
        /// </summary>
        /// <param name="pastCellIndex">Past computer cell index in row-major 8x8 order.</param>
        public void RequestOpenPastCell(int pastCellIndex)
        {
            if (!IsValidCellIndex(pastCellIndex))
            {
                return;
            }

            if (IsNetworkSessionActive())
            {
                RequestOpenPastCellServerRpc(pastCellIndex);
                return;
            }

            ApplyOpenPastCell(pastCellIndex);
        }

        /// <summary>
        /// Returns true when the selected past computer cell has already been attempted.
        /// </summary>
        /// <param name="pastCellIndex">Past computer cell index in row-major 8x8 order.</param>
        public bool IsPastCellAttempted(int pastCellIndex)
        {
            return IsValidCellIndex(pastCellIndex) && HasBit(_attemptedPastMask, pastCellIndex);
        }

        /// <summary>
        /// Returns true when a future storage cell is currently open.
        /// </summary>
        /// <param name="futureCellIndex">Future storage cell index in row-major 8x8 order.</param>
        public bool IsFutureCellOpen(int futureCellIndex)
        {
            return IsValidCellIndex(futureCellIndex) && HasBit(_openedFutureMask, futureCellIndex);
        }

        /// <summary>
        /// Returns true when an opened future storage cell contained a target object.
        /// </summary>
        /// <param name="futureCellIndex">Future storage cell index in row-major 8x8 order.</param>
        public bool IsOpenedTargetCell(int futureCellIndex)
        {
            return IsValidCellIndex(futureCellIndex) && HasBit(_openedTargetMask, futureCellIndex);
        }

        [ServerRpc(RequireOwnership = false)]
        private void RequestOpenPastCellServerRpc(int pastCellIndex)
        {
            ApplyOpenPastCell(pastCellIndex);
        }

        [ClientRpc]
        private void BroadcastStateClientRpc(
            ulong attemptedPastMask,
            ulong openedFutureMask,
            ulong openedTargetMask,
            int attemptsUsed,
            bool isSolved,
            int lastOpenedFutureCell,
            bool lastOpenHitTarget)
        {
            _attemptedPastMask = attemptedPastMask;
            _openedFutureMask = openedFutureMask;
            _openedTargetMask = openedTargetMask;
            _attemptsUsed = attemptsUsed;
            IsSolved = isSolved;
            _lastOpenedFutureCell = lastOpenedFutureCell;
            _lastOpenHitTarget = lastOpenHitTarget;

            NotifyStateChanged();
        }

        private void ApplyOpenPastCell(int pastCellIndex)
        {
            EnsurePuzzleInitialized();

            if (IsSolved || HasBit(_attemptedPastMask, pastCellIndex))
            {
                NotifyStateChanged();
                return;
            }

            int futureCell = MapPastCellToFutureCell(pastCellIndex);
            if (!IsValidCellIndex(futureCell))
            {
                return;
            }

            _attemptedPastMask = SetBit(_attemptedPastMask, pastCellIndex);
            _attemptsUsed++;
            _lastOpenedFutureCell = futureCell;
            _lastOpenHitTarget = HasBit(_targetFutureMask, futureCell);
            _openedFutureMask = SetBit(_openedFutureMask, futureCell);

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
                    $"Major4 bridge opened pastCell={pastCellIndex}, futureCell={futureCell}, hitTarget={_lastOpenHitTarget}, remaining={RemainingAttempts}.",
                    this);
            }

            BroadcastOrNotify();
        }

        private int MapPastCellToFutureCell(int pastCellIndex)
        {
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

        private void ResetAttempts()
        {
            _attemptedPastMask = 0UL;
            _openedFutureMask = 0UL;
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
                    _attemptedPastMask,
                    _openedFutureMask,
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
            if (_randomizeMappingOnSpawn)
            {
                FillIdentity(_regionMap);
                FillIdentity(_rowMap);
                FillIdentity(_columnMap);
                Shuffle(_regionMap);
                Shuffle(_rowMap);
                Shuffle(_columnMap);
                return;
            }

            CopyValidatedMap(_fixedRegionMap, _regionMap);
            CopyValidatedMap(_fixedRowMap, _rowMap);
            CopyValidatedMap(_fixedColumnMap, _columnMap);
        }

        private void InitializeTargets()
        {
            _targetFutureMask = 0UL;

            for (int i = 0; i < _targetFutureCells.Length && CountBits(_targetFutureMask) < TARGET_COUNT; i++)
            {
                if (IsValidCellIndex(_targetFutureCells[i]))
                {
                    _targetFutureMask = SetBit(_targetFutureMask, _targetFutureCells[i]);
                }
            }

            if (CountBits(_targetFutureMask) < TARGET_COUNT)
            {
                Debug.LogWarning(
                    $"Major4 bridge needs {TARGET_COUNT} valid target future cells. Current valid count={CountBits(_targetFutureMask)}.",
                    this);
            }
        }

        private void NormalizeConfiguration()
        {
            _maxAttempts = Mathf.Max(TARGET_COUNT, _maxAttempts);
            EnsureArraySize(ref _targetFutureCells, TARGET_COUNT);
            EnsureArraySize(ref _fixedRegionMap, REGION_COUNT);
            EnsureArraySize(ref _fixedRowMap, REGION_SIZE);
            EnsureArraySize(ref _fixedColumnMap, REGION_SIZE);
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

        private static void CopyValidatedMap(int[] source, int[] target)
        {
            bool[] usedValues = new bool[target.Length];

            for (int i = 0; i < target.Length; i++)
            {
                int value = source != null && i < source.Length ? source[i] : i;
                if (value < 0 || value >= target.Length || usedValues[value])
                {
                    value = FindFirstUnusedValue(usedValues);
                }

                target[i] = value;
                usedValues[value] = true;
            }
        }

        private static int FindFirstUnusedValue(bool[] usedValues)
        {
            for (int i = 0; i < usedValues.Length; i++)
            {
                if (!usedValues[i])
                {
                    return i;
                }
            }

            return 0;
        }

        private static void EnsureArraySize(ref int[] values, int size)
        {
            if (values == null)
            {
                values = new int[size];
            }

            if (values.Length != size)
            {
                Array.Resize(ref values, size);
            }
        }
    }
}

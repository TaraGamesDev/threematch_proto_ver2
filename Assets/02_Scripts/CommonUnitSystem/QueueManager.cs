using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;
using Sirenix.OdinInspector;

/// <summary>
/// 수평 유닛 블록 큐를 관리하는 핵심 클래스입니다.
/// 블록 구매, 배치, 병합 해결, 미스틱 레시피 감지 등의 기능을 담당합니다.
/// </summary>
public class QueueManager : MonoBehaviour
{
    [Title("Settings")]

    [Tooltip("이 크기에 맞에 알아서 유닛 블럭의 크기가 조절됨")]
    [SerializeField] private RectTransform queueContainer;        // 큐 컨테이너의 RectTransform
    [SerializeField, Min(1)] private int maxBlockCount = 9;       // 큐에 들어갈 수 있는 최대 블록 수
    [SerializeField] private float slotPadding = 6f;              // 슬롯 간 간격
    [SerializeField] private float repositionDuration = 0.2f;     // 블록 재배치 애니메이션 시간


    [Title("Generation & Pool")]
    [SerializeField] private UnitBlock unitBlockPrefab;
    [SerializeField] private SpawnProbabilitySystem spawnProbabilityConfig;
    [SerializeField] private string blockPoolKey = "UnitBlock";
    [SerializeField, Min(0)] private int prewarmBlockCount = 9;



    [Title("Merge Rules")]
    [SerializeField] private List<MergeRule> mergeRules = new List<MergeRule>
    {
        new MergeRule { id = "four-chain", requiredCount = 4, outputCount = 2, priority = 2, resultMessage = "Quad merge!" },
        new MergeRule { id = "three-chain", requiredCount = 3, outputCount = 1, priority = 1, resultMessage = "Triple merge!" }
    };
    [SerializeField] private List<MergeRule> orderedRules;

    [Title("Mythic Recipes")]
    [SerializeField] private MythicRecipeConfig mythicRecipeConfig;
    [SerializeField] private Transform mythicButtonsContainer; // 신화 소환 버튼들의 부모 컨테이너
    [SerializeField] private GameObject mythicSpawnButtonPrefab; // 신화 소환 버튼 프리팹


    // private
    private readonly List<UnitBlock> blocks = new List<UnitBlock>();           // 현재 큐에 있는 블록들
    private readonly List<Vector2> slotLocalPositions = new List<Vector2>();   // 각 슬롯의 로컬 위치
    private readonly List<GameObject> mythicButtons = new List<GameObject>();  // 생성된 신화 버튼들
    private float slotSize;                                                    // 슬롯 크기
    
    // 머지 애니메이션 관련
    private bool isProcessingMerges = false;                                   // 머지 처리 중인지 여부
    private readonly Queue<MergeAnimationData> pendingMerges = new Queue<MergeAnimationData>(); // 대기 중인 머지들

    #region Initialize

    private void Start()
    {
        if (queueContainer == null) Debug.LogError("QueueManager: Queue container is not assigned");

        UnitDatabase.Initialize(); // 유닛 데이터 확인 
        RegisterBlockPool(); // 풀에 등록 
        RecalculateSlots(); // 슬롯 위치 계산 
        CreateMythicButtons(); // 신화 버튼들 생성

        orderedRules = mergeRules.OrderByDescending(r => r.priority).ToList(); // 우선순위 순서대로 정렬
    }

    /// <summary> 슬롯 위치를 재계산합니다. 큐 컨테이너 크기에 따라 각 슬롯의 위치와 크기를 결정합니다. </summary>
    private void RecalculateSlots()
    {
        if (queueContainer == null || maxBlockCount <= 0) return;

        // 기존 슬롯 위치 초기화
        slotLocalPositions.Clear();
        
        // 컨테이너 크기 정보 가져오기
        Rect rect = queueContainer.rect;
        float slotWidth = rect.width / maxBlockCount;  // 각 슬롯의 너비
        slotSize = Mathf.Max(0f, slotWidth - slotPadding);  // 패딩을 고려한 실제 블록 크기
        float startX = rect.xMin + slotWidth * 0.5f;  // 첫 번째 슬롯의 X 위치

        // 각 슬롯의 위치 계산
        for (int i = 0; i < maxBlockCount; i++)
        {
            float xPos = startX + slotWidth * i;
            slotLocalPositions.Add(new Vector2(xPos, 0f));
        }
    }

    /// <summary> 신화 레시피 개수만큼 버튼을 자동으로 생성합니다. </summary>
    private void CreateMythicButtons()
    {
        if (mythicRecipeConfig == null || mythicButtonsContainer == null || mythicSpawnButtonPrefab == null)
        {
            Debug.LogWarning("QueueManager: Mythic button creation requires mythicRecipeConfig, mythicButtonsContainer, and mythicSpawnButtonPrefab to be assigned.");
            return;
        }

        // 기존 버튼들 정리
        foreach (var button in mythicButtons) if (button != null) DestroyImmediate(button);
        
        mythicButtons.Clear();

        // unlockWave 오름차순으로 정렬된 레시피들에 대해 버튼 생성
        var sortedRecipes = mythicRecipeConfig.ActiveRecipes.OrderBy(recipe => recipe.unlockWave).ToList();
        foreach (MythicRecipe recipe in sortedRecipes)
        {
            GameObject buttonObj = Instantiate(mythicSpawnButtonPrefab, mythicButtonsContainer);
            buttonObj.name = $"MythicButton_{recipe.Id}";
            
            MythicSpawnButton mythicButton = buttonObj.GetComponent<MythicSpawnButton>();
            if (mythicButton != null) mythicButton.Initialize(recipe, this);
            mythicButtons.Add(buttonObj);
        }

        Debug.Log($"QueueManager: Created {mythicButtons.Count} mythic spawn buttons.");
    }

    #endregion


    #region Interaction

    [Sirenix.OdinInspector.Button]
    /// <summary> 블록을 스폰합니다. </summary>
    /// <returns>스폰 성공 여부</returns>
    public bool TrySpawnBlock()
    {
        if (unitBlockPrefab == null || spawnProbabilityConfig == null) { Debug.LogWarning("QueueManager: Prefab or spawn probability config missing."); return false; }

        // 큐가 가득 찬 경우 구매 불가
        if (blocks.Count >= maxBlockCount){Debug.LogWarning("QueueManager: Queue is full"); return false; }

        if (GameManager.Instance != null && !GameManager.Instance.SpendGold(GameManager.Instance.goldCostPerBlock)) return false;

        UnitData.UnitTier tier = spawnProbabilityConfig.GetRandomTier();
        UnitData unitData = GetRandomUnitForTier(tier);
        if (unitData == null) { Debug.LogWarning($"QueueManager: No UnitData available for tier {tier}."); return false; }

        // 티어 3, 4 축하 메시지 (확률 정보 포함)
        if (tier == UnitData.UnitTier.Tier3)
        {
            float probability = spawnProbabilityConfig.GetNormalizedProbability(tier);
            UIManager.Instance?.ShowMessage($"🎉 축하합니다! {probability:F1}% 확률을 뚫고 티어 3 유닛을 획득했습니다!", 2.5f);
        }
        else if (tier == UnitData.UnitTier.Tier4)
        {
            float probability = spawnProbabilityConfig.GetNormalizedProbability(tier);
            UIManager.Instance?.ShowMessage($"🌟 대박! {probability:F1}% 확률을 뚫고 티어 4 유닛을 획득했습니다!", 2.5f);
        }

        InsertBlock(unitData, blocks.Count);
        
        // 머지 처리 중이 아닐 때만 즉시 재배치
        if (!isProcessingMerges) RelayoutQueue(onComplete: () => TryResolveQueue());
        
        return true;
    }

    /// <summary> TMP Button OnClick 이벤트용 - 블록을 스폰합니다. </summary>
    public void SpawnBlock()
    {
        TrySpawnBlock();
    }

    private UnitData GetRandomUnitForTier(UnitData.UnitTier tier)
    {
        List<UnitData> candidates = UnitDatabase.GetUnitsByTier(tier);
        if (candidates == null || candidates.Count == 0) return null;
        int index = Random.Range(0, candidates.Count);
        return candidates[index];
    }

    private void SpawnUnitFromBlock(UnitBlock block)
    {
        if (block == null || !blocks.Contains(block)) return;

        UnitManager.Instance?.SpawnUnitFromBlock(block);
        RemoveBlock(block);
        
        // 레이아웃 재정렬 후 머지 확인
        if(!isProcessingMerges) RelayoutQueue(onComplete: () => TryResolveQueue());
    }

    #endregion

    #region Block Management

    /// <summary> 새로운 블록을 지정된 위치에 삽입합니다. </summary>
    private UnitBlock InsertBlock(UnitData unitData, int index)
    {
        if (unitData == null) return null;

        UnitBlock block = GetBlockInstance();
        if (block == null){ Debug.LogWarning("QueueManager: 블록 인스턴스를 생성하지 못했습니다."); return null; }

        block.Initialize(unitData);               // 블록 데이터 적용
        block.PrepareForQueue();
        block.OnBlockClicked += SpawnUnitFromBlock; // 이벤트 구독 추가

        // 안전한 인덱스로 조정
        int safeIndex = Mathf.Clamp(index, 0, blocks.Count);
        blocks.Insert(safeIndex, block); // 블록 리스트에 삽입
        block.SetBlockSize(slotSize); // 블럭 크기 설정 
        block.GetComponent<RectTransform>().anchoredPosition = slotLocalPositions[safeIndex]; // 위치 설정 

        block.PlaySpawnAnimation(); // 스폰 애니메이션 재생

        return block;
    }

    private void RemoveBlock(UnitBlock block)
    {
        // 블록의 인덱스 찾기
        int index = blocks.IndexOf(block);
        if (index < 0) return; // 블록이 큐에 없는 경우

        // 리스트에서 제거하고 블록 정리
        blocks.RemoveAt(index);
        ReleaseBlock(block);
        
        // 레이아웃 재정렬은 호출하는 쪽에서 콜백으로 처리
    }

    #endregion

    #region Layout Management & Animation

    /// <summary> 모든 블록의 위치를 애니메이션과 함께 재정렬합니다. </summary>
    /// <param name="onComplete">애니메이션 완료 시 호출될 콜백</param>
    private void RelayoutQueue(System.Action onComplete = null)
    {
        if (blocks.Count == 0)
        {
            Debug.LogWarning("[QueueManager: RelayoutQueue] No blocks to relayout");
            onComplete?.Invoke();
            return;
        }

        // 모든 블록의 애니메이션을 병렬로 실행
        Sequence layoutSequence = DOTween.Sequence();
        for (int i = 0; i < blocks.Count; i++) layoutSequence.Join(AnimateBlockToPosition(blocks[i], i));
        
        // 모든 애니메이션 완료 후 콜백 호출
        layoutSequence.OnComplete(() => onComplete?.Invoke());
    }

    /// <summary> 블록을 지정된 위치로 애니메이션합니다. </summary>
    /// <returns>애니메이션 시퀀스</returns>
    private Tween AnimateBlockToPosition(UnitBlock block, int index)
    {
        if (block == null || queueContainer == null) return null;
        
        RectTransform rectTransform = block.GetComponent<RectTransform>();
        if (rectTransform == null) return null;

        // 부모 설정 및 순서 조정
        rectTransform.SetParent(queueContainer, false);
        rectTransform.SetAsLastSibling();
       
        return rectTransform.DOAnchorPos(slotLocalPositions[index], repositionDuration).SetEase(Ease.OutQuad);  // 애니메이션 실행
    }

    #endregion

    #region Queue Resolution(Merge and Mythic Recipe)

    /// <summary> 큐의 병합과 미스틱 레시피를 해결합니다. 애니메이션 기반으로 순차적으로 처리됩니다. </summary>
    private void TryResolveQueue()
    {
        // 이미 머지 처리 중이면 pass
        if (isProcessingMerges) return;

        // 모든 가능한 머지를 찾아서 대기열에 추가
        FindAndQueueMerges();
        
        // 대기열에 머지가 있으면 처리 시작
        if (pendingMerges.Count > 0) ProcessNextMerge(); 
    }

    /// <summary> 모든 가능한 머지를 찾아서 대기열에 추가합니다. </summary>
    private void FindAndQueueMerges()
    {
        CheckMythicRecipes(); // 신화 레시피 확인 및 버튼 상태 업데이트
        FindAndQueueNormalMerges(); // 일반 머지 찾기
    }

    /// <summary> 일반 머지를 찾아서 대기열에 추가합니다. </summary>
    private void FindAndQueueNormalMerges()
    {
        if (blocks.Count < 2){ Debug.LogWarning("[QueueManager: FindAndQueueNormalMerges] Not enough blocks to merge"); return; }
        
        for (int i = 0; i < blocks.Count; )
        {
            UnitBlock block = blocks[i];
            if (block == null || block.UnitData == null) { i++; continue; }

            // 연속된 같은 유닛들의 개수 찾기
            int streakCount = 1;
            for (int j = i + 1; j < blocks.Count; j++)
            {
                if (blocks[j] != null && blocks[j].UnitData == block.UnitData) streakCount++;
                else break;
            }

            foreach (MergeRule rule in orderedRules)
            {
                if (streakCount >= rule.requiredCount)
                {
                    // 머지할 블록들 수집
                    List<UnitBlock> blocksToMerge = new List<UnitBlock>();
                    for (int k = 0; k < rule.requiredCount; k++) blocksToMerge.Add(blocks[i + k]);

                    // 다음 티어 유닛 가져오기
                    UnitData nextTier = UnitDatabase.GetNextTierUnit(block.UnitData);
                    if (nextTier != null)
                    {
                        string message = rule.resultMessage.Replace("{unit}", nextTier.unitName);
                        MergeAnimationData mergeData = new MergeAnimationData(blocksToMerge, nextTier, rule.outputCount, i, message, isMythic:false);
                        pendingMerges.Enqueue(mergeData);
                        return; // 한 번에 하나의 머지만 큐에 추가
                    }
                }
            }

            i += streakCount;
        }
    }

    /// <summary> 신화 레시피를 확인하고 버튼 상태를 업데이트합니다. </summary>
    private void CheckMythicRecipes()
    {
        if (mythicRecipeConfig == null || mythicButtonsContainer == null) return;

        // 우선 모든 신화 레시피 버튼들을 비활성화
        foreach (var buttonObj in mythicButtons)
        {
            if (buttonObj == null) continue;
            
            MythicSpawnButton mythicButton = buttonObj.GetComponent<MythicSpawnButton>();
            if (mythicButton != null) mythicButton.SetAvailable(false);
        }

        // 각 신화 레시피 확인
        foreach (MythicRecipe recipe in mythicRecipeConfig.ActiveRecipes)
        {
            int window = recipe.Sequence.Count;
            if (window == 0 || recipe.ResultUnit == null) continue;

            for (int start = 0; start <= blocks.Count - window; start++)
            {
                bool match = true;
                for (int offset = 0; offset < window; offset++)
                {
                    if (blocks[start + offset]?.UnitData != recipe.Sequence[offset])
                    {
                        match = false;
                        break;
                    }
                }

                // 조합 조건 만족 시 해당 버튼 활성화
                if (match)
                {
                    MythicSpawnButton mythicButton = FindMythicButtonForRecipe(recipe);
                    if (mythicButton != null)
                    {
                        mythicButton.SetAvailable(true);
                        mythicButton.SetRecipeData(recipe, start); // 레시피 데이터와 시작 인덱스 설정
                    }
                    
                    Debug.Log($"Mythic recipe '{recipe.Id}' is available! Start index: {start}");
                    break; // 해당 레시피는 찾았으므로 다음 레시피로
                }
            }
        }
    }

    /// <summary> 특정 레시피에 해당하는 신화 버튼을 찾습니다. </summary>
    private MythicSpawnButton FindMythicButtonForRecipe(MythicRecipe recipe)
    {
        foreach (var buttonObj in mythicButtons)
        {
            if (buttonObj == null) continue;
            
            MythicSpawnButton mythicButton = buttonObj.GetComponent<MythicSpawnButton>();
            if (mythicButton != null && mythicButton.IsForRecipe(recipe)) return mythicButton;
        }
        return null;
    }

    /// <summary> 다음 머지를 처리합니다. </summary>
    private void ProcessNextMerge()
    {
        if (pendingMerges.Count == 0)
        {
            isProcessingMerges = false;
            RelayoutQueue(onComplete: () => TryResolveQueue()); // 모든 머지 완료 후 최종 재배치
            return;
        }

        isProcessingMerges = true;
        MergeAnimationData mergeData = pendingMerges.Dequeue();
        StartMergeAnimation(mergeData);
    }

    /// <summary> 머지 애니메이션을 시작합니다. </summary>
    private void StartMergeAnimation(MergeAnimationData mergeData)
    {
        // 머지할 블록들의 원래 위치 저장
        List<Vector3> originalPositions = new List<Vector3>();
        foreach (var block in mergeData.blocksToMerge) originalPositions.Add(block.transform.position);
        
        // 중앙 위치 계산
        Vector3 centerPosition = CalculateCenterPosition(mergeData.blocksToMerge);
        
        // 1단계: 블록들을 위로 이동
        Sequence moveUpSequence = DOTween.Sequence();
        foreach (var block in mergeData.blocksToMerge)
        {
            Vector3 upPosition = block.transform.position + Vector3.up * 50f;
            moveUpSequence.Join(block.transform.DOMove(upPosition, 0.3f).SetEase(Ease.OutQuad));
        }

        // 2단계: 중앙으로 이동하며 합쳐지기
        moveUpSequence.AppendCallback(() =>
        {
            Sequence mergeSequence = DOTween.Sequence();
            foreach (var block in mergeData.blocksToMerge) mergeSequence.Join(block.transform.DOMove(centerPosition, 0.4f).SetEase(Ease.InQuad));
                
            // 3단계: 머지 완료 후 블록 제거 및 결과 생성
            mergeSequence.AppendCallback(() => CompleteMerge(mergeData));
        });
    }

    /// <summary> 머지할 블록들의 중앙 위치를 계산합니다. </summary>
    private Vector3 CalculateCenterPosition(List<UnitBlock> blocksToMerge)
    {
        if (blocksToMerge.Count == 0) return Vector3.zero;

        Vector3 sum = Vector3.zero;
        foreach (var block in blocksToMerge) sum += block.transform.position;
        return sum / blocksToMerge.Count;
    }

    /// <summary> 머지를 완료하고 결과 블록을 생성합니다. </summary>
    private void CompleteMerge(MergeAnimationData mergeData)
    {
        // 머지할 블록들을 큐에서 제거
        foreach (var block in mergeData.blocksToMerge) if (blocks.Contains(block)) RemoveBlock(block); 

        // 결과 블록들 생성 (안전한 인덱스로 삽입)
        int safeInsertIndex = Mathf.Clamp(mergeData.insertIndex, 0, blocks.Count);
        for (int i = 0; i < mergeData.outputCount; i++) InsertBlock(mergeData.resultUnitData, safeInsertIndex + i);

        // 결과 메시지 표시
        if (!string.IsNullOrEmpty(mergeData.resultMessage)) UIManager.Instance?.ShowMessage(mergeData.resultMessage);

        // 레이아웃 재정렬 후 다음 머지 처리
        RelayoutQueue(onComplete: () => ProcessNextMerge());
    }

    #endregion

    #region Manual Mythic Spawn

    /// <summary> 신화 유닛을 수동으로 소환합니다. </summary>
    /// <param name="recipe">소환할 신화 레시피</param>
    /// <param name="startIndex">재료 블록들의 시작 인덱스</param>
    public void SpawnMythicUnit(MythicRecipe recipe, int startIndex)
    {
        if (recipe == null || recipe.ResultUnit == null) return;

        int window = recipe.Sequence.Count;
        if (startIndex + window > blocks.Count) return;

        // 재료 블록들 수집
        List<UnitBlock> blocksToRemove = new List<UnitBlock>();
        for (int i = 0; i < window; i++) blocksToRemove.Add(blocks[startIndex + i]);

        // 재료 블록들 제거
        foreach (var block in blocksToRemove) if (blocks.Contains(block)) RemoveBlock(block);

        // 신화 유닛 소환
        for (int i = 0; i < recipe.OutputCount; i++) UnitManager.Instance?.SpawnUnitFromUnitData(recipe.ResultUnit);

        // 레이아웃 재정렬 후 머지 확인
        RelayoutQueue(onComplete: () => TryResolveQueue());
    }

    /// <summary> 신화 버튼들의 상태를 업데이트합니다. (해금 상태 반영) </summary>
    public void UpdateMythicButtonStates()
    {
        foreach (var buttonObj in mythicButtons)
        {
            if (buttonObj == null) continue;
            
            MythicSpawnButton mythicButton = buttonObj.GetComponent<MythicSpawnButton>();
            if (mythicButton != null && mythicButton.assignedRecipe != null)
            {
                // 해금 상태에 따라 자물쇠 설정
                mythicButton.SetLocked(!mythicButton.assignedRecipe.isUnlocked);
            }
        }
    }

    /// <summary> 신화 레시피 설정을 반환합니다. </summary>
    public MythicRecipeConfig GetMythicRecipeConfig()
    {
        if (mythicRecipeConfig == null) {Debug.LogWarning("QueueManager: mythicRecipeConfig가 설정되지 않았습니다."); return null;}
        return mythicRecipeConfig;
    }

    #endregion

    #region Pool Helpers

    private void RegisterBlockPool()
    {
        if (PoolManager.Instance == null) return;
        if (unitBlockPrefab == null){ Debug.LogWarning("QueueManager: unitBlockPrefab이 설정되지 않았습니다."); return; }

        PoolManager.Instance.RegisterPool(blockPoolKey, unitBlockPrefab.gameObject, prewarmBlockCount, null);
    }

    private UnitBlock GetBlockInstance()
    {
        UnitBlock block = null;

        if (PoolManager.Instance != null) block = PoolManager.Instance.Get<UnitBlock>(blockPoolKey, queueContainer);
        if (block == null) Debug.LogWarning("QueueManager: 블록 인스턴스를 생성하지 못했습니다.");
        
        return block;
    }

    private void ReleaseBlock(UnitBlock block)
    {
        if (block == null) return;

        block.transform.DOKill();
        block.OnBlockClicked -= SpawnUnitFromBlock;;
        block.ResetToOriginalSize();
        block.ResetStateForPool();

        if (PoolManager.Instance != null) PoolManager.Instance.Release(block.gameObject);
        else Destroy(block.gameObject);
    }

    #endregion

    #region Data Classes

    /// <summary> 병합 규칙을 정의하는 데이터 클래스입니다. 연속된 같은 유닛들을 병합할 때의 조건과 결과를 설정합니다. </summary>
    [System.Serializable]
    private class MergeRule
    {
        public string id = "merge";
        [Min(2)] public int requiredCount = 3;
        [Min(1)] public int outputCount = 1;
        
        [Tooltip("병합 규칙의 우선순위 (높을수록 우선 적용)")]
        public int priority = 0;
        
        [Tooltip("병합 완료 시 표시할 메시지. {unit}은 결과 유닛 이름으로 치환됩니다.")]
        public string resultMessage = string.Empty;
    }

    /// <summary> 머지 애니메이션에 필요한 데이터를 담는 클래스입니다. </summary>
    private class MergeAnimationData
    {
        public List<UnitBlock> blocksToMerge;     // 머지할 블록들
        public UnitData resultUnitData;           // 결과 유닛 데이터
        public int outputCount;                   // 결과 블록 개수
        public int insertIndex;                   // 결과 블록을 삽입할 인덱스
        public string resultMessage;              // 결과 메시지
        public bool isMythicRecipe;               // 미스틱 레시피인지 여부

        public MergeAnimationData(List<UnitBlock> blocks, UnitData result, int count, int index, string message = "", bool isMythic = false)
        {
            blocksToMerge = blocks;
            resultUnitData = result;
            outputCount = count;
            insertIndex = index;
            resultMessage = message;
            isMythicRecipe = isMythic;
        }
    }
    
    #endregion
}


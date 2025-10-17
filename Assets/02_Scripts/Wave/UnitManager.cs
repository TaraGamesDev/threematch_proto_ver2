using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Sirenix.OdinInspector;
using System.Linq;
using System.Linq.Expressions;

public class UnitManager : MonoBehaviour
{
    public static UnitManager Instance { get; private set; }
    
    
    [Title("오브젝트 풀")]
    [SerializeField, Min(0)] private int prewarmUnitCount = 5;
    
    // 각 유닛별 오브젝트 풀 관리
    private Dictionary<string, string> unitPoolKeys = new Dictionary<string, string>();

    
    [Title("웨이브 설정")]
    [Tooltip("유닛이 스폰될 UI 패널 (UnitBlock의 x좌표에 맞춰 스폰)")]
    public RectTransform unitSpawnParent;
    public Transform unitStopPos; // 유닛들이 멈출 위치
    [Tooltip("유닛 스폰 위치의 좌우 랜덤 범위")]
    public float unitSpawnRandomRange = 5f;

    [Tooltip("타겟 검색 간격")]
    public float TargetSearchInterval = 0.5f;

    [Title("Auto Positioning System")]

    [Tooltip("사거리별 유닛 배치를 위한 Vertical Layout Group")]
    [SerializeField] private VerticalLayoutGroup rangeLayoutGroup;

    [Tooltip("각 사거리별 가로 배치를 위한 Horizontal Layout Group 프리팹")]
    [SerializeField] private GameObject horizontalLayoutPrefab;
    
    
    [Title("웨이브 진행상황")]
    public List<Animal> activeUnits = new List<Animal>();

    // 사거리별 배치 관리 (int 키 사용 - 사거리 * 10)
    private Dictionary<int, GameObject> rangeLayoutGroups = new Dictionary<int, GameObject>();
    
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }
    }
    
    private void Start()
    {
        UnitDatabase.Initialize();  // 유닛 데이터베이스 초기화
    }
    
    #region Spawn Unit
    /// <summary> UnitData로 유닛을 소환합니다. (일반 유닛 및 신화 유닛 공통) </summary>
    public void SpawnUnitFromUnitData(UnitData unitData, bool isMythic = false)
    {
        if (unitData == null) return;
        
        // UnitData의 프리팹을 직접 사용
        if (unitData.unitPrefab == null){ Debug.LogWarning($"UnitManager: {unitData.unitName}의 unitPrefab이 설정되지 않았습니다."); return; }
        
        Animal animal = GetUnitFromPool(unitData);
        if (animal == null) return;

        if(unitSpawnParent == null){ Debug.LogWarning("UnitManager: unitSpawnParent이 설정되지 않았습니다."); return; }
        
        Transform parent = unitSpawnParent;
        animal.transform.SetParent(parent, false);
        
        // 플레이어 위치에서 스폰 (랜덤성 추가)
        if(GameManager.Instance.playerTransform == null){ Debug.LogWarning("UnitManager: GameManager.Instance.playerTransform이 설정되지 않았습니다."); return; }

        Vector3 spawnPosition = GameManager.Instance.playerTransform.position;
        spawnPosition.x += Random.Range(-unitSpawnRandomRange, unitSpawnRandomRange);
        animal.transform.position = spawnPosition;
        animal.transform.rotation = Quaternion.identity;
        
        // 신화 유닛일 때만 스케일 2배
        animal.transform.localScale = isMythic ? Vector3.one * 2f : Vector3.one;

        animal.Init(unitData);
        UpgradeSystem.Instance?.ApplyPermanentUpgradesToUnit(animal); // TODO 

        if (!activeUnits.Contains(animal)) activeUnits.Add(animal);

        // 자동 배치 시스템 적용(전투 턴이 아닐 때만 적용)
        if(GameManager.Instance.CurrentPhase != GamePhase.BattlePhase) AutoPositionUnitByRange(animal);
    }

    #endregion

    #region Auto Positioning System

    /// <summary> 유닛을 사거리에 따라 자동으로 배치합니다. </summary>
    private void AutoPositionUnitByRange(Animal animal)
    {
        if (rangeLayoutGroup == null) return;

        // 사거리에 10을 곱해서 int 키로 사용 (소수점 첫째 자리까지 구분)
        int rangeKey = Mathf.RoundToInt(animal.currentAttackRange * 10f);
        
        // 해당 사거리의 레이아웃 그룹이 없으면 생성
        if (!rangeLayoutGroups.ContainsKey(rangeKey)) CreateRangeLayoutGroup(rangeKey);

        // 유닛을 해당 사거리 그룹에 추가
        GameObject rangeGroup = rangeLayoutGroups[rangeKey];
        if (rangeGroup != null) animal.transform.SetParent(rangeGroup.transform);

        else Debug.LogWarning($"UnitManager: {animal.unitName} 사거리 그룹을 찾을 수 없습니다. {rangeKey}");
    }

    /// <summary> 특정 사거리의 레이아웃 그룹을 생성합니다. </summary>
    private void CreateRangeLayoutGroup(int rangeKey)
    {
        if (horizontalLayoutPrefab == null || rangeLayoutGroup == null) return;

        // Horizontal Layout Group 생성
        GameObject rangeGroup = Instantiate(horizontalLayoutPrefab, rangeLayoutGroup.transform);
        rangeGroup.name = $"Range_{rangeKey}_Group";
        rangeLayoutGroups[rangeKey] = rangeGroup;
        
        // 사거리 순서대로 정렬 (짧은 사거리가 위로)
        SortRangeGroups();
    }

    /// <summary> 사거리 그룹들을 사거리 순서대로 정렬합니다. </summary>
    private void SortRangeGroups()
    {
        if (rangeLayoutGroup == null) return;

        // 사거리 순서대로 정렬 (짧은 사거리가 위로)
        var sortedRangeKeys = rangeLayoutGroups.Keys.OrderBy(r => r).ToList();
        
        for (int i = 0; i < sortedRangeKeys.Count; i++)
        {
            int rangeKey = sortedRangeKeys[i];
            GameObject rangeGroup = rangeLayoutGroups[rangeKey];
            if (rangeGroup != null) rangeGroup.transform.SetSiblingIndex(i);
        }
    }
    
    /// <summary> 모든 유닛을 사거리별로 재배치합니다. </summary>
    public void ReorganizeUnitsByRange()
    {
        if (rangeLayoutGroup == null) return;

        // 기존 그룹들에서 유닛들을 원래 진영으로 이동 (그룹은 삭제하지 않음)
        foreach (var kvp in rangeLayoutGroups) 
        {
            if (kvp.Value == null) continue;
            
            // 그룹의 모든 자식 유닛들을 unitSpawnParent로 이동
            Transform groupTransform = kvp.Value.transform;
            for (int i = groupTransform.childCount - 1; i >= 0; i--) groupTransform.GetChild(i).SetParent(unitSpawnParent, false);
            // 그룹 삭제
            DestroyImmediate(kvp.Value);
        }
        rangeLayoutGroups.Clear(); 

        // 모든 활성 유닛을 사거리별로 재배치
        foreach (var unit in activeUnits) if (unit != null) AutoPositionUnitByRange(unit);
    }

    #endregion

    
    public void RemoveUnit(Animal animal)
    {
        if (activeUnits.Contains(animal)) activeUnits.Remove(animal);
    }
    
    #region On Battle End & Start
    /// <summary> 전투 종료 후 생존한 유닛들을 정리합니다. (유닛은 지속됨) </summary>
    public void OnBattleEnd()
    {
        foreach (var animal in activeUnits) animal.Alive();
    
        // 사거리별 재배치
        ReorganizeUnitsByRange();
    }
    
    /// <summary> 전투 시작 시 모든 유닛의 스탯 초기화 및 움직임 활성화 </summary>
    public void OnBattleStart()
    {
        foreach (var animal in activeUnits) if (animal != null) animal.SetCanMove(true); // 움직임 활성화
    }

    #endregion

    #region Pool
    /// <summary> 특정 유닛의 오브젝트 풀을 등록합니다. </summary>
    private void RegisterUnitPool(UnitData unitData)
    {
        if (PoolManager.Instance == null || unitData == null || unitData.unitPrefab == null) return;

        // 이미 등록된 풀이면 스킵
        if (unitPoolKeys.ContainsKey(unitData.unitName)) return;
        
        PoolManager.Instance.RegisterPool(unitData.unitName, unitData.unitPrefab, prewarmUnitCount, null);
        unitPoolKeys[unitData.unitName] = unitData.unitName;
        
        // Debug.Log($"UnitManager: {unitData.unitName} 풀 등록 완료 (Key: {unitData.unitName})");
    }



    /// <summary> 특정 유닛을 풀에서 가져옵니다. </summary>
    private Animal GetUnitFromPool(UnitData unitData)
    {
        if (unitData == null || unitData.unitPrefab == null) return null;
        
        // 풀이 등록되지 않았으면 등록
        if (!unitPoolKeys.ContainsKey(unitData.unitName)) RegisterUnitPool(unitData);
    
        Animal animal = null;
        if (PoolManager.Instance != null) animal = PoolManager.Instance.Get<Animal>(unitData.unitName, unitSpawnParent);
        
        if(animal == null) Debug.LogWarning($"UnitManager: {unitData.unitName} 유닛 풀에서 유닛을 꺼내오는데 실패했습니다.");

        return animal;
    }

    #endregion
}

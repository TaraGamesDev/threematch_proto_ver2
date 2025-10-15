using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Sirenix.OdinInspector;

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

    
    [Title("웨이브 진행상황")]
    public List<Animal> activeUnits = new List<Animal>();
    
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

        animal.unitData = unitData;
        animal.Init();
        UpgradeSystem.Instance?.ApplyPermanentUpgradesToUnit(animal);
        animal.SetCanMove(WaveManager.Instance.IsWaveActive());

        if (!activeUnits.Contains(animal)) activeUnits.Add(animal);
    }

    #endregion

    
    public void RemoveUnit(Animal animal)
    {
        if (activeUnits.Contains(animal)) activeUnits.Remove(animal);
    }
    
    // 웨이브 완료 시 모든 유닛을 스폰 포지션으로 되돌리기
    public void ResetUnitsToSpawnPosition()
    {
        if (unitSpawnParent == null) return;
        
        foreach (var animal in activeUnits)
        {
            if (animal != null && !animal.IsDead())
            {
                animal.SetCanMove(false); // 움직임 정지
                
                // x위치는 그대로 두고 y좌표만 스폰 위치로 되돌리기
                Vector3 currentPosition = animal.transform.position;
                Vector3 spawnPosition = new Vector3(currentPosition.x, unitSpawnParent.position.y, currentPosition.z);
                animal.transform.position = spawnPosition;
                
                animal.currentTarget = null; // 타겟 초기화
            }
        }
    }
    
    // 웨이브 시작 시 모든 유닛의 스탯 초기화 및 움직임 활성화
    public void ResetUnitsStatsAndEnableMovement()
    {
        foreach (var animal in activeUnits)
        {
            if (animal != null && !animal.IsDead())
            {
                animal.ResetStats(); // 스탯 초기화
                animal.SetCanMove(true); // 움직임 활성화
            }
        }
    }

    #region Pool
    /// <summary> 특정 유닛의 오브젝트 풀을 등록합니다. </summary>
    private void RegisterUnitPool(UnitData unitData)
    {
        if (PoolManager.Instance == null || unitData == null || unitData.unitPrefab == null) return;

        // 이미 등록된 풀이면 스킵
        if (unitPoolKeys.ContainsKey(unitData.unitName)) return;
        
        PoolManager.Instance.RegisterPool(unitData.unitName, unitData.unitPrefab, prewarmUnitCount, null);
        unitPoolKeys[unitData.unitName] = unitData.unitName;
        
        Debug.Log($"UnitManager: {unitData.unitName} 풀 등록 완료 (Key: {unitData.unitName})");
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

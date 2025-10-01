using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Sirenix.OdinInspector;

public class UnitManager : MonoBehaviour
{
    public static UnitManager Instance { get; private set; }
    
    
    [Title("오브젝트 풀")]
    public GameObject unitPrefab;
    [SerializeField] private string unitPoolKey = "player-unit";
    [SerializeField, Min(0)] private int prewarmUnitCount = 5;

    
    [Title("웨이브 설정")]
    [Tooltip("유닛이 스폰될 UI 패널 (UnitBlock의 x좌표에 맞춰 스폰)")]
    public RectTransform unitSpawnZone;
    public Transform unitStopPos; // 유닛들이 멈출 위치

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
        RegisterUnitPool();
    }
    
    private void Start()
    {
        UnitDatabase.Initialize();  // 유닛 데이터베이스 초기화
    }
    
    #region Spawn Unit
    public void SpawnUnitFromBlock(UnitBlock block)
    {
        if (block == null || block.unitData == null) return;
        
        Animal animal = GetUnitFromPool();
        if (animal == null) return;

        if(unitSpawnZone == null){ Debug.LogWarning("UnitManager: unitSpawnZone이 설정되지 않았습니다."); return; }
        
        Transform parent = unitSpawnZone;
        animal.transform.SetParent(parent, false);
        
        // UnitBlock의 x좌표를 UnitSpawnZone에 맞춰 스폰 위치 계산
        Vector3 spawnPosition = GetUnitSpawnPosition(block);
        animal.transform.position = spawnPosition;
        animal.transform.rotation = Quaternion.identity;

        animal.unitData = block.unitData;
        var image = animal.GetComponent<Image>();
        if (image != null) image.sprite = block.unitData.unitSprite;

        animal.Init();
        UpgradeSystem.Instance?.ApplyPermanentUpgradesToUnit(animal);
        animal.SetCanMove(WaveManager.Instance.IsWaveActive());

        if (!activeUnits.Contains(animal)) activeUnits.Add(animal);
    }
    
    /// <summary> UnitData로 직접 유닛을 소환합니다. (신화 유닛 소환용) </summary>
    public void SpawnUnitFromUnitData(UnitData unitData)
    {
        if (unitData == null) return;
        
        Animal animal = GetUnitFromPool();
        if (animal == null) return;

        if(unitSpawnZone == null){ Debug.LogWarning("UnitManager: unitSpawnZone이 설정되지 않았습니다."); return; }
        
        Transform parent = unitSpawnZone;
        animal.transform.SetParent(parent, false);
        
        // UnitSpawnZone의 중앙에서 스폰
        Vector3 spawnPosition = unitSpawnZone.position;
        animal.transform.position = spawnPosition;
        animal.transform.rotation = Quaternion.identity;
        animal.transform.localScale = Vector3.one * 2f;

        animal.unitData = unitData;
        var image = animal.GetComponent<Image>();
        if (image != null) image.sprite = unitData.unitSprite;

        animal.Init();
        UpgradeSystem.Instance?.ApplyPermanentUpgradesToUnit(animal);
        animal.SetCanMove(WaveManager.Instance.IsWaveActive());

        if (!activeUnits.Contains(animal)) activeUnits.Add(animal);
    }

    /// <summary> UnitBlock의 x좌표를 UnitSpawnZone에 맞춰 스폰 위치를 계산합니다. </summary>
    private Vector3 GetUnitSpawnPosition(UnitBlock block)
    {
        if (unitSpawnZone == null){ Debug.LogWarning("UnitManager: unitSpawnZone이 설정되지 않았습니다."); return Vector3.zero; }
        
        // UnitBlock의 x좌표를 가져옴
        float blockX = block.transform.position.x;
        
        // UnitSpawnZone의 y좌표를 사용하고, UnitBlock의 x좌표를 유지
        Vector3 zonePosition = unitSpawnZone.position;
        
        return new Vector3(blockX, zonePosition.y, 0f);
    }

    #endregion

    
    public void RemoveUnit(Animal animal)
    {
        if (activeUnits.Contains(animal)) activeUnits.Remove(animal);
    }
    
    // @ TODO : 업그레이드 적용
    // public void ApplyUpgradeToAllUnits(UpgradeData.UpgradeType upgradeType, float bonus, bool isPercentage)
    // {
    //     foreach (var animal in activeUnits) 
    //         if (animal != null) animal.ApplyUpgrade(upgradeType, bonus, isPercentage);    
        
    // }
    
    // 웨이브 완료 시 모든 유닛을 스폰 포지션으로 되돌리기
    public void ResetUnitsToSpawnPosition()
    {
        if (unitSpawnZone == null) return;
        
        foreach (var animal in activeUnits)
        {
            if (animal != null && !animal.IsDead())
            {
                animal.SetCanMove(false); // 움직임 정지
                
                // x위치는 그대로 두고 y좌표만 스폰 위치로 되돌리기
                Vector3 currentPosition = animal.transform.position;
                Vector3 spawnPosition = new Vector3(currentPosition.x, unitSpawnZone.position.y, currentPosition.z);
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
    private void RegisterUnitPool()
    {
        if (PoolManager.Instance == null) return;
        if (unitPrefab == null){ Debug.LogWarning("UnitManager: unitPrefab이 설정되지 않았습니다."); return; }

        PoolManager.Instance.RegisterPool(unitPoolKey, unitPrefab, prewarmUnitCount, null);
    }

    private Animal GetUnitFromPool()
    {
        Animal animal = null;

        if (PoolManager.Instance != null) animal = PoolManager.Instance.Get<Animal>(unitPoolKey, unitSpawnZone);
        
        if(animal == null) Debug.LogWarning("UnitManager: 유닛 풀에서 유닛을 꺼내오는데 실패했습니다.");

        return animal;
    }

    #endregion
}

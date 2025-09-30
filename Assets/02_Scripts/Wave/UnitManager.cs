using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UnitManager : MonoBehaviour
{
    public static UnitManager Instance { get; private set; }
    
    [Header("유닛 설정")]
    public GameObject unitPrefab;
    public Transform unitContainer;
    public Transform spawnPoint;
    public Transform unitStopPos; // 유닛들이 멈출 위치

    
    [Header("전투 설정")]
    public LayerMask enemyLayer;
    public float detectionRadius = 10f;
    
    public List<Animal> activeUnits = new List<Animal>();
    
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }
    
    private void Start()
    {
        // 유닛 데이터베이스 초기화
        UnitDatabase.Initialize();
    }
    
    public void SpawnUnitFromBlock(UnitBlock block)
    {
        if (unitPrefab == null || block == null || block.unitData == null || spawnPoint == null) return;
        
        GameObject unitObj = Instantiate(unitPrefab, spawnPoint.position, spawnPoint.rotation, unitContainer);
        Animal animal = unitObj.GetComponent<Animal>();
        
        if (animal != null)
        {
            animal.unitData = block.unitData;
            animal.GetComponent<Image>().sprite = block.unitData.unitSprite;
            
            // @TODO : 영구 업그레이드 적용
            // if (UpgradeSystem.Instance != null) UpgradeSystem.Instance.ApplyPermanentUpgradesToUnit(animal);
            activeUnits.Add(animal);
        }
    }
    
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
        if (spawnPoint == null) return;
        
        foreach (var animal in activeUnits)
        {
            if (animal != null && !animal.IsDead())
            {
                animal.SetCanMove(false); // 움직임 정지
                animal.transform.localPosition = new Vector3(0, 0, 0);
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
                // 스탯 초기화
                animal.ResetStats();
                // 움직임 활성화
                animal.SetCanMove(true);
            }
        }
    }
    
    // 웨이브 시작 시 모든 유닛의 움직임 활성화
    public void EnableUnitsMovement()
    {
        foreach (var animal in activeUnits)
        {
            if (animal != null && !animal.IsDead()) animal.SetCanMove(true);
        }
    }
}

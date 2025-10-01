using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public class WaveManager : MonoBehaviour
{
    public static WaveManager Instance { get; private set; }
    
    // 이벤트
    public System.Action<int> OnWaveChanged;

    
    [Title("웨이브 설정")]
    public GameObject enemyPrefab;
    public UnitData enemyUnitData;
    public float spawnInterval = 2f;
    public int enemiesPerWave = 10;
    
    [Title("스폰 존 설정")]
    [Tooltip("몬스터가 스폰될 UI 패널 (랜덤 x위치에서 스폰)")]
    public RectTransform monsterSpawnZone;

    
    [Header("웨이브 진행에 따른 증가율")]
    public float healthMultiplier = 1.2f;
    public float damageMultiplier = 1.1f;
    public float speedMultiplier = 1.05f;


    [Title("오브젝트 풀")]
    [SerializeField] private string enemyPoolKey = "enemy-unit";
    [SerializeField, Min(0)] private int prewarmEnemyCount = 50;


    [Title("웨이브 진행상황")]
    [SerializeField] private List<Enemy> activeEnemies = new List<Enemy>();
    [SerializeField] private int currentWave = 1;
    [SerializeField] private bool isWaveActive = false;
    [SerializeField] private int enemiesSpawned = 0;
    [SerializeField] private int enemiesKilled = 0;
    
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        RegisterEnemyPool();
    }
    
    public void StartWave(int waveNumber)
    {
        Debug.Log($"[WaveManager] StartWave {waveNumber}");

        currentWave = waveNumber;
        isWaveActive = true;
        enemiesSpawned = 0;
        enemiesKilled = 0;
        
        // 웨이브 변경 이벤트 발생
        OnWaveChanged?.Invoke(currentWave);
        
        // 유닛들의 스탯 초기화 및 움직임 활성화
        UnitManager.Instance.ResetUnitsStatsAndEnableMovement();
        
        // 웨이브에 따른 적 수 증가
        int enemyCount = CalculateEnemyCount(waveNumber);
        
        StartCoroutine(SpawnWave(enemyCount));
    }
    
    private int CalculateEnemyCount(int wave)
    {
        // 웨이브가 증가할수록 적 수 증가
        return enemiesPerWave + (wave - 1) * 5;
    }
    
    private IEnumerator SpawnWave(int enemyCount)
    {
        Debug.Log($"[WaveManager] SpawnWave - enemyCount : {enemyCount}");
        
        while (enemiesSpawned < enemyCount && isWaveActive)
        {
            SpawnEnemy();
            enemiesSpawned++;
            yield return new WaitForSeconds(spawnInterval);
        }
    }
    
    #region Spawn Enemy
    private void SpawnEnemy()
    {
        if (enemyPrefab == null || enemyUnitData == null) return;

        Enemy enemy = GetEnemyFromPool();
        if (enemy == null) return;

        // MonsterSpawnZone에서 랜덤 위치 계산
        Vector3 spawnPosition = GetRandomMonsterSpawnPosition();
        enemy.transform.position = spawnPosition;
        enemy.transform.rotation = Quaternion.identity;

        // UnitData로 기본 스탯 설정
        enemy.unitData = enemyUnitData;
        enemy.Init();

        // 웨이브에 따른 스탯 증가 적용
        ApplyWaveScaling(enemy);
        enemy.SetCanMove(true);
    }
    
    /// <summary> MonsterSpawnZone에서 랜덤한 스폰 위치를 계산합니다. </summary>
    private Vector3 GetRandomMonsterSpawnPosition()
    {
        if (monsterSpawnZone == null) { Debug.LogWarning("WaveManager: monsterSpawnZone이 설정되지 않았습니다."); return Vector3.zero; }
        
        // MonsterSpawnZone의 경계 내에서 랜덤 위치 계산
        Rect rect = monsterSpawnZone.rect;
        Vector3 zonePosition = monsterSpawnZone.position;
        
        // UI 좌표를 월드 좌표로 변환
        float randomX = zonePosition.x + Random.Range(-rect.width * 0.5f, rect.width * 0.5f);
        float y = zonePosition.y;
        
        return new Vector3(randomX, y, 0f);
    }
    
    private void ApplyWaveScaling(Enemy enemy)
    {
        // 웨이브에 따른 스탯 증가
        float healthMultiplier = Mathf.Pow(this.healthMultiplier, currentWave - 1);
        float damageMultiplier = Mathf.Pow(this.damageMultiplier, currentWave - 1);
        float speedMultiplier = Mathf.Pow(this.speedMultiplier, currentWave - 1);
        
        // 스탯 적용
        enemy.currentHealth = Mathf.RoundToInt(enemy.health * healthMultiplier);
        enemy.currentAttackDamage = Mathf.RoundToInt(enemy.attackDamage * damageMultiplier);
        enemy.currentMoveSpeed = enemy.moveSpeed * speedMultiplier;
        enemy.currentAttackSpeed = enemy.attackSpeed;
        enemy.currentAttackRange = enemy.attackRange;
    }

    #endregion
    
    public void RegisterEnemy(Enemy enemy)
    {
        if (!activeEnemies.Contains(enemy)) activeEnemies.Add(enemy);
    }
    
    public void UnregisterEnemy(Enemy enemy)
    {
        if (activeEnemies.Contains(enemy))
        {
            activeEnemies.Remove(enemy);
            enemiesKilled++;
            
            // 웨이브 완료 체크
            CheckWaveCompletion();
        }
    }
    

    #region Check Wave Completion

    private void CheckWaveCompletion()
    {
        if (isWaveActive && enemiesKilled >= enemiesSpawned && activeEnemies.Count == 0)
        {
            Debug.Log($"[WaveManager] CheckWaveCompletion - CompleteWave");
            CompleteWave();
        }
    }
    
    private void CompleteWave()
    {
        isWaveActive = false;
        
        // 웨이브 클리어 보상 계산 및 지급
        int waveReward = CalculateWaveReward();
        GameManager.Instance.AddGold(waveReward);
        
        // 웨이브 클리어 메시지 표시
        UIManager.Instance.ShowMessage($"Wave {currentWave} 클리어! \n 보상 : {waveReward} 골드");
        
        // UI 업데이트
        UIManager.Instance.UpdateGoldTextUI();
                
        foreach (var animal in UnitManager.Instance.activeUnits)
            if (animal != null && !animal.IsDead()) animal.SetCanMove(false); // 움직임 정지
            
        GameManager.Instance.CompleteWave();
    }
    
    // 웨이브 클리어 보상 계산
    private int CalculateWaveReward()
    {
        // 현재 웨이브 * 뽑기에 필요한 돈
        int drawCost = GameManager.Instance.goldCostPerBlock;
        return currentWave/drawCost + drawCost*5;
    }

    #endregion
    

    #region Utils
    public void StopWave()
    {
        isWaveActive = false;
    }
    
    public List<Enemy> GetActiveEnemies()
    {
        return new List<Enemy>(activeEnemies);
    }
    
    public int GetCurrentWave()
    {
        return currentWave;
    }
    
    public bool IsWaveActive()
    {
        return isWaveActive;
    }
    
    public int GetEnemiesRemaining()
    {
        return activeEnemies.Count;
    }
    
    public int GetEnemiesKilled()
    {
        return enemiesKilled;
    }
    
    public int GetTotalEnemiesInWave()
    {
        return enemiesSpawned;
    }

    #endregion
    

    #region Pool
    private void RegisterEnemyPool()
    {
        if (PoolManager.Instance == null) return;
        if (enemyPrefab == null){ Debug.LogWarning("WaveManager: enemyPrefab이 설정되지 않았습니다."); return; }
        PoolManager.Instance.RegisterPool(enemyPoolKey, enemyPrefab, prewarmEnemyCount, null);
    }

    private Enemy GetEnemyFromPool()
    {
        Enemy enemy = null;

        if (PoolManager.Instance != null) enemy = PoolManager.Instance.Get<Enemy>(enemyPoolKey, monsterSpawnZone);

        if (enemy == null && enemyPrefab != null) Debug.LogWarning("Enemy Pool에서 오브젝트를 꺼내오는데 실패했습니다.");
        
        return enemy;
    }

    #endregion


}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveManager : MonoBehaviour
{
    public static WaveManager Instance { get; private set; }
    
    // 이벤트
    public System.Action<int> OnWaveChanged;
    
    [Header("웨이브 설정")]
    public GameObject enemyPrefab;
    public Transform spawnPoint;
    public float spawnInterval = 2f;
    public int enemiesPerWave = 10;
    
    
    [Header("적 데이터")]
    public UnitData enemyUnitData;

    
    [Header("웨이브 증가")]
    public float healthMultiplier = 1.2f;
    public float damageMultiplier = 1.1f;
    public float speedMultiplier = 1.05f;

    
   [SerializeField] private List<Enemy> activeEnemies = new List<Enemy>();
    private int currentWave = 1;
    private bool isWaveActive = false;
    private int enemiesSpawned = 0;
    private int enemiesKilled = 0;
    
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
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
    
    private void SpawnEnemy()
    {
        if (enemyPrefab == null || spawnPoint == null || enemyUnitData == null) return;
        
        GameObject enemyObj = Instantiate(enemyPrefab, spawnPoint.position, Quaternion.identity, spawnPoint);
        Enemy enemy = enemyObj.GetComponent<Enemy>();
        
        if (enemy != null)
        {
            // UnitData로 기본 스탯 설정
            enemy.unitData = enemyUnitData;
            enemy.Init();
            
            // 웨이브에 따른 스탯 증가 적용
            ApplyWaveScaling(enemy);
            
            activeEnemies.Add(enemy);
        }
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
        
        // 모든 유닛을 스폰 포지션으로 되돌리기 (움직임도 정지됨)
        // UnitManager.Instance.ResetUnitsToSpawnPosition();

                
        foreach (var animal in UnitManager.Instance.activeUnits)
            if (animal != null && !animal.IsDead()) animal.SetCanMove(false); // 움직임 정지
            
        
        // 2초 후 다음 웨이브 시작
        StartCoroutine(StartNextWaveDelayed());
    }
    
    // 웨이브 클리어 보상 계산
    private int CalculateWaveReward()
    {
        // 현재 웨이브 * 뽑기에 필요한 돈
        // int drawCost = BlockQueueManager.Instance.goldCostPerBlock;
        return currentWave/5 + 20;
    }
    
    private IEnumerator StartNextWaveDelayed()
    {
        yield return new WaitForSeconds(2f);
        GameManager.Instance.CompleteWave();
    }
    
    /// <summary>
    /// 다음 웨이브를 시작합니다
    /// </summary>
    public void StartNextWave()
    {
        int nextWave = currentWave + 1;
        StartWave(nextWave);
    }
    
    public void StopWave()
    {
        isWaveActive = false;
        
        // 모든 적 제거
        foreach (var enemy in activeEnemies)
        {
            if (enemy != null)
            {
                Destroy(enemy.gameObject);
            }
        }
        
        activeEnemies.Clear();
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
    
    private void OnDrawGizmos()
    {
        // 스폰 포인트 표시
        if (spawnPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(spawnPoint.position, 0.5f);
        }
    }
}

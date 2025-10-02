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

    [Title("신화 적 설정")]
    [Tooltip("신화 유닛이 적으로 출현하는 웨이브 번호들")]
    public List<int> mythicEnemyWaves = new List<int>();
    
    [Tooltip("신화 적으로 스폰될 유닛 데이터")]
    [SerializeField] private MythicRecipeConfig config;
    public UnitData mythicEnemyData;

    
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
    [SerializeField] private int currentWaveEnemyCount = 0;
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
        InitializeMythicEnemyWaves();
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
        currentWaveEnemyCount = CalculateEnemyCount(waveNumber);
        
        StartCoroutine(SpawnWave(currentWaveEnemyCount));
    }
    
    private int CalculateEnemyCount(int wave)
    {
        // 웨이브가 증가할수록 적 수 증가
        return enemiesPerWave + (wave - 1) * 5;
    }
    
    private IEnumerator SpawnWave(int enemyCount)
    {
        Debug.Log($"[WaveManager] SpawnWave - enemyCount : {enemyCount}");
        
        // 신화 적 웨이브인지 확인
        bool isMythicWave = mythicEnemyWaves.Contains(currentWave);
        
        while (enemiesSpawned < enemyCount && isWaveActive)
        {
            // 신화 적 웨이브이고 마지막 적이라면 신화 적 스폰
            if (isMythicWave && enemiesSpawned == enemyCount - 1)
            {
                Debug.Log($"[WaveManager] SpawnMythicEnemy - currentWave : {currentWave}");
                yield return StartCoroutine(SpawnMythicEnemy());
            }
            else SpawnEnemy();
            
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

        // MonsterSpawnZone의 중앙 위치에서 스폰
        Vector3 spawnPosition = GetMonsterSpawnPosition();
        enemy.transform.position = spawnPosition;
        enemy.transform.rotation = Quaternion.identity;

        // UnitData로 기본 스탯 설정
        enemy.unitData = enemyUnitData;
        enemy.Init();

        // 웨이브에 따른 스탯 증가 적용
        ApplyWaveScaling(enemy);
        enemy.SetCanMove(true);
    }
    
    /// <summary> MonsterSpawnZone의 중앙 위치를 반환합니다. </summary>
    private Vector3 GetMonsterSpawnPosition()
    {
        if (monsterSpawnZone == null) { Debug.LogWarning("WaveManager: monsterSpawnZone이 설정되지 않았습니다."); return Vector3.zero; }
        
        // MonsterSpawnZone의 중앙 위치 반환
        return monsterSpawnZone.position;
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

    /// <summary> 신화 적을 스폰합니다. 경고 이펙트와 함께 보스 느낌으로 스폰됩니다. </summary>
    private IEnumerator SpawnMythicEnemy()
    {
        // 현재 웨이브에 해당하는 신화 유닛 데이터 가져오기
        UnitData currentMythicData = GetMythicEnemyDataForCurrentWave();
        if (currentMythicData == null) { Debug.LogWarning("WaveManager: 현재 웨이브에 해당하는 신화 유닛 데이터가 없습니다."); yield break; }

        // 1. 빨간색 깜빡깜박 경고 이펙트
        yield return StartCoroutine(ShowMythicWarning());

        // 2. 신화 적 스폰
        Enemy mythicEnemy = GetEnemyFromPool();
        if (mythicEnemy == null) yield break;

        // MonsterSpawnZone 중앙에서 스폰
        Vector3 spawnPosition = monsterSpawnZone != null ? monsterSpawnZone.position : Vector3.zero;
        mythicEnemy.transform.position = spawnPosition;
        mythicEnemy.transform.rotation = Quaternion.identity;

        // 크기 2배로 설정 (보스 느낌)
        mythicEnemy.transform.localScale = Vector3.one * 2f;

        // 신화 적 데이터로 설정
        mythicEnemy.unitData = currentMythicData;
        mythicEnemy.Init();

        mythicEnemy.SetCanMove(true);

        Debug.Log($"Mythic Enemy spawned at wave {currentWave}!");
    }

    /// <summary> 신화 적 스폰 전 빨간색 경고 이펙트를 표시합니다. </summary>
    private IEnumerator ShowMythicWarning()
    {
        // 경고 메시지 표시
        UIManager.Instance?.ShowMessage($"<color=red>⚠️ 신화 적 출현! ⚠️</color>", 2f);
        
        // 경고 이펙트 (빨간색 깜빡깜박)
        if (monsterSpawnZone != null)
        {
            // MonsterSpawnZone에 빨간색 오버레이 효과
            GameObject warningEffect = new GameObject("MythicWarning");
            warningEffect.transform.SetParent(monsterSpawnZone, false);
            
            // 빨간색 이미지 컴포넌트 추가
            UnityEngine.UI.Image warningImage = warningEffect.AddComponent<UnityEngine.UI.Image>();
            warningImage.color = new Color(1f, 0f, 0f, 0.3f); // 반투명 빨간색
            
            // RectTransform 설정
            RectTransform rectTransform = warningEffect.GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            // 깜빡깜박 애니메이션
            for (int i = 0; i < 6; i++)
            {
                warningImage.color = new Color(1f, 0f, 0f, 0.5f);
                yield return new WaitForSeconds(0.2f);
                warningImage.color = new Color(1f, 0f, 0f, 0.1f);
                yield return new WaitForSeconds(0.2f);
            }

            // 경고 이펙트 제거
            Destroy(warningEffect);
        }

        yield return new WaitForSeconds(0.5f);
    }

    /// <summary> 현재 웨이브에 해당하는 신화 유닛 데이터를 반환합니다. </summary>
    private UnitData GetMythicEnemyDataForCurrentWave()
    {
        MythicRecipeConfig config = GameManager.Instance.queueManager.GetMythicRecipeConfig();
        if (config == null) return null;

        // 현재 웨이브에서 해금되는 신화 레시피 찾기
        foreach (MythicRecipe recipe in config.ActiveRecipes)
        {
            if (recipe != null && recipe.unlockWave == currentWave && recipe.ResultUnit != null) return recipe.ResultUnit;
        }

        // 현재 웨이브에 해당하는 레시피가 없으면 
        return null;
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
        if (isWaveActive && enemiesKilled >= currentWaveEnemyCount && activeEnemies.Count == 0)
        {
            Debug.Log($"[WaveManager] CheckWaveCompletion - CompleteWave");
            CompleteWave();
        }
    }
    
    private void CompleteWave()
    {
        isWaveActive = false;
        
        // 신화 해금 확인
        CheckMythicUnlock();
        
        // 웨이브 클리어 보상 계산 및 지급
        int waveReward = CalculateWaveReward();
        GameManager.Instance.AddGold(waveReward);
        
        // 웨이브 클리어 메시지 표시
        UIManager.Instance.ShowMessage($"Wave {currentWave} 클리어! \n 보상 : {waveReward} 골드");
        
        // UI 업데이트
        UIManager.Instance.UpdateGoldTextUI();
                
        // foreach (var animal in UnitManager.Instance.activeUnits)
        //     if (animal != null && !animal.IsDead()) animal.SetCanMove(false); // 움직임 정지

        UnitManager.Instance.ResetUnitsToSpawnPosition();
            
        GameManager.Instance.CompleteWave();
    }
    
    // 웨이브 클리어 보상 계산
    private int CalculateWaveReward()
    {
        // 현재 웨이브 * 뽑기에 필요한 돈
        int drawCost = GameManager.Instance.goldCostPerBlock;
        return currentWave/drawCost + drawCost*5;
    }

    /// <summary> 현재 웨이브에서 해금되는 신화 유닛을 확인하고 해금합니다. </summary>
    private void CheckMythicUnlock()
    {
        // QueueManager에서 신화 레시피 설정 가져오기
        QueueManager queueManager =GameManager.Instance.queueManager;
        if (queueManager == null) return;

        // QueueManager의 public 메서드를 통해 mythicRecipeConfig 가져오기
        MythicRecipeConfig config = queueManager.GetMythicRecipeConfig();
        
        if (config != null)
        {
            foreach (MythicRecipe recipe in config.ActiveRecipes)
            {
                if (recipe.unlockWave == currentWave && !recipe.isUnlocked)
                {
                    Debug.Log($"Mythic unit '{recipe.Id}' unlocked at wave {currentWave}!");
                    
                    // 해금 알림 메시지
                    UIManager.Instance?.ShowMessage($"<color=yellow>✨ {recipe.ResultUnit?.unitName} 해금! ✨</color>", 3f);
                    
                    // QueueManager에 버튼 상태 업데이트 요청
                    queueManager.UpdateMythicButtonStates();
                }
            }
        }
    }

    /// <summary> QueueManager의 mythicRecipeConfig를 순회하여 mythicEnemyWaves를 자동으로 채웁니다. </summary>
    private void InitializeMythicEnemyWaves()
    {
        // 기존 웨이브 리스트 초기화
        mythicEnemyWaves.Clear();

        if (config != null)
        {
            foreach (MythicRecipe recipe in config.ActiveRecipes)
            {
                if (recipe != null && !mythicEnemyWaves.Contains(recipe.unlockWave)) 
                {
                    mythicEnemyWaves.Add(recipe.unlockWave);
                    
                    // 첫 번째 신화 레시피의 ResultUnit을 mythicEnemyData로 설정
                    if (mythicEnemyData == null && recipe.ResultUnit != null)
                    {
                        mythicEnemyData = recipe.ResultUnit;
                        Debug.Log($"WaveManager: Set mythicEnemyData to {recipe.ResultUnit.unitName}");
                    }
                }
            }

            // 웨이브 번호 순으로 정렬
            mythicEnemyWaves.Sort();
            
            Debug.Log($"WaveManager: Initialized mythic enemy waves: [{string.Join(", ", mythicEnemyWaves)}]");
        }
        else Debug.LogWarning("WaveManager: mythicRecipeConfig를 찾을 수 없습니다.");
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

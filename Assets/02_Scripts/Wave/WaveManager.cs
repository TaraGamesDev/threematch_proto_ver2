using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using UnityEngine.UI;
using DG.Tweening;
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

    [SerializeField] private Image WarningImage;
    
    [Title("스폰 존 설정")]
    public RectTransform monsterSpawnZone;


    [Tooltip("신화 적으로 스폰될 유닛 데이터")]
    [SerializeField] private MythicRecipeConfig config;
    [SerializeField, ReadOnly] private List<int> mythicEnemyWaves = new List<int>();

    
    [Header("웨이브 진행에 따른 증가율")]
    [SerializeField] float healthMultiplier = 1.2f;
    [SerializeField] float damageMultiplier = 1.1f;
    [SerializeField] float speedMultiplier = 1.05f;
    [SerializeField] float baseHealthMultiplier = 1.5f; // 웨이브당 체력 증가율


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
    [SerializeField] private bool isBaseDestroyed = false;
    
    
    [Title("Enemy Base Health")]
    [SerializeField] private Transform enemyBaseTransform; // 적 기지 Transform
    [SerializeField] private int baseMaxHealth = 100;
    [SerializeField] private int baseCurrentHealth = 100;


    public int BaseCurrentHealth => baseCurrentHealth;
    public int BaseMaxHealth => baseMaxHealth;
    public Transform EnemyBaseTransform => enemyBaseTransform;
    
    
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
    
    #region Wave Start 

    public void StartWave(int waveNumber)
    {
        Debug.Log($"[WaveManager] StartWave {waveNumber}");

        currentWave = waveNumber;
        isWaveActive = true;
        enemiesSpawned = 0;
        enemiesKilled = 0;
        isBaseDestroyed = false;
        
        // 웨이브 변경 이벤트 발생
        OnWaveChanged?.Invoke(currentWave);
        
        // 기지 체력 초기화 및 증가
        InitializeBaseHealthForWave(waveNumber);
        
        // 유닛들의 스탯 초기화 및 움직임 활성화
        UnitManager.Instance.ResetUnitsStatsAndEnableMovement();
        
        // 웨이브에 따른 적 수 증가
        currentWaveEnemyCount = CalculateEnemyCount(waveNumber);
        
        StartCoroutine(SpawnEnemies(currentWaveEnemyCount));
    }
    
    private int CalculateEnemyCount(int wave)
    {
        // 웨이브가 증가할수록 적 수 증가
        // return enemiesPerWave + (wave - 1) * 5;
        return enemiesPerWave;
    }

    #endregion

    #region Spawn Enemies
    
    private IEnumerator SpawnEnemies(int enemyCount)
    {
        Debug.Log($"[WaveManager] SpawnEnemies - enemyCount : {enemyCount}");
        
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

    /// <summary> 일반 적 스폰  </summary>
    private void SpawnEnemy()
    {
        if (enemyPrefab == null || enemyUnitData == null) return;

        Enemy enemy = GetEnemyFromPool();
        if (enemy == null) return;

        if (monsterSpawnZone == null) { Debug.LogWarning("WaveManager: monsterSpawnZone이 설정되지 않았습니다."); return; }

        enemy.transform.position = monsterSpawnZone.position;
        enemy.transform.rotation = Quaternion.identity;

        // UnitData로 기본 스탯 설정
        enemy.unitData = enemyUnitData;
        enemy.Init();

        // 웨이브에 따른 스탯 증가 적용
        ApplyWaveScaling(enemy);
        enemy.SetCanMove(true);
        RegisterEnemy(enemy);
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

        mythicEnemy.transform.position = monsterSpawnZone.position;
        mythicEnemy.transform.rotation = Quaternion.identity;

        // 크기 2배로 설정 (보스 느낌)
        mythicEnemy.transform.localScale = Vector3.one * 2f;

        // 신화 적 데이터로 설정
        mythicEnemy.unitData = currentMythicData;
        mythicEnemy.GetComponent<Image>().sprite = currentMythicData.unitSprite;
        
        // 신화 적 추가 보너스: 3배 강화
        mythicEnemy.currentHealth = Mathf.RoundToInt(mythicEnemy.currentHealth * 1.5f);
        mythicEnemy.currentAttackDamage = Mathf.RoundToInt(mythicEnemy.currentAttackDamage * 1.5f);
        // mythicEnemy.currentMoveSpeed = mythicEnemy.currentMoveSpeed * 3f;
        // mythicEnemy.currentAttackSpeed = mythicEnemy.currentAttackSpeed * 1.5f;
        mythicEnemy.currentAttackRange = mythicEnemy.currentAttackRange * 2f;

        mythicEnemy.SetCanMove(true);
        RegisterEnemy(mythicEnemy);

        Debug.Log($"Mythic Enemy spawned at wave {currentWave}!");
    }

    /// <summary> 신화 적 스폰 전 빨간색 경고 이펙트를 표시합니다. </summary>
    private IEnumerator ShowMythicWarning()
    {
        // 경고 메시지 표시
        UIManager.Instance?.ShowMessage($"<color=red>⚠️ 신화 적 출현! ⚠️</color>", 2f);
        
        // WarningImage로 간단한 깜빡깜박 효과
        if (WarningImage != null)
        {
            WarningImage.gameObject.SetActive(true);
            WarningImage.color = new Color(1f, 0f, 0f, 0f); // 투명하게 시작
            
            // DOTween으로 깜빡깜박 애니메이션
            Sequence warningSequence = DOTween.Sequence();
            warningSequence.Append(WarningImage.DOFade(0.5f, 0.2f))
                          .Append(WarningImage.DOFade(0.1f, 0.2f))
                          .Append(WarningImage.DOFade(0.5f, 0.2f))
                          .Append(WarningImage.DOFade(0.1f, 0.2f))
                          .Append(WarningImage.DOFade(0.5f, 0.2f))
                          .Append(WarningImage.DOFade(0.1f, 0.2f))
                          .OnComplete(() => WarningImage.gameObject.SetActive(false));
            
            yield return warningSequence.WaitForCompletion();
        }

        yield return new WaitForSeconds(1f);
    }

    /// <summary> 현재 웨이브에 해당하는 신화 유닛 데이터를 반환합니다. </summary>
    private UnitData GetMythicEnemyDataForCurrentWave()
    {
        MythicRecipeConfig config = GameManager.Instance.queueManager.GetMythicRecipeConfig();
        if (config == null) return null;

        // 현재 웨이브에서 해금되는 신화 레시피 찾기
        foreach (MythicRecipe recipe in config.ActiveRecipes)
            if (recipe != null && recipe.unlockWave == currentWave && recipe.ResultUnit != null) return recipe.ResultUnit;

        // 현재 웨이브에 해당하는 레시피가 없으면 
        return null;
    }

    #endregion

    #region Register Enemy
    
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
    
    #endregion

    #region Enemy Base Destroyed

    /// <summary> 적 기지가 파괴되었을 때 호출됩니다. </summary>
    public void OnEnemyBaseDestroyed()
    {
        if (!isWaveActive || isBaseDestroyed) return;
        
        Debug.Log($"[WaveManager] Enemy base destroyed at wave {currentWave}!");
        isBaseDestroyed = true;
        
        // 남은 몬스터가 있다면 일괄 스폰
        if (enemiesSpawned < currentWaveEnemyCount) SpawnRemainingEnemies();
        else CheckWaveCompletion(); // 남은 몬스터가없으면 웨이브 완료 체크
    }

    /// <summary> 기지 파괴 시 남은 몬스터들을 일괄 스폰합니다. </summary>
    private void SpawnRemainingEnemies()
    {
        int remainingEnemies = currentWaveEnemyCount - enemiesSpawned;
        Debug.Log($"[WaveManager] Spawning {remainingEnemies} remaining enemies after base destruction");
        
        for (int i = 0; i < remainingEnemies; i++)
        {
            SpawnEnemy();
            enemiesSpawned++;
        }
    }

    #endregion

    #region Check Wave Completion

    private void CheckWaveCompletion()
    {
        // 기지가 파괴되었고, 모든 몬스터가 스폰되었으며, 모든 몬스터가 죽었을 때 웨이브 클리어
        if (isWaveActive && isBaseDestroyed && enemiesSpawned >= currentWaveEnemyCount && activeEnemies.Count == 0)
        {
            Debug.Log($"[WaveManager] CheckWaveCompletion - CompleteWave (Base destroyed, all enemies killed)");
            CompleteWave();
        }
    }
    
    private void CompleteWave()
    {
        isWaveActive = false;
        
        // 신화 해금 확인 및 메시지 표시
        bool hasMythicUnlock = CheckMythicUnlock();
        
        // 웨이브 클리어 보상 계산 및 지급
        int waveReward = CalculateWaveReward();
        GameManager.Instance.AddGold(waveReward);
        
        // 신화 해금이 있으면 지연 후 웨이브 클리어 메시지 표시, 없으면 즉시 표시
        if (hasMythicUnlock)
        {
            StartCoroutine(ShowWaveCompleteMessageDelayed(waveReward));
        }
        else
        {
            ShowWaveCompleteMessage(waveReward);
        }
        
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
    /// <returns>해금된 신화 유닛이 있는지 여부</returns>
    private bool CheckMythicUnlock()
    {
        bool hasUnlocked = false;
        
        // QueueManager에서 신화 레시피 설정 가져오기
        QueueManager queueManager = GameManager.Instance.queueManager;
        if (queueManager == null) return false;

        // QueueManager의 public 메서드를 통해 mythicRecipeConfig 가져오기
        MythicRecipeConfig config = queueManager.GetMythicRecipeConfig();
        
        if (config != null)
        {
            foreach (MythicRecipe recipe in config.ActiveRecipes)
            {
                if (recipe.unlockWave == currentWave && !recipe.isUnlocked)
                {
                    Debug.Log($"Mythic unit '{recipe.Id}' unlocked at wave {currentWave}!");
                    
                    recipe.isUnlocked = true;
                    hasUnlocked = true;

                    // 해금 알림 메시지
                    UIManager.Instance?.ShowMessage($"<color=yellow>✨ {recipe.ResultUnit?.unitName} 해금! ✨</color>", 3f);
                    
                    // QueueManager에 버튼 상태 업데이트 요청
                    queueManager.UpdateMythicButtonStates();
                }
            }
        }
        
        return hasUnlocked;
    }

    /// <summary> 웨이브 클리어 메시지를 즉시 표시합니다. </summary>
    private void ShowWaveCompleteMessage(int waveReward)
    {
        UIManager.Instance.ShowMessage($"Wave {currentWave} 클리어! \n 보상 : {waveReward} 골드");
    }

    /// <summary> 신화 해금 메시지 후 지연하여 웨이브 클리어 메시지를 표시합니다. </summary>
    private IEnumerator ShowWaveCompleteMessageDelayed(int waveReward)
    {
        // 신화 해금 메시지가 표시되는 시간(3초) + 여유시간(0.5초) 대기
        yield return new WaitForSeconds(3.5f);
        
        // 웨이브 클리어 메시지 표시
        UIManager.Instance.ShowMessage($"Wave {currentWave} 클리어! \n 보상 : {waveReward} 골드");
    }

    /// <summary> QueueManager의 mythicRecipeConfig를 순회하여 mythicEnemyWaves를 자동으로 채웁니다. </summary>
    private void InitializeMythicEnemyWaves()
    {
        // 기존 웨이브 리스트 초기화
        mythicEnemyWaves.Clear();

        if (config != null)
        {
            foreach (MythicRecipe recipe in config.ActiveRecipes)
                if (recipe != null && !mythicEnemyWaves.Contains(recipe.unlockWave)) mythicEnemyWaves.Add(recipe.unlockWave);

            // 웨이브 번호 순으로 정렬
            mythicEnemyWaves.Sort();
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
    
    // 기지 체력 관련 프로퍼티
    
    /// <summary> 적 기지에 데미지를 입힙니다. </summary>
    public void DamageEnemyBase(int amount)
    {
        if (amount <= 0) return;
        
        baseCurrentHealth = Mathf.Max(0, baseCurrentHealth - amount);
        UIManager.Instance?.UpdateEnemyBaseHealthUI();
        
        // 기지 파괴 시 웨이브 클리어 체크
        if (baseCurrentHealth <= 0) OnEnemyBaseDestroyed();
    }

    /// <summary> 웨이브 시작 시 기지 체력을 초기화하고 증가시킵니다. </summary>
    public void InitializeBaseHealthForWave(int waveNumber)
    {
        // 웨이브에 따른 최대 체력 증가
        baseMaxHealth = Mathf.RoundToInt(baseMaxHealth * baseHealthMultiplier);
        baseCurrentHealth = baseMaxHealth;
        
        UIManager.Instance?.UpdateEnemyBaseHealthUI();
        
        Debug.Log($"Wave {waveNumber}: Enemy base health initialized to {baseCurrentHealth}/{baseMaxHealth}");
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

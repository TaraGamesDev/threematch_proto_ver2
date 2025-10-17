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
    public UnitData enemyUnitData;
    public float spawnInterval = 2f;
    public int enemiesPerWave = 10;

    [SerializeField] private Image WarningImage;
    
    [Title("스폰 존 설정")]
    public RectTransform monsterSpawnZone;
    private float spawnRandomRange = 20f;


    [Tooltip("신화 적으로 스폰될 유닛 데이터")]
    [SerializeField] private MythicRecipeConfig config;
    [SerializeField, ReadOnly] private List<int> mythicEnemyWaves = new List<int>();

    
    [Header("웨이브 진행에 따른 증가율")]
    [SerializeField] float healthMultiplier = 1.2f;
    [SerializeField] float damageMultiplier = 1.1f;
    [SerializeField] float speedMultiplier = 1.05f;
    [SerializeField] float baseHealthMultiplier = 1.3f; // 웨이브당 체력 증가율


    [Title("오브젝트 풀")]
    [SerializeField, Min(0)] private int prewarmEnemyCount = 50;


    [Title("웨이브 진행상황")]
    [SerializeField, ReadOnly] private List<Enemy> activeEnemies = new List<Enemy>();
    [SerializeField, ReadOnly] private int currentWave = 1;
    [SerializeField, ReadOnly] private bool isWaveActive = false;
    [SerializeField, ReadOnly] private int currentWaveEnemyCount = 0;
    [SerializeField, ReadOnly] private int enemiesSpawned = 0;
    [SerializeField, ReadOnly] private int enemiesKilled = 0;
    [SerializeField, ReadOnly] private bool isBaseDestroyed = false;
    [SerializeField, ReadOnly] private int currentWaveGoldReward = 1; // 현재 웨이브의 골드 보상
    
    
    [Title("Enemy Base Health")]
    [SerializeField] private Transform enemyBaseTransform; // 적 기지 Transform
    [SerializeField] private int baseMaxHealth = 100;
    [SerializeField] private int baseCurrentHealth = 100;


    public int BaseCurrentHealth => baseCurrentHealth;
    public int BaseMaxHealth => baseMaxHealth;
    public Transform EnemyBaseTransform => enemyBaseTransform;
    public int CurrentWaveGoldReward => currentWaveGoldReward;
    
    
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
        
        // 웨이브별 골드 보상 미리 계산
        currentWaveGoldReward = GameManager.Instance.GetEnemyGoldReward(waveNumber);
        Debug.Log($"[WaveManager] Wave {waveNumber} gold reward: {currentWaveGoldReward}");
        
        // 웨이브 변경 이벤트 발생
        OnWaveChanged?.Invoke(currentWave);
        
        // 기지 체력 초기화 및 증가
        InitializeBaseHealthForWave(waveNumber);
        
        // 유닛들의 스탯 초기화 및 움직임 활성화
        UnitManager.Instance.OnBattleStart();
        
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
        Enemy enemy = GetEnemyFromPool();
        if (enemy == null) return;

        if (monsterSpawnZone == null) { Debug.LogWarning("WaveManager: monsterSpawnZone이 설정되지 않았습니다."); return; }

        // 스폰 위치에 랜덤성 추가
        Vector3 spawnPosition = monsterSpawnZone.position;
        spawnPosition.x += Random.Range(-spawnRandomRange, spawnRandomRange);
        enemy.transform.position = spawnPosition;
        enemy.transform.rotation = Quaternion.identity;

        // UnitData로 기본 스탯 설정
        enemy.Init(enemyUnitData);

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

        // 2. 신화 적 스폰 (풀 사용하지 않고 직접 생성)
        Enemy mythicEnemy = CreateMythicEnemyDirectly(currentMythicData);
        if (mythicEnemy == null) yield break;

        // 신화 적 스폰 위치에도 랜덤성 추가
        Vector3 mythicSpawnPosition = monsterSpawnZone.position;
        mythicSpawnPosition.x += Random.Range(-spawnRandomRange, spawnRandomRange);
        mythicEnemy.transform.position = mythicSpawnPosition;
        mythicEnemy.transform.rotation = Quaternion.identity;

        // 신화 적 데이터로 설정
        mythicEnemy.transform.localScale = Vector3.one * 2f;  // 크기 2배로 설정 (보스 느낌)
        mythicEnemy.Init(enemyUnitData);
        
        // 웨이브에 따른 스탯 증가 적용 (일반 몹과 동일)
        ApplyWaveScaling(mythicEnemy);
        
        // 보스 신화 적 추가 보너스: 공격력 2배, 체력 5배
        mythicEnemy.currentHealth = Mathf.RoundToInt(mythicEnemy.currentHealth * 10f);
        mythicEnemy.currentAttackDamage = Mathf.RoundToInt(mythicEnemy.currentAttackDamage * 2f);

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
        // if (isWaveActive && isBaseDestroyed && enemiesSpawned >= currentWaveEnemyCount && activeEnemies.Count == 0)
        
        // 모든 몬스터가 스폰되었으며, 모든 몬스터가 죽었을 때 웨이브 클리어 (기지 파괴 조건 제거)
        if (isWaveActive && enemiesSpawned >= currentWaveEnemyCount && activeEnemies.Count == 0)
        {
            Debug.Log($"[WaveManager] CheckWaveCompletion - CompleteWave (All enemies killed)");
            CompleteWave();
        }
    }
    
    private void CompleteWave()
    {
        // 소환 턴으로 전환
        GameManager.Instance.CompleteWave();
        
        currentWave++;
        isWaveActive = false;

        // 유닛들 초기화 - 전투 종료 후 원래 진영으로 돌아가도록  (유닛은 지속됨)
        UnitManager.Instance.OnBattleEnd();

        // 신화 관련 
        
        // 신화 해금 확인 및 메시지 표시
        bool hasMythicUnlock = CheckMythicUnlock();
        
        // 웨이브 클리어 보상 계산 및 지급
        int waveReward = CalculateWaveReward();
        GameManager.Instance.AddGold(waveReward);
        
        // 신화 해금이 있으면 지연 후 웨이브 클리어 메시지 표시, 없으면 즉시 표시
        if (hasMythicUnlock) StartCoroutine(ShowWaveCompleteMessageDelayed(waveReward));
        else ShowWaveCompleteMessage(waveReward);
        
        // UI 업데이트
        UIManager.Instance.UpdateGoldTextUI();
        UIManager.Instance.UpdateWaveText(currentWave);
                
        
    }
    
    // 웨이브 클리어 보상 계산
    private int CalculateWaveReward()
    {
        // 현재 웨이브 * 뽑기에 필요한 돈
        // int drawCost = GameManager.Instance.goldCostPerBlock;
        // return currentWave/drawCost + drawCost*5;

        // 지금은 웨이브 클리어 골드 보상 없음 
        return 0;
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
    private void ShowWaveCompleteMessage(int waveReward = 0)
    {
        if(waveReward > 0) UIManager.Instance.ShowMessage($"Wave {currentWave-1} 클리어! \n 보상 : {waveReward} 골드");
        else UIManager.Instance.ShowMessage($"Wave {currentWave-1} 클리어!");
    }

    /// <summary> 신화 해금 메시지 후 지연하여 웨이브 클리어 메시지를 표시합니다. </summary>
    private IEnumerator ShowWaveCompleteMessageDelayed(int waveReward = 0)
    {
        // 신화 해금 메시지가 표시되는 시간(3초) + 여유시간(0.5초) 대기
        yield return new WaitForSeconds(3.5f);
        
        // 웨이브 클리어 메시지 표시
        if(waveReward > 0) UIManager.Instance.ShowMessage($"Wave {currentWave-1} 클리어! \n 보상 : {waveReward} 골드");
        else UIManager.Instance.ShowMessage($"Wave {currentWave-1} 클리어!");
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
        if (PoolManager.Instance == null || enemyUnitData == null || enemyUnitData.unitPrefab == null) return;
        
        string poolKey = enemyUnitData.unitName;
        PoolManager.Instance.RegisterPool(poolKey, enemyUnitData.unitPrefab, prewarmEnemyCount, null);
    }

    private Enemy GetEnemyFromPool()
    {
        Enemy enemy = null;

        if (PoolManager.Instance != null) enemy = PoolManager.Instance.Get<Enemy>(enemyUnitData.unitName, monsterSpawnZone);
        if (enemy == null) Debug.LogWarning("Enemy Pool에서 오브젝트를 꺼내오는데 실패했습니다.");
        
        return enemy;
    }
    
    /// <summary> 신화 유닛 데이터의 프리팹을 사용하여 신화 적을 직접 생성합니다. (풀 사용 안함) </summary>
    private Enemy CreateMythicEnemyDirectly(UnitData mythicData)
    {
        if (mythicData == null || mythicData.unitPrefab == null) { Debug.LogWarning("WaveManager: 신화 유닛 데이터 또는 UnitData의 프리팹이 null입니다."); return null; }
        
        // 신화 유닛 프리팹을 직접 인스턴스화
        GameObject mythicEnemyObj = Instantiate(mythicData.unitPrefab, monsterSpawnZone);
        Enemy mythicEnemy = mythicEnemyObj.GetComponent<Enemy>();
        
        if (mythicEnemy == null) 
        {
            Debug.LogWarning($"WaveManager: {mythicData.unitName} 프리팹에 Enemy 컴포넌트가 없습니다. Enemy 컴포넌트를 추가합니다.");
            mythicEnemy = mythicEnemyObj.AddComponent<Enemy>();
        }

        // 신화 적은 동물이 아니므로 Animal 컴포넌트를 비활성화
        Animal animal = mythicEnemy.GetComponent<Animal>();
        if (animal != null) animal.enabled = false;
    
        return mythicEnemy;
    }

    #endregion


}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using System;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// Central coordinator for runtime game state. Handles player stats, wave scheduling,
/// and provides shared references for other systems.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Title("Player Economy & Progression")]
    [SerializeField] private MoneyDataList moneyDataList;
    [SerializeField] private int playerLevel = 1;
    [SerializeField] private int currentExp = 0;
    [SerializeField] private int expToNextLevel = 5;
    [SerializeField] private int expGrowthPerLevel = 5;
    
    
    [Title("Cost")]
    [SerializeField, ReadOnly] private int currentSpawnCost = 10; // 현재 블럭 스폰 비용
    [SerializeField] TMP_Text currentSpawnCostText;
    [SerializeField, ReadOnly] public int ProbabilityUpgradeCost = 100; // 현재 확률 업그레이드 비용
    


    [Title("Player Health")]
    [SerializeField] private int maxHealth = 10;
    [SerializeField] private int currentHealth = 10;
    [SerializeField] private int playerShield = 0;


    [Title("Manager References")]
    public QueueManager queueManager;
    public UnitManager unitManager;
    public WaveManager waveManager;
    public UIManager uiManager;
    public LevelUpUpgradeSystem upgradeSystem;
    public CombatManager combatManager;


    [Title("Player Avatar")]
    public Transform playerTransform;
    

    [Title("Wave Timing")]
    [SerializeField] private float waveStartDelaySeconds = 5f;

    // [Title("Game Speed")]
    private List<float> speedMultipliers = new List<float> { 0.5f, 1f, 2f, 4f, 5f };
    private int currentSpeedIndex = 1;

    private int currentGold;
    private Coroutine pendingWaveRoutine;
    private int initialExpToNextLevel;

    public int Gold => currentGold;
    public int PlayerLevel => playerLevel;
    public int CurrentExp => currentExp;
    public int ExpToNextLevel => expToNextLevel;
    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public int PlayerShield => playerShield;
    public int CurrentSpawnCost => currentSpawnCost;
    public MoneyDataList MoneyDataList => moneyDataList;
    public float CurrentSpeedMultiplier => speedMultipliers[currentSpeedIndex];
    
    // 이벤트
    public static Action OnGoldChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        initialExpToNextLevel = expToNextLevel;
        
        // 초기 배속 설정
        currentSpeedIndex = 1;
        Time.timeScale = speedMultipliers[currentSpeedIndex];

        // Attempt to auto-wire references when possible; scenes can still override via inspector.
        queueManager ??= FindObjectOfType<QueueManager>();
        unitManager ??= FindObjectOfType<UnitManager>();
        waveManager ??= FindObjectOfType<WaveManager>();
        uiManager ??= FindObjectOfType<UIManager>();
        upgradeSystem ??= FindObjectOfType<LevelUpUpgradeSystem>();
        combatManager ??= FindObjectOfType<CombatManager>();
    }

    public void InitializeMoneyDataList(){
        // MoneyData에서 초기 골드 설정
        moneyDataList = DataManager.Instance.moneyDataList;
        currentGold = Mathf.Max(0, moneyDataList.moneyBaseData.INITIAL_MONEY);
        currentSpawnCost = moneyDataList.moneyBaseData.SPAWN_INITIAL;
        currentSpawnCostText.text = $"{currentSpawnCost} G"; // 텍스트 업데이트
        ProbabilityUpgradeCost = moneyDataList.moneyBaseData.UPGRADE;
        
        // 이벤트 발생
        OnGoldChanged?.Invoke(); // 버튼 텍스트 업데이트 
    }

    public void InitialisePlayerState()
    {
        currentHealth = maxHealth;
        if (currentHealth <= 0) currentHealth = maxHealth;
        playerShield = Mathf.Max(0, playerShield);

        uiManager?.UpdateGoldTextUI();
        uiManager?.UpdateExpTextUI();
        uiManager?.UpdatePlayerHealthUI();
        uiManager?.UpdateLevelText(playerLevel);
        uiManager?.UpdateWaveText(waveManager.GetCurrentWave());
    }

    public void QueueInitialWave()
    {
        if (waveManager == null) return;
        QueueWaveStart(Mathf.Max(1, waveManager.GetCurrentWave()));
    }

    public void QueueWaveStart(int waveNumber)
    {
        if (waveManager == null) return;

        if (pendingWaveRoutine != null) StopCoroutine(pendingWaveRoutine);
        pendingWaveRoutine = StartCoroutine(StartWaveAfterDelay(waveNumber));
    }

    private IEnumerator StartWaveAfterDelay(int waveNumber)
    {
        yield return new WaitForSeconds(Mathf.Max(0f, waveStartDelaySeconds));
        waveManager.StartWave(waveNumber);
        uiManager?.UpdateWaveText(waveNumber);
        pendingWaveRoutine = null;
    }

    public void CompleteWave()
    {
        if (waveManager == null) return;
        QueueWaveStart(waveManager.GetCurrentWave() + 1);
    }

    public void AddGold(int amount)
    {
        if (amount <= 0) return;
        currentGold += amount;
        uiManager?.UpdateGoldTextUI();
        OnGoldChanged?.Invoke();
    }

    public bool SpendGold(int amount)
    {
        if (amount <= 0) return true; 
        if (currentGold < amount) return false;

        currentGold -= amount;
        uiManager?.UpdateGoldTextUI();
        OnGoldChanged?.Invoke();
        return true;
    }

    public void AddExp(int amount = 1)
    {
        if (amount <= 0) return;

        currentExp += amount;
        bool leveledUp = false;

        while (currentExp >= expToNextLevel)
        {
            currentExp -= expToNextLevel;
            playerLevel++;
            expToNextLevel += expGrowthPerLevel;
            leveledUp = true;
        }

        uiManager?.UpdateExpTextUI();

        if (leveledUp)
        {
            uiManager?.UpdateLevelText(playerLevel);
            upgradeSystem?.OnLevelUp();
        }
    }

    public void TakeDamage(int amount)
    {
        if (amount <= 0) return;

        int remainingDamage = amount;

        if (playerShield > 0)
        {
            int absorbed = Mathf.Min(playerShield, remainingDamage);
            playerShield -= absorbed;
            remainingDamage -= absorbed;
        }

        if (remainingDamage <= 0)
        {
            uiManager?.UpdatePlayerHealthUI();
            return;
        }

        currentHealth = Mathf.Max(0, currentHealth - remainingDamage);
        uiManager?.UpdatePlayerHealthUI();

        if (currentHealth <= 0)
        {
            HandlePlayerDefeat();
        }
    }

    public void HealPlayer(int amount)
    {
        if (amount <= 0) return;
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        uiManager?.UpdatePlayerHealthUI();
    }

    public void IncreaseMaxHealth(int amount)
    {
        if (amount <= 0) return;
        maxHealth += amount;
        currentHealth += amount;
        uiManager?.UpdatePlayerHealthUI();
    }

    public void AddShield(int amount)
    {
        if (amount <= 0) return;
        playerShield += amount;
        uiManager?.UpdatePlayerHealthUI();
    }

    /// <summary> 블럭 스폰 비용을 지불하고 다음 스폰 비용을 증가시킵니다. </summary>
    public bool SpendSpawnCost()
    {
        if (moneyDataList?.moneyBaseData == null){Debug.LogWarning("GameManager: moneyDataList.moneyBaseData is null -> return false"); return false; }
        
        if (SpendGold(currentSpawnCost))
        {
            currentSpawnCost += moneyDataList.moneyBaseData.SPAWN_ADDED; // 다음 스폰 비용 증가
            currentSpawnCostText.text = $"{currentSpawnCost} G";
            return true;
        }
        return false;
    }

    /// <summary> 현재 웨이브에서 적을 죽였을 때 받을 골드를 계산합니다. </summary>
    public int GetEnemyGoldReward(int currentWave)
    {
        if (moneyDataList?.waveMoneyDatas == null){Debug.LogWarning("GameManager: moneyDataList.waveMoneyDatas is null -> return 1"); return 1; }
        
        foreach (var waveMoneyData in moneyDataList.waveMoneyDatas)
        {
            // WAVE_MAX가 -1이면 무한대
            bool isInRange = currentWave >= waveMoneyData.waveMin && (waveMoneyData.waveMax == -1 || currentWave <= waveMoneyData.waveMax);
            if (isInRange) return waveMoneyData.enemyGold;
        }
        
        Debug.LogWarning("[GameManager] GetEnemyGoldReward : 속한 웨이브가 없습니다. -> return 1");
        return 1; // 기본값
    }

    private void HandlePlayerDefeat()
    {
        waveManager?.StopWave();
        uiManager?.ShowMessage("Game Over");
        
        // 게임 오버 패널 표시
        uiManager?.ShowGameOverPanel();
    }

    /// <summary> 게임을 다시 시작합니다. (씬 리로드) </summary>
    public void RestartGame()
    {
        // 현재 씬을 다시 로드
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    /// <summary> 배속을 다음 단계로 순환시킵니다. </summary>
    public void CycleGameSpeed()
    {
        if (speedMultipliers == null || speedMultipliers.Count == 0) return;
        
        // 다음 인덱스로 이동 (마지막이면 0으로)
        currentSpeedIndex = (currentSpeedIndex + 1) % speedMultipliers.Count;
        
        // Time.timeScale 적용
        Time.timeScale = speedMultipliers[currentSpeedIndex];
        
        Debug.Log($"[GameManager] Game speed changed to {speedMultipliers[currentSpeedIndex]}x");
    }

}

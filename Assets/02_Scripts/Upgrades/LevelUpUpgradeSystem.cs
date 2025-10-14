using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Sirenix.OdinInspector;
using BansheeGz.BGDatabase;

/// <summary>
/// 레벨업 시 업그레이드 능력을 관리하는 시스템
/// BGDatabase의 UpgradeAbility 데이터를 활용하여 랜덤 능력 선택 및 적용
/// </summary>
public class LevelUpUpgradeSystem : MonoBehaviour
{
    public static LevelUpUpgradeSystem Instance { get; private set; }

    [Title("Upgrade Settings")]
    [SerializeField] private int upgradeChoicesCount = 3;
    [SerializeField] private int rerollCost = 10;

    [Title("UI References")]
    [SerializeField] private GameObject upgradeCard;
    [SerializeField] private Transform upgradeCardsContainer;
    [SerializeField] private GameObject LevelUpPanel;

    // 현재 플레이어의 업그레이드된 능력들 (ID를 키로 사용)
    [ShowInInspector, ReadOnly] private Dictionary<int, PlayerUpgrade> playerUpgrades = new Dictionary<int, PlayerUpgrade>();
    
    // 현재 선택 가능한 업그레이드들
    private List<UpgradeChoice> currentChoices = new List<UpgradeChoice>();
    
    // 모든 업그레이드 능력 캐시 (초기화 시 한번만 로드)
    [ShowInInspector, ReadOnly] private List<UpgradeAbility> allAbilitiesCache = new List<UpgradeAbility>();
    private bool isAbilitiesCacheInitialized = false;
    
    
    // 이벤트
    public static event Action<List<UpgradeChoice>> OnUpgradeChoicesGenerated;
    public static event Action<PlayerUpgrade> OnUpgradeSelected;
    public static event Action OnUpgradePanelClosed;

    #region Singleton

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    #endregion

    #region Initialize

    /// <summary> 업그레이드 능력 캐시를 초기화합니다. </summary>
    public void InitializeAbilitiesCache()
    {
        if (isAbilitiesCacheInitialized) return;
        
        try
        {
            // BGDatabase에서 모든 업그레이드 능력 가져오기
            var allAbilities = UpgradeAbility.FindEntities(null);
            
            if (allAbilities != null && allAbilities.Count > 0)
            {
                allAbilitiesCache = new List<UpgradeAbility>(allAbilities);
                isAbilitiesCacheInitialized = true;
                Debug.Log($"LevelUpUpgradeSystem: {allAbilitiesCache.Count}개 업그레이드 능력 캐싱 완료.");
            }
            else Debug.LogWarning("LevelUpUpgradeSystem: BGDatabase에서 업그레이드 능력을 찾을 수 없습니다.");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"LevelUpUpgradeSystem: 능력 캐시 초기화 중 오류 발생: {e.Message}");
        }

        LevelUpPanel.SetActive(false);
    }

    #endregion

    [Sirenix.OdinInspector.Button("OnLevelUp")]
    public void OnLevelUp()
    {
        GenerateUpgradeChoices();
        ShowUpgradeSelectionPanel();
    }

    #region Generate Upgrade Choices

    /// <summary> 업그레이드 선택지를 생성합니다. </summary>
    private void GenerateUpgradeChoices()
    {
        currentChoices.Clear();
        ClearUpgradeCards(); // 기존 카드들 제거
        
        // 캐싱 안됐으면 캐싱 
        if (!isAbilitiesCacheInitialized) InitializeAbilitiesCache();
        if (allAbilitiesCache.Count == 0){ Debug.LogWarning("LevelUpUpgradeSystem: No upgrade abilities found in cache"); return;}

        // 캐시에서 사용 가능한 능력들 필터링
        var availableAbilities = allAbilitiesCache.Where(ability => 
            !playerUpgrades.ContainsKey(ability.Index) // 새로운 능력이면 후보로 추가 
            || playerUpgrades[ability.Index].currentRank < ability.MAX_RANK // 최대 랭크에 도달하지 않은 능력이면 후보로 추가
        ).ToList();

        // 랜덤하게 선택
        var selectedAbilities = availableAbilities.OrderBy(x => UnityEngine.Random.value)
                                                 .Take(upgradeChoicesCount) // 선택지 개수만큼 선택
                                                 .ToList();

        // 선택지 생성 및 UI 프리팹 생성
        for (int i = 0; i < selectedAbilities.Count; i++)
        {
            var ability = selectedAbilities[i];
            var choice = new UpgradeChoice
            {
                upgrade = ability,
                currentRank = GetAbilityRank(ability.ABILITY_ID),
                nextRank = GetAbilityRank(ability.Index) + 1
            };
            
            currentChoices.Add(choice);
            
            // UI 프리팹 생성
            CreateUpgradeCard(choice, i);
        }

        // 이벤트 발생
        OnUpgradeChoicesGenerated?.Invoke(currentChoices); 
        
        Debug.Log($"LevelUpUpgradeSystem: Generated {currentChoices.Count} upgrade choices");
    }

    private void CreateUpgradeCard(UpgradeChoice choice, int cardIndex)
    {
        if (upgradeCard == null || upgradeCardsContainer == null){ Debug.LogWarning("LevelUpUpgradeSystem: upgradeCard 또는 upgradeCardsContainer가 설정되지 않았습니다."); return; }

        // 프리팹 복사 생성
        GameObject cardInstance = Instantiate(upgradeCard, upgradeCardsContainer);
        cardInstance.SetActive(true);

        // 카드에 선택지 정보 설정
        var upgradeCardUI = cardInstance.GetComponent<UpgradeCardUI>();
        if (upgradeCardUI != null)
        {
            upgradeCardUI.SetUpgradeChoice(choice);
            upgradeCardUI.SetCardIndex(cardIndex);
        }
        else Debug.LogWarning($"LevelUpUpgradeSystem: {cardInstance.name}에 UpgradeCardUI 컴포넌트가 없습니다.");
    }

    #endregion


    /// <summary> 특정 업그레이드를 선택합니다. </summary>
    public void SelectUpgrade(int choiceIndex)
    {
        if (choiceIndex < 0 || choiceIndex >= currentChoices.Count){ Debug.LogWarning($"LevelUpUpgradeSystem: Invalid choice index {choiceIndex}"); return;}

        var selectedChoice = currentChoices[choiceIndex];
        var upgrade = selectedChoice.upgrade;
        
        // 이미 있는 능력이면 레벨업 || 플레이어 업그레이드에 추가 
        if (playerUpgrades.ContainsKey(upgrade.ABILITY_ID)) playerUpgrades[upgrade.ABILITY_ID].currentRank++;
        else playerUpgrades[upgrade.ABILITY_ID] = new PlayerUpgrade{ ability = upgrade, currentRank = 1, };

        // 이벤트 발생
        OnUpgradeSelected?.Invoke(playerUpgrades[upgrade.ABILITY_ID]);
        
        // 패널 닫기
        HideUpgradeSelectionPanel();
        
        Debug.Log($"LevelUpUpgradeSystem: Selected upgrade '{upgrade.name}' (Rank {playerUpgrades[upgrade.ABILITY_ID].currentRank})");
    }

    /// <summary> 업그레이드 선택을 리롤합니다. </summary>
    public void RerollUpgrades()
    {
        if (GameManager.Instance.Gold < rerollCost)
        {
            UIManager.Instance?.ShowMessage("골드가 부족합니다!", 2f);
            return;
        }

        if (GameManager.Instance.SpendGold(rerollCost)) GenerateUpgradeChoices();
    }

    #region Utility Methods

    /// <summary> 특정 능력의 현재 랭크를 반환합니다. </summary>
    public int GetAbilityRank(int abilityId)
    {
        return playerUpgrades.ContainsKey(abilityId) ? playerUpgrades[abilityId].currentRank : 0;
    }

    /// <summary> 모든 업그레이드 카드를 제거합니다. </summary>
    private void ClearUpgradeCards()
    {
        if (upgradeCardsContainer == null) return;
        for (int i = upgradeCardsContainer.childCount - 1; i >= 0; i--)  DestroyImmediate(upgradeCardsContainer.GetChild(i).gameObject);
    }

    #endregion

    #region Show/Hide Upgrade Selection Panel
    private void ShowUpgradeSelectionPanel()
    {
        if (LevelUpPanel != null) LevelUpPanel.SetActive(true);
    }

    private void HideUpgradeSelectionPanel()
    {
        if (LevelUpPanel != null) LevelUpPanel.SetActive(false);
        OnUpgradePanelClosed?.Invoke();
    }

    #endregion

}

/// <summary>
/// 플레이어가 획득한 업그레이드 정보
/// </summary>
[System.Serializable]
public class PlayerUpgrade
{
    public UpgradeAbility ability;
    public int currentRank;
}

/// <summary>
/// 업그레이드 선택지 정보
/// </summary>
[System.Serializable]
public class UpgradeChoice
{
    public UpgradeAbility upgrade;
    public int currentRank;
    public int nextRank;
}

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Sirenix.OdinInspector;
using System;

/// <summary>
/// 확률 업그레이드 버튼 컴포넌트
/// BGDatabase의 Probability 데이터를 활용하여 레벨별 확률을 적용합니다.
/// </summary>
public class ProbabilityUpgradeButton : MonoBehaviour
{
    [Title("UI References")]
    [SerializeField] private Button upgradeButton;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text costText;

    private void Awake()
    {
        if (upgradeButton != null) upgradeButton.onClick.AddListener(OnUpgradeButtonClicked);
        
        // 골드 변경 이벤트 구독
        GameManager.OnGoldChanged += UpdateUI; // 골드가 변경될 때마다 버튼 상태 업데이트
    }
    
    private void OnDestroy()
    {
        // 이벤트 구독 해제
        GameManager.OnGoldChanged -= UpdateUI;
    }

    #region Public Methods

    /// <summary>
    /// 다음 레벨로 업그레이드 가능한지 확인합니다.
    /// </summary>
    public bool CanUpgrade()
    {
        int currentLevel = DatabaseProbabilitySystem.CurrentProbabilityLevel;
        if (currentLevel >= DatabaseProbabilitySystem.MaxLevel) return false;
        
        return GameManager.Instance != null && GameManager.Instance.Gold >= GameManager.Instance.ProbabilityUpgradeCost;
    }

    #endregion

    #region 버튼 클릭 이벤트 
    private void OnUpgradeButtonClicked()
    {
        if (!CanUpgrade()){Debug.LogWarning("[ProbabilityUpgradeButton] CanUpgrade is false"); return;}

        if (GameManager.Instance.SpendGold(GameManager.Instance.ProbabilityUpgradeCost))
        {
            int newLevel = DatabaseProbabilitySystem.CurrentProbabilityLevel + 1;
            DatabaseProbabilitySystem.CurrentProbabilityLevel = newLevel;
            UpdateUI();
            
            // 성공 메시지 표시
            UIManager.Instance?.ShowMessage($"확률 레벨이 {newLevel}로 상승했습니다!", 2f);
            
            Debug.Log($"Probability upgraded to level {newLevel}");
        }
    }

    #endregion  
    
    #region UI Update
    private void UpdateUI()
    {
        UpdateButtonState();
        UpdateTexts();
    }

    private void UpdateButtonState()
    {
        if (upgradeButton == null) {Debug.LogWarning("ProbabilityUpgradeButton: upgradeButton is not assigned"); return;}
        upgradeButton.interactable = CanUpgrade();
    }

    private void UpdateTexts()
    {
        int currentLevel = DatabaseProbabilitySystem.CurrentProbabilityLevel;
        int maxLevel = DatabaseProbabilitySystem.MaxLevel;
        
        // 레벨 텍스트
        if (levelText != null)
        {
            if (currentLevel >= maxLevel) levelText.text = $"Lv.{currentLevel} (MAX)";
            else levelText.text = $"현재 : Lv.{currentLevel}";
        }

        // 비용 텍스트
        if (costText != null)
        {
            if (currentLevel >= maxLevel) costText.text = "MAX";
            else costText.text = $"{GameManager.Instance.ProbabilityUpgradeCost} 골드";
        }
    }

    #endregion
}

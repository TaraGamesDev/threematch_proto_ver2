using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Sirenix.OdinInspector;

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

    [Space(10f)]
    [SerializeField] private int UpgradeCost = 100;

    private void Awake()
    {
        if (upgradeButton != null) upgradeButton.onClick.AddListener(OnUpgradeButtonClicked);
    }

    #region Public Methods

    /// <summary>
    /// 확률 레벨을 설정합니다.
    /// </summary>
    public void SetProbabilityLevel(int level)
    {
        DatabaseProbabilitySystem.CurrentProbabilityLevel = level;
        UpdateUI();
    }

    /// <summary>
    /// 다음 레벨로 업그레이드 가능한지 확인합니다.
    /// </summary>
    public bool CanUpgrade()
    {
        int currentLevel = DatabaseProbabilitySystem.CurrentProbabilityLevel;
        if (currentLevel >= DatabaseProbabilitySystem.MaxLevel) return false;
        
        return GameManager.Instance != null && GameManager.Instance.Gold >= UpgradeCost;
    }

    #endregion

    #region 버튼 클릭 이벤트 
    private void OnUpgradeButtonClicked()
    {
        if (!CanUpgrade()){Debug.LogWarning("[ProbabilityUpgradeButton] CanUpgrade is false"); return;}

        if (GameManager.Instance.SpendGold(UpgradeCost))
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
            else levelText.text = $"Lv.{currentLevel}";
        }

        // 비용 텍스트
        if (costText != null)
        {
            if (currentLevel >= maxLevel) costText.text = "MAX";
            else costText.text = $"{UpgradeCost} 골드";
        }
    }

    #endregion
}

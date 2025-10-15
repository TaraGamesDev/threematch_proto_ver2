using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Sirenix.OdinInspector;

/// <summary>
/// 개별 업그레이드 카드 UI 컴포넌트
/// 업그레이드 선택지의 정보를 표시하고 선택 이벤트를 처리합니다.
/// </summary>
public class UpgradeCardUI : MonoBehaviour
{
    [Title("UI References")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text rankText;
    [SerializeField] private Button selectButton;
    [SerializeField] private GameObject newUpgradeIndicator;

    private UpgradeChoice currentChoice;
    private int cardIndex;

    #region Initialize

    private void Awake()
    {
        InitializeButton();
    }

    private void InitializeButton()
    {
        if (selectButton != null)
        {
            selectButton.onClick.RemoveAllListeners();
            selectButton.onClick.AddListener(OnCardSelected);
        }
    }

    #endregion

    #region Set CradUI

    public void SetUpgradeChoice(UpgradeChoice choice)
    {
        currentChoice = choice;
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (currentChoice == null) return;

        var ability = currentChoice.upgrade;

        // 기본 정보 설정
        if (nameText != null) nameText.text = ability.ABILITY_NAME;

        if (descriptionText != null) descriptionText.text = ability.DESCRIPTION;

        // 랭크 정보 설정
        if (rankText != null) rankText.text = $"Rank {currentChoice.currentRank} → {currentChoice.nextRank}";

        // 새로운 업그레이드인지 확인
        if (newUpgradeIndicator != null) newUpgradeIndicator.SetActive(currentChoice.currentRank == 0);
    }

    /// <summary>
    /// 카드의 인덱스를 설정합니다.
    /// </summary>
    public void SetCardIndex(int index)
    {
        cardIndex = index;
    }

    #endregion


    private void OnCardSelected()
    {
        if (currentChoice == null) return;

        // LevelUpUpgradeSystem에 선택 알림
        if (LevelUpUpgradeSystem.Instance != null) LevelUpUpgradeSystem.Instance.SelectUpgrade(cardIndex);
        Debug.Log($"UpgradeCardUI: Selected upgrade '{currentChoice.upgrade.ABILITY_NAME}' (Index: {cardIndex})");
    }

}

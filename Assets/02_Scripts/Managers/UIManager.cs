using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Handles HUD updates and lightweight feedback for gold, experience, wave state, and messages.
/// Designed to tolerate missing bindings so scenes remain editable without null reference spam.
/// </summary>
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("HUD Elements")]
    [SerializeField] private TMP_Text goldText;
    [SerializeField] private TMP_Text expText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text waveText;

    
    [Header("Player Health")]
    [SerializeField] private Image playerHealthBar;
    [SerializeField] private TMP_Text healthText;
    
    
    [Header("Enemy Base Health")]
    [SerializeField] private TMP_Text enemyBaseHealthText;
    [SerializeField] private Image enemyBaseHealthBar;


    [Header("Message Banner")]
    [SerializeField] private CanvasGroup messagePanel;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private float messageFadeDuration = 0.35f;
    [SerializeField] private float messageVisibleDuration = 2.5f;

    [Header("Probability Upgrade")]
    [SerializeField] private Button probabilityInfoButton;


    private Coroutine messageRoutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (messagePanel != null)
        {
            messagePanel.alpha = 0f;
            messagePanel.gameObject.SetActive(false);
        }
        
        if (probabilityInfoButton != null) probabilityInfoButton.onClick.AddListener(DatabaseProbabilitySystem.ShowCurrentProbabilityInfo);
    }

    public void UpdateGoldTextUI()
    {
        if (goldText == null) return;
        int gold = GameManager.Instance != null ? GameManager.Instance.Gold : 0;
        goldText.text = $"Gold {gold}";
    }

    public void UpdateExpTextUI()
    {
        if (expText == null) return;
        if (GameManager.Instance == null)
        {
            expText.text = "XP 0/0";
            return;
        }

        expText.text = $"XP {GameManager.Instance.CurrentExp}/{GameManager.Instance.ExpToNextLevel}";
    }

    public void UpdateLevelText(int level)
    {
        if (levelText == null) return;
        levelText.text = $"Lv {level}";
    }

    public void UpdatePlayerHealthUI()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("GameManager.Instance is null");
            if (healthText != null) healthText.text = "0/0";
            if (playerHealthBar != null) playerHealthBar.fillAmount = 0f;
            return;
        }

        // 텍스트 업데이트
        if (healthText != null) healthText.text = $"{GameManager.Instance.CurrentHealth}/{GameManager.Instance.MaxHealth}";
        
        // HealthBar 업데이트
        if (playerHealthBar != null)
        {
            float healthPercentage = (float)GameManager.Instance.CurrentHealth / GameManager.Instance.MaxHealth;
            playerHealthBar.fillAmount = healthPercentage;
        }
    }

    public void UpdateWaveText(int wave)
    {
        if (waveText == null) return;
        waveText.text = $"Wave {wave}";
    }

    public void UpdateEnemyBaseHealthUI()
    {
        if (WaveManager.Instance == null) return;
        
        // 텍스트 업데이트
        if (enemyBaseHealthText != null) enemyBaseHealthText.text = $"{WaveManager.Instance.BaseCurrentHealth}/{WaveManager.Instance.BaseMaxHealth}";
        
        // HealthBar 업데이트
        if (enemyBaseHealthBar != null)
        {
            float healthPercentage = (float)WaveManager.Instance.BaseCurrentHealth / WaveManager.Instance.BaseMaxHealth;
            enemyBaseHealthBar.fillAmount = healthPercentage;
        }
    }

    public void ShowMessage(string message)
    {
        ShowMessage(message, messageVisibleDuration);
    }

    public void ShowMessage(string message, float duration)
    {
        if (messagePanel == null || messageText == null) return;

        if (messageRoutine != null)
        {
            StopCoroutine(messageRoutine);
            messageRoutine = null;
        }

        messageRoutine = StartCoroutine(ShowMessageRoutine(message, duration));
    }

    private IEnumerator ShowMessageRoutine(string message, float duration)
    {
        messagePanel.gameObject.SetActive(true);
        messageText.text = message;

        messagePanel.DOFade(1f, messageFadeDuration).SetEase(Ease.OutQuad);
        yield return new WaitForSeconds(duration);
        messagePanel.DOFade(0f, messageFadeDuration).SetEase(Ease.InQuad);
        yield return new WaitForSeconds(messageFadeDuration);

        messagePanel.gameObject.SetActive(false);
        messageRoutine = null;
    }
}

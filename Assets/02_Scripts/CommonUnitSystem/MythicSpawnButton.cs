using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using Sirenix.OdinInspector;

/// <summary>
/// 신화 유닛 소환을 위한 버튼 컴포넌트입니다.
/// 조합이 감지되면 노란색으로 바뀌고 흔들리는 애니메이션을 재생합니다.
/// </summary>
public class MythicSpawnButton : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private Button button;
    [SerializeField] private Image buttonImage;
    [SerializeField] private TextMeshProUGUI buttonText;
    [SerializeField] private Image iconImage;
    [SerializeField] private GameObject lockIcon; // 자물쇠 아이콘

    [Header("Visual Settings")]
    [SerializeField] private Color inactiveColor = Color.gray;
    [SerializeField] private Color activeColor = Color.yellow;
    [SerializeField] private float bounceStrength = 10f;
    [SerializeField] private float bounceDuration = 0.5f;
    [SerializeField] private Ease bounceEase = Ease.OutBounce;

    [Header("Tooltip Settings")]
    [SerializeField] private float tooltipDuration = 3f; // 툴팁 표시 시간

    [Header("Debug")]
    [SerializeField, ReadOnly] public MythicRecipe assignedRecipe;
    [SerializeField, ReadOnly] private int recipeStartIndex = -1;
    [SerializeField, ReadOnly] private bool isAvailable = false;
    [SerializeField, ReadOnly] private bool isLocked = false;

    // References
    private QueueManager queueManager;
    private Tween bounceTween;

    #region Initialize

    /// <summary> 버튼을 특정 신화 레시피로 초기화합니다. </summary>
    public void Initialize(MythicRecipe recipe, QueueManager manager)
    {
        assignedRecipe = recipe;
        queueManager = manager;

        if (button == null) button = GetComponent<Button>();
        if (buttonImage == null) buttonImage = GetComponent<Image>();
        if (buttonText == null) buttonText = GetComponentInChildren<TextMeshProUGUI>();
        if (iconImage == null) iconImage = transform.Find("Icon")?.GetComponent<Image>();

        // 버튼 텍스트 설정
        if (buttonText != null && recipe != null)
        {
            buttonText.text = recipe.ResultUnit != null ? recipe.ResultUnit.unitName : "Mythic";
        }

        // 아이콘 설정
        if (iconImage != null && recipe != null && recipe.ResultUnit != null)
        {
            iconImage.sprite = recipe.ResultUnit.unitSprite;
        }

        // 초기 상태 설정
        SetLocked(!recipe.isUnlocked);
        SetAvailable(false);

        // 버튼 클릭 이벤트 연결
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnButtonClicked);
        }
    }

    #endregion

    #region State Management

    /// <summary> 버튼의 사용 가능 상태를 설정합니다. </summary>
    public void SetAvailable(bool available)
    {
        isAvailable = available;

        if (buttonImage != null)
        {
            buttonImage.color = available ? activeColor : inactiveColor;
        }

        // 애니메이션 관리
        if (available)
        {
            StartBounceAnimation();
        }
        else
        {
            StopBounceAnimation();
        }
    }

    /// <summary> 레시피 데이터와 시작 인덱스를 설정합니다. </summary>
    public void SetRecipeData(MythicRecipe recipe, int startIndex)
    {
        assignedRecipe = recipe;
        recipeStartIndex = startIndex;
    }

    /// <summary> 이 버튼이 특정 레시피에 해당하는지 확인합니다. </summary>
    public bool IsForRecipe(MythicRecipe recipe)
    {
        return assignedRecipe != null && assignedRecipe == recipe;
    }

    /// <summary> 버튼의 잠금 상태를 설정합니다. </summary>
    public void SetLocked(bool locked)
    {
        isLocked = locked;

        if (lockIcon != null)
        {
            lockIcon.SetActive(locked);
        }

        if (button != null)
        {
            button.interactable = !locked;
        }

        // 잠금 상태일 때는 회색으로 설정
        if (locked && buttonImage != null)
        {
            buttonImage.color = inactiveColor;
        }
    }

    #endregion

    #region Animation

    /// <summary> 흔들리는 애니메이션을 시작합니다. </summary>
    private void StartBounceAnimation()
    {
        StopBounceAnimation(); // 기존 애니메이션 정지

        if (transform != null)
        {
            bounceTween = transform.DOPunchScale(Vector3.one * 0.1f, bounceDuration, 10, 1f)
                .SetEase(bounceEase)
                .SetLoops(-1, LoopType.Restart);
        }
    }

    /// <summary> 흔들리는 애니메이션을 정지합니다. </summary>
    private void StopBounceAnimation()
    {
        if (bounceTween != null)
        {
            bounceTween.Kill();
            bounceTween = null;
        }

        // 원래 크기로 복원
        if (transform != null)
        {
            transform.localScale = Vector3.one;
        }
    }

    #endregion

    #region Button Events

    /// <summary> 버튼 클릭 시 호출됩니다. </summary>
    private void OnButtonClicked()
    {
        if (assignedRecipe == null)
        {
            Debug.LogWarning("MythicSpawnButton: No recipe assigned");
            return;
        }

        // 잠금된 버튼: 해금 조건 툴팁 표시
        if (isLocked)
        {
            ShowUnlockTooltip();
            return;
        }

        // 활성화된 버튼: 신화 유닛 소환
        if (isAvailable && queueManager != null && recipeStartIndex >= 0)
        {
            Debug.Log($"MythicSpawnButton: Spawning mythic unit '{assignedRecipe.Id}' at index {recipeStartIndex}");

            // QueueManager를 통해 신화 유닛 소환
            queueManager.SpawnMythicUnit(assignedRecipe, recipeStartIndex);

            // 소환 후 버튼 비활성화
            SetAvailable(false);
            recipeStartIndex = -1;
        }
        // 비활성화된 버튼: 레시피 툴팁 표시
        else if (!isAvailable) ShowRecipeTooltip();
        
    }

    /// <summary> 레시피 툴팁을 표시합니다. </summary>
    private void ShowRecipeTooltip()
    {
        if (assignedRecipe == null) return;

        // 레시피 정보 문자열 생성
        string tooltipText = CreateRecipeTooltipText();
        
        // UIManager를 통해 툴팁 표시
        if (UIManager.Instance != null) UIManager.Instance.ShowMessage(tooltipText, tooltipDuration);
    }

    /// <summary> 레시피 툴팁 텍스트를 생성합니다. </summary>
    private string CreateRecipeTooltipText()
    {
        if (assignedRecipe == null) return "No recipe assigned";

        string result = $"<color=orange>{assignedRecipe.ResultUnit.unitName}</color>\n";
        
        if (assignedRecipe.Sequence != null && assignedRecipe.Sequence.Count > 0)
        {
            result += "필요한 유닛: ";
            for (int i = 0; i < assignedRecipe.Sequence.Count; i++)
            {
                if (assignedRecipe.Sequence[i] != null)
                {
                    result += $"{assignedRecipe.Sequence[i].unitName}";
                    if (i < assignedRecipe.Sequence.Count - 1) result += " + ";
                }
            }
        }
        return result;
    }

    /// <summary> 해금 조건 툴팁을 표시합니다. </summary>
    private void ShowUnlockTooltip()
    {
        if (assignedRecipe == null) return;

        string tooltipText = $"<color=red>🔒 잠금됨</color>\n";
        tooltipText += $"<color=orange>{assignedRecipe.ResultUnit?.unitName}</color>\n";
        tooltipText += $"해금 조건: <color=yellow>웨이브 {assignedRecipe.unlockWave} 클리어</color>";

        if (UIManager.Instance != null) UIManager.Instance.ShowMessage(tooltipText, tooltipDuration);
    }

    #endregion

    #region Unity Events

    private void OnDestroy()
    {
        StopBounceAnimation();
    }

    private void OnDisable()
    {
        StopBounceAnimation();
    }

    #endregion

    #region Debug

    [Sirenix.OdinInspector.Button]
    private void TestSetAvailable()
    {
        SetAvailable(!isAvailable);
    }

    [Sirenix.OdinInspector.Button]
    private void TestBounceAnimation()
    {
        StartBounceAnimation();
    }

    [Sirenix.OdinInspector.Button]
    private void TestShowTooltip()
    {
        ShowRecipeTooltip();
    }

    #endregion
}

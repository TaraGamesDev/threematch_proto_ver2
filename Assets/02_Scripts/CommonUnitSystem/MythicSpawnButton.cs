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

    [Header("Visual Settings")]
    [SerializeField] private Color inactiveColor = Color.gray;
    [SerializeField] private Color activeColor = Color.yellow;
    [SerializeField] private float bounceStrength = 10f;
    [SerializeField] private float bounceDuration = 0.5f;
    [SerializeField] private Ease bounceEase = Ease.OutBounce;

    [Header("Debug")]
    [SerializeField, ReadOnly] private MythicRecipe assignedRecipe;
    [SerializeField, ReadOnly] private int recipeStartIndex = -1;
    [SerializeField, ReadOnly] private bool isAvailable = false;

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

        // 초기 상태: 비활성화
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

        if (button != null)
        {
            button.interactable = available;
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
        if (!isAvailable || assignedRecipe == null || queueManager == null || recipeStartIndex < 0)
        {
            Debug.LogWarning("MythicSpawnButton: Cannot spawn - missing data or not available");
            return;
        }

        Debug.Log($"MythicSpawnButton: Spawning mythic unit '{assignedRecipe.Id}' at index {recipeStartIndex}");

        // QueueManager를 통해 신화 유닛 소환
        queueManager.SpawnMythicUnit(assignedRecipe, recipeStartIndex);

        // 소환 후 버튼 비활성화
        SetAvailable(false);
        recipeStartIndex = -1;
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

    #endregion
}

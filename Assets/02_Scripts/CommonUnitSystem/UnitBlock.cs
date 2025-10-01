using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using DG.Tweening;

/// <summary>
/// 게임 내에서 사용되는 유닛 블록을 나타내는 클래스
/// UnitData를 기반으로 시각적 표현과 상호작용을 담당합니다.
/// </summary>
public class UnitBlock : MonoBehaviour, 
// IBeginDragHandler, 
// IDragHandler, 
// IEndDragHandler, 
IPointerClickHandler
{
    [Header("비주얼 컴포넌트")]
    public Image unitImage;                      // 유닛 이미지 (UI)
    public Image backgroundImage;                // 배경 이미지 (UI)

    
    [Header("티어별 배경 색상")]
    public Color[] tierColors = new Color[]
    {
        Color.gray,                             // [0] None (직접 처리됨)
        Color.white,                            // [1] Tier1
        Color.green,                            // [2] Tier2  
        Color.blue,                             // [3] Tier3
        new Color(0.5f, 0f, 0.5f, 1f),        // [4] Tier4 (보라색)
        Color.red,                              // [5] Tier5+ (확장용)
    };
    
    [Header("애니메이션 설정")]
    public float scaleAnimationDuration = 0.3f;
    public Vector3 selectedScale = Vector3.one * 1.2f;
    public Vector3 normalScale = Vector3.one;

    
    [Header("상태")]
    public bool isDragging = false;
    public bool isSelected = false;
    // public bool isInQueue = false;          // 큐에 배치된 상태인지 여부 - 현재 프로토에서 쓰지 않음 
    
    // 유닛 데이터
    public UnitData unitData;
    public UnitData UnitData => unitData;
    
    // 원래 위치 (드래그 시 되돌리기용)
    private Vector3 originalPosition;
    private Transform originalParent;
    
    // 원래 크기 기억용 (프리팹 기본값)
    private Vector2 originalRectSize;     // RectTransform 기본 크기
    private bool originalSizeStored = false; // 원래 크기가 저장되었는지 여부
    
    // 이벤트
    public System.Action<UnitBlock> OnBlockClicked; // 블록 클릭 이벤트 -> 구독 하여 사용 
    // public System.Action<UnitBlock, int> OnDroppedOnRow; // 현재 프로토에서 쓰지 않음 
    
    private void Awake()
    {
        // 컴포넌트 자동 할당
        if (unitImage == null) unitImage = GetComponentInChildren<Image>();
        if (backgroundImage == null) backgroundImage = GetComponent<Image>();
        
        StoreOriginalSize(); // 원래 크기 저장 
    }
    
    #region 초기화 메서드들
    
    /// <summary>
    /// 유닛 데이터로 블록을 초기화합니다.
    /// </summary>
    public void Initialize(UnitData data)
    {
        if (data == null){ Debug.LogError("UnitData가 null입니다!"); return; }  
        
        unitData = data;
        
        StoreOriginalSize(); // 원래 크기 저장 (최초 한 번만)
        
        UpdateVisuals();
    }
    
    /// <summary>
    /// 프리팹의 원래 크기를 저장합니다.
    /// </summary>
    private void StoreOriginalSize()
    {
        if (originalSizeStored) return; // 이미 저장됐으면 리턴 
        
        
        originalRectSize = GetComponent<RectTransform>().sizeDelta; // RectTransform 크기 저장
        normalScale = transform.localScale; // Transform 스케일 저장
        
        originalSizeStored = true;
    }
    
    /// <summary>
    /// 블록의 시각적 요소들을 업데이트합니다.
    /// </summary>
    private void UpdateVisuals()
    {
        if (unitData == null) return;
        
        // 유닛 이미지 설정
        if (unitImage != null && unitData.unitSprite != null) unitImage.sprite = unitData.unitSprite;
        
        // 배경 색상 설정 (티어별)
        if (backgroundImage != null)
        {
            int tierIndex = (int)unitData.tier;
            if (tierIndex >= 1 && tierIndex < tierColors.Length) backgroundImage.color = tierColors[tierIndex];
        }
    }
    
    #endregion
    
    #region UI 이벤트 인터페이스 구현
    
    public void OnPointerClick(PointerEventData eventData)
    {
        OnBlockClicked?.Invoke(this);
    }
    

    #region 현재 프로토에서 쓰지 않음 
    // public void OnDrag(PointerEventData eventData)
    // {
    //     if (!isDragging) return;
    //     transform.position = eventData.position; // UI 좌표계에서 마우스 위치로 블록 이동
    // }

    // public void OnBeginDrag(PointerEventData eventData)
    // {
    //     if (isDragging || isInQueue) return;
        
    //     SetSelected(true);
        
    //     // 드래그 시작 준비
    //     originalPosition = transform.position;
    //     originalParent = transform.parent;
        
    //     isDragging = true;
        
    //     // 드래그 중인 블록을 최상위로 표시
    //     transform.SetAsLastSibling();
    // }

    // public void OnEndDrag(PointerEventData eventData)
    // {
    //     if (!isDragging) return;
        
    //     isDragging = false;
        
    //     // 드롭할 행 찾기
    //     int targetRow = FindTargetRow();
        
    //     if (targetRow >= 0) OnDroppedOnRow?.Invoke(this, targetRow); // 유효한 행에 드롭
    //     else ReturnToOriginalPosition(); // 유효하지 않은 위치에 드롭 시 원래 위치로 복귀

    //     SetSelected(false);
    // }
    
    // /// <summary>
    // /// 현재 위치에서 가장 가까운 유효한 열을 찾습니다.
    // /// </summary>
    // /// <returns>열 인덱스 (유효하지 않으면 -1)</returns>
    // private int FindTargetRow()
    // {
    //     QueueManager queueManager = GameManager.Instance.queueManager;
    //     if (queueManager == null) return -1;
        
    //     // 마우스 위치에서 가장 가까운 열 찾기
    //     Vector3 worldPosition = transform.position;
    //     int closestColumn = queueManager.GetClosestColumn(worldPosition);

    //     return closestColumn;
    // }
    
    // /// <summary>
    // /// 블록을 원래 위치로 되돌립니다.
    // /// </summary>
    // private void ReturnToOriginalPosition()
    // {
    //     transform.position = originalPosition;
    //     transform.SetParent(originalParent);
    // }

    #endregion
    
    #endregion
    
    #region 상태 관리 메서드들
    
    /// <summary>
    /// 블록의 선택 상태를 설정합니다.
    /// </summary>
    /// <param name="selected">선택 여부</param>
    public void SetSelected(bool selected)
    {
        isSelected = selected;
        
        // 선택 상태에 따른 스케일 애니메이션
        Vector3 targetScale = selected ? selectedScale : normalScale;
        transform.DOScale(targetScale, scaleAnimationDuration).SetEase(Ease.OutBounce);
    }
    
    /// <summary>
    /// 블록을 활성화/비활성화 -> 오브젝트 풀에서 사용중
    /// </summary>
    public void SetActive(bool active)
    {
        if (active && !originalSizeStored) StoreOriginalSize(); // 오브젝트 풀에서 다시 활성화될 때 원래 크기가 저장되지 않았다면 저장
        gameObject.SetActive(active);
    }
    
    /// <summary>
    /// 블록의 투명도를 설정합니다.
    /// </summary>
    /// <param name="alpha">투명도 (0-1)</param>
    public void SetAlpha(float alpha)
    {
        if (unitImage != null)
        {
            Color color = unitImage.color;
            color.a = alpha;
            unitImage.color = color;
        }
        
        if (backgroundImage != null)
        {
            Color color = backgroundImage.color;
            color.a = alpha;
            backgroundImage.color = color;
        }
    }
    
    // public void SetInQueue(bool inQueue)
    // {
    //     isInQueue = inQueue;
    // }

    public void ResetStateForPool()
    {
        isDragging = false;
        isSelected = false;
        transform.localScale = normalScale;
        SetAlpha(1f);
        transform.DOKill();
    }

    public void PrepareForQueue()
    {
        isDragging = false;
        isSelected = false;
        transform.localScale = normalScale;
        SetAlpha(1f);
    }
    
    public void SetBlockSize(float slotSize)
    {
        // RectTransform이 있는 경우 (UI 블록)
        var rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.sizeDelta = new Vector2(slotSize, slotSize);
            return;
        }
    }
    
    /// <summary>
    /// 블록을 프리팹의 원래 크기로 복원합니다. (큐에서 제거될 때 사용)
    /// </summary>
    public void ResetToOriginalSize()
    {
        if (!originalSizeStored){ Debug.LogWarning($"{gameObject.name}: 원래 크기가 저장되지 않아 복원할 수 없습니다."); return; }
        
        // 저장된 원래 RectTransform 크기로 복원
        var rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null) rectTransform.sizeDelta = originalRectSize; 
        
        // Transform 스케일을 원래대로 복원
        transform.localScale = normalScale;
    }
    
    #endregion
    
    #region 애니메이션 메서드들
    public void MoveTo(Vector3 targetPosition, float duration = 0.5f, System.Action onComplete = null)
    {
        transform.DOMove(targetPosition, duration).SetEase(Ease.OutCubic).OnComplete(() => onComplete?.Invoke());
    }
    
    /// <summary>
    /// 등장 애니메이션을 재생합니다.
    /// </summary>
    public void PlaySpawnAnimation()
    {
        // 초기 스케일을 0으로 설정
        transform.localScale = Vector3.zero;
        SetAlpha(0f);
        
        // 스케일 업 애니메이션
        transform.DOScale(normalScale, 0.5f).SetEase(Ease.OutBounce);
                 
        // 페이드 인 애니메이션 (UI Image 기준)
        if (unitImage != null) unitImage.DOFade(1f, 0.3f);
        if (backgroundImage != null) backgroundImage.DOFade(1f, 0.3f);
    }
    
    /// <summary>
    /// 사라지는 애니메이션을 재생합니다.
    /// </summary>
    /// <param name="onComplete">완료 콜백</param>
    public void PlayDestroyAnimation(System.Action onComplete = null)
    {
        // 스케일 다운 애니메이션
        transform.DOScale(Vector3.zero, 0.3f).SetEase(Ease.InBack);
                 
        // 페이드 아웃 애니메이션
        var sequence = DOTween.Sequence();
        
        if (unitImage != null)
            sequence.Join(unitImage.DOFade(0f, 0.3f));
        if (backgroundImage != null) sequence.Join(backgroundImage.DOFade(0f, 0.3f));
        
        sequence.OnComplete(() =>
        {
            onComplete?.Invoke();
            PoolManager.Instance?.Release(gameObject);
        });
    }
    
    #endregion
    
    #region 유틸리티 메서드들
    
    /// <summary>
    /// 두 유닛 블록이 같은 타입인지 확인합니다.
    /// </summary>
    /// <param name="other">비교할 다른 블록</param>
    /// <returns>같은 타입 여부</returns>
    public bool IsSameType(UnitBlock other)
    {
        if (other == null || other.unitData == null || this.unitData == null) return false;
        return this.unitData.name == other.unitData.name;
    }
    
    /// <summary>
    /// 블록의 정보를 문자열로 반환합니다.
    /// </summary>
    /// <returns>블록 정보 문자열</returns>
    public override string ToString()
    {
        if (unitData == null) return "UnitBlock (No Data)";
        return $"UnitBlock ({unitData.unitName} - {unitData.tier})";
    }
    
    #endregion
}

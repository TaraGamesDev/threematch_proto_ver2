using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;

public class Unit : MonoBehaviour
{
    [Header("기본 정보")]
    public string unitName = "Unit";
    protected UnitData unitData;
    public int health = 100;
    public int attackDamage = 10;
    public float moveSpeed = 2f;
    public float attackRange = 1f;
    public float attackSpeed = 1f;

    
    [Header("현재 스탯")]
    public int currentHealth;
    public int currentAttackDamage;
    public float currentMoveSpeed;
    public float currentAttackRange;
    public float currentAttackSpeed;


    [Header("상태")]
    [SerializeField] protected bool canMove = true; // 움직임 제어

    [SerializeField] protected bool isDead = false;
    
    
    [Header("전투")]
    protected float lastAttackTime;
    [SerializeField] protected bool isAttacking = false;
    public Unit currentTarget; // 현재 타겟


    
    
    // private or protected
    public Rigidbody2D rb;
    protected Collider2D bodyCollider;
    private float lastTargetSearchTime; // 마지막 타겟 검색 시간
    private SpriteRenderer spriteRenderer;
    private Image image;
    private float damageFlashDuration = 0.1f;
    private Vector3 originalScale;
    private Color originalColor;
    
    protected virtual void Awake()
    {
        // 스프라이트 기반인 경우 
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null) originalColor = spriteRenderer.color;

        // 이미지 기반인 경우 
        if (image == null) image = GetComponent<Image>();
        if (image != null) originalColor = image.color;

        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (bodyCollider == null) bodyCollider = GetComponent<Collider2D>();
    }

    public virtual void Init(UnitData unitData)
    {
        if (unitData == null) {Debug.LogError("[Unit] Init: unitData is null"); return;}
        
        this.unitData = unitData;
        
        // UnitData로 스탯 초기화
        if (unitData != null)
        {
            unitName = unitData.unitName;
            health = unitData.health;
            attackDamage = unitData.attackDamage;
            moveSpeed = unitData.moveSpeed;
            attackRange = unitData.attackRange;
            attackSpeed = unitData.attackSpeed;
            
            // 현재 스탯 업데이트
            currentHealth = health;
            currentAttackDamage = attackDamage;
            currentMoveSpeed = moveSpeed;
            currentAttackRange = attackRange;
            currentAttackSpeed = attackSpeed;
        }

        // 상태 초기화 
        isDead = false;
        canMove = GameManager.Instance.CurrentPhase == GamePhase.BattlePhase; // 전투 턴일 때만 움직임 허용 
        currentTarget = null;
        lastAttackTime = 0f;
        
        // 시각적 요소 초기화 
        if (spriteRenderer != null) spriteRenderer.color = originalColor;
        if (image != null) image.color = originalColor;
        originalScale = transform.localScale;

        // 충돌 체크 활성화 
        if (bodyCollider != null) bodyCollider.enabled = true;

        // 리지드바디 포지션 프리즈 해제 (Z축 회전은 막기)
        FreezeRotation_FreePosision();
    }
    
    protected virtual void Update()
    {
        if (isDead) return;
        
        // 일정 시간마다 타겟 검색
        if (ShouldSearchTarget())
        {
            UpdateTarget();
            lastTargetSearchTime = Time.time;
        }
        
        // 움직임이 허용된 경우에만 이동(웨이브 완료 시 움직임 정지)
        if (canMove) UpdateMovement();
    }
    
    // 자식 클래스에서 구현할 가상 메서드들
    protected virtual void UpdateTarget() { }
    protected virtual void UpdateMovement() { }
    
    /// <summary> 타겟 검색이 필요한지 확인합니다. </summary>
    protected virtual bool ShouldSearchTarget()
    {
        // UnitManager에서 검색 간격을 가져옴
        float searchInterval = UnitManager.Instance != null ? UnitManager.Instance.TargetSearchInterval : 0.5f;
        
        // 타겟이 없거나, 마지막 검색으로부터 충분한 시간이 지났으면 검색
        return currentTarget == null || Time.time - lastTargetSearchTime >= searchInterval;
    }
    
    #region Attack
    protected virtual void Attack()
    {
        if (Time.time - lastAttackTime < 1f / currentAttackSpeed) return;
        
        lastAttackTime = Time.time;

        Debug.Log($"[Attack] {unitName} Attack");
        AttackAnimation();
    }
    
    protected void AttackAnimation()
    {
        isAttacking = true;

        
        Vector3 squeezedScale = new Vector3(originalScale.x * 1.3f, originalScale.y * 0.7f, originalScale.z); // 눌린 형태
        Vector3 bigScale = new Vector3(originalScale.x * 1.2f, originalScale.y * 1.2f, originalScale.z); // 큰 형태
        
        // 눌렸다가 커지는 애니메이션
        transform.DOScale(squeezedScale, 0.1f)
            .OnComplete(() => {
                transform.DOScale(bigScale, 0.1f)
                    .OnComplete(() => {
                        transform.DOScale(originalScale, 0.1f)
                            .OnComplete(() => {
                                ApplyDamage();
                                isAttacking = false;
                            });
                    });
            });
    }
    protected virtual void ApplyDamage() { 
        if (currentTarget != null && !currentTarget.IsDead()) CombatManager.Instance?.ResolveAttack(this, currentTarget, currentAttackDamage);
    }
    
    #endregion

    #region Takc Damage
    public void TakeDamage(int damage)
    {
        if (isDead) return;
        
        currentHealth -= damage;
        DamageFlash();
        
        if (currentHealth <= 0) Die();
    }
    
    protected void DamageFlash()
    {
        // SpriteRenderer가 있는 경우
        if (spriteRenderer != null)
        {
            spriteRenderer.DOColor(Color.red, damageFlashDuration * 0.5f)
                .OnComplete(() => {
                    spriteRenderer.DOColor(originalColor, damageFlashDuration * 0.5f);
                });
        }

        if (image != null)
        {
            image.DOColor(Color.red, damageFlashDuration * 0.5f)
                .OnComplete(() => {
                    image.DOColor(originalColor, damageFlashDuration * 0.5f);
                });
        }
    }

    #endregion
    
    #region Die & Alive
    protected virtual void Die()
    {
        if (isDead) return;

        // Debug.Log($"{unitName} Die");
        isDead = true;
        canMove = false;
        currentTarget = null;
        isAttacking = false;

        transform.DOKill();
        ResetVisual();

        // 유닛 지속성을 위해 풀로 돌아가지 않음 -> enemy는 풀로 돌아가지만 animal은 지속성을 위해 풀로 돌아가지 않음 -> 각 스크립트에서 오버라이드 해서 관리 
        // PoolManager.Instance.Release(gameObject);
    }

    public virtual void Alive()
    {
        // if (!isDead) return;
        FreezeRotation_FreePosision(); // 리지드바디 포지션 프리즈 해제 (Z축 회전은 막기) -> 여기서 실행시키면 전투 종료 후 유닛이 진영으로 재배치가 안됨 -> 재배치 후 해제 
        bodyCollider.enabled = true;

        isDead = false;
        currentTarget = null;
        isAttacking = false;
        SetCanMove(GameManager.Instance.CurrentPhase == GamePhase.BattlePhase);

        ResetStats();
    }
    
    #endregion

    public bool IsDead()
    {
        return isDead;
    }
    
    // 움직임 제어 메서드들
    public void SetCanMove(bool canMove)
    {
        this.canMove = canMove;
        if (!canMove) StopMovement();
    }
    
    /// <summary> 유닛을 정지시킵니다. </summary>
    protected void StopMovement()
    {
        if (rb == null) {Debug.LogError("[Unit] StopMovement: rb is null"); return;}
        rb.linearVelocity = Vector2.zero; 
    }

    protected virtual void ResetVisual()
    {
        // 스프라이트 기반인 경우
        if (spriteRenderer != null) spriteRenderer.color = originalColor;

        // 이미지 기반인 경우 
        Image image = GetComponent<Image>();
        if (image != null) image.color = originalColor;

        transform.localScale = originalScale;
    }

    public void FreezeRotation_FreePosision()
    {
        // 리지드바디 포지션 프리즈 해제 (Z축 회전은 막기)
        if (rb != null) rb.constraints = RigidbodyConstraints2D.FreezeRotation;
    }

    // 스탯 초기화 및 영구 업그레이드 적용 (웨이브 시작 시 호출)
    public void ResetStats()
    {
        // 기본 스탯으로 초기화
        currentHealth = health;
        currentAttackDamage = attackDamage;
        currentMoveSpeed = moveSpeed;
        currentAttackRange = attackRange;
        currentAttackSpeed = attackSpeed;
        
        // UpgradeSystem.Instance?.ApplyPermanentUpgradesToUnit(this); // TOOD 
    }
}

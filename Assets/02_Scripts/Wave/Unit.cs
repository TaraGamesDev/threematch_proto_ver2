using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;

public class Unit : MonoBehaviour
{
    [Header("기본 정보")]
    public string unitName = "Unit";
    public UnitData unitData;
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
    protected Rigidbody2D rb;
    private float lastTargetSearchTime; // 마지막 타겟 검색 시간
    private SpriteRenderer spriteRenderer;
    private float damageFlashDuration = 0.1f;
    private Vector3 originalScale;
    private Color originalColor;
    
    protected virtual void Awake()
    {
        originalScale = transform.localScale;
        
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null) originalColor = spriteRenderer.color;
        if (rb == null) rb = GetComponent<Rigidbody2D>();
    }

    protected virtual void OnEnable()
    {
        isDead = false;
        canMove = true;
        currentTarget = null;
        lastAttackTime = 0f;

        if (spriteRenderer != null) spriteRenderer.color = originalColor;
        transform.localScale = originalScale;
    }

    protected virtual void Start()
    {
        Init();
    }

    public void Init()
    {
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
    
    
    protected virtual void Attack()
    {
        if (Time.time - lastAttackTime < 1f / currentAttackSpeed) return;
        
        lastAttackTime = Time.time;
        MeleeAttack();
    }
    
    protected void AttackAnimation()
    {
        isAttacking = true;
        
        Vector3 originalScale = transform.localScale;
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
    
    // 근접 공격 (기본 공격)
    protected virtual void MeleeAttack()
    {
        AttackAnimation();
    }
    
    public void TakeDamage(int damage)
    {
        if (isDead) return;
        
        currentHealth -= damage;
        DamageFlash();
        
        if (currentHealth <= 0) Die();
    }
    
    protected void DamageFlash()
    {
        transform.DOKill();

        // SpriteRenderer가 있는 경우
        if (spriteRenderer != null)
        {
            spriteRenderer.DOColor(Color.red, damageFlashDuration * 0.5f)
                .OnComplete(() => {
                    spriteRenderer.DOColor(originalColor, damageFlashDuration * 0.5f);
                });
        }

        // UI Image가 있는 경우
        else
        {
            UnityEngine.UI.Image image = GetComponent<UnityEngine.UI.Image>();
            if (image != null)
            {
                Color originalImageColor = image.color;
                image.DOColor(Color.red, damageFlashDuration * 0.5f)
                    .OnComplete(() => {
                        image.DOColor(originalImageColor, damageFlashDuration * 0.5f);
                    });
            }
        }
    }
    
    protected virtual void Die()
    {
        if (isDead) return;

        // Debug.Log($"{unitName} Die");
        isDead = true;
        canMove = false;
        currentTarget = null;
        transform.DOKill();

        if (spriteRenderer != null) spriteRenderer.color = originalColor;

        PoolManager.Instance.Release(gameObject);
    }
    
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
        if (rb != null)rb.linearVelocity = Vector2.zero;
    }
}

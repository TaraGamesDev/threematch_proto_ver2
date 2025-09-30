using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

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
    public bool isSlowed = false;
    public float slowMultiplier = 1f;
    public float slowDuration = 0f;
    public bool canMove = true; // 움직임 제어
    
    private float originalMoveSpeed;
    
    
    [Header("전투")]
    public float lastAttackTime;
    public bool isAttacking = false;
    public Unit currentTarget; // 현재 타겟
    
    [Header("시각적 요소")]
    public SpriteRenderer spriteRenderer;
    public GameObject attackEffect;
    public CircleCollider2D detectionCollider;
    
    [Header("특수 능력")]
    public GameObject acornProjectilePrefab; // 도토리 프리팹 (토끼용)
    public GameObject bombProjectilePrefab; // 폭탄 프리팹 (원숭이용)
    public GameObject hitEffectPrefab; // 타격 효과 프리팹
    
    [Header("애니메이션")]
    public float attackAnimationDuration = 0.3f;
    public float damageFlashDuration = 0.1f;
    
    protected Vector3 originalScale;
    protected Color originalColor;
    protected bool isDead = false;
    
    protected virtual void Awake()
    {
        originalScale = transform.localScale;
        
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null) originalColor = spriteRenderer.color;
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
            
            // 유닛 이미지 설정
            if (spriteRenderer != null && unitData.unitSprite != null) spriteRenderer.sprite = unitData.unitSprite;
            
            // 콜라이더 설정
            SetupDetectionCollider();
        }
    }
    
    protected virtual void Update()
    {
        if (isDead && !WaveManager.Instance.IsWaveActive()) return;

        // 슬로우 효과 처리
        // UpdateSlowEffect();
        
        // 자식 클래스에서 구현할 메서드들
        UpdateTarget();
        
        // 움직임이 허용된 경우에만 이동(웨이브 완료 시 움직임 정지)
        if (canMove) UpdateMovement();
    }
    
    // 자식 클래스에서 구현할 가상 메서드들
    protected virtual void UpdateTarget() { }
    protected virtual void UpdateMovement() { }
    
    protected void SetupDetectionCollider()
    {
        if (detectionCollider == null)
        {
            detectionCollider = GetComponent<CircleCollider2D>();
            if (detectionCollider == null) detectionCollider = gameObject.AddComponent<CircleCollider2D>();
        }
        
        detectionCollider.isTrigger = true;
        detectionCollider.radius = currentAttackRange;
    }
    
    // 콜라이더 기반 타겟 감지 - 자식 클래스에서 구현
    protected virtual void OnTriggerEnter2D(Collider2D other) { }

    
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
    
    protected virtual void ApplyDamage() { }
    
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
        Debug.Log($"{unitName} Die");
        isDead = true;
        Destroy(gameObject);
    }
    
    public bool IsDead()
    {
        return isDead;
    }
    
    // 움직임 제어 메서드들
    public void SetCanMove(bool canMove)
    {
        this.canMove = canMove;
    }
    
    public bool CanMove()
    {
        return canMove;
    }


    public void ApplySlow(float multiplier, float duration)
    {
        isSlowed = true;
        slowMultiplier = multiplier;
        slowDuration = duration;
        
        // 슬로우 시각적 효과
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.blue;
        }
    }

    private void UpdateSlowEffect()
    {
        if (isSlowed)
        {
            slowDuration -= Time.deltaTime;
            
            if (slowDuration <= 0f)
            {
                // 슬로우 효과 해제
                isSlowed = false;
                moveSpeed = originalMoveSpeed;
                
                if (spriteRenderer != null)
                {
                    spriteRenderer.color = originalColor;
                }
            }
        }
    }
}

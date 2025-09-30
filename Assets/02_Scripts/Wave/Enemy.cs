using System.Collections;
using UnityEngine;
using DG.Tweening;

public class Enemy : Unit
{
    [Header("보상")]
    public int goldReward = 1;
    
    [Header("전투")]
    public Animal currentTarget;
    private Transform targetPlayer; // 플레이어 타겟
    
    protected override void Start()
    {
        // 웨이브 매니저에 등록
        WaveManager.Instance.RegisterEnemy(this);
    }
    
    protected override void UpdateTarget()
    {
        // 타겟이 유효하지 않으면 제거
        if (currentTarget != null && currentTarget.IsDead()) currentTarget = null;
        
        // 플레이어 타겟이 유효하지 않으면 제거
        if (targetPlayer != null)
        {
            // 플레이어와의 거리 재확인
            if (GameManager.Instance != null && GameManager.Instance.HasPlayerImage())
            {
                float distanceToPlayer = Vector3.Distance(transform.position, GameManager.Instance.GetPlayerPosition());
                if (distanceToPlayer > currentAttackRange) targetPlayer = null; // 사거리를 벗어나면 타겟 해제
            }
            else targetPlayer = null; // 플레이어가 없으면 타겟 해제
            
        }
        
        // 타겟이 없으면 범위 내에서 새로운 타겟 탐지
        if (currentTarget == null && targetPlayer == null) FindTargetInRange();
    }
    
    private void FindTargetInRange()
    {
        // 범위 내의 모든 Animal 탐지
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, currentAttackRange);
        
        foreach (Collider2D collider in colliders)
        {
            Animal animal = collider.GetComponent<Animal>();
            if (animal != null && !animal.IsDead())
            {
                // 실제 거리로 공격 가능 여부 확인
                float distanceToAnimal = Vector3.Distance(transform.position, animal.transform.position);
                if (distanceToAnimal <= currentAttackRange)
                {
                    currentTarget = animal;
                    break;
                }
            }
        }
        
        // Animal이 없으면 플레이어 탐지
        if (currentTarget == null) FindPlayerTarget();
        
    }


    
    
    private void FindPlayerTarget()
    {
        // GameManager를 통해 플레이어 위치 확인
        if (GameManager.Instance == null || !GameManager.Instance.HasPlayerImage()) return;
        
        Vector3 playerPosition = GameManager.Instance.GetPlayerPosition();
        Vector3 enemyPosition = transform.position;
        
        // 플레이어와의 거리 계산
        float distanceToPlayer = Vector3.Distance(enemyPosition, playerPosition);
        
        // 플레이어가 사거리 안에 있으면 타겟으로 설정
        if (distanceToPlayer <= currentAttackRange)
        {
            // 플레이어를 타겟으로 설정 (GameManager의 플레이어 Transform 사용)
            targetPlayer = GameManager.Instance.playerImageTransform;
            Debug.Log($"[Enemy] {unitName}이(가) 플레이어를 타겟으로 설정했습니다. 거리: {distanceToPlayer:F2}");
        }
    }
    
    protected override void UpdateMovement()
    {
        if (currentTarget != null)
        {
            // 실제 거리로 공격 가능 여부 확인
            float distanceToTarget = Vector3.Distance(transform.position, currentTarget.transform.position);
            if (distanceToTarget <= currentAttackRange) Attack();
        }
        else if (targetPlayer != null)
        {
            // 플레이어와의 실제 거리로 공격 가능 여부 확인
            float distanceToPlayer = Vector3.Distance(transform.position, targetPlayer.position);
            if (distanceToPlayer <= currentAttackRange) AttackPlayer();
        }
        else MoveLeft();
    }

    private void MoveLeft()
    {
        if (!canMove) return; // 움직임이 제한된 경우 정지
        transform.position += Vector3.left * currentMoveSpeed * Time.deltaTime * 10f;
    }
    
    protected override void ApplyDamage()
    {
        if (currentTarget != null && !currentTarget.IsDead())
        {
            currentTarget.TakeDamage(currentAttackDamage);
        }
    }
    
    private void AttackPlayer()
    {
        if (Time.time - lastAttackTime < 1f / currentAttackSpeed) return;
        
        lastAttackTime = Time.time;
        
        // 공격 애니메이션 실행
        AttackAnimation();
        
        // 플레이어에게 데미지 적용 (GameManager 통해)
        if (targetPlayer != null && GameManager.Instance != null) GameManager.Instance.TakeDamage(currentAttackDamage);
    }
    
    protected override void Die()
    {
        base.Die();
        
        Debug.Log($"[Enemy] {unitName} Die");
        // 보상 지급
        GameManager.Instance.AddGold(goldReward);
        GameManager.Instance.AddExp(); // 몬스터 1마리당 1 경험치
        UIManager.Instance.UpdateGoldTextUI();
        UIManager.Instance.UpdateExpTextUI();
        
        // 웨이브 매니저에서 제거
        WaveManager.Instance.UnregisterEnemy(this);
    }

}

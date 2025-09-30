using UnityEngine;
using Sirenix.OdinInspector;

public class Animal : Unit
{   
    [Title("전투")]
    // currentTarget은 부모 클래스 Unit에서 상속받음
    
    protected override void UpdateTarget()
    {
        // 타겟이 유효하지 않으면 제거
        if (currentTarget != null && currentTarget.IsDead()) currentTarget = null;
        
        // 타겟이 없으면 범위 내에서 새로운 타겟 탐지
        if (currentTarget == null) FindTargetInRange();
    }
    
    private Enemy GetCurrentEnemyTarget()
    {
        return currentTarget as Enemy;
    }
    
    private void FindTargetInRange()
    {
        // 범위 내의 모든 Enemy 탐지
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, currentAttackRange);
        
        foreach (Collider2D collider in colliders)
        {
            Enemy enemy = collider.GetComponent<Enemy>();
            if (enemy != null && !enemy.IsDead())
            {
                // 실제 거리로 공격 가능 여부 확인
                float distanceToEnemy = Vector3.Distance(transform.position, enemy.transform.position);
                if (distanceToEnemy <= currentAttackRange)
                {
                    currentTarget = enemy;
                    break;
                }
            }
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
        else MoveToStopPosition();
    }
    
    private void MoveToStopPosition()
    {
        if (!canMove && !WaveManager.Instance.IsWaveActive()) return; // 움직임이 제한된 경우 정지
        
        Transform stopPos = UnitManager.Instance.unitStopPos;
        if (stopPos == null) return;
        
        // UnitStopPos에 도착했는지 확인
        if (transform.position.x >= stopPos.position.x) return; // 이미 도착한 경우 더 이상 이동하지 않음
        
        // UnitStopPos까지 이동
        transform.position += Vector3.right * currentMoveSpeed * Time.deltaTime * 10f;
    }
    
    protected override void ApplyDamage()
    {
        Enemy enemyTarget = GetCurrentEnemyTarget();
        if (enemyTarget != null && !enemyTarget.IsDead()) enemyTarget.TakeDamage(currentAttackDamage);
        
    }
    
    protected override void Die()
    {
        base.Die();
        // 유닛 매니저에서 제거
        UnitManager.Instance.RemoveUnit(this);
    }
    
    // 스탯 초기화 (웨이브 시작 시 호출)
    public void ResetStats()
    {
        // 기본 스탯으로 초기화
        currentHealth = health;
        currentAttackDamage = attackDamage;
        currentMoveSpeed = moveSpeed;
        currentAttackRange = attackRange;
        currentAttackSpeed = attackSpeed;
        
        // 영구 업그레이드 다시 적용
        // if (UpgradeSystem.Instance != null) UpgradeSystem.Instance.ApplyPermanentUpgradesToUnit(this);
        
    }
    //@ TODO : 업그레이드 적용
    // 업그레이드 적용
    // public void ApplyUpgrade(UpgradeData.UpgradeType upgradeType, float bonus, bool isPercentage)
    // {
    //     switch (upgradeType)
    //     {
    //         case UpgradeData.UpgradeType.UnitAttack:
    //             if (isPercentage) currentAttackDamage = Mathf.RoundToInt(currentAttackDamage * (1f + bonus / 100f));
    //             else currentAttackDamage += Mathf.RoundToInt(bonus);
    //             break;
                
    //         case UpgradeData.UpgradeType.UnitHealth:
    //             if (isPercentage)
    //             {
    //                 int healthIncrease = Mathf.RoundToInt(currentHealth * (bonus / 100f));
    //                 currentHealth += healthIncrease;
    //             }
    //             else
    //             {
    //                 currentHealth += Mathf.RoundToInt(bonus);
    //             }
    //             break;
                
    //         case UpgradeData.UpgradeType.UnitAttackSpeed:
    //             if (isPercentage) currentAttackSpeed *= (1f + bonus / 100f);
    //             else currentAttackSpeed += bonus;
    //             break;
                
    //         case UpgradeData.UpgradeType.UnitMoveSpeed:
    //             if (isPercentage) currentMoveSpeed *= (1f + bonus / 100f);
    //             else currentMoveSpeed += bonus;
    //             break;
                
    //         case UpgradeData.UpgradeType.UnitRange:
    //             if (isPercentage) currentAttackRange *= (1f + bonus / 100f);
    //             else currentAttackRange += bonus;
    //             break;
    //     }
    // }
}

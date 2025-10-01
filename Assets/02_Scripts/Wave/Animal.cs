using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public class Animal : Unit
{   
    [Title("전투")]
    // currentTarget은 부모 클래스 Unit에서 상속받음
    
    #region Update Target
    protected override void UpdateTarget()
    {
        currentTarget = FindNearestEnemy(); // 가장 가까운 적을 찾아서 타겟으로 설정
    }

    /// <summary> 가장 가까운 적을 찾습니다. </summary>
    /// <returns>가장 가까운 적, 없으면 null</returns>
    private Enemy FindNearestEnemy()
    {
        if (WaveManager.Instance == null) return null;
        
        List<Enemy> activeEnemies = WaveManager.Instance.GetActiveEnemies();
        if (activeEnemies == null || activeEnemies.Count == 0) return null;
        
        Enemy nearestEnemy = null;
        float nearestDistance = float.MaxValue;
        
        foreach (Enemy enemy in activeEnemies)
        {
            if (enemy == null || enemy.IsDead()) continue;
            
            float distance = Vector3.Distance(transform.position, enemy.transform.position);
            
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestEnemy = enemy;
            }
        }
        
        return nearestEnemy;
    }

    #endregion
    
    #region Update Movement
    protected override void UpdateMovement()
    {
        if (!canMove && !WaveManager.Instance.IsWaveActive()) return; // 움직임이 제한된 경우 정지
        
        Transform stopPos = UnitManager.Instance.unitStopPos;
        if (stopPos == null) return;
        
        // UnitStopPos에 도착했는지 확인 (위쪽으로 이동)
        if (transform.position.y >= stopPos.position.y) return; // 이미 도착한 경우 더 이상 이동하지 않음
        
        // 타겟이 있고 사거리 안에 있으면 공격
        if (currentTarget != null)
        {
            float distanceToTarget = Vector3.Distance(transform.position, currentTarget.transform.position);
            if (distanceToTarget <= currentAttackRange)
            {
                Attack();
                return; // 공격 중에는 이동하지 않음
            }
        }
        
        // 가장 가까운 적을 향해 이동
        MoveTowardsNearestEnemy();
    }
    
    private void MoveTowardsNearestEnemy()
    {
        Vector3 movement = Vector3.zero;
        
        // 기본 전진 방향 (위쪽)
        movement += Vector3.up * currentMoveSpeed * Time.deltaTime * 10f;
        
        // 가장 가까운 적이 있으면 좌우로도 이동
        if (currentTarget != null)
        {
            float horizontalDistance = currentTarget.transform.position.x - transform.position.x;
            
            // 적이 왼쪽에 있으면 왼쪽으로, 오른쪽에 있으면 오른쪽으로 이동
            if (Mathf.Abs(horizontalDistance) > 0.5f) // 최소 거리 이상일 때만 좌우 이동
            {
                float horizontalMovement = Mathf.Sign(horizontalDistance) * currentMoveSpeed * Time.deltaTime * 5f; // 좌우 이동 속도는 전진 속도의 절반
                movement += Vector3.right * horizontalMovement;
            }
        }
        
        transform.position += movement;
    }
    
    #endregion
    
    protected override void Die()
    {
        if (isDead) return;
        UnitManager.Instance.RemoveUnit(this); // 유닛 매니저의 activeUnits에서 유닛 제거
        base.Die();
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
        
        UpgradeSystem.Instance?.ApplyPermanentUpgradesToUnit(this);
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

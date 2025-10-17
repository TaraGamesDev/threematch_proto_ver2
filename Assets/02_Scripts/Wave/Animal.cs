using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using DG.Tweening;

public class Animal : Unit
{   
    [Title("전투")]
    // currentTarget은 부모 클래스 Unit에서 상속받음
    
    #region Update Target
    protected override void UpdateTarget()
    {
        // 1순위: 가장 가까운 적을 찾아서 타겟으로 설정
        currentTarget = FindNearestEnemy(); // 가장 가까운 적을 찾아서 타겟으로 설정
        
        // 2순위: 적이 없으면 적 기지를 타겟으로 설정 -> null일때 적 기지를 공격하도록 따로 처리 
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
        
        // Enemy 타겟이 있고 사거리 안에 있으면 공격
        if (currentTarget != null && !currentTarget.IsDead())
        {
            float distanceToTarget = Vector3.Distance(transform.position, currentTarget.transform.position);
            if (distanceToTarget <= currentAttackRange)
            {
                StopMovement(); // 공격 중에는 정지
                Attack();
                return; // 공격 중에는 이동하지 않음
            }
        }
        
        // 적 기지 타겟이 있고 사거리 안에 있으면 공격
        // if (WaveManager.Instance.EnemyBaseTransform != null)
        // {
        //     float distanceToBase = Vector3.Distance(transform.position, WaveManager.Instance.EnemyBaseTransform.position);
        //     if (distanceToBase <= currentAttackRange)
        //     {
        //         StopMovement(); // 공격 중에는 정지
        //         AttackEnemyBase();
        //         return; // 공격 중에는 이동하지 않음
        //     }
        // }
        
        // 타겟을 향해 이동
        MoveTowardsTarget();
    }
    
    private void MoveTowardsTarget()
    {
        if (rb == null) {Debug.LogError("Animal: rb is null"); return;}
        
        Vector2 targetPosition = Vector2.zero;
        
        // Enemy 타겟이 있으면 Enemy를 향해 이동
        if (currentTarget != null && !currentTarget.IsDead()) targetPosition = currentTarget.transform.position;
        else currentTarget = FindNearestEnemy(); // 타겟이 없으면 타겟 탐색 
        
        // Enemy 타겟이 없고 적 기지가 있으면 적 기지를 향해 이동
        if (currentTarget == null && WaveManager.Instance?.EnemyBaseTransform != null) targetPosition = WaveManager.Instance.EnemyBaseTransform.position;
        
        // 타겟을 향한 방향 벡터 계산
        Vector2 direction = (targetPosition - (Vector2)transform.position).normalized;
        rb.linearVelocity = direction * currentMoveSpeed;
    }
    
    /// <summary> 적 기지 공격 </summary>
    private void AttackEnemyBase()
    {
        if (Time.time - lastAttackTime < 1f / currentAttackSpeed) return;
        
        lastAttackTime = Time.time;
        
        // 공격 애니메이션 실행
        AttackAnimation();
        
        // 적 기지에 데미지 입히기
        if (WaveManager.Instance != null) WaveManager.Instance.DamageEnemyBase(currentAttackDamage);
    }
    
    #endregion
    
    protected override void Die()
    {
        if (isDead) return;
        // UnitManager.Instance.RemoveUnit(this); // 유닛 매니저의 activeUnits에서 유닛 제거 
        SetCanMove(false);

        // 이동 못하도록 
        rb.constraints = RigidbodyConstraints2D.FreezeAll;
        bodyCollider.enabled = false; // 죽은것들과는 충돌하지 않도록 
        
        // 플레이어 유닛 전멸 체크
        GameManager.Instance?.CheckPlayerUnitsDefeat();
        
        base.Die();
    }

}

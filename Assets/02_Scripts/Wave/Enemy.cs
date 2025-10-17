using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class Enemy : Unit
{
    [Header("전투")]
    private Transform targetPlayer; // 플레이어 타겟
    
    protected void OnEnable()
    {
        targetPlayer = null;
        currentTarget = null;
        if(GameManager.Instance.playerTransform != null) targetPlayer = GameManager.Instance.playerTransform;
        else Debug.LogError("GameManager.Instance.playerTransform is null");
    }
    
    #region Update Target
    protected override void UpdateTarget()
    {
        currentTarget = FindNearestAnimal(); // 가장 가까운 동물을 찾아서 타겟으로 설정
    }

    /// <summary> 가장 가까운 유닛을 찾습니다. /// </summary>
    /// <returns>가장 가까운 유닛, 없으면 null</returns>
    private Unit FindNearestAnimal()
    {
        if (UnitManager.Instance == null) return null;
        
        List<Animal> activeAnimals = UnitManager.Instance.activeUnits;
        if (activeAnimals == null || activeAnimals.Count == 0) return null;
        
        Animal nearestAnimal = null;
        float nearestDistance = float.MaxValue;
        
        foreach (Animal animal in activeAnimals)
        {
            if (animal == null || animal.IsDead()) continue;
            
            float distance = Vector3.Distance(transform.position, animal.transform.position);
            
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestAnimal = animal;
            }
        }
        
        return nearestAnimal;
    }

    #endregion
    
    #region Update Movement
    protected override void UpdateMovement()
    {
        if (!canMove) return; // 움직임이 제한된 경우 정지

        // 유닛 타겟이 있고 사거리 안에 있으면 공격
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

        currentTarget = FindNearestAnimal(); // 타겟이 없으면 한번 찾고
        
        // 그래도 공격할 동물이 없고 플레이어가 사거리 안에 있으면 플레이어 공격
        if (currentTarget == null && targetPlayer != null)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, targetPlayer.position);
            if (distanceToPlayer <= currentAttackRange)
            {
                StopMovement(); // 공격 중에는 정지
                AttackPlayer();
                return;
            }
        }
        
        // 가장 가까운 적을 향해 이동
        MoveTowardsNearestTarget();
    }

    private void MoveTowardsNearestTarget()
    {
        if (rb == null){Debug.LogError("Enemy: rb is null"); return;}
        
        Vector2 targetPosition = Vector2.zero;
        
        // Animal 타겟이 있으면 Animal을 향해 이동
        if (currentTarget != null) targetPosition = currentTarget.transform.position;
        
        // Animal 타겟이 없고 플레이어가 있으면 플레이어를 향해 이동
        else if (targetPlayer != null) targetPosition = targetPlayer.position;

        // 타겟을 향한 방향 벡터 계산
        Vector2 direction = (targetPosition - (Vector2)transform.position).normalized;
        
        rb.linearVelocity = direction * currentMoveSpeed;
    }
    
    #endregion
    
    #region Attack
    
    private void AttackPlayer()
    {
        if (Time.time - lastAttackTime < 1f / currentAttackSpeed) return;
        
        lastAttackTime = Time.time;
        
        // 공격 애니메이션 실행
        AttackAnimation();
        
        if (targetPlayer != null) CombatManager.Instance?.ResolvePlayerHit(currentAttackDamage);
        
    }
    #endregion

    private void OnDisable()
    {
        targetPlayer = null;
        currentTarget = null;
    }
    
    protected override void Die()
    {
        base.Die();

        // 적은 죽으면 풀로 돌아가야함
        PoolManager.Instance.Release(gameObject);

        // 미리 설정된 골드 보상 사용
        GameManager.Instance.AddGold(WaveManager.Instance.CurrentWaveGoldReward);
        GameManager.Instance.AddExp(); // 몬스터 1마리당 1 경험치

        // UI 업데이트
        UIManager.Instance.UpdateGoldTextUI();
        UIManager.Instance.UpdateExpTextUI();

        if (WaveManager.Instance != null) WaveManager.Instance.UnregisterEnemy(this);
    }

}

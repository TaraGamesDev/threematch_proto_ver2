using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class Enemy : Unit
{
    [Header("보상")]
    public int goldReward = 1;
    
    [Header("전투")]
    private Transform targetPlayer; // 플레이어 타겟
    
    protected override void OnEnable()
    {
        base.OnEnable();
        targetPlayer = null;
        currentTarget = null;
        if(GameManager.Instance.playerTransform != null) targetPlayer = GameManager.Instance.playerTransform;
        else Debug.LogError("GameManager.Instance.playerTransform is null");

        if (WaveManager.Instance != null) WaveManager.Instance.RegisterEnemy(this);
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
        
        // 공격할 동물이 없고 플레이어가 사거리 안에 있으면 플레이어 공격
        if (currentTarget == null && targetPlayer != null)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, targetPlayer.position);
            if (distanceToPlayer <= currentAttackRange)
            {
                AttackPlayer();
                return; // 공격 중에는 이동하지 않음
            }
        }
        
        // 유닛 타겟이 있고 사거리 안에 있으면 공격
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
        MoveTowardsNearestTarget();
    }

    private void MoveTowardsNearestTarget()
    {
        Vector3 movement = Vector3.zero;
        
        // 기본 전진 방향 (아래쪽)
        movement += Vector3.down * currentMoveSpeed * Time.deltaTime * 10f;
        
        // 타겟이 있으면 타겟을 향해 좌우로도 이동
        if (currentTarget != null)
        {
            float horizontalDistance = currentTarget.transform.position.x - transform.position.x;
            
            // 타겟이 왼쪽에 있으면 왼쪽으로, 오른쪽에 있으면 오른쪽으로 이동
            if (Mathf.Abs(horizontalDistance) > 0.5f) // 최소 거리 이상일 때만 좌우 이동
            {
                float horizontalMovement = Mathf.Sign(horizontalDistance) * currentMoveSpeed * Time.deltaTime * 5f; // 좌우 이동 속도는 전진 속도의 절반
                movement += Vector3.right * horizontalMovement;
            }
        }
        else if (targetPlayer != null)
        {
            // currentTarget이 null이면 플레이어를 향해 좌우로 이동
            float horizontalDistance = targetPlayer.position.x - transform.position.x;
            
            // 플레이어가 왼쪽에 있으면 왼쪽으로, 오른쪽에 있으면 오른쪽으로 이동
            if (Mathf.Abs(horizontalDistance) > 0.5f) // 최소 거리 이상일 때만 좌우 이동
            {
                float horizontalMovement = Mathf.Sign(horizontalDistance) * currentMoveSpeed * Time.deltaTime * 5f; // 좌우 이동 속도는 전진 속도의 절반
                movement += Vector3.right * horizontalMovement;
            }
        }
        
        transform.position += movement;
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
        if (isDead) return;

        Debug.Log($"[Enemy] {unitName} Die");

        // 골드 & 경험치 획득
        GameManager.Instance.AddGold(goldReward);
        GameManager.Instance.AddExp(); // 몬스터 1마리당 1 경험치

        // UI 업데이트
        UIManager.Instance.UpdateGoldTextUI();
        UIManager.Instance.UpdateExpTextUI();

        if (WaveManager.Instance != null) WaveManager.Instance.UnregisterEnemy(this);

        base.Die();
    }

}

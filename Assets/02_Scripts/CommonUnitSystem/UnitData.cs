using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;


#region 유닛 능력 시스템

[System.Serializable]
public class UnitAbility
{
    [Header("능력 타입")]
    public AbilityType abilityType;


    [Header("능력 정보")]
    public string abilityName;
    public string description;

    
    [Header("능력 수치")]
    public float value;           // 수치값 (데미지, 확률, 지속시간 등)
    public float duration;        // 지속시간
    public float cooldown;        // 쿨다운
    public float range;          // 효과 범위
    public float chance = 100f;  // 발동 확률 (%)
}

public enum AbilityType
{
    // 공격 관련
    AreaDamage,      // 범위 공격
    DoT,             // 지속 데미지
    Stun,            // 기절
    Knockback,       // 넉백
    
    // 디버프
    Slow,            // 이속 감소
    WeaknessDebuff,  // 공격력 감소
    VulnerabilityDebuff, // 방어력 감소
    
    // 버프
    SpeedBoost,      // 이속 증가
    DamageBoost,     // 공격력 증가
    Shield,          // 보호막
    
    // 특수 효과
    Teleport,        // 순간이동
    Invisible,       // 은신
    Regeneration,    // 체력 회복
    
    // 소환/생성
    SummonMinion,    // 하수인 소환
    CreateBarrier    // 장벽 생성
}

#endregion


#region 유닛 데이터베이스
[CreateAssetMenu(fileName = "New Unit", menuName = "Forest Guardians/Unit Data")]
public class UnitData : ScriptableObject
{
    [Header("기본 정보")]
    public string unitName;
    public string description;
    public Sprite unitSprite;
    public UnitTier tier;

    
    [Header("스탯")]
    public int health = 100;
    public int attackDamage = 10;
    public float attackSpeed = 1f;
    public float moveSpeed = 2f;
    public float attackRange = 1f;

    
    [Header("공격 시스템")]
    public AttackType attackType = AttackType.Melee;
    public AttackPattern attackPattern = AttackPattern.Single;
    public ProjectileType projectileType = ProjectileType.None;
    public float splashRadius = 0f;        // 범위 공격 반경
    
    
    [Header("능력 시스템")]
    public List<UnitAbility> abilities = new List<UnitAbility>();

    public enum UnitTier
    {
        None,
        Tier1,
        Tier2,
        Tier3,
        Tier4  
    }
    
    public enum AttackType
    {
        Melee,      // 근접 공격
        Projectile, // 투사체 공격
        Explosion,  // 폭발 공격
        Lightning,  // 번개 공격
        FireBreath  // 불 뿜기
    }
    
    public enum AttackPattern
    {
        Single,          // 단일 대상
        Line,            // 직선 관통
        Splash,          // 범위 공격
        MultiTarget,     // 다중 대상
        Chain            // 연쇄 공격
    }
    
    public enum ProjectileType
    {
        None,            // 근접 공격
        Arrow,           // 화살
        Magic,           // 마법 구체
        Bomb,            // 폭탄
        Lightning,       // 번개
        Fire             // 화염
    }
}

// 유닛 데이터베이스
public static class UnitDatabase
{
    private static Dictionary<string, UnitData> units = new Dictionary<string, UnitData>();
    private static bool isInitialized = false;
    
    public static void Initialize()
    {
        if (isInitialized) return;
        
        LoadUnitDataFromResources(); // Resources 폴더에서 모든 UnitData ScriptableObject 로드
        
        isInitialized = true;
    }
    
    private static void LoadUnitDataFromResources()
    {
        // Resources/Units 폴더에서 모든 UnitData 로드
        UnitData[] loadedUnits = Resources.LoadAll<UnitData>("Units");
        
        foreach (var unit in loadedUnits)
        {
            if (unit != null)
            {
                // 유닛 ID를 키로 사용 (파일명 또는 name 속성)
                string unitId = unit.name;
                
                if (!units.ContainsKey(unitId))
                {
                    units[unitId] = unit;
                    // Debug.Log($"유닛 로드됨: {unit.unitName} (ID: {unitId})");
                }
                else
                {
                    Debug.LogWarning($"중복된 유닛 ID 발견: {unitId}");
                }
            }
        }
        
        Debug.Log($"총 {units.Count}개의 유닛이 로드되었습니다.");
    }
    
    public static UnitData GetUnit(string id)
    {
        if (!isInitialized) Initialize();
        return units.ContainsKey(id) ? units[id] : null;
    }
    
    public static List<UnitData> GetUnitsByTier(UnitData.UnitTier tier)
    {
        if (!isInitialized) Initialize();
        
        List<UnitData> tierUnits = new List<UnitData>();
        foreach (var unit in units.Values) if (unit.tier == tier) tierUnits.Add(unit);
        return tierUnits;
    }
    
    public static UnitData GetNextTierUnit(UnitData currentUnit)
    {
        if (!isInitialized) Initialize();
        
        // 현재 티어를 int로 변환
        int currentTier = (int)currentUnit.tier;
        int nextTier = currentTier+1;
        
        // 최대 티어 체크 (enum의 최댓값을 자동으로 가져옴)
        var maxTierValue = Enum.GetValues(typeof(UnitData.UnitTier)).Cast<int>().Max();
        if (nextTier > maxTierValue) return null;
        
        // 다음 티어의 유닛들 가져오기
        UnitData.UnitTier nextTierEnum = (UnitData.UnitTier)nextTier;
        List<UnitData> nextTierUnits = GetUnitsByTier(nextTierEnum);
        
        // 다음 티어에 유닛이 있으면 랜덤 유닛 반환
        return nextTierUnits.Count > 0 ? nextTierUnits[UnityEngine.Random.Range(0, nextTierUnits.Count)] : null;
    }
}

#endregion
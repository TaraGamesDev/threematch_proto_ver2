using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 유닛 티어별 스폰 확률을 관리하는 시스템
/// 확장성을 고려하여 설계되었으며, 티어가 추가/삭제되어도 코드 수정 없이 동작합니다.
/// </summary>
[CreateAssetMenu(fileName = "New Spawn Probability Config", menuName = "FIFO Defence/Spawn Probability Config")]
public class SpawnProbabilitySystem : ScriptableObject
{
    [Header("티어별 스폰 확률 설정")]
    [SerializeField] private List<TierSpawnProbability> tierProbabilities = new List<TierSpawnProbability>();

    
    [Header("정규화된 확률 (수정하지 마슈)")]
    [SerializeField] private List<NormalizedTierProbability> normalizedProbabilities = new List<NormalizedTierProbability>();

    
    [Header("총 확률 합계")]
    [SerializeField, ReadOnly] private float totalProbabilitySum = 0f;
    
    // 캐시된 정규화 확률
    private Dictionary<UnitData.UnitTier, float> normalizedCache = new Dictionary<UnitData.UnitTier, float>();
    private bool isCacheValid = false;
    


    #region 내부 클래스들
    
    [System.Serializable]
    public class TierSpawnProbability
    {
        public UnitData.UnitTier tier;

        [Range(0f, 100f)]
        public float probability = 10f;
        
        public TierSpawnProbability(UnitData.UnitTier tier, float probability)
        {
            this.tier = tier;
            this.probability = probability;
        }
    }
    
    [System.Serializable]
    public class NormalizedTierProbability
    {
        public UnitData.UnitTier tier;
        public float originalProbability;
        public float normalizedProbability;
        public float cumulativeProbability;
        
        public NormalizedTierProbability(UnitData.UnitTier tier, float original, float normalized, float cumulative)
        {
            this.tier = tier;
            this.originalProbability = original;
            this.normalizedProbability = normalized;
            this.cumulativeProbability = cumulative;
        }
    }
    
    #endregion
    
    #region 초기화 및 설정
    
    private void OnValidate()
    {
        // 인스펙터에서 값이 변경될 때마다 정규화 갱신
        RefreshNormalizedProbabilities();
    }
    
    /// <summary>
    /// 정규화된 확률을 다시 계산합니다.
    /// </summary>
    public void RefreshNormalizedProbabilities()
    {
        normalizedProbabilities.Clear();
        normalizedCache.Clear();
        isCacheValid = false;
        
        // 총 확률 합계 계산
        totalProbabilitySum = tierProbabilities.Sum(tp => tp.probability);
        
        if (totalProbabilitySum <= 0f) { Debug.LogWarning("SpawnProbabilitySystem: 총 확률 합계가 0 이하입니다!"); return; }
        
        // 정규화 및 누적 확률 계산
        float cumulativeProbability = 0f;
        
        foreach (var tierProb in tierProbabilities.OrderBy(tp => (int)tp.tier))
        {
            float normalizedProb = (tierProb.probability / totalProbabilitySum) * 100f;
            cumulativeProbability += normalizedProb;
            
            var normalizedTierProb = new NormalizedTierProbability(
                tierProb.tier, 
                tierProb.probability, 
                normalizedProb, 
                cumulativeProbability
            );
            
            normalizedProbabilities.Add(normalizedTierProb);
            normalizedCache[tierProb.tier] = normalizedProb;
        }
        
        isCacheValid = true;
    }
    
    #endregion
    
    #region 공개 메서드들
    
    /// <summary>
    /// 확률에 따라 랜덤 티어를 선택합니다.
    /// </summary>
    public UnitData.UnitTier GetRandomTier()
    {
        if (!isCacheValid) RefreshNormalizedProbabilities();
        
        if (normalizedProbabilities.Count == 0)
        {
            Debug.LogWarning("SpawnProbabilitySystem: 설정된 티어 확률이 없습니다. Tier1을 반환합니다.");
            return UnitData.UnitTier.Tier1;
        }
        
        // 0~100 사이의 랜덤 값 생성
        float randomValue = UnityEngine.Random.Range(0f, 100f);
        
        // 누적 확률을 이용해 티어 선택
        foreach (var normalizedProb in normalizedProbabilities)
            if (randomValue <= normalizedProb.cumulativeProbability) return normalizedProb.tier;
        
        // 혹시라도 여기까지 온다면 마지막 티어 반환
        return normalizedProbabilities.Last().tier;
    }
    
    /// <summary>
    /// 특정 티어의 정규화된 확률을 반환합니다.
    /// </summary>
    /// <param name="tier">확인할 티어</param>
    /// <returns>정규화된 확률 (0~100%)</returns>
    public float GetNormalizedProbability(UnitData.UnitTier tier)
    {
        if (!isCacheValid) RefreshNormalizedProbabilities();
        
        return normalizedCache.ContainsKey(tier) ? normalizedCache[tier] : 0f;
    }

    
    /// <summary>
    /// 특정 티어의 스폰 확률을 설정합니다.
    /// </summary>
    /// <param name="tier">설정할 티어</param>
    /// <param name="probability">새로운 확률</param>
    public void SetTierProbability(UnitData.UnitTier tier, float probability)
    {
        var existingTierProb = tierProbabilities.FirstOrDefault(tp => tp.tier == tier);
        
        if (existingTierProb != null) existingTierProb.probability = probability;
        else tierProbabilities.Add(new TierSpawnProbability(tier, probability));
        
        RefreshNormalizedProbabilities();
    }
    
    
    #endregion
    
    #region 디버그 및 유틸리티
    
    /// <summary>
    /// 현재 확률 설정을 콘솔에 출력합니다.
    /// </summary>
    [ContextMenu("Debug Print Probabilities")]
    public void DebugPrintProbabilities()
    {
        if (!isCacheValid) RefreshNormalizedProbabilities();
        
        Debug.Log("=== Spawn Probability System ===");
        Debug.Log($"총 확률 합계: {totalProbabilitySum}");
        Debug.Log("티어별 확률:");
        
        foreach (var normalizedProb in normalizedProbabilities)
        {
            Debug.Log($"  {normalizedProb.tier}: {normalizedProb.originalProbability} → {normalizedProb.normalizedProbability:F2}% (누적: {normalizedProb.cumulativeProbability:F2}%)");
        }
    }
    
    /// <summary>
    /// 확률 테스트를 실행합니다.
    /// </summary>
    /// <param name="testCount">테스트 횟수</param>
    [ContextMenu("Test Spawn Distribution")]
    public void TestSpawnDistribution(int testCount = 1000)
    {
        Dictionary<UnitData.UnitTier, int> spawnCounts = new Dictionary<UnitData.UnitTier, int>();
        
        // 테스트 실행
        for (int i = 0; i < testCount; i++)
        {
            var tier = GetRandomTier();
            if (!spawnCounts.ContainsKey(tier)) spawnCounts[tier] = 0;
            spawnCounts[tier]++;
        }
        
        // 결과 출력
        Debug.Log($"=== Spawn Distribution Test ({testCount}회) ===");
        foreach (var kvp in spawnCounts.OrderBy(kv => (int)kv.Key))
        {
            float actualPercentage = (kvp.Value / (float)testCount) * 100f;
            float expectedPercentage = GetNormalizedProbability(kvp.Key);
            Debug.Log($"  {kvp.Key}: {kvp.Value}회 ({actualPercentage:F2}%) - 예상: {expectedPercentage:F2}%");
        }
    }
    
    #endregion
}

#region 커스텀 프로퍼티 어트리뷰트

/// <summary>
/// 읽기 전용 필드를 인스펙터에 표시하기 위한 어트리뷰트
/// </summary>
public class ReadOnlyAttribute : PropertyAttribute { }

#if UNITY_EDITOR
[UnityEditor.CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
public class ReadOnlyDrawer : UnityEditor.PropertyDrawer
{
    public override void OnGUI(Rect position, UnityEditor.SerializedProperty property, GUIContent label)
    {
        GUI.enabled = false;
        UnityEditor.EditorGUI.PropertyField(position, property, label, true);
        GUI.enabled = true;
    }
}
#endif

#endregion

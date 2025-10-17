using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// BGDatabase의 Probability 데이터를 직접 활용하는 확률 시스템
/// SpawnProbabilitySystem을 대체하여 더 간단하고 직접적인 확률 관리
/// </summary>
public static class DatabaseProbabilitySystem
{
    // 현재 확률 레벨
    private static int currentProbabilityLevel = 1;
    public static int CurrentProbabilityLevel 
    { 
        get => currentProbabilityLevel; 
        set => currentProbabilityLevel = Mathf.Max(1, value);
    }
    
    // 최대 레벨
    private static int maxLevel = 10;
    public static int MaxLevel 
    { 
        get => maxLevel; 
        set => maxLevel = Mathf.Max(1, value);
    }
    
    // 캐시된 확률 데이터
    private static Dictionary<int, ProbabilityData> probabilityCache = new Dictionary<int, ProbabilityData>();
    private static bool isCacheInitialized = false;
    
    /// <summary>
    /// 확률 데이터를 담는 구조체
    /// </summary>
    [System.Serializable]
    public struct ProbabilityData
    {
        public float tier1;
        public float tier2;
        public float tier3;
        public float tier4;
        public float total;
        
        public ProbabilityData(float t1, float t2, float t3, float t4)
        {
            tier1 = t1;
            tier2 = t2;
            tier3 = t3;
            tier4 = t4;
            total = t1 + t2 + t3 + t4;
        }
    }
    
    
    /// <summary> 확률 시스템을 초기화합니다. 게임 시작 시 한번 호출됩니다. </summary>
    public static void Initialize()
    {
        if (isCacheInitialized) return;
        
        try
        {
            // BGDatabase에서 모든 Probability 엔티티 가져오기
            var allProbabilities = Probability.FindEntities(null);
            
            if (allProbabilities != null && allProbabilities.Count > 0)
            {
                // 모든 레벨의 확률 데이터를 캐시에 미리 로드
                foreach (var probabilityEntity in allProbabilities)
                {
                    var probabilityData = new ProbabilityData(
                        probabilityEntity.TIER1,
                        probabilityEntity.TIER2,
                        probabilityEntity.TIER3,
                        probabilityEntity.TIER4
                    );
                    
                    probabilityCache[probabilityEntity.LEVEL] = probabilityData;
                }
                
                isCacheInitialized = true;
                Debug.Log($"DatabaseProbabilitySystem: {probabilityCache.Count}개 레벨의 확률 데이터를 캐시에 로드했습니다.");

                // 현재 & 최대 레벨 설정
                currentProbabilityLevel = probabilityCache.Keys.Min();
                maxLevel = probabilityCache.Keys.Max(); 
            }
            else Debug.LogWarning("DatabaseProbabilitySystem: BGDatabase에서 Probability 데이터를 찾을 수 없습니다.");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"DatabaseProbabilitySystem: 초기화 중 오류 발생: {e.Message}");
        }
    }

    /// <summary> 확률 레벨을 초기값으로 리셋합니다. 게임 시작 시 호출됩니다. </summary>
    public static void ResetProbabilityLevel()
    {
        if (probabilityCache.Count > 0)
        {
            currentProbabilityLevel = probabilityCache.Keys.Min();
            Debug.Log($"DatabaseProbabilitySystem: 확률 레벨을 {currentProbabilityLevel}으로 초기화했습니다.");
        }
        else
        {
            currentProbabilityLevel = 1;
            Debug.LogWarning("DatabaseProbabilitySystem: 캐시가 비어있어 확률 레벨을 1로 설정했습니다.");
        }
    }
    
    /// <summary> 확률에 따라 랜덤 티어를 반환 </summary>
    public static UnitData.UnitTier GetRandomTier()
    {
        var probabilityData = GetProbabilityData(currentProbabilityLevel);
        
        if (probabilityData.total <= 0f)
        {
            Debug.LogWarning("DatabaseProbabilitySystem: 총 확률이 0 이하입니다. Tier1을 반환합니다.");
            return UnitData.UnitTier.Tier1;
        }
        
        // 0~100 사이의 랜덤 값 생성
        float randomValue = UnityEngine.Random.Range(0f, 100f);
        
        // 누적 확률 계산
        float cumulativeProbability = 0f;
        
        // Tier1 확률
        cumulativeProbability += (probabilityData.tier1 / probabilityData.total) * 100f;
        if (randomValue <= cumulativeProbability) return UnitData.UnitTier.Tier1;
        
        // Tier2 확률
        cumulativeProbability += (probabilityData.tier2 / probabilityData.total) * 100f;
        if (randomValue <= cumulativeProbability) return UnitData.UnitTier.Tier2;
        
        // Tier3 확률
        cumulativeProbability += (probabilityData.tier3 / probabilityData.total) * 100f;
        if (randomValue <= cumulativeProbability) return UnitData.UnitTier.Tier3;
        
        // Tier4 확률 (마지막이므로 무조건 반환)
        return UnitData.UnitTier.Tier4;
    }
    
    /// <summary>
    /// 특정 레벨의 확률 데이터를 반환합니다.
    /// </summary>
    public static ProbabilityData GetProbabilityData(int level)
    {
        // 캐시에서 확인
        if (probabilityCache.ContainsKey(level)) return probabilityCache[level];
        
        // 캐시에 없는 경우 기본 확률 반환
        Debug.LogWarning($"DatabaseProbabilitySystem: 레벨 {level}에 해당하는 확률 데이터를 찾을 수 없습니다. 기본 확률을 사용합니다.");
        return new ProbabilityData(60f, 30f, 8f, 2f); // 기본 확률
    }
    
    /// <summary>
    /// 현재 확률 정보를 ShowMessage로 출력합니다.
    /// </summary>
    public static void ShowCurrentProbabilityInfo()
    {
        var probabilityData = GetProbabilityData(CurrentProbabilityLevel);
        
        string message = $"현재 확률 레벨: {CurrentProbabilityLevel}\n" +
                        $"Tier1: {probabilityData.tier1:F1}%\n" +
                        $"Tier2: {probabilityData.tier2:F1}%\n" +
                        $"Tier3: {probabilityData.tier3:F1}%\n" +
                        $"Tier4: {probabilityData.tier4:F1}%";
        
        UIManager.Instance?.ShowMessage(message, 3f);
    }

}

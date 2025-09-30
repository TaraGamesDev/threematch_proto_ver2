# 🎲 Spawn Probability System

**확장 가능한 스폰 확률 시스템** 문서입니다.

## 📋 **목차**

1. [개요](#개요)
2. [시스템 특징](#시스템-특징)
3. [파일 구조](#파일-구조)
4. [설치 및 설정](#설치-및-설정)
5. [사용법](#사용법)
6. [확장성](#확장성)
7. [디버깅 도구](#디버깅-도구)
8. [고급 기능](#고급-기능)
9. [문제 해결](#문제-해결)

---

## 📖 **개요**

**SpawnProbabilitySystem**은 유닛 티어별 스폰 확률을 효율적으로 관리하는 시스템입니다. 

### 🎯 **해결하는 문제**
- ❌ **기존 방식**: 하드코딩된 확률 변수들
```csharp
public float tier1SpawnChance = 0.8f;
public float tier2SpawnChance = 0.15f; 
public float tier3SpawnChance = 0.05f;
// 새 티어 추가 시마다 코드 수정 필요!
```

- ✅ **새로운 방식**: 확장 가능한 설정 기반 시스템
```csharp
public SpawnProbabilitySystem spawnProbabilityConfig;
// 에셋 파일로 관리, 코드 수정 불필요!
```

---

## 🚀 **시스템 특징**

### ✨ **주요 장점**

| 특징 | 설명 |
|------|------|
| 🔧 **완전한 확장성** | 새 티어 추가 시 코드 수정 불필요 |
| ⚡ **자동 정규화** | 입력된 확률들이 자동으로 100%로 정규화 |
| 🎯 **정확한 선택** | 누적 확률 알고리즘으로 정확한 랜덤 선택 |
| 💾 **에셋 관리** | ScriptableObject로 프로젝트 내에서 쉽게 관리 |
| 🔄 **동적 변경** | 런타임에 확률 조정 가능 |
| 🛠️ **디버깅 도구** | 테스트 및 디버그 기능 내장 |

### 📊 **확률 정규화 예시**
```
입력값:
- Tier1: 70
- Tier2: 20  
- Tier3: 8
- Tier4: 2
총합: 100

자동 정규화 결과:
- Tier1: 70.0% (누적: 70.0%)
- Tier2: 20.0% (누적: 90.0%)
- Tier3: 8.0%  (누적: 98.0%)
- Tier4: 2.0%  (누적: 100.0%)
```


---

## 🛠️ **설치 및 설정**

### **1단계: 스폰 확률 설정 에셋 생성**

1. 프로젝트 창에서 **우클릭**
2. **Create → FIFO Defence → Spawn Probability Config** 선택
3. 파일명 설정 (예: `DefaultSpawnConfig`)

### **2단계: 확률 설정**

생성된 에셋을 선택하고 인스펙터에서 설정:

```
티어별 스폰 확률 설정:
┌─────────┬─────────┬─────────────────────────┐
│ 티어    │ 확률    │ 설명                    │
├─────────┼─────────┼─────────────────────────┤
│ Tier1   │ 70.0    │ 가장 기본적인 유닛      │
│ Tier2   │ 20.0    │ 중급 유닛               │
│ Tier3   │ 8.0     │ 고급 유닛               │
│ Tier4   │ 2.0     │ 전설 유닛               │
└─────────┴─────────┴─────────────────────────┘
```

### **3단계: UnitSpawner에 연결**

1. `UnitSpawner` 컴포넌트를 선택
2. `Spawn Probability Config` 필드에 생성한 에셋을 드래그 앤 드롭

---

## 🎮 **사용법**

### **기본 사용법**

```csharp
// UnitSpawner에서 자동으로 사용됨
private UnitData.UnitTier GetRandomTier()
{
    if (spawnProbabilityConfig != null)
    {
        return spawnProbabilityConfig.GetRandomTier();
    }
    return UnitData.UnitTier.Tier1; // 기본값
}
```

### **코드에서 확률 조회**

```csharp
// 특정 티어의 정규화된 확률 확인
float tier1Probability = spawnProbabilityConfig.GetNormalizedProbability(UnitData.UnitTier.Tier1);
Debug.Log($"Tier1 확률: {tier1Probability}%");

// 모든 티어의 확률 조회
var allProbabilities = spawnProbabilityConfig.GetAllNormalizedProbabilities();
foreach (var kvp in allProbabilities)
{
    Debug.Log($"{kvp.Key}: {kvp.Value}%");
}
```

### **런타임 확률 변경**

```csharp
// 특정 티어 확률 변경
spawnProbabilityConfig.SetTierProbability(UnitData.UnitTier.Tier1, 50f);

// 모든 확률을 1.5배로 스케일링 (비율은 유지)
spawnProbabilityConfig.ScaleAllProbabilities(1.5f);

// 티어 제거
spawnProbabilityConfig.RemoveTier(UnitData.UnitTier.Tier4);
```

---

## 🔄 **확장성**

### **새로운 티어 추가**

1. **UnitData.cs**에서 enum 확장:
```csharp
public enum UnitTier
{
    None, Tier1, Tier2, Tier3, Tier4, 
    Tier5, Tier6  // ← 새 티어 추가
}
```

2. **스폰 확률 에셋**에서 새 티어의 확률 설정:
```
Tier5: 0.5% (레어 유닛)
Tier6: 0.1% (최상급 유닛)
```

3. **코드 수정 불필요!** 시스템이 자동으로 처리합니다.

### **게임 진행에 따른 동적 확률 조정**

```csharp
public class WaveManager : MonoBehaviour
{
    public SpawnProbabilitySystem earlyGameConfig;
    public SpawnProbabilitySystem lateGameConfig;
    public UnitSpawner unitSpawner;
    
    void OnWaveStart(int waveNumber)
    {
        if (waveNumber <= 5)
        {
            unitSpawner.spawnProbabilityConfig = earlyGameConfig;
        }
        else if (waveNumber <= 15)
        {
            // 중간 게임: 고급 유닛 확률 점진적 증가
            float tierBonus = (waveNumber - 5) * 0.5f;
            var config = unitSpawner.spawnProbabilityConfig;
            config.SetTierProbability(UnitData.UnitTier.Tier3, 8f + tierBonus);
            config.SetTierProbability(UnitData.UnitTier.Tier4, 2f + tierBonus);
        }
        else
        {
            unitSpawner.spawnProbabilityConfig = lateGameConfig;
        }
    }
}
```

---

## 🛠️ **디버깅 도구**

### **인스펙터 디버그 메뉴**

SpawnProbabilitySystem 에셋에서 **우클릭** → **Debug Print Probabilities**
```
=== Spawn Probability System ===
총 확률 합계: 100
티어별 확률:
  Tier1: 70 → 70.00% (누적: 70.00%)
  Tier2: 20 → 20.00% (누적: 90.00%)
  Tier3: 8 → 8.00% (누적: 98.00%)
  Tier4: 2 → 2.00% (누적: 100.00%)
```

### **스폰 분포 테스트**

UnitSpawner에서 **우클릭** → **Test Spawn Distribution**
```
=== Spawn Distribution Test (1000회) ===
  Tier1: 703회 (70.30%) - 예상: 70.00%
  Tier2: 201회 (20.10%) - 예상: 20.00%
  Tier3: 78회 (7.80%) - 예상: 8.00%
  Tier4: 18회 (1.80%) - 예상: 2.00%
```

### **코드에서 디버깅**

```csharp
// UnitSpawner에서 확률 디버그
[ContextMenu("Debug Spawn Probabilities")]
public void DebugSpawnProbabilities()
{
    if (spawnProbabilityConfig != null)
    {
        spawnProbabilityConfig.DebugPrintProbabilities();
    }
}

// 분포 테스트
[ContextMenu("Test Spawn Distribution")]
public void TestSpawnDistribution()
{
    if (spawnProbabilityConfig != null)
    {
        spawnProbabilityConfig.TestSpawnDistribution(1000);
    }
}
```

---

## ⚡ **고급 기능**

### **확률 시스템 내부 동작**

#### **1. 정규화 과정**
```csharp
private void RefreshNormalizedProbabilities()
{
    // 1단계: 총 확률 합계 계산
    totalProbabilitySum = tierProbabilities.Sum(tp => tp.probability);
    
    // 2단계: 각 확률을 100% 기준으로 정규화
    float normalizedProb = (tierProb.probability / totalProbabilitySum) * 100f;
    
    // 3단계: 누적 확률 계산 (랜덤 선택용)
    cumulativeProbability += normalizedProb;
}
```

#### **2. 랜덤 선택 알고리즘**
```csharp
public UnitData.UnitTier GetRandomTier()
{
    float randomValue = UnityEngine.Random.Range(0f, 100f);
    
    // 누적 확률을 이용한 효율적인 선택
    foreach (var normalizedProb in normalizedProbabilities)
    {
        if (randomValue <= normalizedProb.cumulativeProbability)
            return normalizedProb.tier;
    }
}
```

### **캐시 시스템**

```csharp
// 성능 최적화를 위한 캐시
private Dictionary<UnitData.UnitTier, float> normalizedCache;
private bool isCacheValid = false;

// 캐시 무효화 조건
private void OnValidate()
{
    RefreshNormalizedProbabilities(); // 인스펙터 값 변경 시
}
```

### **커스텀 에디터 확장**

```csharp
// ReadOnly 어트리뷰트로 읽기 전용 필드 표시
[SerializeField, ReadOnly] 
private List<NormalizedTierProbability> normalizedProbabilities;

// 에디터에서 회색으로 표시되어 수정 불가
```

---

## 🔧 **문제 해결**

### **자주 발생하는 문제들**

#### **Q: SpawnProbabilitySystem 타입을 찾을 수 없다는 오류**
```
error CS0246: The type or namespace name 'SpawnProbabilitySystem' could not be found
```

**A: 해결 방법**
1. Unity 에디터에서 **스크립트 재컴파일** 대기
2. `Assets → Reimport All` 실행
3. Unity 재시작

#### **Q: 확률이 정규화되지 않는 문제**
```
총 확률 합계가 0 이하입니다!
```

**A: 해결 방법**
1. 스폰 확률 에셋에서 **모든 확률값이 0보다 큰지** 확인
2. **최소 하나의 티어**에는 확률이 설정되어 있어야 함
3. `RefreshNormalizedProbabilities()` 수동 호출

#### **Q: 런타임에 확률 변경이 적용되지 않음**

**A: 해결 방법**
```csharp
// 확률 변경 후 반드시 갱신 호출
spawnProbabilityConfig.SetTierProbability(UnitData.UnitTier.Tier1, 50f);
spawnProbabilityConfig.RefreshNormalizedProbabilities(); // 자동 호출됨
```

#### **Q: 특정 티어가 전혀 나오지 않음**

**A: 해결 방법**
1. **해당 티어의 UnitData**가 `Resources/Units` 폴더에 있는지 확인
2. **UnitDatabase 초기화** 확인
3. **디버그 테스트** 실행하여 확률 분포 확인

### **성능 최적화 팁**

#### **1. 자주 호출되는 메서드 최적화**
```csharp
// ✅ 좋은 예: 캐시 활용
public float GetNormalizedProbability(UnitData.UnitTier tier)
{
    if (!isCacheValid) RefreshNormalizedProbabilities();
    return normalizedCache.ContainsKey(tier) ? normalizedCache[tier] : 0f;
}

// ❌ 나쁜 예: 매번 계산
public float GetNormalizedProbability(UnitData.UnitTier tier)
{
    RefreshNormalizedProbabilities(); // 매번 호출하면 성능 저하
    // ...
}
```


---

## 📚 **추가 자료**

### **관련 스크립트**
- `UnitData.cs` - 유닛 데이터 및 티어 정의
- `UnitSpawner.cs` - 유닛 스폰 시스템  
- `UnitDatabase.cs` - 유닛 데이터베이스 관리

### **확장 아이디어**
- 🎯 **조건부 확률**: 특정 조건에서만 스폰되는 유닛
- 🌟 **특별 이벤트**: 특정 시간대에 확률 변경
- 🎲 **가중치 시스템**: 플레이어 레벨에 따른 동적 확률
- 📊 **통계 수집**: 실제 스폰 데이터 수집 및 분석

---

## 📄 **라이선스**

이 시스템은 FIFO Defence 프로젝트의 일부로 개발되었습니다.


---

**Happy Spawning! 🎮✨**

using UnityEngine;

public class Init_Manager : MonoBehaviour
{
    public static Init_Manager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
                
        UnitDatabase.Initialize(); // 유닛 데이터 확인 
        QueueManager.Instance.RegisterBlockPool(); // 풀에 등록 
        QueueManager.Instance.RecalculateSlots(); // 슬롯 위치 계산 
        QueueManager.Instance.CreateMythicButtons(); // 신화 버튼들 생성

        DatabaseProbabilitySystem.Initialize(); // 확률 데이터 베이스 초기화
    }

}

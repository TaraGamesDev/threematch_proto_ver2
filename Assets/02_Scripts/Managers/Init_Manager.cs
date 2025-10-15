using UnityEngine;
using System.Threading.Tasks;

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
        StartAsync();
    }
    
    private async void StartAsync()
    {
        await Task.Delay(1000);

        DataManager.Instance.Init();
        
        UnitDatabase.Initialize(); // 유닛 데이터 확인 
        QueueManager.Instance.RegisterBlockPool(); // 풀에 등록 
        QueueManager.Instance.RecalculateSlots(); // 슬롯 위치 계산 
        QueueManager.Instance.CreateMythicButtons(); // 신화 버튼들 생성

        DatabaseProbabilitySystem.Initialize(); // 확률 데이터 베이스 초기화
        GameManager.Instance.InitializeMoneyDataList(); // 골드 데이터 초기화

        LevelUpUpgradeSystem.Instance.InitializeAbilitiesCache(); // 업그레이드 능력들 미리 캐싱 

        GameManager.Instance.InitialisePlayerState(); // 플레이어 상태 초기화
        GameManager.Instance.QueueInitialWave(); // 초기 웨이브 큐 생성
    }

}

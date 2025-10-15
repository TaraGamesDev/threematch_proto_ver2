using UnityEngine;
using System.Collections.Generic;

public class DataManager : MonoBehaviour
{

    public static DataManager Instance;
    public MoneyDataList moneyDataList;

    public void Awake()
    {
        Instance = this;
    }

    public void Init()
    {
        moneyDataList = new MoneyDataList();
        moneyDataList.waveMoneyDatas = new List<WaveMoneyData>();
        moneyDataList.moneyBaseData = new MoneyBaseData();

        int count = MONEY.CountEntities;

        for (int i = 0; i < count; i++)
        {
            moneyDataList.waveMoneyDatas.Add(new WaveMoneyData(MONEY.GetEntity(i).ENEMY_GOLD, MONEY.GetEntity(i).WAVE_MIN, MONEY.GetEntity(i).WAVE_MAX));
        }

        moneyDataList.moneyBaseData.INITIAL_MONEY = MONEY.GetEntity(0).INITIAL_MONEY;
        moneyDataList.moneyBaseData.SPAWN_INITIAL = MONEY.GetEntity(0).SPAWN_INITIAL;
        moneyDataList.moneyBaseData.SPAWN_ADDED = MONEY.GetEntity(0).SPAWN_ADDED;
        moneyDataList.moneyBaseData.UPGRADE = MONEY.GetEntity(0).UPGRADE;
    }
}
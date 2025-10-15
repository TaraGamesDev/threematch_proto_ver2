using UnityEngine;
using System;
using System.Collections.Generic;

[Serializable]
public class MoneyDataList
{
    public List<WaveMoneyData> waveMoneyDatas;
    public MoneyBaseData moneyBaseData;

    public MoneyDataList()
    {
        waveMoneyDatas = new List<WaveMoneyData>();
        moneyBaseData = new MoneyBaseData();

        int count = MONEY.CountEntities;

        for (int i = 0; i < count; i++)
        {
            waveMoneyDatas.Add(new WaveMoneyData(MONEY.GetEntity(i).ENEMY_GOLD, MONEY.GetEntity(i).WAVE_MIN, MONEY.GetEntity(i).WAVE_MAX));
        }

        moneyBaseData.INITIAL_MONEY = MONEY.GetEntity(0).INITIAL_MONEY;
        moneyBaseData.SPAWN_INITIAL = MONEY.GetEntity(0).SPAWN_INITIAL;
        moneyBaseData.SPAWN_ADDED = MONEY.GetEntity(0).SPAWN_ADDED;
        moneyBaseData.UPGRADE = MONEY.GetEntity(0).UPGRADE;
    }
}

[Serializable]
public class WaveMoneyData
{
    public int enemyGold;
    public int waveMin;
    public int waveMax;

    public WaveMoneyData(int enemyGold, int waveMin, int waveMax)
    {
        this.enemyGold = enemyGold;
        this.waveMin = waveMin;
        this.waveMax = waveMax;
    }
}

[Serializable]
public class MoneyBaseData
{
    public int INITIAL_MONEY;
    public int SPAWN_INITIAL;
    public int SPAWN_ADDED;
    public int UPGRADE;
}
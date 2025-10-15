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
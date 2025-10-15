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
}
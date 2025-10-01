using UnityEngine;

/// <summary>
/// 풀에 의해 관리되는 오브젝트가 반드시 포함해야 하는 컴포넌트.
/// 풀 키와 반환 로직을 보관하여 재사용을 단순화합니다.
/// </summary>
public class PooledObject : MonoBehaviour
{
    public string PoolKey { get; private set; }
    
    public void Initialise(string key)
    {
        PoolKey = key;
    }
}

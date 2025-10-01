using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// 프로젝트 전역에서 재사용되는 경량 오브젝트 풀 매니저.
/// 키 기반으로 풀을 등록하고, GameObject 또는 컴포넌트를 요청/반환할 수 있습니다.
/// </summary>
public class PoolManager : MonoBehaviour
{
    private class Pool
    {
        public string key;
        public GameObject prefab;
        public Transform root;
        public readonly Queue<GameObject> cache = new Queue<GameObject>();
    }

    private static PoolManager instance;

    public static PoolManager Instance
    {
        get
        {
            if (instance != null) return instance;

            instance = FindObjectOfType<PoolManager>();
            if (instance != null) return instance;

            GameObject go = new GameObject("PoolManager");
            instance = go.AddComponent<PoolManager>();
            return instance;
        }
    }

    [ShowInInspector, ReadOnly] private readonly Dictionary<string, Pool> pools = new Dictionary<string, Pool>(); // 풀 딕셔너리(키, 풀)

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary> 지정한 키로 풀을 등록합니다. 이미 동일한 키가 존재한다면 무시됩니다. </summary>
    public void RegisterPool(string key, GameObject prefab, int preloadCount = 0, Transform poolParent = null)
    {
        if (string.IsNullOrEmpty(key) || prefab == null){ Debug.LogWarning("PoolManager: 키 또는 프리팹이 유효하지 않습니다."); return; }
        if (pools.ContainsKey(key)){ Debug.LogWarning($"PoolManager: 이미 등록된 키 {key} 를 다시 등록하려 했습니다."); return; }

        var pool = new Pool
        {
            key = key,
            prefab = prefab,
            root = new GameObject($"[Pool] {key}").transform
        };

        Transform parent = poolParent != null ? poolParent : transform;
        pool.root.SetParent(parent, false);
        pool.root.gameObject.SetActive(false);

        pools.Add(key, pool);

        for (int i = 0; i < preloadCount; i++)
        {
            GameObject instance = CreateInstance(pool);
            PrepareForPool(instance, pool);
            pool.cache.Enqueue(instance);
        }
    }

    /// <summary> GameObject를 풀에서 꺼냅니다. </summary>
    public GameObject Get(string key, Transform parent = null, bool worldPositionStays = false)
    {
        if (!pools.TryGetValue(key, out Pool pool)){ Debug.LogWarning($"PoolManager: 등록되지 않은 키 {key} 를 요청했습니다."); return null; }

        GameObject instance = pool.cache.Count > 0 ? pool.cache.Dequeue() : CreateInstance(pool);

        if (instance == null){ Debug.LogWarning($"{key} 풀에서 오브젝트를 꺼내오는데 실패했습니다."); return null; }

        if (parent != null) instance.transform.SetParent(parent, worldPositionStays);
        instance.SetActive(true);

        return instance;
    }

    /// <summary>
    /// 특정 컴포넌트 타입을 직접 요청합니다.
    /// </summary>
    /// <param name="key">풀 키</param>
    /// <param name="parent">부모 트랜스폼</param>
    /// <param name="worldPositionStays">월드 포지션 스테이즈</param>
    /// <returns>컴포넌트</returns>
    public T Get<T>(string key, Transform parent = null, bool worldPositionStays = false) where T : Component
    {
        GameObject go = Get(key, parent, worldPositionStays);
        return go != null ? go.GetComponent<T>() : null;
    }

    /// <summary>
    /// 사용이 끝난 오브젝트를 풀로 되돌립니다.
    /// </summary>
    public void Release(GameObject instance)
    {
        // 예외처리
        if (instance == null) { Debug.LogWarning("PoolManager: null 오브젝트를 반환하려 했습니다."); return; }

        if (!instance.TryGetComponent<PooledObject>(out var pooled) || string.IsNullOrEmpty(pooled.PoolKey))
        {
            Debug.LogWarning("PoolManager: 오브젝트에 PooledObject 컴포넌트가 없거나 키가 비어있습니다.");
            Destroy(instance);
            return;
        }

        if (!pools.TryGetValue(pooled.PoolKey, out Pool pool))
        {
            Debug.LogWarning($"PoolManager: {pooled.PoolKey} 풀에 등록되지 않았습니다.");
            Destroy(instance);
            return;
        }

        // 오브젝트 반환 
        PrepareForPool(instance, pool);
        pool.cache.Enqueue(instance);
    }

    private GameObject CreateInstance(Pool pool)
    {
        GameObject instance = Instantiate(pool.prefab, pool.root);
        if (!instance.TryGetComponent<PooledObject>(out var pooled)) pooled = instance.AddComponent<PooledObject>(); // 없으면 자동으로 붙여줌 
        pooled.Initialise(pool.key);
        instance.SetActive(false);
        return instance;
    }

    /// <summary> 오브젝트를 풀에 반환하기 추후 재사용을 위한 초기화 </summary>
    private void PrepareForPool(GameObject instance, Pool pool)
    {
        instance.transform.SetParent(pool.root, false);
        instance.SetActive(false);
    }
}

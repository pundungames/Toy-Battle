using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

public class PoolingSystem : Singleton<PoolingSystem>
{
    public List<SourceObjects> SourceObjects = new List<SourceObjects>();

    public int DefaultCount = 10;

    [HideInInspector] public Vector3 initScale;
    private void Awake()
    {
       // DontDestroyOnLoad(gameObject);
       
    }

    private void ResetAllObjects()
    {
        foreach (var item in SourceObjects)
        {
            foreach (var clone in item.clones)
            {
                if (clone != null)
                    if (clone.activeSelf) DestroyAPS(clone);
            }
        }
    }

    private void Start()
    {
        InitilizePool();
    }

    public void InitilizePool()
    {
        InitilizeGameObjects();
    }

    private void InitilizeGameObjects()
    {
        for (int i = 0; i < SourceObjects.Count; i++)
        {
            if (SourceObjects[i].ID == "BossArrow" && PlayerPrefs.GetInt("_level") != 16) continue;
            int copyNumber = DefaultCount;
            if (SourceObjects[i].MinNumberOfObject != 0)
                copyNumber = SourceObjects[i].MinNumberOfObject;

            for (int j = 0; j < copyNumber; j++)
            {
                GameObject go = Instantiate(SourceObjects[i].SourcePrefab, transform);
                go.SetActive(false);
                if (SourceObjects[i].AutoDestroy)
                    go.AddComponent<PoolObject>();

                SourceObjects[i].clones.Add(go);
            }
        }
    }

    public GameObject InstantiateAPS(string Id)
    {
        for (int i = 0; i < SourceObjects.Count; i++)
        {
            if (string.Equals(SourceObjects[i].ID, Id))
            {
                for (int a = SourceObjects[i].clones.Count - 1; a >= 0; a--)
                {
                    if (!SourceObjects[i].clones[a])
                    {
                        SourceObjects[i].clones.RemoveAt(a);
                        continue;
                    }
                    if (!SourceObjects[i].clones[a].activeInHierarchy)
                    {
                        if (SourceObjects[i].clones[a].TryGetComponent<NavMeshAgent>(out NavMeshAgent agent))
                            agent.enabled = false;
                        SourceObjects[i].clones[a].SetActive(true);

                        IPoolable poolable = SourceObjects[i].clones[a].GetComponent<IPoolable>();
                        if (poolable != null)
                            poolable.Initilize();

                        return SourceObjects[i].clones[a];
                    }
                }

                if (SourceObjects[i].AllowGrow)
                {
                    GameObject go = Instantiate(SourceObjects[i].SourcePrefab, transform);
                    SourceObjects[i].clones.Add(go);
                    IPoolable poolable = go.GetComponent<IPoolable>();
                    if (poolable != null)
                        poolable.Initilize();

                    if (SourceObjects[i].AutoDestroy)
                        go.AddComponent<PoolObject>();
                    return go;
                }
            }
        }
        return null;
    }

    public GameObject InstantiateAPS(string iD, Vector3 position)
    {
        GameObject go = InstantiateAPS(iD);
        if (go)
        {
            go.transform.position = position;
            return go;
        }
        else
            return null;
    }

    public GameObject InstantiateAPS(string iD, Vector3 position, Quaternion rotation)
    {
        GameObject go = InstantiateAPS(iD);
        if (go)
        {
            go.transform.position = position;
            go.transform.rotation = rotation;
            return go;
        }
        else
            return null;
    }

    public GameObject InstantiateAPS(GameObject sourcePrefab)
    {
        for (int i = 0; i < SourceObjects.Count; i++)
        {
            if (ReferenceEquals(SourceObjects[i].SourcePrefab, sourcePrefab))
            {
                for (int j = 0; j < SourceObjects[i].clones.Count; j++)
                {
                    if (!SourceObjects[i].clones[j].activeInHierarchy)
                    {
                        SourceObjects[i].clones[j].SetActive(true);
                        return SourceObjects[i].clones[j];
                    }
                }
                if (SourceObjects[i].AllowGrow)
                {
                    GameObject go = Instantiate(SourceObjects[i].SourcePrefab, transform);
                    SourceObjects[i].clones.Add(go);
                    return go;
                }
            }
        }
        return null;
    }

    public GameObject InstantiateAPS(GameObject sourcePrefab, Vector3 position)
    {
        GameObject go = InstantiateAPS(sourcePrefab);
        if (go)
        {
            go.transform.position = position;
            return go;
        }
        else
            return null;
    }

    public void DestroyAPS(GameObject clone)
    {
        clone.transform.position = transform.position;
        clone.transform.rotation = transform.rotation;
        if (clone.TryGetComponent<PoolObject>(out var poolObject))
        {
            clone.transform.localScale = poolObject.initScale;
        }
        clone.transform.SetParent(transform);

        IPoolable poolable = clone.GetComponent<IPoolable>();
        if (poolable != null)
            poolable.Dispose();
        clone.SetActive(false);
    }

    public void DestroyAPS(GameObject clone, float waitTime)
    {
        StartCoroutine(DestroyAPSCo(clone, waitTime));
    }

    IEnumerator DestroyAPSCo(GameObject clone, float waitTime)
    {
        yield return new WaitForSeconds(waitTime);
        DestroyAPS(clone);
    }
}

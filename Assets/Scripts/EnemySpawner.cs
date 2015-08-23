using UnityEngine;
using System.Collections;


public class EnemySpawner : MonoBehaviour 
{
    public int m_maxSpawns = 50;
    private int m_spawnCount;

    public bool Depleted
    {
        get { return m_spawnCount == m_maxSpawns; }
    }

    public Enemy m_enemyPrefab;
    public int m_maxEnemies = 10;
    private const float MIN_SPAWN_TIME = 1.5f;
    private const float MAX_SPAWN_TIME = 4.0f;
    public Transform m_enemyRoot;

    public int m_defaultHP = 3;
    public float m_defaultSpeed = 1.0f;

    private Enemy[] m_enemyPool;

    private float m_lastSpawn;
    private float m_nextSpawn;
    
	// Use this for initialization
	void Start () 
    {
        m_spawnCount = 0;
        m_lastSpawn = m_nextSpawn = Time.time;
        InitializePool();
	}
	
	// Update is called once per frame
	void Update () 
    {
        if (GameplayManager.Instance.paused)
        {
            return;
        }
        if (m_spawnCount == m_maxSpawns)
        {
            return;
        }
        if (Time.time - m_lastSpawn >= m_nextSpawn)
        {
            Spawn();

            m_nextSpawn = Random.Range(MIN_SPAWN_TIME, MAX_SPAWN_TIME);
            m_lastSpawn = Time.time;
        }
	
	}

    void InitializePool()
    {
        m_enemyPool = new Enemy[m_maxEnemies];
        for (int i = 0; i < m_maxEnemies; ++i)
        {
            m_enemyPool[i] = Instantiate<Enemy>(m_enemyPrefab);
            m_enemyPool[i].transform.SetParent(m_enemyRoot);
            m_enemyPool[i].transform.position = transform.position;

            m_enemyPool[i].gameObject.SetActive(false);
        }
    }
    void Spawn()
    {
        Enemy candidate = null;
        for (int i = 0; i < m_enemyPool.Length; ++i)
        {
            if (!m_enemyPool[i].gameObject.activeSelf)
            {
                candidate = m_enemyPool[i];
                m_spawnCount++;
                break;
            }
        }

        if (candidate != null)
        {
            candidate.name = string.Format("Enemy{0}", m_spawnCount - 1);
            candidate.gameObject.SetActive(true);
            candidate.Initialize(m_defaultHP, m_defaultSpeed);
            candidate.SetPersonality(EnemyPersonality.Cautious);
            candidate.transform.position = transform.position;

            GameplayManager.Instance.AddEnemy(candidate);            
        }
    }

    public void Recycle(Enemy instance)
    {
        instance.gameObject.SetActive(false);
    }
}

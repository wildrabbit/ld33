using UnityEngine;
using System.Collections.Generic;

public enum GameOverType
{
    None,
    PlayerDeath,
    Genocidal,
    Extermination,
    Resistance,
    Research
}

public class GameplayManager : MonoBehaviour 
{
    public const int CRITICAL_MASS = 30;
    public bool paused = false;

    public int m_killedCreatureCount;
    public int m_killedNPCCount;

    public int m_spawnedNPCCount;
    public int m_spawnedCreatures;

    public float m_victoryTime = 120.0f;
    private float m_elapsed;

    public static GameplayManager Instance;

    private PlayerControl m_player;
    public PlayerControl Player { get { return m_player; } }
    private List<Enemy> m_activeEnemies;
    private List<NPC> m_activeNPCs;

    private List<Entity> m_allEntities;

    private List<EnemySpawner> m_spawners;

    private GameOverType m_gameOver;

    private Rect m_boundaries;
    public Rect Boundaries
    {
        get { return m_boundaries; }
    }

    void Awake ()
    {
        Instance = this;
        m_allEntities = new List<Entity>();
        m_activeEnemies = new List<Enemy>();
        m_activeNPCs = new List<NPC>();
        m_player = null;
        m_spawners = new List<EnemySpawner>(FindObjectsOfType<EnemySpawner>());

        m_gameOver = GameOverType.None;

        m_killedCreatureCount = m_killedNPCCount = 0;
        m_spawnedCreatures = m_spawnedNPCCount = 0;
        m_elapsed = 0.0f;

        m_boundaries = new Rect();
        Camera cam = Camera.main;
        float camHHeight = cam.orthographicSize;
        float camHWidth = camHHeight * cam.aspect;
        cam.transform.position.Set(0,0,cam.transform.position.z);
        m_boundaries.x = -camHWidth;
        m_boundaries.width = camHWidth * 2;
        m_boundaries.y = 2 * camHHeight;
        m_boundaries.height = camHHeight * 4; // At the moment there are two areas on top of each other
    }

    void Update()
    {
        if (m_gameOver == GameOverType.None)
        {
            if (paused) { return; }

            m_elapsed += Time.deltaTime;
            if (m_elapsed >= m_victoryTime)
            {
                int totalSpawned = m_spawnedNPCCount + m_spawnedCreatures;
                int totalKilled = m_killedCreatureCount + m_killedNPCCount;
                if ((float)totalKilled / (float)totalSpawned > 0.3f)
                {
                    SetGameOverCondition(GameOverType.Genocidal);
                }
                else
                {
                    SetGameOverCondition(GameOverType.Resistance);
                }
            }

            int numDepleted = 0;
            for (int i = 0; i < m_spawners.Count; ++i)
            {
                if (m_spawners[i].Depleted)
                {
                    numDepleted++;
                }
            }

            if (numDepleted == m_spawners.Count)
            {
                SetGameOverCondition(GameOverType.Extermination);
            }
        }
    }

    public void OnPlayerDied()
    {
        SetGameOverCondition(GameOverType.PlayerDeath);
    }

    private void SetGameOverCondition (GameOverType condition)
    {
        paused = true;
        m_gameOver = condition;
        Debug.LogFormat("GAME OVER!! {0}", m_gameOver);

        for (int i = 0; i < m_allEntities.Count; ++i)
        {
            m_allEntities[i].OnGameOver(m_gameOver);
        }
    }

    public void OnPlayerAction()
    {
        for (int i = 0; i < m_activeNPCs.Count; ++i)
        {
            if (m_activeNPCs[i].CanTalk && Vector2.Distance(m_player.transform.position, m_activeNPCs[i].transform.position) < m_activeNPCs[i].TalkDistance)
            {
                m_activeNPCs[i].OnPlayerAction();
            }
        }
    }

    public void AddPlayer (PlayerControl p)
    {
        m_player = p;
        m_allEntities.Add(p);
    }

    public void RemovePlayer(PlayerControl p)
    {
        m_player = null;
        m_allEntities.Remove(p);
    }

    public void AddEnemy(Enemy e)
    {
        m_spawnedCreatures++;
        m_activeEnemies.Add(e);
        m_allEntities.Add(e);
    }

    public void RemoveEnemy(Enemy e)
    {
        m_killedCreatureCount++;
        m_activeEnemies.Remove(e);
        m_allEntities.Remove(e);
    }

    public void AddNPC(NPC n)
    {
        m_spawnedNPCCount++;
        m_activeNPCs.Add(n);
        m_allEntities.Add(n);
    }

    public void RemoveNPC(NPC n)
    {
        m_killedNPCCount++;
        m_activeNPCs.Remove(n);
        m_allEntities.Remove(n);
    }

    public void OnEnemyWasHit (Enemy e)
    {
        for (int i = 0; i < m_activeEnemies.Count; ++i)
        {
            if (m_activeEnemies[i] != e && m_activeEnemies[i].CanSee(e))
            {
                m_activeEnemies[i].OnSawEnemyHit(e);
            }
        }
    }

    public void OnEnemyDied(Enemy e)
    {
        for (int i = 0; i < m_activeEnemies.Count; ++i)
        {
            if (m_activeEnemies[i] != e && m_activeEnemies[i].CanSee(e))
            {
                m_activeEnemies[i].OnSawEnemyDie(e);
            }
        }
    }

    public List<Entity> GetTargetsOnSight(Entity e, System.Type[] exclusionList = null)
    {
        List<Entity> targets = new List<Entity>();
        for (int i = 0; i < m_allEntities.Count; ++i )
        {
            if (m_allEntities[i] != e &&  e.CanSee(m_allEntities[i]) && (exclusionList == null || System.Array.IndexOf(exclusionList, m_allEntities[i].GetType()) == -1))
            {
                targets.Add(m_allEntities[i]);
            }
        }
        targets.Sort((x,y) => 
            {
                if (x is PlayerControl && !(y is PlayerControl)) return -1;
                if (y is PlayerControl && !(x is PlayerControl)) return 1;
                return Vector2.Distance(x.transform.position, e.transform.position).CompareTo(Vector2.Distance(y.transform.position, e.transform.position));
            }
            );
        return targets;
    }
}

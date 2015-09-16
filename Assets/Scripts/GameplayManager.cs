using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public enum GameOverType
{
    None,
    PlayerDeath,
    Genocidal,
    Extermination,
    Resistance,
    Psychopath
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
    public List<Entity> AllEntities { get { return m_allEntities; } }

    private List<EnemySpawner> m_spawners;

    private GameOver m_gameOverScreen;
    private GameOverType m_gameOver;

    private Rect m_boundaries;
    public Rect Boundaries
    {
        get { return m_boundaries; }
    }

    private Text m_time;
    private Text m_creeps;
    private Text m_npcs;
    

    void Awake ()
    {
        Instance = this;
        m_allEntities = new List<Entity>();
        m_activeEnemies = new List<Enemy>();
        m_activeNPCs = new List<NPC>();
        m_player = null;
        m_spawners = new List<EnemySpawner>(FindObjectsOfType<EnemySpawner>());

        m_gameOver = GameOverType.None;
        m_gameOverScreen = FindObjectOfType<GameOver>();
        if (m_gameOverScreen != null)
        {
            m_gameOverScreen.gameObject.SetActive(false);
        }

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

    void Start ()
    {

        Canvas c = GetComponentInChildren<Canvas>();
        if (c != null)
        {
            m_time = c.transform.FindChild("Time").GetComponent<Text>();
            m_creeps = c.transform.FindChild("Creeps").GetComponent<Text>();
            m_npcs = c.transform.FindChild("NPCs").GetComponent<Text>();
        }
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
                else if (m_killedNPCCount == m_spawnedNPCCount)
                {
                    SetGameOverCondition(GameOverType.Psychopath);
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

            if (numDepleted == m_spawners.Count && m_spawnedCreatures == m_killedCreatureCount)
            {
                SetGameOverCondition(GameOverType.Extermination);
            }

            m_time.text = string.Format("Time: {0:0.##}s", (m_victoryTime - m_elapsed));
            m_creeps.text = string.Format("Creeps: {0}", m_killedCreatureCount);            
            m_npcs.text = string.Format("NPCs: {0}", m_killedNPCCount);
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

        StartCoroutine(GameOverSequence());
    }

    IEnumerator GameOverSequence()
    {
        switch (m_gameOver)
        {
            case GameOverType.Extermination:
            case GameOverType.PlayerDeath:
                {
                    yield return new WaitForSeconds(0.5f);
                }
                break;
            case GameOverType.Resistance:
            case GameOverType.Genocidal:
            case GameOverType.Psychopath:
                {
                    for (int i = 0; i < m_activeEnemies.Count; ++i)
                    {
                        m_activeEnemies[i].StartFlyAway(3.0f);
                    }
                    yield return new WaitForSeconds(3.0f);
                }
                break;
            default: break;
        }

        if (m_gameOverScreen != null)
        {
            m_gameOverScreen.gameObject.SetActive(true);
            m_gameOverScreen.SetGameOverType(m_gameOver);
        }
        Destroy(gameObject);
        yield return null;
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

        CameraFollower cf = Camera.main.GetComponent<CameraFollower>();
        if (cf != null)
        {
            cf.m_target = m_player.transform;
        }
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

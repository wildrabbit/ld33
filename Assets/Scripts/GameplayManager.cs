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
    private List<Enemy> m_activeEnemies;
    private List<NPC> m_activeNPCs;

    private List<EnemySpawner> m_spawners;

    private GameOverType m_gameOver;

    void Awake ()
    {
        Instance = this;
        m_activeEnemies = new List<Enemy>();
        m_activeNPCs = new List<NPC>();
        m_player = FindObjectOfType<PlayerControl>();
        m_spawners = new List<EnemySpawner>(FindObjectsOfType<EnemySpawner>());

        m_gameOver = GameOverType.None;

        m_killedCreatureCount = m_killedNPCCount = 0;
        m_spawnedCreatures = m_spawnedNPCCount = 0;
        m_elapsed = 0.0f;
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
    
    public void AddEnemy(Enemy e)
    {
        m_spawnedCreatures++;
        m_activeEnemies.Add(e);
    }

    public void RemoveEnemy(Enemy e)
    {
        m_killedCreatureCount++;
        m_activeEnemies.Remove(e);
    }

    public void AddNPC(NPC n)
    {
        m_spawnedNPCCount++;
        m_activeNPCs.Add(n);
    }

    public void RemoveNPC(NPC n)
    {
        m_killedNPCCount++;
        m_activeNPCs.Remove(n);
    }

}

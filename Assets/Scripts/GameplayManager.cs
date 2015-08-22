using UnityEngine;
using System.Collections.Generic;

public class GameplayManager : MonoBehaviour 
{
    public const int CRITICAL_MASS = 15;
    public bool paused = false;

    public int killedCreatureCount = 0;
    public int killedNPCCount = 0;

    public static GameplayManager Instance;

    private PlayerControl m_player;
    private List<Enemy> m_activeEnemies;
    private List<NPC> m_activeNPCs;
    void Awake ()
    {
        Instance = this;
        m_activeEnemies = new List<Enemy>();
        m_activeNPCs = new List<NPC>();
        m_player = FindObjectOfType<PlayerControl>();
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
        m_activeEnemies.Add(e);
    }

    public void RemoveEnemy(Enemy e)
    {
        m_activeEnemies.Remove(e);
    }

    public void AddNPC(NPC n)
    {
        m_activeNPCs.Add(n);
    }

    public void RemoveNPC(NPC n)
    {
        m_activeNPCs.Remove(n);
    }

}

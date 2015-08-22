using UnityEngine;
using System.Collections;

public class CharacterLife
{
    private int m_maxHP;
    public int MaxHP
    {
        get { return m_maxHP; }
    }

    private int m_hp;
    public int HP
    {
        get { return m_hp; }
    }

    public float HPRatio
    {
        get { return m_maxHP == 0 ? 0.0f : (float)m_hp/(float)m_maxHP;}
    }

    public CharacterLife()
    {
        m_maxHP = m_hp = 0;
    }
       
    public void Initialise(int maxHP)
    {
        m_maxHP = maxHP;
        m_hp = m_maxHP;
    }

    public void SetHP (int amount)
    {
        m_hp = amount;
    }

    public bool UpdateHP(int amount)
    {
        m_hp += amount;
        if (m_hp >= m_maxHP)
        {
            m_hp = m_maxHP;
        }
        else if (m_hp < 0)
        {
            m_hp = 0;
        }
        return m_hp == 0;
    }

}

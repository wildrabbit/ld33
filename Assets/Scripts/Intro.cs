using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class Intro : MonoBehaviour {

    public GameObject m_gamePrefab;
    public float m_lineDelay = 1.2f;
    public float m_endDelay = 0.3f;
    public float m_minTime = 0.3f;

    private float m_startTime;

    public GameObject m_textGroupRef;
    public GameObject m_skip;
    private Text[] m_texts;

    void Awake ()
    {
        m_texts = m_textGroupRef.GetComponentsInChildren<Text>();
        System.Array.Sort(m_texts, (x, y) => System.Int32.Parse(x.name).CompareTo(System.Int32.Parse(y.name)));

        for (int i = 0; i < m_texts.Length; ++i)
        {
            m_texts[i].gameObject.SetActive(false);
        }
        gameObject.SetActive(false);        
    }

	// Use this for initialization
	void Start () 
    {
        m_startTime = Time.time;
        m_skip.SetActive(false);
        StartCoroutine("IntroTexts");
	}
	
	// Update is called once per frame
	void Update () 
    {
        if (m_startTime >= 0)
        {
            if (Time.time - m_startTime >= m_minTime)
            {
                m_skip.SetActive(true);
                m_startTime = -1.0f;
            }
            return; 
        }
            
	    if (Input.anyKey || Input.GetMouseButton(0))
        {
            GameObject go = Instantiate(m_gamePrefab);
            go.name = m_gamePrefab.name;
            Destroy(gameObject);
        }
	}

    IEnumerator IntroTexts()
    {
        for (int i = 0; i < m_texts.Length; ++i)
        {
            m_texts[i].gameObject.SetActive(true);
            yield return new WaitForSeconds(m_lineDelay);
        }
        yield return new WaitForSeconds(m_endDelay);
    }
}

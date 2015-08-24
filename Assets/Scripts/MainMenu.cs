using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class MainMenu : MonoBehaviour {

    public GameObject m_intro;

    public Text m_keyText;
    private Vector3 m_scale;
	// Use this for initialization
	void Start () 
    {
        m_scale = m_keyText.transform.localScale;
	}
	
	// Update is called once per frame
	void Update () 
    {
        m_keyText.rectTransform.localScale = (0.9f + Mathf.PingPong(0.5f * Time.time, 0.2f)) * m_scale;
        
        if (Input.anyKey || Input.GetMouseButton(0))
        {
            m_intro.SetActive(true);
            Destroy(gameObject);
        }
	}
}

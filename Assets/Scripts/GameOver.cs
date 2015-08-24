using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class GameOver : MonoBehaviour 
{
    private float timer;
    public float timerMin = 1.25f;

    public Text m_skip;

    public Text m_Death;
    public Text m_Resistance;
    public Text m_Extermination;

    public Text m_GenocidalSub;
    public Text m_PsychopathSub;
    public Text m_PacifistSub;

    public Canvas m_canvas;

    public bool m_init = false;

	// Use this for initialization
	void OnEnable () 
    {
        m_canvas.gameObject.SetActive(false);
        m_skip.gameObject.SetActive(false);

        m_Death.gameObject.SetActive(false);
        m_Resistance.gameObject.SetActive(false);
        m_Extermination.gameObject.SetActive(false);
        m_GenocidalSub.gameObject.SetActive(false);
        m_PacifistSub.gameObject.SetActive(false);
        m_PsychopathSub.gameObject.SetActive(false);
    }
	
	// Update is called once per frame
	void Update () 
    {
        if (!m_init) return;

	    if (timer > 0)
        {
            if (Time.time - timer >= timerMin)
            {
                m_skip.gameObject.SetActive(true);
                timer = -1.0f;
            }
            return;
        }
        if (Input.anyKey || Input.GetMouseButton(0))
        {
            Application.LoadLevel(0);
        }

	}

    public void SetGameOverType(GameOverType gameOver)
    {
        timer = Time.time;
        m_init = true;
        m_canvas.gameObject.SetActive(true);

        switch (gameOver)
        {
            case GameOverType.None:
                break;
            case GameOverType.PlayerDeath:
                m_Death.gameObject.SetActive(true);
                break;
            case GameOverType.Genocidal:
                m_Resistance.gameObject.SetActive(true);
                m_GenocidalSub.gameObject.SetActive(true);
                break;
            case GameOverType.Extermination:
                m_Extermination.gameObject.SetActive(true);
                break;
            case GameOverType.Resistance:
                m_Resistance.gameObject.SetActive(true);
                m_PacifistSub.gameObject.SetActive(true);
                break;
            case GameOverType.Psychopath:
                m_Resistance.gameObject.SetActive(true);
                m_PsychopathSub.gameObject.SetActive(true);
                break;
            default:
                break;
        }
    }
	
}

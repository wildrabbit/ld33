using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class DebugText : MonoBehaviour
{
    private Text m_text;
    private Entity m_entity;
    private Vector2 m_offset;

    // Use this for initialization
    void Awake()
    {
        m_text = GetComponent<Text>();
    }

    public void Initialize (Entity entity, Transform parent)
    {
        m_entity = entity;
        m_offset = transform.localPosition;
        transform.SetParent(parent);
    }
    // Update is called once per frame
    void Update()
    {
        m_text.text = m_entity.GetDebugLabel();
        transform.position = (Vector2)m_entity.transform.position + m_offset;
    }
}

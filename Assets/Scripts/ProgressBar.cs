using UnityEngine;
using System.Collections;

public class ProgressBar : MonoBehaviour 
{
    static Color s_nullColour
    {
        get
        {
            return new Color(0.0f, 0.0f, 0.0f, 0.0f);
        }
    }
    public Sprite baseBarSection = null;
    public Color baseBarColour = s_nullColour;
    
    public Sprite previewBarSection = null;
    public Color previewBarColour = s_nullColour;
    public Color previewBarAltColour = s_nullColour;

    public Sprite mainBarSection = null;
    public Color mainBarColour = s_nullColour;

    public float width = 128.0f;    //Assuming 1:1 pixel to unit ratio. If it's not the case, re-scale accordingly
    public float height = 16;

    private SpriteRenderer m_baseRenderer;
    private SpriteRenderer m_previewRenderer;
    private SpriteRenderer m_mainRenderer;

    private string m_sortingLayerName;
    private int m_sortingOrder;


    public void Build (float initialRatio, float initialPreviewRatio, string sortingLayerName, int baseSortingOrder = 0)
    {
        m_sortingLayerName = sortingLayerName;
        m_sortingOrder = baseSortingOrder;

        if (baseBarSection != null)
        {
            m_baseRenderer = BuildSection("base", baseBarSection, baseBarColour, width, height, m_sortingLayerName, m_sortingOrder - 2);
        }
        
        if (previewBarSection != null)
        {
            m_previewRenderer = BuildSection("preview", previewBarSection, previewBarColour, width * initialPreviewRatio, height, m_sortingLayerName, m_sortingOrder - 1);
        }

        m_mainRenderer = BuildSection("main", mainBarSection, mainBarColour, width * initialRatio, height, m_sortingLayerName, m_sortingOrder);
    }

    SpriteRenderer BuildSection (string name, Sprite spriteRef, Color tint, float width, float height, string sortingLayerName, int sortingLayerOrder)
    {
        GameObject obj = new GameObject();
        SpriteRenderer rendererRef = obj.AddComponent<SpriteRenderer>();
        rendererRef.sprite = spriteRef;
        
        if (tint != s_nullColour)
        {
            rendererRef.color = tint;
        }
        rendererRef.sortingLayerName = sortingLayerName;
        rendererRef.sortingOrder = sortingLayerOrder;

        obj.name = name;

        obj.transform.parent = transform;
        obj.transform.localPosition = Vector2.zero;
        obj.transform.localScale = new Vector3(width, height, 1.0f);
        
        return rendererRef;        
    }

    public void SetValue (float ratio)
    {
        Vector3 localScale = Vector3.zero;
        if (m_previewRenderer != null)
        {
            localScale = m_previewRenderer.transform.localScale;
            localScale.x = 0.0f;
            m_previewRenderer.transform.localScale = localScale;
        }

        localScale = m_mainRenderer.transform.localScale;
        localScale.x = ratio * width;
        m_mainRenderer.transform.localScale = localScale;
    }

    public void SetPreviewValue (float previewRatio, float mainRatio, bool useAltColour = false)
    {
        Vector3 localScale = m_previewRenderer.transform.localScale;
        localScale.x = previewRatio * width;
        m_previewRenderer.transform.localScale = localScale;
        m_previewRenderer.color = (useAltColour) ? previewBarAltColour : previewBarColour;
        localScale = m_mainRenderer.transform.localScale;
        localScale.x = mainRatio * width;
        m_mainRenderer.transform.localScale = localScale;
    }

    void Awake ()
    {
        m_baseRenderer = null;
        m_previewRenderer = null;
        m_mainRenderer = null;

        m_sortingLayerName = "";
        m_sortingOrder = 0;
    }
}

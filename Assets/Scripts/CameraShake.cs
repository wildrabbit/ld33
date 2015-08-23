// Adapted from https://gist.github.com/ftvs/5822103
using UnityEngine;
using System.Collections;

public class CameraShake : MonoBehaviour
{
    // Transform of the camera to shake. Grabs the gameObject's transform
    // if null.
    [SerializeField]
    private Transform m_camTransform;

    [SerializeField]
    private float m_shakeDuration = 0.0f;

    [SerializeField]
    private float m_shakeAmplitude = 0.5f;

    [SerializeField]
    private float m_decreaseFactor = 1.0f;

    private float m_shakeTime = 0.0f;

    private Vector2 m_originalPos;
    private float m_z;

    public void StartShake ()
    {
        this.enabled = true;
        m_shakeTime = m_shakeDuration;
    }

	public void StartShakeWithDuration(float aDuration)
	{
        this.enabled = true;
		m_shakeTime = aDuration;
	}

    void Awake ()
    {
        if (m_camTransform == null)
        {
            m_camTransform = GetComponent(typeof(Transform)) as Transform;
            m_z = m_camTransform.localPosition.z;

        }
    }

    void OnEnable ()
    {
        m_originalPos = m_camTransform.localPosition;
    }

    void Update ()
    {
        if (m_shakeTime > 0)
        {
            Vector2 newPos = m_originalPos + (Vector2)Random.insideUnitSphere * m_shakeAmplitude;
            m_camTransform.localPosition = new Vector3(newPos.x, newPos.y, m_z);

            m_shakeTime -= Time.deltaTime * m_decreaseFactor;
        }
        else
        {
            m_shakeTime = 0f;
            m_camTransform.localPosition = new Vector3(m_originalPos.x, m_originalPos.y, m_z);
            this.enabled = false;
        }
    }
}
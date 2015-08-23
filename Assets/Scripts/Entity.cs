using UnityEngine;
using System.Collections;

public class Entity : MonoBehaviour
{
    public float m_recoil = 0.2f;
    public float m_maxSpeed = 1.0f;
    public float m_sightRadius = 6.0f;

    public LayerMask m_visibilityMask;

    public AudioClip m_deathSound;
    public AudioClip m_hitSound;

    protected SpriteRenderer m_renderer;
    protected AudioSource m_audioSource;
    protected Rigidbody2D m_body;

    protected Collider2D m_mainCollider;
    protected Collider2D m_bodyCollider;

    protected CharacterLife m_lifeData;

    // Physics vars
    private Vector2 m_velocityTarget;
    private Vector2 m_orientationTarget;
    private Vector2 m_currentOrientation;

    public float Orientation
    {
        get
        {
            float angle = Mathf.Atan2(m_currentOrientation.y, m_currentOrientation.x);
            return Mathf.Rad2Deg * angle;
        }
    }

    void Awake ()
    {
        m_renderer = GetComponent<SpriteRenderer>();
        m_audioSource = GetComponent<AudioSource>();
        m_body = GetComponent<Rigidbody2D>();

        m_mainCollider = GetComponent<Collider2D>();
        m_bodyCollider = GetComponentInChildren<Collider2D>();

        m_lifeData = new CharacterLife();
    }

    // Use this for initialization
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
}

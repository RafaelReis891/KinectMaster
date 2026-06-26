using UnityEngine;

public class FallingObject : MonoBehaviour
{
    public float speed = 500f;
    public AudioClip clipCollect;
    public bool answer;

    [HideInInspector]
    public FallingObjectsGame manager;

    private RectTransform rect;

    float timer;
    private RectTransform canvasRect;

    void Start()
    {
        rect = GetComponent<RectTransform>();
        canvasRect = GetComponentInParent<Canvas>().GetComponent<RectTransform>();
    }

    void Update()
    {
        rect.anchoredPosition += Vector2.down * speed * Time.deltaTime;

        // Saiu da tela (abaixo)
        if (timer < 20)
        timer =+ Time.deltaTime;
        else
        {
            Destroy(gameObject);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            manager.RegistrarResposta(answer);
            manager.PlaySource(clipCollect);
            Destroy(gameObject);
        }
    }
}
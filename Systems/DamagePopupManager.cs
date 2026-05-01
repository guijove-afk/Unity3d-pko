using UnityEngine;
using System.Collections.Generic;

public class DamagePopupManager : MonoBehaviour
{
    public static DamagePopupManager Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private GameObject damagePopupPrefab;
    [SerializeField] private int poolSize = 50;
    [SerializeField] private float popupDuration = 1.5f;
    [SerializeField] private float popupSpeed = 2f;
    [SerializeField] private float popupSpread = 0.5f;

    private Queue<GameObject> popupPool = new Queue<GameObject>();
    private List<ActivePopup> activePopups = new List<ActivePopup>();
    private Camera mainCamera;

    [System.Serializable]
    private class ActivePopup
    {
        public GameObject gameObject;
        public TextMesh textMesh;
        public float startTime;
        public Vector3 startPosition;
        public Vector3 velocity;
        public Color color;
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        mainCamera = Camera.main;
        InitializePool();
    }

    private void InitializePool()
    {
        if (damagePopupPrefab == null)
        {
            damagePopupPrefab = CreateDefaultPopupPrefab();
        }

        for (int i = 0; i < poolSize; i++)
        {
            GameObject popup = Instantiate(damagePopupPrefab, transform);
            popup.SetActive(false);
            popupPool.Enqueue(popup);
        }
    }

    private GameObject CreateDefaultPopupPrefab()
    {
        GameObject go = new GameObject("DamagePopup");
        TextMesh textMesh = go.AddComponent<TextMesh>();
        textMesh.characterSize = 0.1f;
        textMesh.fontSize = 48;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;

        go.AddComponent<Billboard>();

        return go;
    }

    void Update()
    {
        for (int i = activePopups.Count - 1; i >= 0; i--)
        {
            var popup = activePopups[i];
            float elapsed = Time.time - popup.startTime;

            if (elapsed >= popupDuration)
            {
                ReturnPopup(popup);
                activePopups.RemoveAt(i);
                continue;
            }

            float t = elapsed / popupDuration;
            popup.gameObject.transform.position = popup.startPosition + popup.velocity * elapsed;

            float alpha = 1f - Mathf.Pow(t, 2f);
            popup.textMesh.color = new Color(popup.color.r, popup.color.g, popup.color.b, alpha);

            float scale = 1f + Mathf.Sin(t * Mathf.PI) * 0.5f;
            popup.gameObject.transform.localScale = Vector3.one * scale;
        }
    }

    public void ShowDamage(Vector3 position, int damage, bool isCritical = false)
    {
        GameObject popup = GetPopup();
        if (popup == null) return;

        TextMesh textMesh = popup.GetComponent<TextMesh>();
        textMesh.text = damage.ToString();

        Color color = isCritical ? new Color(1f, 0.3f, 0f) : Color.white;
        if (damage <= 0) color = Color.gray;

        textMesh.color = color;

        Vector3 offset = new Vector3(
            Random.Range(-popupSpread, popupSpread),
            0,
            Random.Range(-popupSpread, popupSpread)
        );

        popup.transform.position = position + offset;
        popup.SetActive(true);

        activePopups.Add(new ActivePopup
        {
            gameObject = popup,
            textMesh = textMesh,
            startTime = Time.time,
            startPosition = position + offset + Vector3.up * 0.5f,
            velocity = new Vector3(Random.Range(-0.5f, 0.5f), popupSpeed, 0),
            color = color
        });
    }

    public void ShowHeal(Vector3 position, int amount)
    {
        GameObject popup = GetPopup();
        if (popup == null) return;

        TextMesh textMesh = popup.GetComponent<TextMesh>();
        textMesh.text = $"+{amount}";
        textMesh.color = Color.green;

        Vector3 offset = new Vector3(
            Random.Range(-popupSpread, popupSpread),
            0,
            Random.Range(-popupSpread, popupSpread)
        );

        popup.transform.position = position + offset;
        popup.SetActive(true);

        activePopups.Add(new ActivePopup
        {
            gameObject = popup,
            textMesh = textMesh,
            startTime = Time.time,
            startPosition = position + offset + Vector3.up * 0.5f,
            velocity = new Vector3(0, popupSpeed * 0.8f, 0),
            color = Color.green
        });
    }

    public void ShowText(Vector3 position, string text, Color color)
    {
        GameObject popup = GetPopup();
        if (popup == null) return;

        TextMesh textMesh = popup.GetComponent<TextMesh>();
        textMesh.text = text;
        textMesh.color = color;

        popup.transform.position = position;
        popup.SetActive(true);

        activePopups.Add(new ActivePopup
        {
            gameObject = popup,
            textMesh = textMesh,
            startTime = Time.time,
            startPosition = position,
            velocity = new Vector3(0, popupSpeed * 0.5f, 0),
            color = color
        });
    }

    private GameObject GetPopup()
    {
        if (popupPool.Count > 0)
            return popupPool.Dequeue();

        if (activePopups.Count > 0)
        {
            var oldest = activePopups[0];
            activePopups.RemoveAt(0);
            return oldest.gameObject;
        }

        return null;
    }

    private void ReturnPopup(ActivePopup popup)
    {
        popup.gameObject.SetActive(false);
        popupPool.Enqueue(popup.gameObject);
    }
}

public class Billboard : MonoBehaviour
{
    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;
    }

    void LateUpdate()
    {
        if (mainCamera != null)
        {
            transform.LookAt(transform.position + mainCamera.transform.rotation * Vector3.forward,
                mainCamera.transform.rotation * Vector3.up);
        }
    }
}
using UnityEngine;
using System;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game Settings")]
    [SerializeField] private float dayLength = 20f;
    [SerializeField] private float timeScale = 1f;

    private float gameTime;
    private int gameDay = 1;
    private bool isDaytime = true;

    public float GameTime => gameTime;
    public int GameDay => gameDay;
    public bool IsDaytime => isDaytime;

    public event Action<float> OnTimeChanged;
    public event Action<bool> OnDayNightChanged;
    public event Action<int> OnNewDay;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Update()
    {
        gameTime += Time.deltaTime * timeScale / (dayLength * 60f);

        if (gameTime >= 1f)
        {
            gameTime = 0f;
            gameDay++;
            OnNewDay?.Invoke(gameDay);
        }

        bool wasDaytime = isDaytime;
        isDaytime = gameTime >= 0.25f && gameTime < 0.75f;

        if (wasDaytime != isDaytime)
        {
            OnDayNightChanged?.Invoke(isDaytime);
        }

        OnTimeChanged?.Invoke(gameTime);
    }

    public void SetTime(float time)
    {
        gameTime = Mathf.Clamp01(time);
    }

    public void SetDay(int day)
    {
        gameDay = Mathf.Max(1, day);
    }
}
using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class Quota : NetworkBehaviour
{
    [Header("Quota Settings")]
    [SerializeField] private float quotaAmount = 300f;
    [SerializeField] private float currentAmount = 0f;
    public Scoring scoring;
    public StartRun startRun;
    public static int daysLeft = 1;

    private static float staticQuotaAmount = 300f;
    private static int staticDaysLeft = 1;
    private static bool isReturningFromRun = false;
    private static bool hasInitializedStatics = false;

    private bool isGameOver = false;

    private void Awake()
    {
        if (!hasInitializedStatics)
        {
            staticQuotaAmount = quotaAmount;
            staticDaysLeft = daysLeft;
            hasInitializedStatics = true;
        }
        else
        {
            quotaAmount = staticQuotaAmount;
            daysLeft = staticDaysLeft;
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        if (startRun == null)
        {
            startRun = FindFirstObjectByType<StartRun>();
        }

        if (scoring == null)
        {
            scoring = FindFirstObjectByType<Scoring>();
        }

        // When returning to the hub after completing a run, decrease the days left
        if (isReturningFromRun)
        {
            daysLeft--;
            staticDaysLeft = daysLeft;
            isReturningFromRun = false;
            Debug.Log($"[Quota] Returned to hub after run. Days left: {daysLeft}");
        }

        UpdateStartRunState();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "MainMenu")
        {
            ResetSession();
            return;
        }

        if (scene.name == "MainMap")
        {
            MarkRunStarted();
        }
    }

    public static void MarkRunStarted()
    {
        isReturningFromRun = true;
    }

    public static void ResetSession()
    {
        staticQuotaAmount = 300f;
        staticDaysLeft = 1;
        daysLeft = 1;
        isReturningFromRun = false;
        hasInitializedStatics = false;
    }

    private void Update()
    {
        if (scoring != null)
        {
            currentAmount = scoring.TotalScore;
        }

        UpdateStartRunState();
    }

    private void UpdateStartRunState()
    {
        if (isGameOver)
        {
            if (startRun != null && startRun.enabled)
            {
                startRun.enabled = false;
            }
            return;
        }

        bool quotaMet = currentAmount >= quotaAmount;

        if (quotaMet)
        {
            quotaAmount = Mathf.Round(quotaAmount * 1.5f);
            staticQuotaAmount = quotaAmount;
            daysLeft++;
            staticDaysLeft = daysLeft;
            if (startRun != null)
            {
                startRun.enabled = true;
            }
        }
        else if (daysLeft <= 0)
        {
            if (startRun != null)
            {
                startRun.enabled = false;
            }
            GameOver();
            Debug.Log("Quota not met! Current quota: " + quotaAmount);
        }
    }

    public void GameOver()
    {
        isGameOver = true;

        // Disable next run script (StartRun) so the player cannot start a run and is forced to kill themselves
        if (startRun != null)
        {
            startRun.enabled = false;
        }
        else
        {
            StartRun[] runScripts = FindObjectsByType<StartRun>(FindObjectsSortMode.None);
            foreach (StartRun run in runScripts)
            {
                if (run != null)
                {
                    run.enabled = false;
                }
            }
        }

    }
}

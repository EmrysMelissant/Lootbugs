using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;

public class Quota : NetworkBehaviour
{
    [Header("Quota Settings")]
    [SerializeField] private float quotaAmount = 300f;
    [SerializeField] private float currentAmount = 0f;

    [Header("UI References")]
    [Tooltip("TMP_Text component displaying the quota target on the player UI.")]
    public TMP_Text QoutaText;

    [Tooltip("TMP_Text component displaying the current collected amount on the player UI.")]
    public TMP_Text AmountText;

    [Header("Dependencies")]
    public Scoring scoring;
    public StartRun startRun;
    public static int daysLeft = 1;

    private static float staticQuotaAmount = 300f;
    private static int staticDaysLeft = 1;
    private static bool isReturningFromRun = false;
    private static bool hasInitializedStatics = false;

    public static Quota Instance { get; private set; }

    private bool isGameOver = false;

    private void Awake()
    {
        Instance = this;

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

        AssignPlayerUITexts();

        // When returning to the hub after completing a run, decrease the days left
        if (isReturningFromRun)
        {
            daysLeft--;
            staticDaysLeft = daysLeft;
            isReturningFromRun = false;
            Debug.Log($"[Quota] Returned to hub after run. Days left: {daysLeft}");
        }

        UpdateUITexts();
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

        AssignPlayerUITexts();
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
        if (QoutaText == null || AmountText == null)
        {
            AssignPlayerUITexts();
        }

        if (scoring != null)
        {
            currentAmount = scoring.TotalScore;
        }

        UpdateUITexts();
        UpdateStartRunState();
    }

    public void AssignPlayerUITexts()
    {
        if (QoutaText != null && AmountText != null) return;

        // 1. Search player instances (prefer local owner)
        PlayerController[] players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        for (int i = 0; i < players.Length; i++)
        {
            PlayerController player = players[i];
            if (player == null) continue;

            if (!player.IsOwner && players.Length > 1) continue;

            TMP_Text[] playerTexts = player.GetComponentsInChildren<TMP_Text>(true);
            FindAndAssignFromList(playerTexts);

            if (QoutaText != null && AmountText != null)
            {
                UpdateUITexts();
                return;
            }
        }

        // 2. Fallback: Search all TMP_Text in the scene
        TMP_Text[] allTexts = FindObjectsByType<TMP_Text>(FindObjectsSortMode.None);
        FindAndAssignFromList(allTexts);

        UpdateUITexts();
    }

    private void FindAndAssignFromList(TMP_Text[] texts)
    {
        if (texts == null) return;

        for (int i = 0; i < texts.Length; i++)
        {
            TMP_Text t = texts[i];
            if (t == null) continue;

            Transform parent = t.transform.parent;
            string parentName = parent != null ? parent.name.ToLower() : "";
            string objName = t.gameObject.name.ToLower();

            if (QoutaText == null)
            {
                if (parentName == "quota" || parentName == "qouta" ||
                    parentName.Contains("quota") || parentName.Contains("qouta") ||
                    objName.Contains("quota") || objName.Contains("qouta"))
                {
                    QoutaText = t;
                }
            }

            if (AmountText == null)
            {
                if (parentName == "currentamount" || parentName == "amount" ||
                    parentName.Contains("currentamount") || parentName.Contains("amount") ||
                    objName.Contains("currentamount") || objName.Contains("amount"))
                {
                    AmountText = t;
                }
            }
        }
    }

    public void AssignPlayerUI(TMP_Text quota, TMP_Text amount)
    {
        if (quota != null) QoutaText = quota;
        if (amount != null) AmountText = amount;
        UpdateUITexts();
    }

    public static void RegisterPlayerUI(TMP_Text quota, TMP_Text amount)
    {
        if (Instance != null)
        {
            Instance.AssignPlayerUI(quota, amount);
        }
    }

    public void UpdateUITexts()
    {
        if (QoutaText != null)
        {
            QoutaText.text = $"{quotaAmount}";
        }

        if (AmountText != null)
        {
            AmountText.text = $"{currentAmount}";
        }
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
            UpdateUITexts();
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


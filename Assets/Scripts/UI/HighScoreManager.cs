using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class HighScoreEntry
{
    public string playerName;
    public int score;
    public string date;

    public HighScoreEntry(string playerName, int score, string date)
    {
        this.playerName = playerName;
        this.score = score;
        this.date = date;
    }
}

[System.Serializable]
public class HighScoreData
{
    public List<HighScoreEntry> entries = new List<HighScoreEntry>();
}

public class HighScoreManager : MonoBehaviour
{
    public static HighScoreManager Instance { get; private set; }

    private const string PREFS_KEY = "CYBERPUNK_HIGHSCORES_SAVE";
    private const int MAX_ENTRIES = 10;

    [SerializeField] private HighScoreData scoreData = new HighScoreData();

    public event Action OnScoresChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadScores();
    }

    public void LoadScores()
    {
        if (PlayerPrefs.HasKey(PREFS_KEY))
        {
            try
            {
                string json = PlayerPrefs.GetString(PREFS_KEY);
                scoreData = JsonUtility.FromJson<HighScoreData>(json) ?? new HighScoreData();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[HighScoreManager] Failed to load scores: {ex.Message}");
                PopulateDefaultScores();
            }
        }
        else
        {
            PopulateDefaultScores();
        }

        SortAndTrim();
    }

    public void SaveScores()
    {
        try
        {
            string json = JsonUtility.ToJson(scoreData);
            PlayerPrefs.SetString(PREFS_KEY, json);
            PlayerPrefs.Save();
            OnScoresChanged?.Invoke();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[HighScoreManager] Failed to save scores: {ex.Message}");
        }
    }

    private void PopulateDefaultScores()
    {
        scoreData = new HighScoreData
        {
            entries = new List<HighScoreEntry>
            {
                new HighScoreEntry("CyberScavenger", 5000, "2025-01-10"),
                new HighScoreEntry("NeonSpider", 3500, "2025-01-12"),
                new HighScoreEntry("ByteCollector", 2200, "2025-01-15"),
                new HighScoreEntry("GridWalker", 1500, "2025-02-01"),
                new HighScoreEntry("RookieBot", 800, "2025-02-14")
            }
        };
        SaveScores();
    }

    public int GetTopHighScore()
    {
        if (scoreData != null && scoreData.entries != null && scoreData.entries.Count > 0)
        {
            return scoreData.entries[0].score;
        }
        return 0;
    }

    public IReadOnlyList<HighScoreEntry> GetScoreList()
    {
        if (scoreData == null) scoreData = new HighScoreData();
        return scoreData.entries;
    }

    public bool AddScore(int score, string playerName = "Player")
    {
        if (score <= 0) return false;

        string today = DateTime.Now.ToString("yyyy-MM-dd");
        var newEntry = new HighScoreEntry(string.IsNullOrWhiteSpace(playerName) ? "Player" : playerName, score, today);

        scoreData.entries.Add(newEntry);
        SortAndTrim();
        SaveScores();

        // Return true if this score reached #1
        return scoreData.entries.Count > 0 && scoreData.entries[0] == newEntry;
    }

    public void ClearScores()
    {
        scoreData.entries.Clear();
        SaveScores();
    }

    private void SortAndTrim()
    {
        if (scoreData?.entries == null) return;

        scoreData.entries.Sort((a, b) => b.score.CompareTo(a.score));

        if (scoreData.entries.Count > MAX_ENTRIES)
        {
            scoreData.entries.RemoveRange(MAX_ENTRIES, scoreData.entries.Count - MAX_ENTRIES);
        }
    }
}

using System;
using System.IO;
using UnityEngine;

[Serializable]
public sealed class ProgressData
{
    public int version;
    public string format;
    public int highestUnlockedLevel;
    public int currentLevel;
    public bool[] completedLevels;
    public float soundVolume;
}

/// <summary>Small, synchronous JSON store. A failed disk write never stops gameplay.</summary>
public sealed class ProgressStore
{
    public const int LevelCount = 3; // Save schema, not gameplay balance.
    public string FilePath { get; }
    public ProgressData Data { get; private set; }
    public string LastError { get; private set; }
    public string LastSaveReason { get; private set; }
    public int SuccessfulWrites { get; private set; }

    public ProgressStore(string filePath) { FilePath = filePath; Load(); }

    public static ProgressData Defaults() => new ProgressData {
        version = 1, format = "PirateBombs.Progress", highestUnlockedLevel = 1,
        currentLevel = 1, completedLevels = new bool[LevelCount], soundVolume = 1f
    };

    public static bool IsValid(ProgressData d)
    {
        if (d == null || d.version != 1 || d.format != "PirateBombs.Progress" ||
            d.completedLevels == null || d.completedLevels.Length != LevelCount ||
            d.highestUnlockedLevel < 1 || d.highestUnlockedLevel > LevelCount ||
            d.currentLevel < 1 || d.currentLevel > d.highestUnlockedLevel ||
            float.IsNaN(d.soundVolume) || float.IsInfinity(d.soundVolume) ||
            d.soundVolume < 0f || d.soundVolume > 1f) return false;
        for (int i = 0; i < LevelCount; i++)
        {
            if (i < d.highestUnlockedLevel - 1 && !d.completedLevels[i]) return false;
            if (i >= d.highestUnlockedLevel && d.completedLevels[i]) return false;
        }
        return true;
    }

    public void Load()
    {
        LastError = null;
        try {
            if (File.Exists(FilePath)) {
                string json = File.ReadAllText(FilePath);
                // Missing fields must not silently become valid zero/default values.
                string[] fields = { "version", "format", "highestUnlockedLevel", "currentLevel", "completedLevels", "soundVolume" };
                foreach (string field in fields)
                    if (!json.Contains("\"" + field + "\"")) throw new InvalidDataException("Missing save field: " + field);
                var loaded = JsonUtility.FromJson<ProgressData>(json);
                if (!IsValid(loaded)) throw new InvalidDataException("Invalid or unsupported progress.");
                Data = loaded;
                return;
            }
        } catch (Exception e) {
            LastError = e.Message;
            try { if (File.Exists(FilePath)) File.Copy(FilePath, FilePath + ".invalid-" + DateTime.UtcNow.Ticks, false); }
            catch (Exception) { /* Best effort: recovery must work even on read-only storage. */ }
        }
        Data = Defaults();
        Save("defaults / recovery");
    }

    public bool Save(string reason)
    {
        try {
            if (!IsValid(Data)) throw new InvalidDataException("Refusing invalid progress.");
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
            string temp = FilePath + ".tmp";
            File.WriteAllText(temp, JsonUtility.ToJson(Data, true));
            // Replace on the same filesystem: don't leave a half-written main JSON.
            if (File.Exists(FilePath)) File.Replace(temp, FilePath, FilePath + ".bak");
            else File.Move(temp, FilePath);
            LastError = null;
            LastSaveReason = reason;
            SuccessfulWrites++;
            return true;
        } catch (Exception e) {
            LastError = e.Message;
            Debug.LogWarning("Progress could not be saved: " + e.Message);
            return false;
        }
    }

    public void Complete(int level)
    {
        if (level < 1 || level > Data.highestUnlockedLevel || level > LevelCount) return;
        Data.completedLevels[level - 1] = true;
        Data.highestUnlockedLevel = Mathf.Max(Data.highestUnlockedLevel, Mathf.Min(level + 1, LevelCount));
        Save("level completed");
    }

    public void SetVolume(float value)
    {
        if (float.IsNaN(value) || float.IsInfinity(value)) return;
        Data.soundVolume = Mathf.Clamp01(value);
        AudioListener.volume = Data.soundVolume;
        Save("settings changed");
    }

    public void Reset()
    {
        Data = Defaults();
        AudioListener.volume = Data.soundVolume;
        Save("reset confirmed");
    }
}

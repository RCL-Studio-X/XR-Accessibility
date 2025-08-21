using UnityEngine;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public static class SRTParser
{
    public static List<CaptionEntry> ParseSRT(string srtContent)
    {
        List<CaptionEntry> captions = new List<CaptionEntry>();

        if (string.IsNullOrEmpty(srtContent))
        {
            Debug.LogError("SRT content is empty or null");
            return captions;
        }

        // Split by double newlines to separate subtitle blocks
        string[] blocks = srtContent.Split(new string[] { "\n\n", "\r\n\r\n" }, System.StringSplitOptions.RemoveEmptyEntries);

        foreach (string block in blocks)
        {
            if (string.IsNullOrWhiteSpace(block)) continue;

            CaptionEntry entry = ParseBlock(block.Trim());
            if (entry != null)
            {
                captions.Add(entry);
            }
        }

        Debug.Log($"Parsed {captions.Count} caption entries from SRT");
        return captions;
    }

    private static CaptionEntry ParseBlock(string block)
    {
        string[] lines = block.Split(new char[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);

        if (lines.Length < 3)
        {
            Debug.LogWarning($"Invalid SRT block: {block}");
            return null;
        }

        // Parse index
        if (!int.TryParse(lines[0].Trim(), out int index))
        {
            Debug.LogWarning($"Could not parse index from: {lines[0]}");
            return null;
        }

        // Parse time range
        var timeMatch = Regex.Match(lines[1], @"(\d{2}):(\d{2}):(\d{2}),(\d{3})\s*-->\s*(\d{2}):(\d{2}):(\d{2}),(\d{3})");
        if (!timeMatch.Success)
        {
            Debug.LogWarning($"Could not parse time from: {lines[1]}");
            return null;
        }

        float startTime = ParseTime(timeMatch.Groups[1].Value, timeMatch.Groups[2].Value,
                                   timeMatch.Groups[3].Value, timeMatch.Groups[4].Value);
        float endTime = ParseTime(timeMatch.Groups[5].Value, timeMatch.Groups[6].Value,
                                 timeMatch.Groups[7].Value, timeMatch.Groups[8].Value);

        // Combine all remaining lines as the text
        string text = "";
        for (int i = 2; i < lines.Length; i++)
        {
            if (i > 2) text += " ";
            text += lines[i].Trim();
        }

        return new CaptionEntry(index, startTime, endTime, text);
    }

    private static float ParseTime(string hours, string minutes, string seconds, string milliseconds)
    {
        int h = int.Parse(hours);
        int m = int.Parse(minutes);
        int s = int.Parse(seconds);
        int ms = int.Parse(milliseconds);

        return h * 3600f + m * 60f + s + ms / 1000f;
    }
}
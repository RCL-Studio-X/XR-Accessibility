using UnityEngine;

[System.Serializable]
public class CaptionEntry
{
    public int index;
    public float startTime;
    public float endTime;
    public string speaker;
    public string text;

    public CaptionEntry(int index, float startTime, float endTime, string text)
    {
        this.index = index;
        this.startTime = startTime;
        this.endTime = endTime;

        // Parse speaker and text from the caption text
        ParseSpeakerAndText(text);
    }

    private void ParseSpeakerAndText(string fullText)
    {
        // Check if text has "Speaker:" format
        if (fullText.Contains(":"))
        {
            int colonIndex = fullText.IndexOf(':');
            string potentialSpeaker = fullText.Substring(0, colonIndex).Trim();

            // Check if it looks like a speaker name (not just random colon usage)
            if (IsValidSpeakerName(potentialSpeaker))
            {
                this.speaker = potentialSpeaker;
                this.text = fullText.Substring(colonIndex + 1).Trim();

                // Remove quotes if present
                if (this.text.StartsWith("\"") && this.text.EndsWith("\""))
                {
                    this.text = this.text.Substring(1, this.text.Length - 2);
                }
                return;
            }
        }

        // Fallback: Check if text contains quotes (indicating dialogue)
        if (fullText.Contains("\""))
        {
            // Extract quoted text as dialogue
            int firstQuote = fullText.IndexOf('"');
            int lastQuote = fullText.LastIndexOf('"');

            if (firstQuote != lastQuote && firstQuote >= 0)
            {
                this.text = fullText.Substring(firstQuote + 1, lastQuote - firstQuote - 1);
                this.speaker = "Unknown"; // Default when no speaker info available
            }
            else
            {
                this.text = fullText;
                this.speaker = "Narrator";
            }
        }
        else
        {
            // No quotes, likely narration
            this.text = fullText;
            this.speaker = "Narrator";
        }
    }

    private bool IsValidSpeakerName(string potentialSpeaker)
    {
        // Simple validation - speaker names should be reasonable length and not contain weird characters
        if (string.IsNullOrWhiteSpace(potentialSpeaker)) return false;
        if (potentialSpeaker.Length > 20) return false; // Probably not a name if too long
        if (potentialSpeaker.Contains("\"")) return false; // Probably not a speaker format

        return true;
    }

    public bool IsActiveAtTime(float currentTime)
    {
        return currentTime >= startTime && currentTime <= endTime;
    }
}
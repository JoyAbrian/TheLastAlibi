[System.Serializable]
public class GeminiMessagePart
{
    public string text;
}

[System.Serializable]
public class GeminiContent
{
    public GeminiMessagePart[] parts;
}

[System.Serializable]
public class GeminiRequest
{
    public GeminiContent[] contents;
}

[System.Serializable]
public class GeminiResponse
{
    public Candidate[] candidates;
}

[System.Serializable]
public class Candidate
{
    public Content content;
}

[System.Serializable]
public class Content
{
    public GeminiMessagePart[] parts;
}
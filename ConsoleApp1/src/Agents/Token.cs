using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AgentsSample;
public class TokenDetails
{
    [JsonPropertyName("ReasoningTokenCount")]
    public int ReasoningTokenCount { get; set; }

    [JsonPropertyName("AudioTokenCount")]
    public int AudioTokenCount { get; set; }

    [JsonPropertyName("AcceptedPredictionTokenCount")]
    public int AcceptedPredictionTokenCount { get; set; }

    [JsonPropertyName("RejectedPredictionTokenCount")]
    public int RejectedPredictionTokenCount { get; set; }
}

public class InputTokenDetails
{
    [JsonPropertyName("AudioTokenCount")]
    public int AudioTokenCount { get; set; }

    [JsonPropertyName("CachedTokenCount")]
    public int CachedTokenCount { get; set; }
}

public class TokenCounts
{
    [JsonPropertyName("OutputTokenCount")]
    public int OutputTokenCount { get; set; }

    [JsonPropertyName("InputTokenCount")]
    public int InputTokenCount { get; set; }

    [JsonPropertyName("TotalTokenCount")]
    public int TotalTokenCount { get; set; }

    [JsonPropertyName("OutputTokenDetails")]
    public TokenDetails OutputTokenDetails { get; set; }

    [JsonPropertyName("InputTokenDetails")]
    public InputTokenDetails InputTokenDetails { get; set; }
}

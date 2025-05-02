using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AgentsSample;
public class TokenDetails
{
    [JsonPropertyName("ReasoningToken")]
    public int ReasoningToken { get; set; }

    [JsonPropertyName("AudioToken")]
    public int AudioToken { get; set; }

    [JsonPropertyName("AcceptedPredictionToken")]
    public int AcceptedPredictionToken { get; set; }

    [JsonPropertyName("RejectedPredictionToken")]
    public int RejectedPredictionToken { get; set; }
}

public class InputTokenDetails
{
    [JsonPropertyName("AudioToken")]
    public int AudioToken { get; set; }

    [JsonPropertyName("CachedToken")]
    public int CachedToken { get; set; }
}

public class Tokens
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

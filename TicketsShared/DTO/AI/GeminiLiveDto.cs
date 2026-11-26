using System.Text.Json.Serialization;

namespace LaundryShared.DTO.AI
{
    // =========================================================================
    //  SETUP  (Client → Gemini, first message)
    // =========================================================================

    public class BidiGenerateContentSetup
    {
        [JsonPropertyName("setup")]
        public BidiSetupContent Setup { get; set; } = new BidiSetupContent();
    }

    public class BidiSetupContent
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = "models/gemini-2.5-flash-native-audio-latest";

        [JsonPropertyName("generationConfig")]
        public BidiGenerationConfig GenerationConfig { get; set; } = new BidiGenerationConfig();

        [JsonPropertyName("systemInstruction")]
        public BidiSystemInstruction SystemInstruction { get; set; } = new BidiSystemInstruction();

        [JsonPropertyName("tools")]
        public List<BidiTool> Tools { get; set; } = new List<BidiTool>();

        /// <summary>Enables speech-to-text of user mic input (forwarded to client as transcripts).</summary>
        [JsonPropertyName("inputAudioTranscription")]
        public object InputAudioTranscription { get; set; } = new { };

        /// <summary>Enables transcription of model audio output.</summary>
        [JsonPropertyName("outputAudioTranscription")]
        public object OutputAudioTranscription { get; set; } = new { };

        [JsonPropertyName("realtimeInputConfig")]
        public BidiRealtimeInputConfig RealtimeInputConfig { get; set; } = new BidiRealtimeInputConfig();
    }

    public class BidiRealtimeInputConfig
    {
        [JsonPropertyName("automaticActivityDetection")]
        public BidiAutomaticActivityDetection AutomaticActivityDetection { get; set; } = new();
    }

    public class BidiAutomaticActivityDetection
    {
        [JsonPropertyName("disabled")]
        public bool Disabled { get; set; }

        [JsonPropertyName("silenceDurationMs")]
        public int SilenceDurationMs { get; set; } = 400;

        [JsonPropertyName("endOfSpeechSensitivity")]
        public string EndOfSpeechSensitivity { get; set; } = "END_SENSITIVITY_HIGH";
    }

    public class BidiGenerationConfig
    {
        [JsonPropertyName("responseModalities")]
        public List<string> ResponseModalities { get; set; } = new List<string> { "AUDIO" };

        [JsonPropertyName("temperature")]
        public float Temperature { get; set; } = 0.7f;

        [JsonPropertyName("maxOutputTokens")]
        public int MaxOutputTokens { get; set; } = 256;

        [JsonPropertyName("speechConfig")]
        public BidiSpeechConfig SpeechConfig { get; set; } = new BidiSpeechConfig();
    }

    public class BidiSpeechConfig
    {
        [JsonPropertyName("languageCode")]
        public string LanguageCode { get; set; } = "ar-EG";

        [JsonPropertyName("voiceConfig")]
        public BidiVoiceConfig VoiceConfig { get; set; } = new BidiVoiceConfig();
    }

    public class BidiVoiceConfig
    {
        [JsonPropertyName("prebuiltVoiceConfig")]
        public BidiPrebuiltVoiceConfig PrebuiltVoiceConfig { get; set; } = new BidiPrebuiltVoiceConfig();
    }

    public class BidiPrebuiltVoiceConfig
    {
        /// <summary>
        /// Available voices: Puck, Aoede, Charon, Fenrir, Kore
        /// </summary>
        [JsonPropertyName("voiceName")]
        public string VoiceName { get; set; } = "Puck";
    }

    public class BidiSystemInstruction
    {
        [JsonPropertyName("parts")]
        public List<BidiPart> Parts { get; set; } = new List<BidiPart>();
    }

    // =========================================================================
    //  SHARED  Parts / InlineData
    // =========================================================================

    public class BidiPart
    {
        [JsonPropertyName("text")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Text { get; set; }

        [JsonPropertyName("inlineData")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public BidiInlineData? InlineData { get; set; }
    }

    public class BidiInlineData
    {
        [JsonPropertyName("mimeType")]
        public string MimeType { get; set; } = "audio/pcm;rate=16000";

        [JsonPropertyName("data")]
        public string Data { get; set; } = string.Empty; // Base64 encoded PCM
    }

    // =========================================================================
    //  REALTIME INPUT  (Client → Gemini, audio chunks)
    // =========================================================================

    public class BidiRealtimeInputMessage
    {
        [JsonPropertyName("realtimeInput")]
        public BidiRealtimeInput RealtimeInput { get; set; } = new BidiRealtimeInput();
    }

    public class BidiRealtimeInput
    {
        /// <summary>Preferred field for realtime microphone PCM (see Live API docs).</summary>
        [JsonPropertyName("audio")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public BidiInlineData? Audio { get; set; }

        /// <summary>Deprecated by Google; kept for backward compatibility only.</summary>
        [JsonPropertyName("mediaChunks")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<BidiInlineData>? MediaChunks { get; set; }
    }

    // =========================================================================
    //  TOOLS  (declared in setup)
    // =========================================================================

    public class BidiTool
    {
        [JsonPropertyName("functionDeclarations")]
        public List<BidiFunctionDeclaration> FunctionDeclarations { get; set; } = new List<BidiFunctionDeclaration>();
    }

    public class BidiFunctionDeclaration
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// JSON Schema object describing parameters. Use anonymous type or JsonNode.
        /// Null means no parameters required.
        /// </summary>
        [JsonPropertyName("parameters")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public object? Parameters { get; set; }
    }

    // =========================================================================
    //  SERVER MESSAGES  (Gemini → Client)
    // =========================================================================

    /// <summary>
    /// Top-level server message. A message will contain ONE of the following:
    /// setupComplete, serverContent, toolCall.
    /// </summary>
    public class BidiServerMessage
    {
        [JsonPropertyName("setupComplete")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public BidiSetupComplete? SetupComplete { get; set; }

        [JsonPropertyName("serverContent")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public BidiServerContent? ServerContent { get; set; }

        [JsonPropertyName("toolCall")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public BidiToolCall? ToolCall { get; set; }

        /// <summary>May appear at top-level (not only inside serverContent).</summary>
        [JsonPropertyName("inputTranscription")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public BidiTranscription? InputTranscription { get; set; }

        /// <summary>May appear at top-level (not only inside serverContent).</summary>
        [JsonPropertyName("outputTranscription")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public BidiTranscription? OutputTranscription { get; set; }
    }

    public class BidiSetupComplete
    {
        // Intentionally empty — Gemini sends {} to confirm session is ready
    }

    public class BidiServerContent
    {
        [JsonPropertyName("modelTurn")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public BidiModelTurn? ModelTurn { get; set; }

        /// <summary>
        /// True when all audio for the current turn has been sent.
        /// </summary>
        [JsonPropertyName("turnComplete")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? TurnComplete { get; set; }

        /// <summary>
        /// True if generation was interrupted by new input from user.
        /// </summary>
        [JsonPropertyName("interrupted")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? Interrupted { get; set; }

        [JsonPropertyName("generationComplete")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? GenerationComplete { get; set; }

        [JsonPropertyName("inputTranscription")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public BidiTranscription? InputTranscription { get; set; }

        [JsonPropertyName("outputTranscription")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public BidiTranscription? OutputTranscription { get; set; }
    }

    public class BidiTranscription
    {
        [JsonPropertyName("text")]
        public string? Text { get; set; }

        [JsonPropertyName("finished")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? Finished { get; set; }
    }

    public class BidiModelTurn
    {
        [JsonPropertyName("parts")]
        public List<BidiPart> Parts { get; set; } = new List<BidiPart>();
    }

    // =========================================================================
    //  TOOL CALL  (Gemini → Client, requesting a function call)
    // =========================================================================

    public class BidiToolCall
    {
        [JsonPropertyName("functionCalls")]
        public List<BidiFunctionCall> FunctionCalls { get; set; } = new List<BidiFunctionCall>();
    }

    public class BidiFunctionCall
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Arguments as a dictionary. Values will be JsonElement at runtime when
        /// deserialized by System.Text.Json — use ExtractString() helper to unpack.
        /// </summary>
        [JsonPropertyName("args")]
        public Dictionary<string, object> Args { get; set; } = new Dictionary<string, object>();
    }

    // =========================================================================
    //  TOOL RESPONSE  (Client → Gemini, function call result)
    // =========================================================================

    public class BidiToolResponseMessage
    {
        [JsonPropertyName("toolResponse")]
        public BidiToolResponse ToolResponse { get; set; } = new BidiToolResponse();
    }

    public class BidiToolResponse
    {
        [JsonPropertyName("functionResponses")]
        public List<BidiFunctionResponse> FunctionResponses { get; set; } = new List<BidiFunctionResponse>();
    }

    public class BidiFunctionResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("response")]
        public object Response { get; set; } = new object();
    }
}

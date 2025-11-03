namespace Emotions.Application.DTOs.Streaming
{
    public sealed class StartSessionRequest
    {
        public string? Mode { get; set; } = "talk"; // talk|text
        public string? Locale { get; set; } = "en-US";
    }

    public sealed class EndSessionRequest
    {
        public bool GenerateReply { get; set; } = true;
    }

    public sealed class AudioChunkDto
    {
        public int Seq { get; set; }
        public byte[] Data { get; set; } = Array.Empty<byte>();
        public int SampleRate { get; set; } = 16000;
        public string Encoding { get; set; } = "pcm16"; // or opus
        public int Channels { get; set; } = 1;
        public int DurationMs { get; set; }
    }

    public sealed class CaptionDelta
    {
        public string Text { get; set; } = "";
        public bool IsFinal { get; set; }
    }

    public sealed class AiTokenDelta
    {
        public string Text { get; set; } = "";
        public bool IsFinal { get; set; }
    }
}
namespace Emotions.Application.Interfaces.AI.Records;

public record SafetyCheckResponse(string level, List<string> hints);
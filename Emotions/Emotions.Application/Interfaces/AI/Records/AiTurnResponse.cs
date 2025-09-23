namespace Emotions.Application.Interfaces.AI.Records;

public record AiTurnResponse(string Text, string Model, int TokensIn, int TokensOut);
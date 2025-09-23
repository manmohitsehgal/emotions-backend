using Emotions.Domain.Enums;
using OpenAI.Chat;

namespace Emotions.Infrastructure.AI;

internal static class AuthorTypeMap
{
    public static ChatMessageRole ToChatRole(AuthorType author) =>
        author == AuthorType.User ? ChatMessageRole.User :
        author == AuthorType.Therapist ? ChatMessageRole.Assistant :
        ChatMessageRole.System;

    public static string ToDtoRole(AuthorType author) =>
        author == AuthorType.User ? "user" :
        author == AuthorType.Therapist ? "therapist" :
        "system";

    public static AuthorType FromUser() => AuthorType.User;
    public static AuthorType FromTherapist() => AuthorType.Therapist;
}
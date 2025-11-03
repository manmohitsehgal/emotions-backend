using System.ComponentModel.DataAnnotations;

namespace Emotions.Domain.Entities;

public class AppSetting
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [MaxLength(100)] public string Key { get; set; } = string.Empty;
    [MaxLength(200)] public string? Value { get; set; }
}
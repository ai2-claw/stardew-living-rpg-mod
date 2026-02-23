namespace StardewLivingRPG.CustomNpcFramework.Models;

public enum ValidationSeverity
{
    Warning = 0,
    Error = 1
}

public sealed class ValidationIssue
{
    public ValidationSeverity Severity { get; init; } = ValidationSeverity.Warning;
    public string Code { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string PackId { get; init; } = string.Empty;
    public string NpcId { get; init; } = string.Empty;
    public string SourcePath { get; init; } = string.Empty;
}


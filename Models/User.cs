namespace MyApp.Models;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? PhotoPath { get; set; }
    public string? ContractPath { get; set; }
    public bool IsFired { get; set; } = false;

    public override string ToString() => $"{FullName} ({Role})";
}
using System.Text;

namespace Veyra.Api.Application.Security;

public class JwtSettings
{
    public const string SectionName = "Jwt";

    public const int MinimumKeyBytes = 32;

    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public int AccessTokenMinutes { get; set; } = 15;
    public int RefreshTokenDays { get; set; } = 7;

    public byte[] KeyBytes => Encoding.UTF8.GetBytes(Key);

    public bool Validate(out string error)
    {
        if (string.IsNullOrWhiteSpace(Issuer))
        {
            error = $"{SectionName}:Issuer nao pode ser vazio.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(Audience))
        {
            error = $"{SectionName}:Audience nao pode ser vazio.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(Key))
        {
            error = $"{SectionName}:Key nao foi configurada. Defina a variavel de ambiente " +
                    $"'{SectionName}__Key' ou use 'dotnet user-secrets set \"{SectionName}:Key\" \"<chave>\"'.";
            return false;
        }

        if (KeyBytes.Length < MinimumKeyBytes)
        {
            error = $"{SectionName}:Key precisa de pelo menos {MinimumKeyBytes} bytes " +
                    $"({MinimumKeyBytes} caracteres ASCII) para HMAC-SHA256; tem {KeyBytes.Length}.";
            return false;
        }

        if (AccessTokenMinutes <= 0)
        {
            error = $"{SectionName}:AccessTokenMinutes precisa ser maior que zero.";
            return false;
        }

        if (RefreshTokenDays <= 0)
        {
            error = $"{SectionName}:RefreshTokenDays precisa ser maior que zero.";
            return false;
        }

        error = string.Empty;
        return true;
    }
}

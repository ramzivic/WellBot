using System;
using System.Security.Cryptography;
using System.Text;

namespace WellBot.Desktop.Services;

/// <summary>
/// Protects sensitive credentials using Windows DPAPI (Data Protection API).
/// Data is encrypted per-user: only the Windows user who encrypted it can decrypt it.
/// </summary>
public static class CredentialProtector
{
    /// <summary>
    /// Encrypts a plaintext string using DPAPI (CurrentUser scope).
    /// Returns a Base64-encoded string of the encrypted bytes.
    /// </summary>
    public static string Protect(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
            return string.Empty;

        try
        {
            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            var encryptedBytes = ProtectedData.Protect(plainBytes, null, DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(encryptedBytes);
        }
        catch
        {
            // Fallback: return empty if protection fails
            return string.Empty;
        }
    }

    /// <summary>
    /// Decrypts a Base64-encoded DPAPI-encrypted string back to plaintext.
    /// </summary>
    public static string Unprotect(string encryptedBase64)
    {
        if (string.IsNullOrEmpty(encryptedBase64))
            return string.Empty;

        try
        {
            var encryptedBytes = Convert.FromBase64String(encryptedBase64);
            var plainBytes = ProtectedData.Unprotect(encryptedBytes, null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(plainBytes);
        }
        catch
        {
            // If decryption fails (e.g. data was stored in plaintext before migration),
            // return the raw value so it can be re-encrypted on next save.
            return encryptedBase64;
        }
    }

    /// <summary>
    /// Checks whether a string looks like it's already DPAPI-encrypted (valid Base64 that decrypts successfully).
    /// </summary>
    public static bool IsProtected(string value)
    {
        if (string.IsNullOrEmpty(value))
            return false;

        try
        {
            var bytes = Convert.FromBase64String(value);
            ProtectedData.Unprotect(bytes, null, DataProtectionScope.CurrentUser);
            return true;
        }
        catch
        {
            return false;
        }
    }
}

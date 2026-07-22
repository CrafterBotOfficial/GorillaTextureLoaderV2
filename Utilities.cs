using System;
using System.IO;
using System.Security.Cryptography;

namespace GorillaTextureLoader;

public static class Utilities
{
    public static string GetHash(FileStream stream)
    {
        using var hashAlgorithm = SHA256.Create();
        var hashBytes = hashAlgorithm.ComputeHash(stream);
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
    }
}

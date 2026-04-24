using FinAssist.Core.Interfaces;
using Isopoh.Cryptography.Argon2;

namespace FinAssist.Infrastructure.Services;

public class Argon2PasswordService : IPasswordService
{
    public string Hash(string password) => Argon2.Hash(password);

    public bool Verify(string password, string hash) => Argon2.Verify(hash, password);
}

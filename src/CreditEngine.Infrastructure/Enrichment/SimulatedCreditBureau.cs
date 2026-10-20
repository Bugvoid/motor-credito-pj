using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using CreditEngine.Domain;

namespace CreditEngine.Infrastructure.Enrichment;

/// <summary>Bureau simulado: score 0–1000 determinístico a partir do CNPJ, para testes reproduzíveis.</summary>
public sealed class SimulatedCreditBureau : ICreditBureau
{
    public int Score(Cnpj cnpj)
    {
        var hash = SHA256.HashData(Encoding.ASCII.GetBytes(cnpj.Digits));
        return (int)(BinaryPrimitives.ReadUInt32BigEndian(hash) % 1001);
    }
}

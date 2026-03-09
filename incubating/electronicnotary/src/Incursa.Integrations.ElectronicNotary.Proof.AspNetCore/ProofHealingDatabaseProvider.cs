namespace Incursa.Integrations.ElectronicNotary.Proof.AspNetCore;

/// <summary>
/// Identifies the database engine used for Proof healing persistence.
/// </summary>
public enum ProofHealingDatabaseProvider
{
    /// <summary>
    /// Uses Microsoft SQL Server.
    /// </summary>
    SqlServer = 0,

    /// <summary>
    /// Uses PostgreSQL.
    /// </summary>
    Postgres = 1,
}

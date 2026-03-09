namespace Incursa.Integrations.ElectronicNotary.Proof.AspNetCore;

/// <summary>
/// Configures persistent storage for Proof healing tracking and polling.
/// </summary>
public sealed class ProofHealingPersistenceOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether persistence-backed healing is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the database provider used by the healing store.
    /// </summary>
    public ProofHealingDatabaseProvider DatabaseProvider { get; set; } = ProofHealingDatabaseProvider.Postgres;

    /// <summary>
    /// Gets or sets the database connection string for the healing store.
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether schema migrations should run at startup.
    /// </summary>
    public bool ApplyMigrationsAtStartup { get; set; } = true;

    /// <summary>
    /// Gets or sets the timeout used to acquire the migration coordination lock.
    /// </summary>
    public TimeSpan MigrationLockTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets a value indicating whether schema migrations should run at startup.
    /// </summary>
    public bool EnableSchemaDeployment
    {
        get => this.ApplyMigrationsAtStartup;
        set => this.ApplyMigrationsAtStartup = value;
    }
}

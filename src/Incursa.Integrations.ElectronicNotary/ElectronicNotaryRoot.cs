// <copyright file="ElectronicNotaryRoot.cs" company="Incursa">
// CONFIDENTIAL - Copyright (c) Incursa. All rights reserved.
// See NOTICE.md for full restrictions and usage terms.
// </copyright>

namespace Incursa.Integrations.ElectronicNotary;

/// <summary>
/// Marker root type for the electronic notary integration package.
/// </summary>
public static class ElectronicNotaryRoot
{
    /// <summary>
    /// Returns the fully qualified type name for package identity checks.
    /// </summary>
    /// <returns>The fully qualified marker type name.</returns>
    public static string TypeName() => typeof(ElectronicNotaryRoot).FullName ?? nameof(ElectronicNotaryRoot);
}

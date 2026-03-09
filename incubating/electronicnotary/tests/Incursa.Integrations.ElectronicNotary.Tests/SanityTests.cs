// <copyright file="SanityTests.cs" company="Incursa">
// CONFIDENTIAL - Copyright (c) Incursa. All rights reserved.
// See NOTICE.md for full restrictions and usage terms.
// </copyright>

namespace Incursa.Integrations.ElectronicNotary.Tests;

[TestClass]
public sealed class SanityTests
{
    [TestMethod]
    public void TypeNameReturnsExpectedValue()
    {
        Assert.AreEqual("Incursa.Integrations.ElectronicNotary.ElectronicNotaryRoot", global::Incursa.Integrations.ElectronicNotary.ElectronicNotaryRoot.TypeName());
    }
}

// Copyright (c) Incursa
// See NOTICE.md for full restrictions and usage terms.
// </copyright>

namespace Incursa.Integrations.WorkOS.Tests;

[TestClass]
public sealed class SanityTests
{
    [TestMethod]
    public void TypeName_ReturnsExpectedValue()
    {
        Assert.AreEqual("Incursa.Integrations.WorkOS.WorkOSRoot", global::Incursa.Integrations.WorkOS.WorkOSRoot.TypeName());
    }
}

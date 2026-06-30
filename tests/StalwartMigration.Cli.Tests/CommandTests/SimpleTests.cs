// <copyright file="SimpleTests.cs" company="Stalwart Labs">
// Copyright © Stalwart Labs 2024. All rights reserved.
// </copyright>

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace StalwartMigration.Cli.Tests.CommandTests;

/// <summary>
/// Simple tests to ensure the CLI test infrastructure works.
/// </summary>
[TestClass]
public class SimpleTests
{
    [TestMethod]
    public void Test_Cli_True_Is_True()
    {
        Assert.IsTrue(true);
    }
}

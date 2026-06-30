// <copyright file="SimpleTests.cs" company="Stalwart Labs">
// Copyright © Stalwart Labs 2024. All rights reserved.
// </copyright>

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace StalwartMigration.Tests.Unit;

/// <summary>
/// Simple tests to ensure the test infrastructure works.
/// </summary>
[TestClass]
public class SimpleTests
{
    [TestMethod]
    public void Test_True_Is_True()
    {
        Assert.IsTrue(true);
    }

    [TestMethod]
    public void Test_One_Equals_One()
    {
        Assert.AreEqual(1, 1);
    }

    [TestMethod]
    public void Test_String_Not_Null()
    {
        string test = "test";
        Assert.IsNotNull(test);
    }
}

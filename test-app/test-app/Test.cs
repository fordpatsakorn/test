using NUnit.Framework;


namespace test_app;

public class Test
{
    [SetUp]
    public void Setup()
    {
        Console.WriteLine("Setup");
        // This method is used for any setup code you might need before each test runs.
    }

    [Test]
    public void TestThatAlwaysFails()
    {
        Assert.That(false, Is.True, "This test is designed to always fail.");
    }

}
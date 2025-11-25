namespace CompilerBrain.Tests;

public class UnitTest1
{
    [Test]
    public async Task SampleTest()
    {
        var t = true;
        await Assert.That(t).IsTrue();
    }
}

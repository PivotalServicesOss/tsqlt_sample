public class TsqlUnitTests : IClassFixture<TsqltTestFactory>
{
    private readonly TsqltTestFactory factory;

    public TsqlUnitTests(TsqltTestFactory factory, ITestOutputHelper testOutputLogger)
    {
        this.factory = factory;
        factory.Initialize(testOutputLogger);
    }

    [Fact]
    public async Task ExecuteAllTsqltTests()
    {
        await factory.ExecuteNonQueryAsync("EXEC tSQLt.RunAll;");
    }
}

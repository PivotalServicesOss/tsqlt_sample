[assembly: CollectionBehavior(DisableTestParallelization = true)]
[TestCaseOrderer("AlphabeticalOrderer", "Sample.DB.TsqlTests")]
public class TsqlUnitTests
{
    private const string TSQLT_FOLDER_NM = "tSQLt_V1.0.8083.3529";
    private const string TSQLT_PREPARE_SERVER_SQL_FILE = "PrepareServer.sql";
    private const string TSQLT_FRAMEWORK_SQL_FILE = "tSQLt.class.sql";
    private const string DB_CONNECTION_STR_ENV_VAR_NM = "ConnectionStrings:SampleDatabase";
    private readonly string rootDirectory = Directory.GetParent(typeof(TsqlUnitTests).Assembly.Location).FullName;
    private readonly ITestOutputHelper testOutputLogger;

    public TsqlUnitTests(ITestOutputHelper testOutputLogger)
    {
        this.testOutputLogger = testOutputLogger;
    }

    [Fact]
    public async Task A_PrepareSqlServer()
    {
        var prepareServerSqlFile = Path.Combine(rootDirectory, TSQLT_FOLDER_NM, TSQLT_PREPARE_SERVER_SQL_FILE);
        var prepareServerSql = File.ReadAllLines(prepareServerSqlFile);

        WriteOutput($"Executing sql file {prepareServerSqlFile}");

        using var sqlConnection = GetSqlConnection();
        await ExecuteScriptsInChunks(sqlConnection, prepareServerSql);
    }

    [Fact]
    public async Task B_ConfigureTsqltFramework()
    {
        var tSQLtClassSqlFile = Path.Combine(rootDirectory, TSQLT_FOLDER_NM, TSQLT_FRAMEWORK_SQL_FILE);
        var tSQLtClassSql = File.ReadAllLines(tSQLtClassSqlFile);

        WriteOutput($"Executing sql file {tSQLtClassSqlFile}");

        using var sqlConnection = GetSqlConnection();
        await ExecuteScriptsInChunks(sqlConnection, tSQLtClassSql);
    }

    [Fact]
    public async Task C_DeployAllTsqltTests()
    {
        var tsqltTestFiles = Directory.GetFiles(rootDirectory, "*Tests.sql", SearchOption.AllDirectories);

        using var sqlConnection = GetSqlConnection();
        foreach (var tsqltTestFile in tsqltTestFiles)
        {
            WriteOutput($"Executing sql file {tsqltTestFile}");
            var tsqltTestFileSql = File.ReadAllLines(tsqltTestFile);
            await ExecuteScriptsInChunks(sqlConnection, tsqltTestFileSql);
        }
    }

    [Fact]
    public async Task D_ExecuteAllTsqltTests()
    {
        using var sqlConnection = GetSqlConnection();
        await ExecuteNonQueryAsync(sqlConnection, "EXEC tSQLt.RunAll;");
    }

    private async Task ExecuteScriptsInChunks(SqlConnection sqlConnection, string[] sqlContent)
    {
        var sqlBuilder = new StringBuilder();
        foreach (var line in sqlContent)
        {
            if (line.Trim().ToLower() == "go")
            {
                await ExecuteNonQueryAsync(sqlConnection, sqlBuilder.ToString());
                sqlBuilder = new StringBuilder();
            }
            else
                sqlBuilder.AppendLine(line);
        }
    }

    private async Task<int> ExecuteNonQueryAsync(SqlConnection sqlConnection, string sql)
    {
        using var cmd = new SqlCommand(sql, sqlConnection);
        return await cmd.ExecuteNonQueryAsync();
    }

    private SqlConnection GetSqlConnection()
    {
        var sqlConnection = new SqlConnection(GetConnectionStringFromEnvironmentVariable());
        sqlConnection.InfoMessage += (s, a) => WriteOutput(a);
        sqlConnection.Open();
        return sqlConnection;
    }

    private void WriteOutput(SqlInfoMessageEventArgs e)
    {
        Debug.WriteLine(e.Message);
        Console.WriteLine(e.Message);
        testOutputLogger.WriteLine(e.Message);
    }

    private void WriteOutput(string text)

    {
        Debug.WriteLine(text);
        Console.WriteLine(text);
        testOutputLogger.WriteLine(text);
    }

    private string GetConnectionStringFromEnvironmentVariable()
    {
        var connectionString = Environment.GetEnvironmentVariable(DB_CONNECTION_STR_ENV_VAR_NM);

        if (string.IsNullOrWhiteSpace(connectionString))
            return "Server=localhost,2000;Database=Sample;User Id=sa;Password=AlwaysBeKind@;";

        return connectionString;
    }
}

// Reference: https://docs.microsoft.com/en-us/dotnet/core/testing/order-unit-tests?pivots=xunit
public class AlphabeticalOrderer : ITestCaseOrderer
{
    public IEnumerable<TTestCase> OrderTestCases<TTestCase>(IEnumerable<TTestCase> testCases) where TTestCase : ITestCase
    {
        return testCases.OrderBy(testCase => testCase.TestMethod.Method.Name);
    }
}
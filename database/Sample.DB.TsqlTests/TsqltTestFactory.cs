[assembly: CollectionBehavior(DisableTestParallelization = true)]
public class TsqltTestFactory : IDisposable
{
    private readonly string rootDirectory = Directory.GetParent(typeof(TsqltTestFactory).Assembly.Location).FullName;
    private bool _disposed;
    private ITestOutputHelper testOutputLogger;
    private SqlConnection connection;
    private const string TSQLT_FOLDER_NM = "tSQLt_V1.0.8083.3529";
    private const string TSQLT_PREPARE_SERVER_SQL_FILE = "PrepareServer.sql";
    private const string TSQLT_FRAMEWORK_SQL_FILE = "tSQLt.class.sql";
    private const string DB_CONNECTION_STR_ENV_VAR_NM = "ConnectionStrings:SampleDatabase";

    public void Initialize(ITestOutputHelper testOutputLogger)
    {
        this.testOutputLogger = testOutputLogger;
        connection = GetSqlConnection();
        Task.WaitAll(PrepareSqlServer());
        Task.WaitAll(ConfigureTsqltFramework());
        Task.WaitAll(DeployAllTsqltTests());
    }

    private async Task PrepareSqlServer()
    {
        var prepareServerSqlFile = Path.Combine(rootDirectory, TSQLT_FOLDER_NM, TSQLT_PREPARE_SERVER_SQL_FILE);
        var prepareServerSql = File.ReadAllLines(prepareServerSqlFile);

        WriteOutput($"Executing sql file {prepareServerSqlFile}");
        await ExecuteScriptsInChunks(prepareServerSql);
    }

    private async Task ConfigureTsqltFramework()
    {
        var tSQLtClassSqlFile = Path.Combine(rootDirectory, TSQLT_FOLDER_NM, TSQLT_FRAMEWORK_SQL_FILE);
        var tSQLtClassSql = File.ReadAllLines(tSQLtClassSqlFile);

        WriteOutput($"Executing sql file {tSQLtClassSqlFile}");
        await ExecuteScriptsInChunks(tSQLtClassSql);
    }

    private async Task DeployAllTsqltTests()
    {
        var tsqltTestFiles = Directory.GetFiles(rootDirectory, "*Tests.sql", SearchOption.AllDirectories);

        foreach (var tsqltTestFile in tsqltTestFiles)
        {
            WriteOutput($"Executing sql file {tsqltTestFile}");
            var tsqltTestFileSql = File.ReadAllLines(tsqltTestFile);
            await ExecuteScriptsInChunks(tsqltTestFileSql);
        }
    }
    
    private async Task ExecuteScriptsInChunks(string[] sqlContent)
    {
        var sqlBuilder = new StringBuilder();
        foreach (var line in sqlContent)
        {
            if (line.Trim().ToLower() == "go")
            {
                await ExecuteNonQueryAsync(sqlBuilder.ToString());
                sqlBuilder = new StringBuilder();
            }
            else
                sqlBuilder.AppendLine(line);
        }
    }

    public async Task<int> ExecuteNonQueryAsync(string sql)
    {
        using var cmd = new SqlCommand(sql, connection);
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
        testOutputLogger?.WriteLine(e.Message);
    }

    private void WriteOutput(string text)
    {
        Debug.WriteLine(text);
        Console.WriteLine(text);
        testOutputLogger?.WriteLine(text);
    }

    private string GetConnectionStringFromEnvironmentVariable()
    {
        var connectionString = Environment.GetEnvironmentVariable(DB_CONNECTION_STR_ENV_VAR_NM);

        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException($"Connection string environment variable `{DB_CONNECTION_STR_ENV_VAR_NM}` is not set!");;

        return connectionString;
    }

    ~TsqltTestFactory()
    {
        Dispose(false);
    }

        public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            connection?.Close();
        }

        _disposed = true;
    }
}
using loggerApp.AppSettings;
using loggerApp.Models;
using loggerApp.Producers;
using loggerApp.Queue;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using static loggerApp.CppWrapper.ClkLibConstants;

namespace loggerApp.Consumers
{
    public class DbConsumer
    {
        private string ConnectionString;
        private BaseLoggerContext BaseLoggerContext;

        BlockingCollection<IQueueingData> BlockingCollection;
        CancellationToken CancellationToken;
        Task DbConsumerTask;

        public DbConsumer()
        {
            ConnectionString = ConfigurationManager.ConnectionStrings["BaseLoggerContext"].ConnectionString;
            BaseLoggerContext = new BaseLoggerContext();
            //TestSMO();
            return;
        }
        private void TestSMO()
        {
            var server = new Server(new ServerConnection(new SqlConnection(ConnectionString)));
            var database = new Microsoft.SqlServer.Management.Smo.Database(server, "logger");
            database.Refresh();

            //  database.Dump(); // Error
            // Microsoft.SqlServer.Management.Smo.Table     SMO 用？

            // System.Data.DataTable                        普通に内部で使うTable?
            var tables = database.Tables.Cast<Table>();
            tables.ToList().ForEach(f => Console.WriteLine(f.Name));
            var table = tables.FirstOrDefault();
            foreach (Column column in table.Columns)
            {
                Console.Write(column.Name + column.DataType);
            }
        }
        /// <summary>
        /// Recipe追加用
        /// TODO: SqlBulkInsert 対応したほうがいいかも。複数行同時追加があるようであれば
        /// </summary>
        /// <param name="recipeList"></param>
        /// <returns></returns>
        private int InsertRecipe(RecipeList recipeList)
        {
            // Initialize the return value to zero and create a StringWriter to display results.
            int affectedCountOfRows = 0;

            try
            {
                // Create the TransactionScope to execute the commands, guaranteeing
                // that both commands can commit or roll back as a single unit of work.
                using (var scope = new TransactionScope())
                {
                    recipeList.Recipes.ForEach(f=> BaseLoggerContext.Recipes.Add(f));
                    BaseLoggerContext.SaveChanges();

                    // The Complete method commits the transaction. If an exception has been thrown,
                    // Complete is not  called and the transaction is rolled back.
                    scope.Complete();
                }
            }
            catch (TransactionAbortedException ex)
            {
                Log.Error("TransactionAbortedException Message: {0}", ex.Message);
            }
            catch (ApplicationException ex)
            {
                Log.Error("ApplicationException Message: {0}", ex.Message);
            }
            catch(Exception ex)
            {
                Log.Error(ex, "Exception at InsertRecipe");
            }
            // Display messages.
            return affectedCountOfRows;
        }
        /// <summary>
        /// SqlCommand の事前生成
        /// EntityBaseのPropertyは手動で突っ込む必要あり。静的なので。
        /// 但し、Id, Inserted はDB側で生成するので無視
        /// LineName, Created は手動生成してるんで、文字列をConst化したいとこだけど、Append噛んでるとこは微妙なんで、現状放置
        /// TODO: EntityBase からの自動結合も考えないとね、面倒なんで
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="lineName"></param>
        /// <param name="infos">このListを基にCommand生成するので、不要なものは除外しておくこと</param>
        /// <returns></returns>
        static public SqlCommand GetSqlCommandAsInsert(string tableName, string lineName, IEnumerable<PlcMemoryInfo> infos)
        {
            var commandText = new StringBuilder("INSERT INTO ");
            commandText.Append(tableName);
            infos.Aggregate(commandText.Append("("), (a, b) => a.Append(b.Name).Append(","), (a) => a.Append("LineName, Created)"));
            //settings.ReadingTargets.Aggregate(commandText.Append("("), (a, b) => a.Append(b.Name).Append(","), (a) => a.Replace(",", ")", a.Length - 1, 1));
            infos.Aggregate(commandText.Append(" Values ("), (a, b) => a.Append("@").Append(b.Name).Append(","), (a) => a.Append("@LineName,@Created)"));

            void AddParameters(PlcMemoryInfo info, SqlCommand command)
            {
                switch (info.AccessSize)
                {
                    case AccessSize.BIT:
                        command.Parameters.Add(info.Name, SqlDbType.Bit);
                        break;
                    case AccessSize.BYTE:
                        command.Parameters.Add(info.Name, SqlDbType.Binary, 1);
                        break;
                    case AccessSize.WORD:
                        command.Parameters.Add(info.Name, SqlDbType.Binary, 2);
                        break;
                    case AccessSize.WORD2SmallInt:
                        command.Parameters.Add(info.Name, SqlDbType.SmallInt);
                        break;
                    case AccessSize.DWORD:
                    default:
                        command.Parameters.Add(info.Name, SqlDbType.Binary, 4);
                        break;
                }
            }
            var sqlCommand = new SqlCommand(commandText.ToString());
            infos.ToList().ForEach(f => AddParameters(f, sqlCommand));
            sqlCommand.Parameters.Add("LineName", SqlDbType.NVarChar);
            sqlCommand.Parameters["LineName"].Value = lineName;
            sqlCommand.Parameters.Add("Created", SqlDbType.DateTime2);

            return sqlCommand;
        }
        static public SqlCommand GetSqlCommandAsUpdate(string tableName, string lineName, IEnumerable<PlcMemoryInfo> infos)
        {
            var commandText = new StringBuilder("UPDATE TOP(1) ");
            commandText.Append(tableName);
            infos.Aggregate(commandText.Append(" SET "), (a, b) => a.Append(b.Name).Append("=@").Append(b.Name).Append(","), (a) => a.Append("Created=@Created "));
            commandText.Append("WHERE LineName='").Append(lineName).Append("'");

            void AddParameters(PlcMemoryInfo info, SqlCommand command)
            {
                switch (info.AccessSize)
                {
                    case AccessSize.BIT:
                        command.Parameters.Add(info.Name, SqlDbType.Bit);
                        break;
                    case AccessSize.BYTE:
                        command.Parameters.Add(info.Name, SqlDbType.Binary, 1);
                        break;
                    case AccessSize.WORD:
                        command.Parameters.Add(info.Name, SqlDbType.Binary, 2);
                        break;
                    case AccessSize.WORD2SmallInt:
                        command.Parameters.Add(info.Name, SqlDbType.SmallInt);
                        break;
                    case AccessSize.DWORD:
                    default:
                        command.Parameters.Add(info.Name, SqlDbType.Binary, 4);
                        break;
                }
            }
            var sqlCommand = new SqlCommand(commandText.ToString());
            infos.ToList().ForEach(f => AddParameters(f, sqlCommand));
            sqlCommand.Parameters.Add("Created", SqlDbType.DateTime2);

            return sqlCommand;
        }
        static public SqlCommand GetSqlCommandAsUpdateOrInsert(string tableName, string lineName, IEnumerable<PlcMemoryInfo> infos)
        {
            // 現状 source は不要だけど、Merge には USING 必須っぽいので、付けてるだけ as も要らんけど
            var commandText = new StringBuilder("MERGE TOP(1) ").Append(tableName).Append(" as target USING ").Append(tableName).Append(" as source");
            commandText.Append(" ON (target.LineName='").Append(lineName).Append("')");
            commandText.Append(" WHEN MATCHED THEN");

            infos.Aggregate(commandText.Append(" UPDATE SET "), (a, b) => a.Append(b.Name).Append("=@").Append(b.Name).Append(","), (a) => a.Append("Created=@Created "));

            commandText.Append(" WHEN NOT MATCHED BY target THEN");

            infos.Aggregate(commandText.Append(" INSERT ("), (a, b) => a.Append(b.Name).Append(","), (a) => a.Append("LineName, Created)"));
            infos.Aggregate(commandText.Append(" Values ("), (a, b) => a.Append("@").Append(b.Name).Append(","), (a) => a.Append("'").Append(lineName).Append("',@Created);"));

            void AddParameters(PlcMemoryInfo info, SqlCommand command)
            {
                switch (info.AccessSize)
                {
                    case AccessSize.BIT:
                        command.Parameters.Add(info.Name, SqlDbType.Bit);
                        break;
                    case AccessSize.BYTE:
                        command.Parameters.Add(info.Name, SqlDbType.Binary, 1);
                        break;
                    case AccessSize.WORD2SmallInt:
                        command.Parameters.Add(info.Name, SqlDbType.SmallInt);
                        break;
                    case AccessSize.WORD:
                        command.Parameters.Add(info.Name, SqlDbType.Binary, 2);
                        break;
                    default:
                        command.Parameters.Add(info.Name, SqlDbType.Binary, 4);
                        break;
                }
            }
            var sqlCommand = new SqlCommand(commandText.ToString());
            infos.ToList().ForEach(f => AddParameters(f, sqlCommand));
            sqlCommand.Parameters.Add("Created", SqlDbType.DateTime2);

            return sqlCommand;
        }

        private int InsertData(PlcMemoryValueList valueList)
        {
            // Initialize the return value to zero and create a StringWriter to display results.
            int affectedCountOfRows = 0;

            var sqlCommands = valueList.PlcMemoryValuesGroups.Select(s => new { Command = valueList.SqlCommand.Clone(), Values = s })
                .Select(f => {
                    f.Values.ForEach(fo => f.Command.Parameters[fo.PlcMemoryInfo.Name].Value = fo.Value);
                    f.Command.Parameters["Created"].Value = valueList.Created;
                    return f.Command; });
            try
            {
                // Create the TransactionScope to execute the commands, guaranteeing
                // that both commands can commit or roll back as a single unit of work.
                using (var scope = new TransactionScope())
                {
                    using (var sqlConnection = new SqlConnection(ConnectionString))
                    {
                        // Opening the connection automatically enlists it in the 
                        // TransactionScope as a lightweight transaction.
                        sqlConnection.Open();

                        // Create the SqlCommand object and execute the first command.
                        affectedCountOfRows = sqlCommands.ToList().Aggregate(0, (x, y) => { y.Connection = sqlConnection; return x + y.ExecuteNonQuery(); });
                    }
                    // The Complete method commits the transaction. If an exception has been thrown,
                    // Complete is not  called and the transaction is rolled back.
                    scope.Complete();
                }
            }
            catch (TransactionAbortedException ex)
            {
                Log.Error("TransactionAbortedException Message: {0}", ex.Message);
            }
            catch (ApplicationException ex)
            {
                Log.Error("ApplicationException Message: {0}", ex.Message);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Exception at InsertData ;{0}", valueList.SqlCommand.CommandText);
            }

            // Display messages.
            return affectedCountOfRows;
        }
        public DbConsumer(BlockingCollection<IQueueingData> bc, CancellationToken ct) : this()
        {
            BlockingCollection = bc;
            CancellationToken = ct;
        }
        public void Start()
        {
            var countMax = loggerConstants.DbConsummerWarningQueueCount;
            DbConsumerTask = Task.Factory.StartNew(() =>
            {
                while (!BlockingCollection.IsCompleted)
                {
                    IQueueingData nextItem = null;
                    try
                    {
                        if (!BlockingCollection.TryTake(out nextItem, 1000, CancellationToken))
                        {
                            Log.Information("Taking no data...");
                        }
                        else
                        {
                            if (BlockingCollection.Count >= countMax)
                            {
                                // TODO : 暫定として登録側の処理が遅れている（countMax 件未処理が貯まっている）かを確認するようにしておく
                                Log.Warning("more than {0}", countMax);
                                countMax <<= 1; // 算術シフト。最上位BitまでShiftした場合、0になるが、else 側のResetになるから気にしない
                            }
                            else
                            {
                                countMax = (countMax > loggerConstants.DbConsummerWarningQueueCount) ? countMax >> 1 : loggerConstants.DbConsummerWarningQueueCount;
                            }
                            switch (nextItem)
                            {
                                case PlcMemoryValueList valueList:
                                    InsertData(valueList);
                                    break;
                                case RecipeList recipeList:
                                    InsertRecipe(recipeList);
                                    break;
                                default:
                                    break;
                            }
                        }
                    }

                    catch (OperationCanceledException)
                    {
                        Log.Information("Taking canceled.");
                        break;
                    }
                    catch(Exception ex)
                    {
                        Log.Error(ex, "Something happend while consuming.");
                    }
                }
            });
        }
    }
}

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;

namespace PlanServerService
{
    public static class SQLiteHelper
    {
        private static int _commandTimeout = 30;
        /// <summary>
        /// 获取或设置SQLiteCommand的CommandTimeout属性，默认30秒，设置小于1时,不生效
        /// </summary>
        public static int CommandTimeout
        {
            get { return _commandTimeout; }
            set
            {
                if (_commandTimeout > 0)
                    _commandTimeout = value;
            }
        }

        #region 私有静态工具方法
        /// <summary>
        /// 准备数据操作命令
        /// </summary>
        /// <param name="command">待准备的命令对象</param>
        /// <param name="connection">执行该命令的有效数据库连接</param>
        /// <param name="transaction">有效数据事务对象，或者 null</param>
        /// <param name="commandType">获取或设置一个值，该值指示如何解释 CommandText 属性</param>
        /// <param name="commandText">获取或设置要对数据源执行的 Transact-SQL 语句或存储过程</param>
        /// <param name="commandParameters">SQLiteParameter 参数数组，如果无参数则为 null</param>
        /// <param name="mustCloseConnection"><c>true</c> 如果打开数据库连接则为 true，否则为 false</param>
        private static void PrepareCommand(SQLiteCommand command, SQLiteConnection connection,
            SQLiteTransaction transaction, CommandType commandType,
            string commandText, IEnumerable<SQLiteParameter> commandParameters, out bool mustCloseConnection)
        {
            if (command == null)
            {
                throw new ArgumentNullException("command");
            }
            if (string.IsNullOrEmpty(commandText))
            {
                throw new ArgumentNullException("commandText");
            }

            // 如果该数据库连接没有打开，则设置为打开状态
            if (connection.State != ConnectionState.Open)
            {
                mustCloseConnection = true;
                connection.Open();
            }
            else
            {
                mustCloseConnection = false;
            }

            command.Connection = connection;
            command.CommandText = commandText;
            command.CommandTimeout = CommandTimeout;

            // 如果有提供数据事务
            if (transaction != null)
            {
                if (transaction.Connection == null)
                {
                    throw new ArgumentException("打开状态的事务允许数据操作回滚或者提交。", "transaction");
                }
                command.Transaction = transaction;
            }

            command.CommandType = commandType;

            if (commandParameters != null)
            {
                foreach (SQLiteParameter parameter in commandParameters)
                {
                    if (parameter != null)
                    {
                        if ((parameter.Direction == ParameterDirection.InputOutput || parameter.Direction == ParameterDirection.Input) &&
                            (parameter.Value == null))
                        {
                            parameter.Value = DBNull.Value;
                        }
                        command.Parameters.Add(parameter);
                    }
                }
            }
        }

        static SQLiteConnection PrepareConnection(string constr)
        {
            SQLiteConnectionStringBuilder conString = new SQLiteConnectionStringBuilder();
            conString.DataSource = constr;
            return new SQLiteConnection(conString.ToString());
        }
        #endregion

        #region ExecuteNonQuery
        // 主调方法
        /// <summary>
        /// 对连接执行 Transact-SQL 语句并返回受影响的行数。
        /// </summary>
        /// <remarks>
        /// </remarks>
        /// <param name="connection">有效的数据库连接对象</param>
        /// <param name="commandText">获取或设置要对数据源执行的 Transact-SQL 语句或存储过程</param>
        /// <param name="commandType">获取或设置一个值，该值指示如何解释 CommandText 属性</param>
        /// <param name="commandParameters">用来执行命令的参数数组</param>
        /// <returns>执行命令后受影响的行数</returns>
        public static int ExecuteNonQuery(SQLiteConnection connection,
            string commandText, CommandType commandType = CommandType.Text,
            params SQLiteParameter[] commandParameters)
        {
            if (connection == null)
            {
                throw new ArgumentNullException("connection");
            }

            SQLiteCommand cmd = new SQLiteCommand();
            bool mustCloseConnection;
            PrepareCommand(cmd, connection, null, commandType, commandText, commandParameters, out mustCloseConnection);
            int result = cmd.ExecuteNonQuery();
            cmd.Parameters.Clear();

            if (mustCloseConnection)
                connection.Close();

            return result;
        }

        /// <summary>
        /// 对连接执行 Transact-SQL 语句并返回受影响的行数。
        /// </summary>
        /// <remarks>
        /// </remarks>
        /// <param name="connectionString">有效的数据库连接串</param>
        /// <param name="commandText">获取或设置要对数据源执行的 Transact-SQL 语句或存储过程</param>
        /// <param name="commandType">获取或设置一个值，该值指示如何解释 CommandText 属性</param>
        /// <param name="commandParameters">用来执行命令的参数数组</param>
        /// <returns>执行命令后受影响的行数</returns>
        public static int ExecuteNonQuery(string connectionString, string commandText,
            CommandType commandType = CommandType.Text, params SQLiteParameter[] commandParameters)
        {
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new ArgumentNullException("connectionString");
            }

            using (SQLiteConnection connection = PrepareConnection(connectionString))
            {
                connection.Open();
                int result = ExecuteNonQuery(connection, commandText, commandType, commandParameters);
                return result;
            }
        }

        /// <summary>
        /// 对连接执行 Transact-SQL 语句并返回受影响的行数。
        /// </summary>
        /// <remarks>
        /// </remarks>
        /// <param name="connectionString">有效的数据库连接串</param>
        /// <param name="commandText">获取或设置要对数据源执行的 Transact-SQL 语句或存储过程</param>
        /// <param name="commandParameters">用来执行命令的参数数组</param>
        /// <returns>执行命令后受影响的行数</returns>
        public static int ExecuteNonQuery(string connectionString, string commandText,
            params SQLiteParameter[] commandParameters)
        {
            int result = ExecuteNonQuery(connectionString, commandText, CommandType.Text, commandParameters);
            return result;
        }
        #endregion ExecuteNonQuery

        #region ExecuteDataset
        // 主调方法
        /// <summary>
        /// 对连接执行 Transact-SQL 语句并返回 DataSet 数据集。
        /// </summary>
        /// <remarks>
        /// </remarks>
        /// <param name="connection">有效的数据连接对象</param>
        /// <param name="commandText">获取或设置要对数据源执行的 Transact-SQL 语句或存储过程</param>
        /// <param name="commandType">获取或设置一个值，该值指示如何解释 CommandText 属性</param>
        /// <param name="commandParameters">用于执行命令的参数数组</param>
        /// <returns>执行命令后返回一个包含结果的数据集</returns>
        public static DataSet ExecuteDataset(SQLiteConnection connection, string commandText,
            CommandType commandType = CommandType.Text,
            params SQLiteParameter[] commandParameters)
        {
            if (connection == null)
            {
                throw new ArgumentNullException("connection");
            }

            SQLiteCommand cmd = new SQLiteCommand();
            bool mustCloseConnection;
            PrepareCommand(cmd, connection, null, commandType, commandText, commandParameters, out mustCloseConnection);

            using (var da = new SQLiteDataAdapter(cmd))
            {
                DataSet ds = new DataSet();
                da.Fill(ds);

                cmd.Parameters.Clear();
                if (mustCloseConnection)
                    connection.Close();

                return ds;
            }
        }

        /// <summary>
        /// 对连接执行 Transact-SQL 语句并返回 DataSet 数据集。
        /// </summary>
        /// <remarks>
        /// </remarks>
        /// <param name="connectionString">有效的数据库连接串</param>
        /// <param name="commandText">获取或设置要对数据源执行的 Transact-SQL 语句或存储过程</param>
        /// <param name="commandType">获取或设置一个值，该值指示如何解释 CommandText 属性</param>
        /// <param name="commandParameters">用来执行命令的参数数组</param>
        /// <returns>执行命令后返回一个包含结果的数据集</returns>
        public static DataSet ExecuteDataset(string connectionString, string commandText,
            CommandType commandType = CommandType.Text,
            params SQLiteParameter[] commandParameters)
        {
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new ArgumentNullException("connectionString");
            }

            using (SQLiteConnection connection = PrepareConnection(connectionString))
            {
                connection.Open();
                DataSet result = ExecuteDataset(connection, commandText, commandType, commandParameters);
                return result;
            }
        }

        /// <summary>
        /// 对连接执行 Transact-SQL 语句并返回 DataSet 数据集。
        /// </summary>
        /// <remarks>
        /// </remarks>
        /// <param name="connectionString">有效的数据库连接串</param>
        /// <param name="commandText">获取或设置要对数据源执行的 Transact-SQL 语句或存储过程</param>
        /// <param name="commandParameters">用来执行命令的参数数组</param>
        /// <returns>执行命令后返回一个包含结果的数据集</returns>
        public static DataSet ExecuteDataset(string connectionString, string commandText,
            params SQLiteParameter[] commandParameters)
        {
            DataSet result = ExecuteDataset(connectionString, commandText, CommandType.Text, null);
            return result;
        }

        #endregion ExecuteDataset

        #region ExecuteReader
        // 主调方法
        /// <summary>
        /// 将 CommandText 发送到 Connection 并生成一个数据读取对象。
        /// </summary>
        /// <remarks>
        /// </remarks>
        /// <param name="connection">有效的数据库连接对象</param>
        /// <param name="commandText">获取或设置要对数据源执行的 Transact-SQL 语句或存储过程</param>
        /// <param name="commandType">获取或设置一个值，该值指示如何解释 CommandText 属性</param>
        /// <param name="commandParameters">用来执行命令的参数数组</param>
        /// <returns>执行命令后返回包含结果的数据读取对象</returns>
        public static SQLiteDataReader ExecuteReader(SQLiteConnection connection, string commandText,
            CommandType commandType = CommandType.Text,
            params SQLiteParameter[] commandParameters)
        {
            if (connection == null)
            {
                throw new ArgumentNullException("connection");
            }

            bool mustCloseConnection = false;
            SQLiteCommand cmd = new SQLiteCommand();
            try
            {
                PrepareCommand(cmd, connection, null, commandType, commandText, commandParameters, out mustCloseConnection);

                SQLiteDataReader dataReader = cmd.ExecuteReader(CommandBehavior.CloseConnection);
                cmd.Parameters.Clear();

                return dataReader;
            }
            catch
            {
                if (mustCloseConnection)
                {
                    connection.Close();
                }
                throw;
            }
        }

        /// <summary>
        /// 将 CommandText 发送到 Connection 并生成一个数据读取对象。
        /// </summary>
        /// <remarks>
        /// </remarks>
        /// <param name="connectionString">有效的数据库连接串</param>
        /// <param name="commandText">获取或设置要对数据源执行的 Transact-SQL 语句或存储过程</param>
        /// <param name="commandType">获取或设置一个值，该值指示如何解释 CommandText 属性</param>
        /// <param name="commandParameters">用来执行命令的参数数组</param>
        /// <returns>执行命令后受影响的行数</returns>
        public static SQLiteDataReader ExecuteReader(string connectionString, string commandText, 
            CommandType commandType = CommandType.Text, params SQLiteParameter[] commandParameters)
        {
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new ArgumentNullException("connectionString");
            }

            SQLiteConnection connection = PrepareConnection(connectionString);
            connection.Open();
            var reader = ExecuteReader(connection, commandText, commandType, commandParameters);
            return reader;
        }

        /// <summary>
        /// 将 CommandText 发送到 Connection 并生成一个数据读取对象。
        /// </summary>
        /// <remarks>
        /// </remarks>
        /// <param name="connectionString">有效的数据库连接串</param>
        /// <param name="commandText">获取或设置要对数据源执行的 Transact-SQL 语句或存储过程</param>
        /// <param name="commandParameters">用来执行命令的参数数组</param>
        /// <returns>执行命令后受影响的行数</returns>
        public static SQLiteDataReader ExecuteReader(string connectionString, string commandText,
            params SQLiteParameter[] commandParameters)
        {
            SQLiteDataReader dataReader = ExecuteReader(connectionString, commandText, CommandType.Text, commandParameters);
            return dataReader;
        }

        #endregion ExecuteReader

        #region ExecuteScalar
        // 主调方法
        /// <summary>
        /// 执行查询，并返回查询所返回的结果集中第一行的第一列。忽略额外的列或行。
        /// </summary>
        /// <remarks>
        /// </remarks>
        /// <param name="connection">有效的数据库连接对象</param>
        /// <param name="commandType">获取或设置一个值，该值指示如何解释 CommandText 属性</param>
        /// <param name="commandText">获取或设置要对数据源执行的 Transact-SQL 语句或存储过程</param>
        /// <param name="commandParameters">用来执行命令的参数数组</param>
        /// <returns>执行命令后返回结果集中第一行的第一列的值</returns>
        public static object ExecuteScalar(SQLiteConnection connection, string commandText, 
            CommandType commandType = CommandType.Text,
            params SQLiteParameter[] commandParameters)
        {
            if (connection == null)
            {
                throw new ArgumentNullException("connection");
            }

            SQLiteCommand cmd = new SQLiteCommand();

            bool mustCloseConnection;
            PrepareCommand(cmd, connection, null, commandType, commandText, commandParameters, out mustCloseConnection);

            object retval = cmd.ExecuteScalar();

            cmd.Parameters.Clear();
            if (mustCloseConnection)
                connection.Close();

            return retval;
        }

        /// <summary>
        /// 执行查询，并返回查询所返回的结果集中第一行的第一列。忽略额外的列或行。
        /// </summary>
        /// <remarks>
        /// </remarks>
        /// <param name="connectionString">有效的数据库连接串</param>
        /// <param name="commandType">获取或设置一个值，该值指示如何解释 CommandText 属性</param>
        /// <param name="commandText">获取或设置要对数据源执行的 Transact-SQL 语句或存储过程</param>
        /// <param name="commandParameters">用来执行命令的参数数组</param>
        /// <returns>执行命令后返回结果集中第一行的第一列的值</returns>
        public static object ExecuteScalar(string connectionString, string commandText,
            CommandType commandType = CommandType.Text,
            params SQLiteParameter[] commandParameters)
        {
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new ArgumentNullException("connectionString");
            }

            using (SQLiteConnection connection = PrepareConnection(connectionString))
            {
                connection.Open();
                object result = ExecuteScalar(connection, commandText, commandType, commandParameters);
                return result;
            }
        }


        /// <summary>
        /// 执行查询，并返回查询所返回的结果集中第一行的第一列。忽略额外的列或行。
        /// </summary>
        /// <remarks>
        /// 示例:  
        ///  int orderCount = (int)ExecuteScalar(connString, "GetOrderCount", 24, 36);
        /// </remarks>
        /// <param name="connectionString">有效的数据库连接串</param>
        /// <param name="commandText">获取或设置要对数据源执行的 Transact-SQL 语句</param>
        /// <param name="parameterValues">参数对象数组，赋值为存储过程输入参数</param>
        /// <returns>执行命令后返回结果集中第一行的第一列的值</returns>
        public static object ExecuteScalar(string connectionString, string commandText,
            params SQLiteParameter[] parameterValues)
        {
            object result = ExecuteScalar(connectionString, commandText, CommandType.Text, parameterValues);
            return result;
        }

        #endregion ExecuteScalar

        #region FillDataset
        // 主调方法
        /// <summary>
        /// 在 DataSet 中添加或刷新行以匹配使用 DataSet 名称的数据源中的行，并创建一个名为 TableNames 数组表名。
        /// </summary>
        /// <remarks>
        /// </remarks>
        /// <param name="connection">有效的数据库连接对象</param>
        /// <param name="commandType">获取或设置一个值，该值指示如何解释 CommandText 属性</param>
        /// <param name="commandText">获取或设置要对数据源执行的Transact-SQL 语句或存储过程</param>
        /// <param name="dataSet">执行命令后返回包含结果集的数据集</param>
        /// <param name="tableNames">创建的表映射，该映射允许这些表通过用户自定义名（也可以为真实表名）被引用</param>
        /// <param name="commandParameters">用来执行命令的参数数组</param>
        public static void FillDataset(SQLiteConnection connection, DataSet dataSet, 
            string commandText, CommandType commandType = CommandType.Text,
            string[] tableNames = null, params SQLiteParameter[] commandParameters)
        {
            if (connection == null)
            {
                throw new ArgumentNullException("connection");
            }
            if (dataSet == null)
            {
                throw new ArgumentNullException("dataSet");
            }

            SQLiteCommand command = new SQLiteCommand();
            bool mustCloseConnection;
            PrepareCommand(command, connection, null, commandType, commandText, commandParameters, out mustCloseConnection);

            using (SQLiteDataAdapter dataAdapter = new SQLiteDataAdapter(command))
            {
                if (tableNames != null && tableNames.Length > 0)
                {
                    string tableName = "Table";
                    for (int index = 0; index < tableNames.Length; index++)
                    {
                        if (string.IsNullOrEmpty(tableNames[index]))
                        {
                            throw new ArgumentException("输入表名必须为数组列表, 无时可为null 或空字符串.", "tableNames");
                        }
                        dataAdapter.TableMappings.Add(tableName, tableNames[index]);
                        tableName += (index + 1).ToString();
                    }
                }

                dataAdapter.Fill(dataSet);

                command.Parameters.Clear();
            }

            if (mustCloseConnection)
            {
                connection.Close();
            }
        }

        /// <summary>
        /// 在 DataSet 中添加或刷新行以匹配使用 DataSet 名称的数据源中的行，并创建一个名为 TableNames 数组表名。
        /// </summary>
        /// <remarks>
        /// </remarks>
        /// <param name="connectionString">有效的数据库连接串</param>
        /// <param name="commandType">获取或设置一个值，该值指示如何解释 CommandText 属性</param>
        /// <param name="commandText">获取或设置要对数据源执行的Transact-SQL 语句或存储过程</param>
        /// <param name="dataSet">执行命令后返回包含结果集的数据集</param>
        /// <param name="tableNames">创建的表映射，该映射允许这些表通过用户自定义名（也可以为真实表名）被引用</param>
        /// <param name="commandParameters">用来执行命令的参数数组</param>
        public static void FillDataset(string connectionString, DataSet dataSet, 
            string commandText, CommandType commandType = CommandType.Text,
            string[] tableNames = null, params SQLiteParameter[] commandParameters)
        {
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new ArgumentNullException("connectionString");
            }
            if (dataSet == null)
            {
                throw new ArgumentNullException("dataSet");
            }

            using (SQLiteConnection connection = PrepareConnection(connectionString))
            {
                connection.Open();
                FillDataset(connection, dataSet, commandText, commandType, tableNames, commandParameters);
            }
        }

        /// <summary>
        /// 在 DataSet 中添加或刷新行以匹配使用 DataSet 名称的数据源中的行，并创建一个名为 TableNames 数组表名。
        /// </summary>
        /// <remarks>
        /// 示例:  
        ///  FillDataset(connString, CommandType.StoredProcedure, "GetOrders", ds, new string[] {"orders"}, new SQLiteParameter("@prodid", 24));
        /// </remarks>
        /// <param name="connectionString">有效的数据库连接串</param>
        /// <param name="commandText">获取或设置要对数据源执行的Transact-SQL 语句或存储过程</param>
        /// <param name="commandParameters">用来执行命令的参数数组</param>
        /// <param name="dataSet">执行命令后返回包含结果集的数据集</param>
        /// <param name="tableNames">创建的表映射，该映射允许这些表通过用户自定义名（也可以为真实表名）被引用</param>
        public static void FillDataset(string connectionString, DataSet dataSet, 
            string commandText,
            string[] tableNames = null, params SQLiteParameter[] commandParameters)
        {
            FillDataset(connectionString, dataSet, commandText, CommandType.Text, tableNames, commandParameters);
        }

        #endregion
    }
}

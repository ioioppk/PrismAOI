using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using SystemLib.Core;

namespace SystemLib.Services
{
    public class DatabaseService : IDatabaseOperator
    {
        private readonly string _connectionString;
        private readonly object _lock = new object();

        public DatabaseService(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        public Task<int> ExecuteAsync(string sql, object param = null)
        {
            return Task.Run(() =>
            {
                lock (_lock)
                {
                    using (var connection = new SqliteConnection(_connectionString))
                    {
                        connection.Open();
                        using (var command = connection.CreateCommand())
                        {
                            command.CommandText = sql;
                            AddParameters(command, param);
                            return command.ExecuteNonQuery();
                        }
                    }
                }
            });
        }

        public Task<T> QuerySingleAsync<T>(string sql, object param = null)
        {
            return Task.Run(() =>
            {
                lock (_lock)
                {
                    using (var connection = new SqliteConnection(_connectionString))
                    {
                        connection.Open();
                        using (var command = connection.CreateCommand())
                        {
                            command.CommandText = sql;
                            AddParameters(command, param);
                            using (var reader = command.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    return Map<T>(reader);
                                }
                                return default;
                            }
                        }
                    }
                }
            });
        }

        public Task<IEnumerable<T>> QueryAsync<T>(string sql, object param = null)
        {
            return Task.Run(() =>
            {
                lock (_lock)
                {
                    var results = new List<T>();
                    using (var connection = new SqliteConnection(_connectionString))
                    {
                        connection.Open();
                        using (var command = connection.CreateCommand())
                        {
                            command.CommandText = sql;
                            AddParameters(command, param);
                            using (var reader = command.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    results.Add(Map<T>(reader));
                                }
                            }
                        }
                    }
                    return (IEnumerable<T>)results;
                }
            });
        }

        public Task<int> InsertAsync(string table, object data)
        {
            var props = data.GetType().GetProperties();
            var columns = new List<string>();
            var values = new List<string>();
            var parameters = new Dictionary<string, object>();

            foreach (var prop in props)
            {
                var value = prop.GetValue(data);
                if (value != null)
                {
                    columns.Add(prop.Name);
                    var paramName = "@" + prop.Name;
                    values.Add(paramName);
                    parameters[paramName] = value;
                }
            }

            var sql = $"INSERT INTO {table} ({string.Join(", ", columns)}) VALUES ({string.Join(", ", values)})";
            return ExecuteAsync(sql, parameters);
        }

        public Task<int> UpdateAsync(string table, object data, string where)
        {
            var props = data.GetType().GetProperties();
            var setClauses = new List<string>();
            var parameters = new Dictionary<string, object>();

            foreach (var prop in props)
            {
                var value = prop.GetValue(data);
                if (value != null)
                {
                    var paramName = "@" + prop.Name;
                    setClauses.Add($"{prop.Name} = {paramName}");
                    parameters[paramName] = value;
                }
            }

            var sql = $"UPDATE {table} SET {string.Join(", ", setClauses)} WHERE {where}";
            return ExecuteAsync(sql, parameters);
        }

        private void AddParameters(SqliteCommand command, object param)
        {
            if (param == null) return;

            if (param is Dictionary<string, object> dict)
            {
                foreach (var kvp in dict)
                {
                    command.Parameters.AddWithValue(kvp.Key, kvp.Value ?? DBNull.Value);
                }
            }
            else
            {
                foreach (var prop in param.GetType().GetProperties())
                {
                    var value = prop.GetValue(param);
                    command.Parameters.AddWithValue("@" + prop.Name, value ?? DBNull.Value);
                }
            }
        }

        private T Map<T>(SqliteDataReader reader)
        {
            var type = typeof(T);

            // Handle simple types
            if (type == typeof(int) || type == typeof(long) || type == typeof(string)
                || type == typeof(double) || type == typeof(float) || type == typeof(bool)
                || type == typeof(DateTime) || type == typeof(byte[]))
            {
                var value = reader.GetValue(0);
                if (value == DBNull.Value) return default;
                return (T)Convert.ChangeType(value, type);
            }

            // Handle complex types via property mapping
            var instance = Activator.CreateInstance<T>();
            var properties = type.GetProperties();

            for (int i = 0; i < reader.FieldCount; i++)
            {
                var columnName = reader.GetName(i);
                foreach (var prop in properties)
                {
                    if (string.Equals(prop.Name, columnName, StringComparison.OrdinalIgnoreCase) && prop.CanWrite)
                    {
                        var value = reader.GetValue(i);
                        if (value != DBNull.Value)
                        {
                            prop.SetValue(instance, Convert.ChangeType(value, prop.PropertyType));
                        }
                    }
                }
            }

            return instance;
        }
    }
}
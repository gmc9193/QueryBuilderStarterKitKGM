using Microsoft.Data.Sqlite;
using System.Reflection;
using System.Text;

namespace QueryBuilderStarter
{
    public class QueryBuilder : IDisposable
    {
        SqliteConnection? connection;

        public QueryBuilder(string dbPath)
        {
            connection = new SqliteConnection($"Data Source={ dbPath }");
            connection.Open();
        }

        /// <summary>
        /// Read command to retrieve a single record given a property called 'Id'
        /// - Data types that are in the DateTime format, SQLite has to store them in YYYY-MM-DD format
        /// - If not, null will be stored
        /// </summary>
        /// <param name="id">The Id value of the object's corresponding DB record</param>
        /// <returns>An object of type T</returns>
        public T Read<T> (int id) where T : new()
        {
            var command = connection!.CreateCommand();
            command.CommandText = $"SELECT * FROM {typeof(T).Name} where Id = {id}";

            var reader = command.ExecuteReader();

            T data = new T();

            while(reader.Read())
            {
                for(int i = 0; i < reader.FieldCount; i++)
                {
                    // convert integer data to int data type from SQLite's int64 default
                    if(typeof(T).GetProperty(reader.GetName(i)).PropertyType == typeof(int))
                    {
                        typeof(T).GetProperty(reader.GetName(i)).SetValue(data, Convert.ToInt32(reader.GetValue(i)));
                    }
                    // if datetime, format the string correctly
                    else if(typeof(T).GetProperty(reader.GetName(i)).PropertyType == typeof(DateTime) && reader.GetValue(i).ToString().Split('-').Length ==  3)
                    {
                        string[] dateString = reader.GetValue(i).ToString().Split('-');
                        int[] dateNum = new int[3];
                        for(int k = 0; k < 3; k++)
                        {
                            dateNum[k] = Convert.ToInt32(dateString[k]);
                        }
                        var dateTime = new DateTime(dateNum[0], dateNum[1], dateNum[2]);
                        typeof(T).GetProperty(reader.GetName(i)).SetValue(data, dateTime);
                    }
                    // other data types
                    else
                    {
                        typeof(T).GetProperty(reader.GetName(i)).SetValue(data, reader.GetValue(i));
                    }
                }
            }
            return data;
        }

        /// <summary>
        /// ReadAll operation to retrieve all records from a SQLite table
        /// The same data type concerns from Read apply here, as well
        /// </summary>
        /// <returns>A list of T objects</returns>
        public List<T> ReadAll<T>() where T : new()
        {
            var command = connection.CreateCommand();
            command.CommandText = $"SELECT * FROM {typeof(T).Name}";
            var reader = command.ExecuteReader();

            T data;
            var dataList = new List<T>();

            while(reader.Read())
            {
                data = new T();
                for(int i = 0; i < reader.FieldCount; i++)
                {
                    // same code as the Read

                    // convert integer data to int data type from SQLite's int64 default
                    if (typeof(T).GetProperty(reader.GetName(i)).PropertyType == typeof(int))
                    {
                        typeof(T).GetProperty(reader.GetName(i)).SetValue(data, Convert.ToInt32(reader.GetValue(i)));
                    }
                    // if datetime, format the string correctly
                    else if (typeof(T).GetProperty(reader.GetName(i)).PropertyType == typeof(DateTime) && reader.GetValue(i).ToString().Split('-').Length == 3)
                    {
                        string[] dateString = reader.GetValue(i).ToString().Split('-');
                        int[] dateNum = new int[3];
                        for (int k = 0; k < 3; k++)
                        {
                            dateNum[k] = Convert.ToInt32(dateString[k]);
                        }
                        var dateTime = new DateTime(dateNum[0], dateNum[1], dateNum[2]);
                        typeof(T).GetProperty(reader.GetName(i)).SetValue(data, dateTime);
                    }
                    // other data types
                    else
                    {
                        typeof(T).GetProperty(reader.GetName(i)).SetValue(data, reader.GetValue(i));
                    }
                }
                dataList.Add(data);
            }
            return dataList;
        }

        public void Create<T>(T obj) where T : IClassModel
        {
            
            PropertyInfo[] properties = typeof(T).GetProperties();

           
            var values = new List<string>();
            var names = new List<string>();
            PropertyInfo property;

            for (int i = 0; i < properties.Length; i++)
            {
                property = properties[i];

              
                if (property.Name.Equals("Id", StringComparison.OrdinalIgnoreCase))
                    continue;

                // format DateTime for DB (reverse the process in Read)
                if (property.PropertyType == typeof(DateTime))
                {
                    values.Add($"\"{((DateTime)property.GetValue(obj)).Year}-{((DateTime)property.GetValue(obj)).Month}-{((DateTime)property.GetValue(obj)).Day}\"");
                }
                // format string for DB
                else if (property.PropertyType == typeof(string))
                {
                    values.Add($"\"{property.GetValue(obj)?.ToString()}\"");
                }
               
                else
                {
                    values.Add(property.GetValue(obj)?.ToString());
                }

                names.Add(property.Name);
            }

            var namesJoined = string.Join(", ", names);
            var valuesJoined = string.Join(", ", values);

            var command = connection.CreateCommand();
            command.CommandText = $"INSERT INTO {typeof(T).Name} ({namesJoined}) VALUES ({valuesJoined})";
            command.ExecuteNonQuery();
        }

        /// <summary>
        /// Update operation to update a record in the SQLite database
        /// </summary>
        /// <param name="obj">Object of type T to be updated (its Id param will be used to find it)</param>
        public void Update<T>(T obj) where T : IClassModel
        {
            // this code will be the same as Create, minus the SQL command
            // get the property names of the object 
            PropertyInfo[] properties = typeof(T).GetProperties();

            // get property values
            var values = new List<string>();
            var names = new List<string>();
            PropertyInfo property;

            for (int i = 1; i < properties.Length; i++)
            {
                property = properties[i];
                // format DateTime for DB (reverse the process in Read)
                // quotation marks are necessary for the date format
                if (property.PropertyType == typeof(DateTime))
                {
                    values.Add($"\"{((DateTime)property.GetValue(obj)).Year}-{((DateTime)property.GetValue(obj)).Month}-{((DateTime)property.GetValue(obj)).Day}\"");
                }
                // format string for DB
                // quotation marks are necessary for the text format
                else if (property.PropertyType == typeof(string))
                {
                    values.Add($"\"{property.GetValue(obj).ToString()}\"");
                }
                // format other data types for DB
                else
                {
                    values.Add(property.GetValue(obj).ToString());
                }
                names.Add(property.Name);
            }

            // build the insert statement
            StringBuilder sb = new StringBuilder();

            // include commas in between values / names UNLESS you're on the last item
            for (int i = 0; i < values.Count; i++)
            {
                if (i == values.Count - 1)
                {
                    sb.Append($"{names[i]} = {values[i]}");
                }
                else
                {
                    sb.Append($"{names[i]} = {values[i]}, ");
                }
            }

            var command = connection.CreateCommand();
            command.CommandText = $"UPDATE {typeof(T).Name} SET {sb} WHERE Id = {obj.Id}";
            var reader = command.ExecuteNonQuery();
        }

        /// <summary>
        /// Delete operation to remove the parameter object from the SQLite database
        /// </summary>
        /// <param name="obj">Object of type T to be deleted</param>
        public void Delete<T>(T obj) where T : IClassModel
        {
            var command = connection.CreateCommand();
            command.CommandText = $"DELETE FROM {typeof(T).Name} WHERE Id = {obj.Id}";
            var reader = command.ExecuteNonQuery();
        }

        public void DeleteAll<T>()
        {
            var command = connection.CreateCommand();
            command.CommandText = $"DELETE FROM {typeof(T).Name}";
            command.ExecuteNonQuery();
        }

        /// <summary>
        /// Closes resources committed to reading SQLite file
        /// (required for a 'using' block)
        /// </summary>
        public void Dispose()
        {
            connection!.Dispose();
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            string dbPath = "data.db";

            List<Pokemon> pokemons = new List<Pokemon>
        {
            new Pokemon { Id = 1, Name = "Bulbasaur" },
          .
        };

            List<BannedGame> bannedGames = new List<BannedGame>
        {
            new BannedGame { Id = 1, Title = "50 Cent: Bulletproof" },
        };

            using (var builder = new QueryBuilderStarter.QueryBuilder(dbPath))
            {
                // Erase all records of Pokemon from the database
                builder.DeleteAll<Pokemon>();
                Console.WriteLine("All Pokemon records have been deleted.");

                // Write all objects in the Pokemon collection to the database
                foreach (var pokemon in pokemons)
                {
                    builder.Create(pokemon);
                }
                Console.WriteLine("All Pokemon records were added.");

                // Similarly, for BannedGame objects
                builder.DeleteAll<BannedGame>();
                Console.WriteLine("All BannedGame records have been deleted.");

                foreach (var game in bannedGames)
                {
                    builder.Create(game);
                }
                Console.WriteLine("All BannedGame records were added.");

                // Write a single object to the database
                Pokemon singlePokemon = new Pokemon { Id = 100, Name = "Voltorb" };
                builder.Create(singlePokemon);
                Console.WriteLine("Single Pokemon record has been added.");

                BannedGame singleGame = new BannedGame { Id = 100, Title = "RapeLay" };
                builder.Create(singleGame);
                Console.WriteLine("Single BannedGame record has been added.");
            }
        }

    }
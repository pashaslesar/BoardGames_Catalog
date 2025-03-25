using System;
using System.Collections;
using System.Data.SQLite;
using DataModels;


namespace DataControll
{
    public class DBController
    {

        private static string connectionString = "Data Source=BoardDataBase.db; Version=3;";

        public static List<string> GetGenres()
        {
            Console.WriteLine("Database full path: " + System.IO.Path.GetFullPath("BoardDataBase.db"));

            List<string> genres = new List<string>();

            try
            {
                using (var connection = new SQLiteConnection(connectionString))
                {
                    Console.WriteLine("Database path: " + connectionString);

                    connection.Open();
                    using (var command = new SQLiteCommand("SELECT Name FROM Genres", connection))
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string genre = reader.GetString(0);
                            genres.Add(genre);
                            Console.WriteLine("Loaded genre: " + genre);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error loading genres: " + ex.Message);
            }

            return genres;
        }

        public static List<Game> GetGames()
        {
            Console.WriteLine("Database full path: " + System.IO.Path.GetFullPath("BoardDataBase.db"));

            List<Game> games = new List<Game>();

            try
            {
                using (var connection = new SQLiteConnection(connectionString))
                {
                    Console.WriteLine("Database path: " + connectionString);
                    connection.Open();

                    string query = "SELECT Id, Name, GenreId, AuthorId, MinPlayers, MaxPlayers, PlayTime, Price, ImagePath, Age FROM Games";

                    using (var command = new SQLiteCommand(query, connection))
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Game game = new Game
                            {
                                Id = reader.GetInt32(0),
                                Name = reader.GetString(1),
                                GenreId = reader.GetInt32(2),
                                AuthorId = reader.GetInt32(3),
                                MinPlayers = reader.GetInt32(4),
                                MaxPlayers = reader.GetInt32(5),
                                PlayTime = reader.GetInt32(6),
                                Price = reader.GetInt32(7),
                                ImagePath = reader.IsDBNull(8) ? null : reader.GetString(8), // !
                                Age = reader.GetInt32(9)
                            };

                            games.Add(game);
                            Console.WriteLine($"Loaded game: {game.Name}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error loading games: " + ex.Message);
            }

            return games;
        }

        public static List<string> GetGenreNamesByGameId(int gameId)
        {
            List<string> genreNames = new List<string>();

            try
            {
                using (var connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();

                    string query = @"
                        SELECT g.Name 
                        FROM GameCategories gc
                        JOIN Genres g ON gc.GenreId = g.Id
                        WHERE gc.GameId = @gameId";

                    using (var command = new SQLiteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@gameId", gameId);

                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                genreNames.Add(reader.GetString(0));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error getting genres: " + ex.Message);
            }

            return genreNames;
        }

        public static int GetGenreIdByName(string genreName)
        {
            try
            {
                using var connection = new SQLiteConnection(connectionString);
                connection.Open();

                string query = "SELECT Id FROM Genres WHERE Name = @name";
                using var command = new SQLiteCommand(query, connection);
                command.Parameters.AddWithValue("@name", genreName);

                object result = command.ExecuteScalar();
                return result != null ? Convert.ToInt32(result) : -1;
            }
            catch
            {
                return -1;
            }
        }

        public static List<int> GetGameIdsByGenreId(int genreId)
        {
            List<int> gameIds = new();

            try
            {
                using var connection = new SQLiteConnection(connectionString);
                connection.Open();

                string query = "SELECT GameId FROM GameCategories WHERE GenreId = @genreId";
                using var command = new SQLiteCommand(query, connection);
                command.Parameters.AddWithValue("@genreId", genreId);

                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    gameIds.Add(reader.GetInt32(0));
                }
            }
            catch { }

            return gameIds;
        }

        public static string GetAuthorNameById(int authorId)
        {
            try
            {
                using var connection = new SQLiteConnection(connectionString);
                connection.Open();

                string query = "SELECT Name FROM Authors WHERE Id = @id";
                using var command = new SQLiteCommand(query, connection);
                command.Parameters.AddWithValue("@id", authorId);

                object result = command.ExecuteScalar();
                return result != null ? result.ToString() : "Neznámý autor";
            }
            catch
            {
                return "Neznámý autor";
            }
        }

        public static List<int> GetGenreIdsByGameId(int gameId)
        {
            List<int> genreIds = new();

            try
            {
                using var connection = new SQLiteConnection(connectionString);
                connection.Open();

                string query = "SELECT GenreId FROM GameCategories WHERE GameId = @gameId";
                using var command = new SQLiteCommand(query, connection);
                command.Parameters.AddWithValue("@gameId", gameId);

                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    genreIds.Add(reader.GetInt32(0));
                }
            }
            catch { }

            return genreIds;
        }

        public static int InsertOrGetAuthorId(string name, string country)
        {
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                var checkCommand = new SQLiteCommand("SELECT Id FROM Authors WHERE Name = @name", connection);
                checkCommand.Parameters.AddWithValue("@name", name);

                var result = checkCommand.ExecuteScalar();
                if (result != null)
                {
                    return Convert.ToInt32(result);
                }

                var insertCommand = new SQLiteCommand("INSERT INTO Authors (Name, Country) VALUES (@name, @country); SELECT last_insert_rowid();", connection);
                insertCommand.Parameters.AddWithValue("@name", name);
                insertCommand.Parameters.AddWithValue("@country", country);

                long insertedId = (long)insertCommand.ExecuteScalar();
                return (int)insertedId;
            }
        }

        public static int InsertGame(Game game)
        {
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                var command = new SQLiteCommand(@"
                        INSERT INTO Games 
                        (Name, GenreId, AuthorId, MinPlayers, MaxPlayers, PlayTime, Price, ImagePath, Age) 
                        VALUES 
                        (@name, @genreId, @authorId,
                        @minPlayers, @maxPlayers, @playTime, @price, @imagePath, @age);
                        SELECT last_insert_rowid();", connection);

                command.Parameters.AddWithValue("@name", game.Name);
                command.Parameters.AddWithValue("@genreId", game.GenreId);
                command.Parameters.AddWithValue("@authorId", game.AuthorId);
                command.Parameters.AddWithValue("@minPlayers", game.MinPlayers);
                command.Parameters.AddWithValue("@maxPlayers", game.MaxPlayers);
                command.Parameters.AddWithValue("@playTime", game.PlayTime);
                command.Parameters.AddWithValue("@price", game.Price);
                command.Parameters.AddWithValue("@imagePath", game.ImagePath);
                command.Parameters.AddWithValue("@age", game.Age);

                long insertedId = (long)command.ExecuteScalar();
                return (int)insertedId;
            }
        }


        public static void AddGameGenre(int gameId, int genreId)
        {
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                var command = new SQLiteCommand("INSERT INTO GameCategories (GameId, GenreId) VALUES (@gameId, @genreId)", connection);
                command.Parameters.AddWithValue("@gameId", gameId);
                command.Parameters.AddWithValue("@genreId", genreId);

                command.ExecuteNonQuery();
            }
        }
    }
}

using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.IO;

namespace WordOfTheDay
{

    public class DBInterface
    {
        private SqliteConnection conn;

        private static DBInterface _instance;
        private DBInterface()
        {
            string DBpath = "StudySessionTracker.db";
            if (!File.Exists(DBpath)) File.Create(DBpath);
            conn = new SqliteConnection("Data Source=" + DBpath);
            conn.Open();
        }
        public static DBInterface Instance
        {
            get => _instance ?? (_instance ??= new DBInterface());
        }

        #region adders
        public void AddUser(string ID, string name)
        {
            using (SqliteCommand command = conn.CreateCommand())
            {
                command.CommandText = @"INSERT INTO users VALUES (@ID, @name)";
                command.Parameters.AddWithValue("@ID", ID);
                command.Parameters.AddWithValue("@name", name);
                command.ExecuteNonQuery();
            }
        }
        public void AddTime(string ID, string subject, DateTime starttime, DateTime endtime)
        {
            using (SqliteCommand command = conn.CreateCommand())
            {
                command.CommandText = @"INSERT INTO Study_WorkSheet (UserID, Subject, Starttime, EndTime)
                                        VALUES 	(@userid, @subject, @starttime, @endtime)";
                command.Parameters.AddWithValue("@userid", ID);
                command.Parameters.AddWithValue("@subject", subject);
                command.Parameters.AddWithValue("@starttime", starttime.ToString("s"));
                command.Parameters.AddWithValue("@endtime", endtime.ToString("s"));
                command.ExecuteNonQuery();
            }
        }
        #endregion

        #region dleters
        public void DeleteTime(string ID)
        {
            using (SqliteCommand command = conn.CreateCommand())
            {
                command.CommandText = @"DELETE FROM Study_WorkSheet WHERE reg_ID = @id"; //NTODPEWEEDF
                command.Parameters.AddWithValue("@id", ID);
                command.ExecuteNonQuery();
            }
        }
        public void DeleteTime(string ID, bool deletetimes)
        {
            using (SqliteCommand command = conn.CreateCommand())
            {
                command.CommandText = @"DELETE FROM users WHERE ID = @id";
                command.Parameters.AddWithValue("@id", ID);
                command.ExecuteNonQuery();
                if (deletetimes)
                {
                    command.CommandText = @"DELETE FROM Study_WorkSheet WHERE UserID = '" + ID + "'";
                    command.ExecuteNonQuery();
                }
            }
        }
        #endregion

        #region executables
        public string Exec(string sql)
        {
            using (SqliteCommand command = conn.CreateCommand())
            {
                try
                {
                    command.CommandText = sql;
                    return "Rows affected = " + command.ExecuteNonQuery();
                }
                catch (Exception e)
                {
                    return e.Message;
                }
            }
        }
        public string ExecQuery(string sql)
        {
            try
            {
                String salida = "";
                using (SqliteCommand command = conn.CreateCommand())
                {
                    command.CommandText = sql;
                    using (SqliteDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                salida += reader.GetString(i) + " | ";
                            }
                            salida += "\n";
                        }
                    }
                }
                return salida;
            }
            catch (Exception e)
            {
                return e.Message;
            }
        }
        #endregion

        #region queries
        public string GetUserByID(string ID)
        {
            using (SqliteCommand command = conn.CreateCommand())
            {
                command.CommandText = @"SELECT name FROM users WHERE id = @id";
                command.Parameters.AddWithValue("@id", ID);
                using (SqliteDataReader reader = command.ExecuteReader())
                {
                    reader.Read();
                    if (reader.HasRows)
                    {
                        return reader.GetString(0);
                    }
                    else
                    {
                        return "";
                    }
                }
            }
        }

        public TimeSpan GetHoursByID(string ID)
        {
            using (SqliteCommand command = conn.CreateCommand())
            {
                //TODO evitar que usuarios que no existen den execpciones
                command.CommandText = @"SELECT Userid, CAST(SUM(((julianday(EndTime) - julianday(Starttime)) * 1440)) AS integer) AS Hours FROM study_worksheet WHERE userid = @id";
                command.Parameters.AddWithValue("@id", ID);
                using (SqliteDataReader reader = command.ExecuteReader())
                {
                    reader.Read();
                    try
                    {
                        return TimeSpan.FromMinutes(reader.GetDouble(1));
                    }
                    catch (Exception)
                    {
                        //So tara can stop trying to break my bot
                        return TimeSpan.Zero;
                    }
                }
            }
        }

        public Dictionary<string, TimeSpan> GetRanking()
        {
            Dictionary<string, TimeSpan> dict = new Dictionary<string, TimeSpan>();
            using (SqliteCommand command = conn.CreateCommand())
            {
                //TODO refinar query para dar datos mas precisos
                command.CommandText = @"SELECT users.Name, CAST(SUM(((julianday(study_worksheet.EndTime) - julianday(study_worksheet.Starttime)) * 24)) AS integer) AS Hours FROM study_worksheet INNER JOIN users ON study_worksheet.UserID=users.ID GROUP BY userid ORDER BY HOURS DESC;";
                using (SqliteDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        dict.Add(reader.GetString(0), TimeSpan.FromHours(reader.GetDouble(1)));
                    }
                    return dict;
                }
            }
        }

        public TimeSpan GetTotalHours()
        {
            using (SqliteCommand command = conn.CreateCommand())
            {
                //TODO refinar query para dar datos mas precisos
                command.CommandText = @"SELECT CAST(SUM(((julianday(EndTime) - julianday(Starttime)) * 24)) AS integer) AS 'TOTAL HOURS' FROM study_worksheet;";
                using (SqliteDataReader reader = command.ExecuteReader())
                {
                    reader.Read();
                    return TimeSpan.FromHours(reader.GetDouble(0));
                }
            }
        }

        #endregion
        
        public void ClosecConn() => conn.Close();
    }

}

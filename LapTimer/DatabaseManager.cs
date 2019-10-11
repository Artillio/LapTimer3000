using System.Data.SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows;
using System.Collections.ObjectModel;

namespace LapTimer
{
    class DatabaseManager
    {
        private const string dataSource = "Data Source=.\\LapTimer3001.db;";
        internal SQLiteConnection connection;

        public DatabaseManager()
        {
            CreateConnection();
        }

        private void CreateConnection()
        {
            try
            {
                connection = new SQLiteConnection(dataSource);
                connection.Open();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
                using (EventLog eventLog = new EventLog("Application"))
                {
                    eventLog.Source = "Application";
                    eventLog.WriteEntry(e.Message, EventLogEntryType.Error);
                }
            }
        }

        public void CloseConnection()
        {
            connection.Close();
        }

        public void Delete_Player_Queue(DataGrid dataGrid_Player_Queue, Player current_Player)
        {
            try
            {
                if (dataGrid_Player_Queue.SelectedItems.Count == 1)
                {
                    DataGridRow dgr = dataGrid_Player_Queue.ItemContainerGenerator.ContainerFromItem(dataGrid_Player_Queue.SelectedItem) as DataGridRow;
                    current_Player = new Player();
                    current_Player = (Player)dgr.Item;

                    if (connection.State == System.Data.ConnectionState.Closed)
                        connection.Open();

                    using (SQLiteCommand command = new SQLiteCommand("delete from Queue where ID_User = @ID_User", connection))
                    {
                        command.Parameters.AddWithValue("@ID_User", current_Player.ID);
                        command.ExecuteNonQuery();
                    }

                    MessageBox.Show("Eliminato la corsa di " + current_Player.Name + " " + current_Player.Surname, "Alert", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                Console.Write(ex);
                using (EventLog eventLog = new EventLog("Application"))
                {
                    eventLog.Source = "Application";
                    eventLog.WriteEntry(ex.Message, EventLogEntryType.Error);
                }
            }
        }

        public int Check_Existing_Player(String Name, String Surname)
        {
            try
            {
                if (connection.State == System.Data.ConnectionState.Closed)
                    connection.Open();

                SQLiteCommand command = new SQLiteCommand("select ID from Player where Name = @Name and Surname = @Surname", connection);
                command.Parameters.AddWithValue("@Name", Name);
                command.Parameters.AddWithValue("@Surname", Surname);
                SQLiteDataReader reader = command.ExecuteReader();

                if (reader.Read())
                    return Convert.ToInt32(reader["ID"]);
                return 0;
            }
            catch (Exception e)
            {
                Console.Write(e);
                using (EventLog eventLog = new EventLog("Application"))
                {
                    eventLog.Source = "Application";
                    eventLog.WriteEntry(e.Message, EventLogEntryType.Error);
                }
                return 0;
            }
        }

        public void Add_New_Player(string name, string surname, string Contact, string Age)
        {
            if (connection.State == System.Data.ConnectionState.Closed)
                connection.Open();

            using (SQLiteCommand command = new SQLiteCommand("insert into Player(Name, Surname, Contact, Age) values (@Name, @Surname, @Contact, @Age)", connection))
            {
                command.Parameters.AddWithValue("@Name", name);
                command.Parameters.AddWithValue("@Surname", surname);
                command.Parameters.AddWithValue("@Contact", Contact);
                command.Parameters.AddWithValue("@Age", Age);
                command.ExecuteNonQuery();
            }
        }

        public void Add_Player_In_Queue(int ID_User, string Number_Race, bool Paid)
        {
            if (connection.State == System.Data.ConnectionState.Closed)
                connection.Open();

            using (SQLiteCommand command = new SQLiteCommand("insert into Queue(ID_User, Number_Race, Paid) values (@ID_User, @Number_Race, @Paid)", connection))
            {
                command.Parameters.AddWithValue("@ID_User", ID_User);
                command.Parameters.AddWithValue("@Number_Race", Number_Race);
                if (Paid == true)
                    command.Parameters.AddWithValue("@Paid", 1);
                else
                    command.Parameters.AddWithValue("@Paid", 0);
                command.ExecuteNonQuery();
            }
        }

        public void Save_Best_Time_Lap(int ID_User, string Time_Score)
        {
            if (connection.State == System.Data.ConnectionState.Closed)
                connection.Open();

            using (SQLiteCommand command = new SQLiteCommand("insert into Ranking(ID_User, Time_Score) values (@ID_User, @Time_Score)", connection))
            {
                command.Parameters.AddWithValue("@ID_User", ID_User);
                command.Parameters.AddWithValue("@Time_Score", Time_Score);
                command.ExecuteNonQuery();
            }
        }

        public void Save_Time_Lap(int ID_User, string Time_Score)
        {
            if (connection.State == System.Data.ConnectionState.Closed)
                connection.Open();

            using (SQLiteCommand command = new SQLiteCommand("insert into Time_Story(ID_Player, Time) values (@ID_Player, @Time)", connection))
            {
                command.Parameters.AddWithValue("@ID_Player", ID_User);
                command.Parameters.AddWithValue("@Time", Time_Score);
                command.ExecuteNonQuery();
            }
        }

        public ObservableCollection<Player> Retrieve_Player_Queue()
        {
            try
            {
                if (connection.State == System.Data.ConnectionState.Closed)
                    connection.Open();

                ObservableCollection<Player> player = new ObservableCollection<Player>();

                SQLiteCommand command = new SQLiteCommand("select P.ID, P.Name, P.Surname, Q.Number_Race, Q.Paid from Player P, Queue Q where P.ID = Q.ID_User order by Q.Datetime", connection);
                SQLiteDataReader reader = command.ExecuteReader();

                while (reader.Read())
                    player.Add(new Player { ID = Convert.ToInt32(reader["ID"]), Name = reader["Name"].ToString(), Surname = reader["Surname"].ToString(), Number_Race = Convert.ToInt32(reader["Number_Race"]), Paid = Convert.ToInt32(reader["Paid"]) });

                reader.Close();
                return player;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                using (EventLog eventLog = new EventLog("Application"))
                {
                    eventLog.Source = "Application";
                    eventLog.WriteEntry(e.Message, EventLogEntryType.Error);
                }
                return null;
            }
        }

        public ObservableCollection<Player> Retrieve_Player_Ranking()
        {
            try
            {
                if (connection.State == System.Data.ConnectionState.Closed)
                    connection.Open();

                ObservableCollection<Player> player = new ObservableCollection<Player>();

                SQLiteCommand command = new SQLiteCommand("select ID, Name, Surname, Time_Score from Player, Ranking where ID = ID_User order by Time_Score", connection);
                SQLiteDataReader reader = command.ExecuteReader();

                while (reader.Read())
                    player.Add(new Player { ID = Convert.ToInt32(reader["ID"]), Name = reader["Name"].ToString(), Surname = reader["Surname"].ToString(), Time = reader["Time_Score"].ToString() });

                reader.Close();
                return player;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                using (EventLog eventLog = new EventLog("Application"))
                {
                    eventLog.Source = "Application";
                    eventLog.WriteEntry(e.Message, EventLogEntryType.Error);
                }
                return null;
            }
        }
    }
}

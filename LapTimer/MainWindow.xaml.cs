﻿using System;
using System.Activities.Statements;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.SQLite;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.IO.Ports;
using System.Data;
using System.Media;
using System.IO;

namespace LapTimer
{
    public class Player
    {
        public int ID { get; set; }
        public String Name { get; set; }
        public String Surname { get; set; }
        public String Time { get; set; }
        public int Age { get; set; }
        public int Paid { get; set; }
        public int Number_Race { get; set; }
    }

    /// <summary>
    /// Logica di interazione per MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        int sem = 0;                        // selettore dei semafori da accendere
        long race_time = 0;
        long lap_time = 0, best_lap = 0;
        bool found = false;                 // true=macchinina presente, false altrimenti
        private const string dataSource = "Data Source=C:\\Users\\artil\\Documents\\GitHub\\LapTimer3000\\LapTimer\\LapTimer3001.db;";
        //private const string dataSource = "Data Source=C:\\Users\\Lorenzo\\Desktop\\Macchinine sagra\\LapTimer3000\\LapTimer\\LapTimer3001.db;";
        internal SQLiteConnection connection;
        SerialPort serialPort;
        Stopwatch stopWatch_lap, stopWatch_race;    // real-time timer
        DispatcherTimer timer_giro, timercorsa; // timer per la visualizzazione il giro corrente
        Player current_Player;

        public MainWindow()
        {
            CreateConnection();
            InitializeComponent();
            Fill_DataGrid_Ranking();
            Fill_DataGrid_Queue();

            //btn_StartRace.IsEnabled = false;  // da inserire in release
            // cerco le porte seriali utilizzate e le elenco nel ComboBox
            String[] ports = SerialPort.GetPortNames();
            comboBox_COM.ItemsSource = ports;
        }

        /*---------------------------- FUNZIONI PER IL DATABASE --------------------------------------------*/

        private void Fill_DataGrid_Ranking()
        {
            dataGrid_Ranking.ItemsSource = Retrieve_Player_Ranking();
        }
        private void Fill_DataGrid_Queue()
        {
            dataGrid_Player_Queue.ItemsSource = Retrieve_Player_Queue();
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

        /// <summary>
        /// Function to Close a Connection
        /// </summary>
        public void CloseConnection()
        {
            //connection.Close();
        }

        private ObservableCollection<Player> Retrieve_Player_Queue()
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
            finally
            {
                CloseConnection();
            }
        }

        public ObservableCollection<Player> Retrieve_Player_Ranking()
        {
            try
            {
                if (connection.State == System.Data.ConnectionState.Closed)
                    connection.Open();
                ObservableCollection<Player> player = new ObservableCollection<Player>();


                SQLiteCommand command = new SQLiteCommand("select Name, Surname, Time_Score from Player, Ranking where ID = ID_User order by Time_Score", connection);
                SQLiteDataReader reader = command.ExecuteReader();

                while (reader.Read())
                    player.Add(new Player { Name = reader["Name"].ToString(), Surname = reader["Surname"].ToString(), Time = reader["Time"].ToString() });


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
            finally
            {
                CloseConnection();
            }
        }

        private int Check_Existing_Player(String Name, String Surname)
        {
            try
            {
                if (connection.State == System.Data.ConnectionState.Closed)
                    connection.Open();

                SQLiteCommand command = new SQLiteCommand("select ID from Player where Name = @Name AND Surname = @Surname", connection);
                command.Parameters.AddWithValue("@Name", Name);
                command.Parameters.AddWithValue("@Surname", Surname);
                SQLiteDataReader reader = command.ExecuteReader();

                if (reader != null)
                {
                    if (reader.Read())
                        return Convert.ToInt32(reader["ID"]);
                    else
                        return 0;
                }
                else
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
            finally
            {
                CloseConnection();
            }
        }

        private void Btn_AddPlayer_Click(object sender, RoutedEventArgs e)
        {
            int ID_User = Check_Existing_Player(txt_Name.Text, txt_Surname.Text);

            if (ID_User == 0)
            {
                if (connection.State == System.Data.ConnectionState.Closed)
                    connection.Open();

                using (SQLiteCommand command = new SQLiteCommand("insert into Player(Name, Surname, Contact, Age) values (@Name, @Surname, @Contact, @Age)", connection))
                {
                    command.Parameters.AddWithValue("@Name", txt_Name.Text);
                    command.Parameters.AddWithValue("@Surname", txt_Surname.Text);
                    command.Parameters.AddWithValue("@Contact", txt_Contact.Text);
                    command.Parameters.AddWithValue("@Age", txt_Age.Text);
                    command.ExecuteNonQuery();

                    CloseConnection();

                    ID_User = Check_Existing_Player(txt_Name.Text, txt_Surname.Text);
                }


            }

            if (connection.State == System.Data.ConnectionState.Closed)
                connection.Open();
            using (SQLiteCommand command = new SQLiteCommand("insert into Queue(ID_User, Number_Race, Paid) values (@ID_User, @Number_Race, @Paid)", connection))
            {
                command.Parameters.AddWithValue("@ID_User", ID_User);
                command.Parameters.AddWithValue("@Number_Race", txt_Number_Race.Text);
                if (radioButton_Paid.IsChecked == true)
                    command.Parameters.AddWithValue("@Paid", 1);
                else
                    command.Parameters.AddWithValue("@Paid", 0);
                command.ExecuteNonQuery();
            }

            CloseConnection();

            Fill_DataGrid_Queue();


        }

        private void Btn_Delete_Player_Queue_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (dataGrid_Player_Queue.SelectedItems != null && dataGrid_Player_Queue.SelectedItems.Count == 1)
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
            finally
            {
                CloseConnection();
                Fill_DataGrid_Queue();
            }
        }

        private void player_Queue_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender != null)
            {
                DataGrid grid = sender as DataGrid;
                if (grid != null && grid.SelectedItems != null && grid.SelectedItems.Count == 1)
                {
                    DataGridRow dgr = grid.ItemContainerGenerator.ContainerFromItem(grid.SelectedItem) as DataGridRow;
                    current_Player = new Player();
                    current_Player = (Player)dgr.Item;

                    if (current_Player.Paid == 0)
                        MessageBox.Show("Questo giocatore non ha pagato", "Alert", MessageBoxButton.OK, MessageBoxImage.Information);

                }
            }
        }

        /*---------------------------- FUNZIONI PER ARDUINO --------------------------------------------*/

        private void ConnectBtn_Click(object sender, RoutedEventArgs e)
        {
            if (comboBox_COM.Text.StartsWith("COM"))
            {
                comboBox_COM.IsEnabled = false;
                ConnectBtn.IsEnabled = false;
                btn_StartRace.IsEnabled = true;
                serialPort = new SerialPort("COM1", 9600);
                serialPort.PortName = comboBox_COM.Text;
                serialPort.DataReceived += new SerialDataReceivedEventHandler(serialPort_DataReceived);
                serialPort.DtrEnable = true;
                serialPort.Open();
            }
        }

        private void serialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort sp = (SerialPort)sender;
            char indata = (char)sp.ReadChar();
            if (indata == '0')
                found = true;
        }

        /*---------------------------- FUNZIONI GARA E TIMER --------------------------------------------*/

        private void Btn_StartRace_Click(object sender, RoutedEventArgs e)
        {
            if(current_Player != null)
            {
                DispatcherTimer sem_timer = new DispatcherTimer();
                sem_timer.Interval = TimeSpan.FromMilliseconds(1000);
                sem_timer.Tick += Timer_Tick_Sem;
                btn_StartRace.IsHitTestVisible = false;
                SetRaceTimer();
                SetLapTimer();
                sem = 0;
                sem_timer.Start();
            }
            else
            {
                MessageBox.Show("Prima di inizare una gara bisogna selezionare un giocatore", "Alert", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        public void Timer_Tick_Sem(object sender, EventArgs e)
        {
            DispatcherTimer sem_timer = (DispatcherTimer)sender;
            switch (sem)
            {
                case 0:
                    ellipse_Light_1.Fill = new SolidColorBrush(Color.FromRgb(0, 0, 255));
                    break;
                case 1:
                    ellipse_Light_2.Fill = new SolidColorBrush(Color.FromRgb(0, 0, 255));
                    break;
                case 2:
                    ellipse_Light_3.Fill = new SolidColorBrush(Color.FromRgb(0, 0, 255));
                    break;
                case 3:
                    ellipse_Light_4.Fill = new SolidColorBrush(Color.FromRgb(0, 0, 255));
                    break;
                case 4:
                    ellipse_Light_5.Fill = new SolidColorBrush(Color.FromRgb(0, 0, 255));
                    break;
                case 5:
                    ellipse_Light_1.Fill = new SolidColorBrush(Color.FromRgb(0, 255, 0));
                    ellipse_Light_2.Fill = new SolidColorBrush(Color.FromRgb(0, 255, 0));
                    ellipse_Light_3.Fill = new SolidColorBrush(Color.FromRgb(0, 255, 0));
                    ellipse_Light_4.Fill = new SolidColorBrush(Color.FromRgb(0, 255, 0));
                    ellipse_Light_5.Fill = new SolidColorBrush(Color.FromRgb(0, 255, 0));
                    stopWatch_race.Start();
                    btn_PauseRace.IsHitTestVisible = true;
                    break;
                default:
                    sem_timer.Stop();
                    ellipse_Light_1.Fill = new SolidColorBrush();
                    ellipse_Light_2.Fill = new SolidColorBrush();
                    ellipse_Light_3.Fill = new SolidColorBrush();
                    ellipse_Light_4.Fill = new SolidColorBrush();
                    ellipse_Light_5.Fill = new SolidColorBrush();
                    break;
            }
            sem++;
        }

        private void SetRaceTimer()
        {
            // imposto i timer per la corsa
            stopWatch_race = new Stopwatch();       // real-time timer
            timercorsa = new DispatcherTimer();     // timer per la visualizzazione
            timercorsa.Interval = TimeSpan.FromMilliseconds(1);
            timercorsa.Tick += Timer_Tick_Race;
            race_time = 60000;
            long cent = (race_time / 10) % 100;
            long sec = (race_time / 1000) % 60;
            long min = (race_time / 1000) / 60;
            lbl_Time_Race.Content = string.Format("{0:00}:{1:00},{2:00}", min, sec, cent);
            timercorsa.Start();
        }

        public void Timer_Tick_Race(object sender, EventArgs e)
        {
            race_time = 60000 - stopWatch_race.ElapsedMilliseconds;

            if (race_time > 0)
            {
                if (found == true && lap_time == 0)
                {
                    stopWatch_lap.Start();
                    found = false;
                }
            }
            else
            {
                race_time = 0;
                stopWatch_lap.Stop();
                stopWatch_race.Stop();
                timercorsa.Stop();
                timer_giro.Stop();
                btn_PauseRace.IsHitTestVisible = false;
                btn_StartRace.IsHitTestVisible = true;
            }

            long cent = (race_time / 10) % 100;
            long sec = (race_time / 1000) % 60;
            long min = (race_time / 1000) / 60;
            lbl_Time_Race.Content = string.Format("{0:00}:{1:00},{2:00}", min, sec, cent);
        }

        private void SetLapTimer()
        {
            // imposto i timer per il giro
            stopWatch_lap = new Stopwatch();        // real-time timer
            timer_giro = new DispatcherTimer();     // timer per la visualizzazione
            timer_giro.Interval = TimeSpan.FromMilliseconds(1);
            timer_giro.Tick += Timer_Tick_Lap;
            lap_time = best_lap = 0;
            lap_label.Content = string.Format("{0:00}:{1:00},{2:00}", 0, 0, 0);
            last_label.Content = string.Format("{0:00}:{1:00},{2:00}", 0, 0, 0);
            best_label.Content = string.Format("{0:00}:{1:00},{2:00}", 0, 0, 0);
            found = false;
            timer_giro.Start();
        }

        public void Timer_Tick_Lap(object sender, EventArgs e)
        {
            lap_time = stopWatch_lap.ElapsedMilliseconds;
            long cent = (lap_time / 10) % 100;
            long sec = (lap_time / 1000) % 60;
            long min = (lap_time / 1000) / 60;
            lap_label.Content = string.Format("{0:00}:{1:00},{2:00}", min, sec, cent);

            if ((found || min > 5) && lap_time > 0)
            {
                if (best_lap == 0 || lap_time < best_lap)
                {
                    best_lap = lap_time;
                    best_label.Content = string.Format("{0:00}:{1:00},{2:00}", min, sec, cent);
                }
                last_label.Content = string.Format("{0:00}:{1:00},{2:00}", min, sec, cent);
                lap_time = 0;
                stopWatch_lap.Restart();
                found = false;
            }
        }

        private void Btn_PauseRace_Click(object sender, RoutedEventArgs e)
        {
            if (stopWatch_race.IsRunning == true)
            {
                stopWatch_lap.Stop();
                stopWatch_race.Stop();
                timer_giro.Stop();
                timercorsa.Stop();
            }
            else
            {
                stopWatch_lap.Start();
                stopWatch_race.Start();
                timer_giro.Start();
                timercorsa.Start();
            }
        }

        // da rimuovere in release
        private void Btn_Simula_sensore_Click(object sender, RoutedEventArgs e)
        {
            found = true;
        }
    }
}

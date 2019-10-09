using System;
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
        int sem;                                // selettore dei semafori da accendere
        long race_time, lap_time, best_time;    // tempi in millisecondi
        bool found;                             // true=macchinina presente, false altrimenti
        private const string dataSource = "Data Source=.\\LapTimer3001.db;Version=3;";
        internal SQLiteConnection connection;
        DispatcherTimer timer_sem, timer_race, timer_lap;   // timer per la visualizzazione
        Stopwatch stopWatch_race, stopWatch_lap;            // real-time timers
        Player current_Player;
        SoundPlayer beep1, beep2;

        public MainWindow()
        {
            InitializeComponent();

            Init();

            CreateConnection();
            Fill_DataGrid_Ranking();
            Fill_DataGrid_Queue();

            btn_Start.IsHitTestVisible = true; // da rimuovere in release
        }

        private void Init()
        {
            sem = 0;
            race_time = 60000;
            lap_time = best_time = 0;
            found = false;
            beep1 = new SoundPlayer(".\\Sounds\\beep1.wav");
            beep2 = new SoundPlayer(".\\Sounds\\beep2.wav");
            InitTimers();
            SearchCOM();
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
                    player.Add(new Player { Name = reader["Name"].ToString(), Surname = reader["Surname"].ToString(), Time = reader["Time_Score"].ToString() });


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

        private void Save_Best_Time_Lap(int ID_User, string Time_Score)
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

        private void Save_Time_Lap(int ID_User, string Time_Score)
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

        /*---------------------------- FUNZIONI PER ARDUINO --------------------------------------------*/

        // cerco le porte seriali utilizzate e le elenco nel ComboBox
        private void SearchCOM()
        {
            String[] ports = SerialPort.GetPortNames();
            comboBox_COM.ItemsSource = ports;
        }
        
        private void ConnectBtn_Click(object sender, RoutedEventArgs e)
        {
            if (comboBox_COM.Text.StartsWith("COM"))
            {
                comboBox_COM.IsEnabled = false;
                ConnectBtn.IsEnabled = false;
                SerialPort serialPort = new SerialPort("COM1", 9600);
                serialPort.PortName = comboBox_COM.Text;
                serialPort.DataReceived += new SerialDataReceivedEventHandler(serialPort_DataReceived);
                serialPort.DtrEnable = true;
                serialPort.Open();
                btn_Start.IsHitTestVisible = true;
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

        private void InitTimers()
        {
            timer_sem = new DispatcherTimer();
            timer_sem.Interval = TimeSpan.FromMilliseconds(1000);
            timer_sem.Tick += Timer_Tick_Sem;

            stopWatch_race = new Stopwatch();
            timer_race = new DispatcherTimer();
            timer_race.Interval = TimeSpan.FromMilliseconds(1);
            timer_race.Tick += Timer_Tick_Race;

            stopWatch_lap = new Stopwatch();
            timer_lap = new DispatcherTimer();
            timer_lap.Interval = TimeSpan.FromMilliseconds(1);
            timer_lap.Tick += Timer_Tick_Lap;
        }
        
        private void Btn_Start_Click(object sender, RoutedEventArgs e)
        {
            if (current_Player != null)
            {
                btn_Start.IsHitTestVisible = false;
                InitTimesAndLabels();
                sem = 0;
                timer_sem.Start();
            }
            else
            {
                MessageBox.Show("Prima di inizare una gara bisogna selezionare un giocatore", "Alert", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void InitTimesAndLabels()
        {
            race_time = 60000;
            WriteTime(lbl_Time_Race, race_time);

            lap_time = best_time = 0;
            WriteTime(lap_label, lap_time);
            WriteTime(last_label, 0);
            WriteTime(best_label, best_time);

            stopWatch_race.Reset();
            stopWatch_lap.Reset();
        }

        private void WriteTime(Label label, long time)
        {
            long cent = (time / 10) % 100;
            long sec = (time / 1000) % 60;
            long min = (time / 1000) / 60;
            label.Content = string.Format("{0:00}:{1:00},{2:00}", min, sec, cent);
        }

        public void Timer_Tick_Sem(object sender, EventArgs e)
        {
            switch (sem)
            {
                case 0:
                    beep1.Play();
                    ellipse_Light_1.Fill = new SolidColorBrush(Color.FromRgb(0, 0, 255));
                    break;
                case 1:
                    beep1.Play();
                    ellipse_Light_2.Fill = new SolidColorBrush(Color.FromRgb(0, 0, 255));
                    break;
                case 2:
                    beep1.Play();
                    ellipse_Light_3.Fill = new SolidColorBrush(Color.FromRgb(0, 0, 255));
                    break;
                case 3:
                    beep1.Play();
                    ellipse_Light_4.Fill = new SolidColorBrush(Color.FromRgb(0, 0, 255));
                    break;
                case 4:
                    beep1.Play();
                    ellipse_Light_5.Fill = new SolidColorBrush(Color.FromRgb(0, 0, 255));
                    break;
                case 5:
                    beep2.Play();
                    ellipse_Light_1.Fill = new SolidColorBrush(Color.FromRgb(0, 255, 0));
                    ellipse_Light_2.Fill = new SolidColorBrush(Color.FromRgb(0, 255, 0));
                    ellipse_Light_3.Fill = new SolidColorBrush(Color.FromRgb(0, 255, 0));
                    ellipse_Light_4.Fill = new SolidColorBrush(Color.FromRgb(0, 255, 0));
                    ellipse_Light_5.Fill = new SolidColorBrush(Color.FromRgb(0, 255, 0));
                    StartRace();
                    break;
                default:
                    timer_sem.Stop();
                    ellipse_Light_1.Fill = new SolidColorBrush();
                    ellipse_Light_2.Fill = new SolidColorBrush();
                    ellipse_Light_3.Fill = new SolidColorBrush();
                    ellipse_Light_4.Fill = new SolidColorBrush();
                    ellipse_Light_5.Fill = new SolidColorBrush();
                    break;
            }
            sem++;
        }

        private void StartRace()
        {
            timer_race.Start();
            timer_lap.Start();
            found = false;
            stopWatch_race.Start();
            btn_Pause.IsHitTestVisible = true;
        }

        private void StopRace()
        {
            stopWatch_lap.Stop();
            stopWatch_race.Stop();
            timer_lap.Stop();
            timer_race.Stop();
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
                race_time = 0;  // se era per caso diventato negativo
                StopRace();
                btn_Pause.IsHitTestVisible = false;
                btn_Start.IsHitTestVisible = true;
            }

            WriteTime(lbl_Time_Race, race_time);
        }

        public void Timer_Tick_Lap(object sender, EventArgs e)
        {
            lap_time = stopWatch_lap.ElapsedMilliseconds;
            WriteTime(lap_label, lap_time);

            long minuto = 1000 * 60;
            if ((found == true || lap_time > 5 * minuto) && lap_time > 0)
            {
                if (best_time == 0 || lap_time < best_time)
                {
                    best_time = lap_time;
                    WriteTime(best_label, lap_time);
                    Save_Best_Time_Lap(current_Player.ID, best_label.Content.ToString());
                    Fill_DataGrid_Ranking();
                }
                Save_Time_Lap(current_Player.ID, last_label.Content.ToString());
                WriteTime(last_label, lap_time);
                lap_time = 0;
                found = false;
                stopWatch_lap.Restart();
            }
        }

        private void Btn_Pause_Click(object sender, RoutedEventArgs e)
        {
            if (stopWatch_race.IsRunning == true)
            {
                StopRace();
                btn_Reset.IsHitTestVisible = true;
            }
            else
            {
                stopWatch_lap.Start();
                stopWatch_race.Start();
                timer_lap.Start();
                timer_race.Start();
                btn_Reset.IsHitTestVisible = false;
            }
        }

        private void Btn_Reset_Click(object sender, RoutedEventArgs e)
        {
            btn_Reset.IsHitTestVisible = false;
            btn_Pause.IsHitTestVisible = false;
            btn_Start.IsHitTestVisible = true;
            InitTimesAndLabels();
        }

        // da rimuovere in release
        private void Btn_Simula_sensore_Click(object sender, RoutedEventArgs e)
        {
            found = true;
        }
    }
}

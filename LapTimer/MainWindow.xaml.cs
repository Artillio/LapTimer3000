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

namespace LapTimer
{

    public class Player
    {
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
        int i = 0;
        int time = 0;
        long lap_time = 0;
        bool found = false; // true=macchinina presente, false altrimenti
        private const string dataSource = "Data Source=C:\\Users\\artil\\Documents\\GitHub\\LapTimer3000\\LapTimer\\LapTimer3001.db;";
        internal SQLiteConnection connection;
        SerialPort serialPort;
        Stopwatch stopWatch_lap;    // real-time timer per il giro corrente
        DispatcherTimer timer_giro; // timer per la visualizzazione il giro corrente

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
            connection.Close();
        }

        private ObservableCollection<Player> Retrieve_Player_Queue()
        {
            try
            {
                if (connection.State == System.Data.ConnectionState.Closed)
                    connection.Open();

                ObservableCollection<Player> player = new ObservableCollection<Player>();

                SQLiteCommand command = new SQLiteCommand("select P.Name, P.Surname, Q.Number_Race, Q.Paid from Player P, Queue Q where P.ID = Q.ID_User order by Q.Datetime", connection);
                SQLiteDataReader reader = command.ExecuteReader();

                while (reader.Read())
                    player.Add(new Player { Name = reader["Name"].ToString(), Surname = reader["Surname"].ToString(), Number_Race = Convert.ToInt32(reader["Number_Race"]), Paid = Convert.ToInt32(reader["Paid"]) });


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

        private void Btn_StartRace_Click(object sender, RoutedEventArgs e)
        {
            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(1000);
            timer.Tick += Timer_Tick;
            btn_StartRace.IsEnabled = false;
            timer.Start();
        }

        public void Timer_Tick(object sender, EventArgs e)
        {
            DispatcherTimer timer = (DispatcherTimer)sender;
            switch (i)
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
                    DispatcherTimer timercorsa = new DispatcherTimer();
                    timercorsa.Interval = TimeSpan.FromMilliseconds(1);
                    timercorsa.Tick += Timer_Tick_Race;
                    SetLapTimer();
                    timercorsa.Start();
                    break;
                default:
                    timer.Stop();
                    break;
            }
            i++;
        }

        public void Timer_Tick_Race(object sender, EventArgs e)
        {
            DispatcherTimer timer = (DispatcherTimer)sender;
            if (time <= 60000)
            {
                time++;
                lbl_Time_Race.Content = string.Format("00:{00}:{01}", time / 60, time % 60);

                if (found == true && lap_time == 0)
                {
                    stopWatch_lap.Start();
                    found = false;
                }
            }
            else
            {
                timer.Stop();
                stopWatch_lap.Stop();
                timer_giro.Stop();
                btn_StartRace.IsEnabled = true;
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

        //VALUTARE SE RITORNARE L'ID DEL PLAYER PER USALRO EVENTUALMENTE DOPO
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
                if(radioButton_Paid.IsChecked == true)
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
                //PRENDERE ID UTENTE DALLA TABELLA DELLE QUEUE PER CANCELLARE
                int ID_User = 0;

                if (connection.State == System.Data.ConnectionState.Closed)
                    connection.Open();

                using (SQLiteCommand command = new SQLiteCommand("delete from Queue where ID_User = @ID_User", connection))
                {
                    command.Parameters.AddWithValue("@ID_User", ID_User);
                    command.ExecuteNonQuery();
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

        private void SetLapTimer()
        {
            // imposto i timer per la corsa
            stopWatch_lap = new Stopwatch();        // real-time timer
            timer_giro = new DispatcherTimer();     // timer per la visualizzazione
            timer_giro.Interval = TimeSpan.FromMilliseconds(10);
            timer_giro.Tick += Timer_Tick_Lap;
            lap_time = 0;
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

            // quando arriva il prossimo segnale dal sensore parte il giro successivo
            if ((found || min > 5) && lap_time > 0)
            {
                // devo ancora salvare i tempi dei giri
                lap_time = 0;
                stopWatch_lap.Restart();
                found = false;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            found = true;
        }
    }
}

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
using System.Media;

namespace LapTimer
{

    /// <summary>
    /// Logica di interazione per MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        int sem;                                // selettore dei semafori da accendere
        long race_time, lap_time, best_time;    // tempi in millisecondi
        bool found;                             // true=macchinina presente, false altrimenti
        DispatcherTimer timer_sem, timer_race, timer_lap;   // timer per la visualizzazione
        Stopwatch stopWatch_race, stopWatch_lap;            // real-time timers
        Player current_Player;
        SoundPlayer beep1, beep2;
        DatabaseManager databaseManager;
        SerialPort serialPort;

        public MainWindow()
        {
            InitializeComponent();
            Init();
            Fill_DataGrid_Ranking();
            Fill_DataGrid_Queue();

            //btn_Start.IsHitTestVisible = true; // da rimuovere in release
            //btn_Simula_sensore.Visibility = Visibility.Visible;  // da rimuovere in release
        }

        private void Init()
        {
            sem = 0;
            race_time = 60000;
            lap_time = best_time = 0;
            found = false;
            beep1 = new SoundPlayer(".\\Sounds\\beep1.wav");
            beep2 = new SoundPlayer(".\\Sounds\\beep2.wav");
            current_Player = null;
            serialPort = null;
            databaseManager = new DatabaseManager();
            InitTimers();
            SearchCOM();
        }

        /*---------------------------- FUNZIONI PER IL DATABASE --------------------------------------------*/

        /// <summary>
        /// Funzione che serve per fillare le datagrid con la classifica dei giocatori
        /// </summary>
        private void Fill_DataGrid_Ranking()
        {
            dataGrid_Ranking.ItemsSource = databaseManager.Retrieve_Player_Ranking();
        }

        /// <summary>
        /// Funzione che serve per fillare le datagrid i giocatori in coda
        /// </summary>
        private void Fill_DataGrid_Queue()
        {
            dataGrid_Player_Queue.ItemsSource = databaseManager.Retrieve_Player_Queue();
        }

        private void Btn_AddPlayer_Click(object sender, RoutedEventArgs e)
        {
            int ID_User = databaseManager.Check_Existing_Player(txt_Name.Text, txt_Surname.Text);

            if (ID_User == 0)
            {
                databaseManager.Add_New_Player(txt_Name.Text, txt_Surname.Text);
                ID_User = databaseManager.Check_Existing_Player(txt_Name.Text, txt_Surname.Text);
            }

            databaseManager.Add_Player_In_Queue(ID_User, txt_Number_Race.Text, (bool)radioButton_Paid.IsChecked);

            Fill_DataGrid_Queue();

            txt_Name.Text = "Nome";
            txt_Surname.Text = "Cognome";
            txt_Number_Race.Text = "Numero corse";
            radioButton_Paid.IsChecked = false;
        }

        private void Btn_Delete_Player_Queue_Click(object sender, RoutedEventArgs e)
        {
            if (current_Player != null)
            {
                databaseManager.Delete_Player_Queue(current_Player, true);
                current_Player = null;
                Fill_DataGrid_Queue();
            }
        }

        private void DataGrid_Player_Queue_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (dataGrid_Player_Queue.SelectedItems.Count == 1)
            {
                DataGridRow dgr = dataGrid_Player_Queue.ItemContainerGenerator.ContainerFromItem(dataGrid_Player_Queue.SelectedItem) as DataGridRow;
                current_Player = new Player();
                current_Player = (Player)dgr.Item;

                if (current_Player.Paid == 0)
                    MessageBox.Show("Questo giocatore non ha pagato.", "Alert", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else if (dataGrid_Player_Queue.SelectedItems.Count > 1)
            {
                MessageBox.Show("Seleziona un solo giocatore.", "Alert", MessageBoxButton.OK, MessageBoxImage.Information);
                current_Player = null;
                dataGrid_Player_Queue.SelectedItems.Clear();
            }
        }

        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            TextBox tb = (TextBox)sender;
            tb.Text = "";
        }

        /*---------------------------- FUNZIONI PER ARDUINO --------------------------------------------*/

        // cerco le porte seriali utilizzate e le elenco nel ComboBox
        private void SearchCOM()
        {
            String[] ports = SerialPort.GetPortNames();
            foreach(string port in ports)
                comboBox_COM.Items.Add(port);
            comboBox_COM.Items.Insert(0, " -- Select COM -- ");
            comboBox_COM.SelectedIndex = 0;
        }
        
        private void ConnectBtn_Click(object sender, RoutedEventArgs e)
        {
            if (comboBox_COM.Text.StartsWith("COM"))
            {
                comboBox_COM.IsEnabled = false;
                ConnectBtn.IsEnabled = false;
                serialPort = new SerialPort("COM1", 9600);
                serialPort.PortName = comboBox_COM.Text;
                serialPort.DataReceived += new SerialDataReceivedEventHandler(serialPort_DataReceived);
                serialPort.DtrEnable = true;
                serialPort.Open();
                btn_Start.IsHitTestVisible = true;
            }
        }

        private void serialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            char indata = (char)serialPort.ReadChar();
            if (indata == '0')
                found = true;
        }

        private void Btn_Simula_sensore_Click(object sender, RoutedEventArgs e)
        {
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
                dataGrid_Player_Queue.IsHitTestVisible = false;
                InitTimesAndLabels();
                sem = 0;
                timer_sem.Start();
            }
            else
            {
                MessageBox.Show("Prima di inizare una gara bisogna selezionare un giocatore.", "Alert", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void InitTimesAndLabels()
        {
            if (current_Player != null)
                race_time = 60000 * current_Player.Number_Race;
            else
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
            long actual_race_time = race_time - stopWatch_race.ElapsedMilliseconds;

            if (actual_race_time > 0)
            {
                if (found == true && lap_time == 0)
                {
                    stopWatch_lap.Start();
                    found = false;
                }
            }
            else
            {
                actual_race_time = 0;  // se era per caso diventato negativo
                StopRace();
                if (best_time > 0)
                {
                    databaseManager.Save_Best_Time_Lap(current_Player.ID, best_label.Content.ToString());
                    Fill_DataGrid_Ranking();
                }
                // inserire qui l'animazione di fine gara
                databaseManager.Delete_Player_Queue(current_Player, false);
                current_Player = null;
                Fill_DataGrid_Queue();
                ResetButtons();
            }

            WriteTime(lbl_Time_Race, actual_race_time);
        }

        private void ResetButtons()
        {
            btn_Pause.IsHitTestVisible = false;
            dataGrid_Player_Queue.IsHitTestVisible = true;
            btn_Start.IsHitTestVisible = true;
        }

        public void Timer_Tick_Lap(object sender, EventArgs e)
        {
            lap_time = stopWatch_lap.ElapsedMilliseconds;
            WriteTime(lap_label, lap_time);

            long minuto = 1000 * 60;
            if ((found == true || lap_time > 5 * minuto) && lap_time > 2000)
            {
                if (best_time == 0 || lap_time < best_time)
                {
                    best_time = lap_time;
                    WriteTime(best_label, lap_time);
                }
                databaseManager.Save_Time_Lap(current_Player.ID, last_label.Content.ToString());
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

        /// <summary>
        /// Alla pressione del bottone di reset la funziona rimette tutti i valori a uno stato iniziale
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Btn_Reset_Click(object sender, RoutedEventArgs e)
        {
            btn_Reset.IsHitTestVisible = false;
            ResetButtons();
            InitTimesAndLabels();
        }

        /*---------------------------- FUNZIONI GESTIONE APPLICAZIONE --------------------------------------------*/

        /// <summary>
        /// Funzione che alla chiusura dell'applicazione chiude la connessione col database
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            databaseManager.CloseConnection();
        }
    }
}

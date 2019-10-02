using System;
using System.Activities.Statements;
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

namespace LapTimer
{

    public class Player
    {
        public String Name { get; set; }
        public String Time { get; set; }
    }

    /// <summary>
    /// Logica di interazione per MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        int i = 0;
        int time = 0;

        public MainWindow()
        {
            InitializeComponent();
            Fill_DataGrid();

        }

        private void Fill_DataGrid()
        {
            dataGrid_Ranking.ItemsSource = Retrieve_Player_Ranking();
        }

        private void Btn_StartRace_Click(object sender, RoutedEventArgs e)
        {
            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(1000);
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        public void Timer_Tick(object sender, EventArgs e)
        {
            DispatcherTimer timer = (DispatcherTimer)sender;
            switch (i)
            {
                case 0:
                    ellipse_Light_1.Fill = new SolidColorBrush(Color.FromRgb(255, 0, 0));
                    break;
                case 1:
                    ellipse_Light_2.Fill = new SolidColorBrush(Color.FromRgb(255, 0, 0));
                    break;
                case 2:
                    ellipse_Light_3.Fill = new SolidColorBrush(Color.FromRgb(255, 0, 0));
                    break;
                case 3:
                    ellipse_Light_4.Fill = new SolidColorBrush(Color.FromRgb(255, 0, 0));
                    break;
                case 4:
                    ellipse_Light_5.Fill = new SolidColorBrush(Color.FromRgb(255, 0, 0));
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
            }
            else
            {
                timer.Stop();
            }

        }

        public ObservableCollection<Player> Retrieve_Player_Ranking()
        {
            ObservableCollection<Player> player = new ObservableCollection<Player>();
            string path = System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            using (SQLiteConnection conn = new SQLiteConnection(@"Data Source=H:\Telegram Download\LapTimer3001.db;"))
            {
                conn.Open();

                SQLiteCommand command = new SQLiteCommand("select Name, Surname, Time_Score from Player, Ranking where ID = ID_User order by Time_Score", conn);
                SQLiteDataReader reader = command.ExecuteReader();

                while (reader.Read())
                    player.Add(new Player { Name = reader["Name"].ToString(), Time = reader["Time"].ToString() });


                reader.Close();
                return player;
            }
        }
    }
}

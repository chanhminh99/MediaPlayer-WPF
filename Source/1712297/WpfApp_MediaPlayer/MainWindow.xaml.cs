using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Gma.System.MouseKeyHook;
using Microsoft.Win32;
using NAudio.Wave;

namespace WpfApp_MediaPlayer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        MediaPlayer _player = new MediaPlayer();
        DispatcherTimer _timer;
        Random random = new Random();
        int _lastIndex = -1;
        bool _isPlaying = false;
        bool _isShuffle = false;
        bool _isLoaded = false;

        private IKeyboardMouseEvents _hook;



        // == 1:lap vo tan ; == 0: lap 1 lans
        int _modeReplay = -1;

        public bool _inPlaylist = false;



        public class itemList
        {
            public int Index { get; set; }

            public string Name { get; set; }

            public string Timeduration { get; set; }



        }


        public MainWindow()
        {
            InitializeComponent();
            _player.MediaEnded += _player_MediaEnded;
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += _timer_Tick;

            // dang ki su kien hook

            _hook = Hook.GlobalEvents();
            _hook.KeyUp += KeyUp_hook;
        }

        void Dispose()
        {

        }

        private void _timer_Tick(object sender, EventArgs e)
        {
            if (_player.Source != null)
            {
                var filename = _fullPaths[_lastIndex].Name;
                var converter = new NameConverter();
                var shortname = converter.Convert(filename, null, null, null);
                var currentPos = _player.Position.ToString(@"mm\:ss");

                var duration = items[_lastIndex].Timeduration;
                
                Time.Text = String.Format($"{currentPos} - {duration} ");
                NameSong.Text = shortname.ToString();
            }
        }
        int index_shuffle;
        List<int> Indexes = null;
        private void _player_MediaEnded(object sender, EventArgs e)
        {
            if (_modeReplay == -1) // mode binh thuong
            {
                if (_isShuffle) // che do ngau nhien
                {
                    if (Indexes.Count > 0) // chon 1 bai
                    {
                        index_shuffle = Indexes[random.Next(0, Indexes.Count)];
                        _lastIndex = index_shuffle;
                        Indexes.Remove(index_shuffle);
                        PlaySelectedIndex(_lastIndex);
                    }
                    else // TH da choi het cac bai trong che do ngau nhien
                    {
                        _player.Stop();
                        _timer.Stop();
                        _lastIndex = -1;
                        _isPlaying = false;
                        PlayIcon.Kind = MaterialDesignThemes.Wpf.PackIconKind.Play;
                        if (items.Count > 0)
                        {
                            for (int i = 0; i < items.Count; i++)
                            {
                                Indexes.Add(i);
                            }
                        }
                    }
                }
                else // khong phai che do ngau nhien
                {
                    _lastIndex++;
                    if (_lastIndex != items.Count) // vi tri bai hat chua phai cuoi cung`
                    {
                        PlaySelectedIndex(_lastIndex);
                    }
                    else // het list, stop, tra ve trang thai cu
                    {
                        _player.Stop();
                        _timer.Stop();
                        _lastIndex = -1;
                        _isPlaying = false;
                        PlayIcon.Kind = MaterialDesignThemes.Wpf.PackIconKind.Play;
                    }
                }

            }
            else if (_modeReplay == 0) // chi hat 1 lan 1 bai hat , khong quan tam Shuffle
            {
                PlaySelectedIndex(_lastIndex);
            }
            else if (_modeReplay == 1) // lap vo tan
            {
                if (_isShuffle) // che do ngau nhien
                {
                    if (Indexes.Count > 0) // chon 1 bai
                    {
                        index_shuffle = Indexes[random.Next(0, Indexes.Count)];
                        _lastIndex = index_shuffle;

                        Indexes.Remove(index_shuffle);
                        PlaySelectedIndex(_lastIndex);
                    }
                    else
                    {
                        if (items.Count > 0)
                        {
                            for (int i = 0; i < items.Count; i++)
                            {
                                Indexes.Add(i);
                            }
                        }
                        index_shuffle = Indexes[random.Next(0, Indexes.Count)];
                        _lastIndex = index_shuffle;

                        Indexes.Remove(index_shuffle);
                        PlaySelectedIndex(_lastIndex);
                    }
                }
                else
                {
                    _lastIndex++;
                    if (_lastIndex == items.Count)
                    {
                        _lastIndex = 0;
                    }
                    PlaySelectedIndex(_lastIndex);
                }
            }
            else
            {
                // do nothing
            }


        }

        private void ButtonExit_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Application.Current.Shutdown();
        }


        private void Button_Create_Click(object sender, RoutedEventArgs e) // tao playlist
        {
            var screen = new CreatePlaylistDialog(); // chuyen sang man hinh create Playlist
            if (screen.ShowDialog() == true)
            {
                name_playlist.Text = screen.AlteredData;
                _inPlaylist = true;
                _fullPaths = new List<FileInfo>();
                items = new BindingList<itemList>();
                Indexes = new List<int>();
                NameSong.Text = "Name a Song";

                listmusic_playlist.ItemsSource = items;
            }
            else
            {
                _inPlaylist = false;
            }


        }

        private void Play_pause_btn_Click(object sender, RoutedEventArgs e)
        {
            if (_inPlaylist) // da o trong 1 playlist -> co the choi nhac
            {
                if (_isPlaying)
                {
                    _player.Pause();
                    _isPlaying = false;
                    PlayIcon.Kind = MaterialDesignThemes.Wpf.PackIconKind.Play;
                }
                else
                {
                    if(items?.Count > 0)
                    {
                        if (_lastIndex != -1) // kiem tra vi tri bai hat hien tai de thuc hien Play sau khi Pause  
                        {
                            if (_isLoaded) // trang thai vua load, kh duoc Play
                            {
                                string filename = _fullPaths[_lastIndex].FullName;
                                _player.Open(new Uri(filename, UriKind.Absolute)); // duong dan tuyet doi

                                //System.Threading.Thread.Sleep(600);

                                ////test nhanh
                                //var duration = _player.NaturalDuration.TimeSpan;
                                //var testDuration = new TimeSpan(duration.Hours, duration.Minutes, duration.Seconds - 10);
                                //_player.Position = testDuration;

                                _player.Play();
                                PlayIcon.Kind = MaterialDesignThemes.Wpf.PackIconKind.Pause;
                                _isPlaying = true;
                                _isLoaded = false;
                                _timer.Start();
                            }
                            else
                            {
                                _player.Play();
                                _isPlaying = true;
                                PlayIcon.Kind = MaterialDesignThemes.Wpf.PackIconKind.Pause;
                            }
                        }
                        else // TH kh chon bai hat ma an luon nut Play
                        {
                            if (_isShuffle) // che do ngau nhien
                            {
                                index_shuffle = Indexes[random.Next(0, Indexes.Count)];
                                _lastIndex = index_shuffle;
                                Indexes.Remove(index_shuffle);
                                PlayIcon.Kind = MaterialDesignThemes.Wpf.PackIconKind.Pause;
                                PlaySelectedIndex(_lastIndex);
                            }
                            else
                            {
                                _lastIndex++;
                                PlaySelectedIndex(_lastIndex);
                            }
                        }
                    }
                    else
                    {
                        System.Windows.MessageBox.Show("Please add song to  your playlist...");
                    }
                }
            }
            else
            {
                System.Windows.MessageBox.Show("Please Create Playlist to play music...");
            }
        }

        private void PlaySelectedIndex(int lastIndex)
        {
            string filename = _fullPaths[lastIndex].FullName;
            _player.Open(new Uri(filename, UriKind.Absolute)); // duong dan tuyet doi
            //System.Threading.Thread.Sleep(600);


            //test nhanh
            //if(_player.NaturalDuration.HasTimeSpan)
            //{
            //    var duration = _player.NaturalDuration.TimeSpan;
            //    var testDuration = new TimeSpan(duration.Hours, duration.Minutes, duration.Seconds - 10);
            //    _player.Position = testDuration;
            //}
           

            _player.Play();
            PlayIcon.Kind = MaterialDesignThemes.Wpf.PackIconKind.Pause;
            _isPlaying = true;
            _timer.Start();
        }

        List<FileInfo> _fullPaths = null;
        BindingList<itemList> items = null;

        private void Add_Button_Click(object sender, RoutedEventArgs e)
        {
            if (_inPlaylist) // neu playlist ton tai thi moi duoc add vao
            {
                
                var screen = new Microsoft.Win32.OpenFileDialog();
                screen.Filter = "Mp3 (*.MP3)|*.MP3|" + "All file (*.*)|*.*";

                screen.Multiselect = true; // cho phep chon nhieu bai

                if (screen.ShowDialog() == true)
                {
                    foreach (var item in screen.FileNames)
                    {

                        var info = new FileInfo(item);
                        _fullPaths.Add(info);

                        Mp3FileReader reader = new Mp3FileReader(info.FullName);
                        TimeSpan duration1 = reader.TotalTime;

                        var time = duration1.TotalSeconds;

                        var filename = _fullPaths[_fullPaths.Count - 1].Name;
                        var converter = new NameConverter();
                        var shortname = converter.Convert(filename, null, null, null).ToString();

                        var duration =TimeSpan.FromSeconds(time).ToString(@"mm\:ss");

                        //Debug.WriteLine($"{shortname}");

                        var itemBinding = new itemList();
                        itemBinding.Name = shortname;
                        itemBinding.Index = _fullPaths.Count - 1;
                        itemBinding.Timeduration = duration;

                        items.Add(itemBinding);
                        Indexes.Add(_fullPaths.Count - 1);
                    }
                }
                _isPlaying = false;
                PlayIcon.Kind = MaterialDesignThemes.Wpf.PackIconKind.Play;
            }
            else
            {
                System.Windows.MessageBox.Show("Please Create playlist to add file...");
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            string load_playlist = isLastPlayList();
            if (load_playlist == "")
            {
                System.Windows.MessageBox.Show("Khong co playlist de load");
            }
            else
            {
                var reader = new StreamReader(load_playlist);

                var firstLine = reader.ReadLine(); // luu tong so bai hat co trong playlist

                if (firstLine == null)
                {
                    System.Windows.MessageBox.Show("File error! Can't load playlist. Please choose another file..");
                }
                else
                {
                    int count = int.Parse(firstLine); // so luong 
                    var secondLine = reader.ReadLine(); // luu bai hat phat cuoi cung truoc khi save - khong su dung

                    var token = load_playlist.Split(new string[] { "." }, StringSplitOptions.None); // tach .txt


                    name_playlist.Text = token[0];

                    var curSong = new FileInfo(secondLine);

                    var nameSong = curSong.Name;
                    var converter1 = new NameConverter();
                    var shortnameSong = converter1.Convert(nameSong, null, null, null).ToString();

                    NameSong.Text = shortnameSong;

                    _fullPaths = new List<FileInfo>();
                    items = new BindingList<itemList>();
                    Indexes = new List<int>();
                    _lastIndex = -1;
                    _isPlaying = false;
                    _isShuffle = false;
                    _inPlaylist = true;

                     //== 1:lap vo tan ; == 0: lap 1 lans
                    _modeReplay = -1;

                    for (int i = 0; i < count; i++)
                    {
                        var info = new FileInfo(reader.ReadLine());
                        _fullPaths.Add(info);
                        var filename = _fullPaths[_fullPaths.Count - 1].Name;
                        if (filename == curSong.Name) // tim index
                        {
                            _lastIndex = i; // cap nhat lastIndex
                                            // MessageBox.Show($"{_lastIndex}");
                        }
                        var converter = new NameConverter();
                        var shortname = converter.Convert(filename, null, null, null).ToString();


                        Mp3FileReader reader_mp3 = new Mp3FileReader(info.FullName);

                        TimeSpan duration1 = reader_mp3.TotalTime;
                        var time = duration1.TotalSeconds;
                        var duration = TimeSpan.FromSeconds(time).ToString(@"mm\:ss");

                        //Debug.WriteLine($"{shortname}");

                        var itemBinding = new itemList();
                        itemBinding.Name = shortname;
                        itemBinding.Index = _fullPaths.Count - 1;
                        itemBinding.Timeduration = duration;
                        items.Add(itemBinding);
                        Indexes.Add(_fullPaths.Count - 1);

                    }

                    listmusic_playlist.ItemsSource = items;
                    _isLoaded = true;
                    System.Windows.MessageBox.Show("Load successfully..");
                }
            }

        }

        private void Listmusic_playlist_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var item = (sender as System.Windows.Controls.ListView).SelectedItem;
            if (item != null)
            {
                _lastIndex = listmusic_playlist.SelectedIndex;
                PlaySelectedIndex(_lastIndex);
            }
        }

        private void Down_Button_Click(object sender, RoutedEventArgs e) // back ve ban nhac truoc do
        {
            if(items?.Count > 1)
            {
                if (_isShuffle) // che do ngau nhien
                {
                    if (Indexes.Count > 0) // chon 1 bai
                    {
                        index_shuffle = Indexes[random.Next(0, Indexes.Count)];
                        _lastIndex = index_shuffle;
                        Indexes.Remove(index_shuffle);
                        PlaySelectedIndex(_lastIndex);
                    }
                    else // TH da choi het cac bai trong che do ngau nhien
                    {
                        if (items.Count > 0)
                        {
                            for (int i = 0; i < items.Count; i++)
                            {
                                Indexes.Add(i);
                            }
                        }
                        index_shuffle = Indexes[random.Next(0, Indexes.Count)];
                        _lastIndex = index_shuffle;
                        Indexes.Remove(index_shuffle);
                        PlaySelectedIndex(_lastIndex);
                    }
                }
                else
                {
                    if (_lastIndex != -1 && _lastIndex != 0)
                    {
                        _lastIndex -= 1;
                        PlaySelectedIndex(_lastIndex);
                    }
                }
                
            }
            
        }

        private void Up_Button_Click(object sender, RoutedEventArgs e) // tua den ban nhac ke tiep
        {
            if(items?.Count > 1 )
            {
                if (_isShuffle) // che do ngau nhien
                {
                    if (Indexes.Count > 0) // chon 1 bai
                    {
                        index_shuffle = Indexes[random.Next(0, Indexes.Count)];
                        _lastIndex = index_shuffle;
                        Indexes.Remove(index_shuffle);
                        PlaySelectedIndex(_lastIndex);
                    }
                    else // TH da choi het cac bai trong che do ngau nhien
                    {
                        if (items.Count > 0)
                        {
                            for (int i = 0; i < items.Count; i++)
                            {
                                Indexes.Add(i);
                            }
                        }
                        index_shuffle = Indexes[random.Next(0, Indexes.Count)];
                        _lastIndex = index_shuffle;
                        Indexes.Remove(index_shuffle);
                        PlaySelectedIndex(_lastIndex);
                    }
                }
                else
                {
                    if (_lastIndex != items.Count - 1 && _lastIndex != -1)
                    {
                        _lastIndex += 1;
                        PlaySelectedIndex(_lastIndex);
                    }
                }
            }
        }


        private void Replay_loop_button_Click(object sender, RoutedEventArgs e)
        {

            if (_modeReplay == -1) // chuyen sang mode loop 1
            {
                Replay_animation.Kind = MaterialDesignThemes.Wpf.PackIconKind.RepeatOnce;
                _modeReplay = 0;
                // MessageBox.Show("You are playing with mode : Loop 1...");
            }
            else if (_modeReplay == 0) // chuyen sang mode lap vo tan
            {
                Replay_animation.Kind = MaterialDesignThemes.Wpf.PackIconKind.Repeat;
                Replay_animation.Background = new SolidColorBrush(Colors.Aqua);
                _modeReplay = 1;
                //MessageBox.Show("You are playing with mode: Infinity Loop ...");
            }
            else
            {
                Replay_animation.Background = new SolidColorBrush(Colors.Transparent);
                _modeReplay = -1;
            }
        }

        private void Shuffle_button_Click(object sender, RoutedEventArgs e)
        {
            if (!_isShuffle) // kh bi ngau nhien
            {
                Shuffle_animation.Background = new SolidColorBrush(Colors.Aqua); // chuyen sang ngau nhien
            }
            else
            {
                Shuffle_animation.Background = new SolidColorBrush(Colors.Transparent);
            }
            _isShuffle = !_isShuffle;
            
        }


        private void Save_button_Click(object sender, RoutedEventArgs e)
        {
            if (items?.Count > 0)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(name_playlist.Text).Append(".txt");

                var writer = new StreamWriter(sb.ToString());

                writer.WriteLine(_fullPaths.Count);

                if(_lastIndex==-1)
                {
                    _lastIndex++;
                }
                writer.WriteLine(_fullPaths[_lastIndex].FullName);

                for (int i = 0; i < _fullPaths.Count; i++)
                {
                    writer.WriteLine(_fullPaths[i].FullName);
                }

                writer.Close();

                System.Windows.MessageBox.Show("Save successfully!");
            }
            else
            {
                // khong co bai hat de save
            }

        }

        public string isLastPlayList()
        {

            string name_lastplaylist = "";

            string path = AppDomain.CurrentDomain.BaseDirectory;
            string[] filePaths = Directory.GetFiles(path, "*.txt",
                                         SearchOption.AllDirectories);
            if (filePaths.Length > 0) // co danh sach cac playlist
            {
                long max = -1;
                foreach (var item in filePaths)
                {
                    var info = new FileInfo(item);
                    if (info.CreationTime.Ticks > max)
                    {
                        max = info.CreationTime.Ticks;
                    }
                }

                foreach (var item in filePaths)
                {
                    var info = new FileInfo(item);
                    if (info.CreationTime.Ticks == max)
                    {
                        name_lastplaylist = info.Name;
                        break;
                    }
                }
            }


            return name_lastplaylist;
        }

        private void Load_button_Click(object sender, RoutedEventArgs e)
        {
            var screen = new Microsoft.Win32.OpenFileDialog();
            screen.Filter = "Text files (*.TXT)|*.TXT|" + "All file (*.*)|*.*";

            if (screen.ShowDialog() == true)
            {
                var filename_playlist = screen.FileName;
                var temp = new FileInfo(filename_playlist);

                var reader = new StreamReader(filename_playlist);

                var firstLine = reader.ReadLine(); // luu tong so bai hat co trong playlist

                if (firstLine == null)
                {
                    System.Windows.MessageBox.Show("File error! Can't load playlist. Please choose another file..");
                }
                else
                {
                    if (items.Count > 0)
                    {
                        _player.Pause();
                        _timer.Stop();
                    }
                    int count = int.Parse(firstLine); // so luong

                    var FileNameplaylist = temp.Name;

                    var secondLine = reader.ReadLine(); // luu bai hat phat cuoi cung truoc khi save - khong su dung

                    var token = FileNameplaylist.Split(new string[] { "." }, StringSplitOptions.None);

                    name_playlist.Text = token[0];

                    _fullPaths = new List<FileInfo>();
                    items = new BindingList<itemList>();
                    Indexes = new List<int>();
                    _lastIndex = -1;
                    _isPlaying = false;
                    PlayIcon.Kind = MaterialDesignThemes.Wpf.PackIconKind.Play;
                    _isShuffle = false;
                    _inPlaylist = true;

                    // == 1:lap vo tan ; == 0: lap 1 lans
                    _modeReplay = -1;

                    for (int i = 0; i < count; i++)
                    {
                        var info = new FileInfo(reader.ReadLine());
                        _fullPaths.Add(info);

                        Mp3FileReader reader1 = new Mp3FileReader(info.FullName);
                        TimeSpan duration1 = reader1.TotalTime;

                        var time = duration1.TotalSeconds;

                        var filename = _fullPaths[_fullPaths.Count - 1].Name;
                        var converter = new NameConverter();
                        var shortname = converter.Convert(filename, null, null, null).ToString();

                        var duration = TimeSpan.FromSeconds(time).ToString(@"mm\:ss");

                        //Debug.WriteLine($"{shortname}");

                        var itemBinding = new itemList();
                        itemBinding.Name = shortname;
                        itemBinding.Index = _fullPaths.Count - 1;
                        itemBinding.Timeduration = duration;

                        items.Add(itemBinding);
                        Indexes.Add(_fullPaths.Count - 1);
                        //
                    }

                    listmusic_playlist.ItemsSource = items;
                }
                System.Windows.MessageBox.Show("Load thanh cong");
            }
            else
            {
                // Khong load file
            }
            

        }

        private void KeyUp_hook(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            if (e.Control && e.Shift && (e.KeyCode == Keys.B)) // Next song
            {
                if(items?.Count > 1)
                {
                    if (_isShuffle) // che do ngau nhien
                    {
                        if (Indexes.Count > 0) // chon 1 bai
                        {
                            index_shuffle = Indexes[random.Next(0, Indexes.Count)];
                            _lastIndex = index_shuffle;
                            Indexes.Remove(index_shuffle);
                            PlaySelectedIndex(_lastIndex);
                        }
                        else // TH da choi het cac bai trong che do ngau nhien
                        {
                            if (items.Count > 0)
                            {
                                for (int i = 0; i < items.Count; i++)
                                {
                                    Indexes.Add(i);
                                }
                            }
                            index_shuffle = Indexes[random.Next(0, Indexes.Count)];
                            _lastIndex = index_shuffle;
                            Indexes.Remove(index_shuffle);
                            PlaySelectedIndex(_lastIndex);
                        }
                    }
                    else
                    {
                        if (_lastIndex != items.Count - 1 && _lastIndex != -1)
                        {
                            _lastIndex += 1;
                            PlaySelectedIndex(_lastIndex);
                        }
                    }
                    
                }
                
            }
            if (e.Control && e.Shift && (e.KeyCode == Keys.V)) // previous song
            {
                if (items?.Count > 1)
                {
                    if (_isShuffle) // che do ngau nhien
                    {
                        if (Indexes.Count > 0) // chon 1 bai
                        {
                            index_shuffle = Indexes[random.Next(0, Indexes.Count)];
                            _lastIndex = index_shuffle;
                            Indexes.Remove(index_shuffle);
                            PlaySelectedIndex(_lastIndex);
                        }
                        else // TH da choi het cac bai trong che do ngau nhien
                        {
                            if (items.Count > 0)
                            {
                                for (int i = 0; i < items.Count; i++)
                                {
                                    Indexes.Add(i);
                                }
                            }
                            index_shuffle = Indexes[random.Next(0, Indexes.Count)];
                            _lastIndex = index_shuffle;
                            Indexes.Remove(index_shuffle);
                            PlaySelectedIndex(_lastIndex);
                        }
                    }
                    else
                    {
                        if (_lastIndex != -1 && _lastIndex != 0)
                        {
                            _lastIndex -= 1;
                            PlaySelectedIndex(_lastIndex);
                        }
                    }
                }
                
                
                
            }
            if (e.Control && e.Shift && (e.KeyCode == Keys.P)) // play song
            {
                if (_inPlaylist ) // da o trong 1 playlist -> co the choi nhac
                {
                    if(items?.Count > 0)
                    {
                        if (_lastIndex != -1) // kiem tra vi tri bai hat hien tai de thuc hien Play sau khi Pause  
                        {
                            if (_isLoaded) // trang thai vua load
                            {
                                string filename = _fullPaths[_lastIndex].FullName;
                                _player.Open(new Uri(filename, UriKind.Absolute)); // duong dan tuyet doi
                                //System.Threading.Thread.Sleep(600);

                                //test nhanh
                                //if(_player.NaturalDuration.HasTimeSpan)
                                //{
                                //    var duration = _player.NaturalDuration.TimeSpan;
                                //    var testDuration = new TimeSpan(duration.Hours, duration.Minutes, duration.Seconds - 10);
                                //    _player.Position = testDuration;
                                //}
                                

                                _player.Play();
                                PlayIcon.Kind = MaterialDesignThemes.Wpf.PackIconKind.Pause;
                                _isPlaying = true;
                                _isLoaded = false;
                                _timer.Start();
                            }
                            else // TH kh phai load
                            {
                                _player.Play();
                                _isPlaying = true;
                                PlayIcon.Kind = MaterialDesignThemes.Wpf.PackIconKind.Pause;
                            }
                        }
                        else // TH kh chon bai hat ma an luon nut Play
                        {
                            if (_isShuffle) // che do ngau nhien
                            {
                                index_shuffle = Indexes[random.Next(0, Indexes.Count)];
                                _lastIndex = index_shuffle;
                                Indexes.Remove(index_shuffle);
                                PlayIcon.Kind = MaterialDesignThemes.Wpf.PackIconKind.Pause;
                                PlaySelectedIndex(_lastIndex);
                            }
                            else
                            {
                                _lastIndex++;
                                PlaySelectedIndex(_lastIndex);
                            }
                        }
                    }
                    else
                    {
                        System.Windows.MessageBox.Show("Please add song to your playlist");
                    }
                    
                }
                else
                {
                    System.Windows.MessageBox.Show("Please create playlist to play music...");
                }
            }
            if (e.Control && e.Shift && (e.KeyCode == Keys.L)) // pause song
            {
                if (_inPlaylist) // da o trong 1 playlist -> co the choi nhac
                {
                    if(items?.Count > 0)
                    {
                        if (_isPlaying)
                        {
                            _player.Pause();
                            _isPlaying = false;
                            PlayIcon.Kind = MaterialDesignThemes.Wpf.PackIconKind.Play;
                        }
                    }
                    
                }
                else
                {
                    System.Windows.MessageBox.Show("Please Create Playlist to play music...");
                }
            }

        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            _hook.KeyUp -= KeyUp_hook;
            _hook.Dispose();
        }
    }
}

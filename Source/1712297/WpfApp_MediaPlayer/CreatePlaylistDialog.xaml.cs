using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace WpfApp_MediaPlayer
{
    /// <summary>
    /// Interaction logic for CreatePlaylistDialog.xaml
    /// </summary>
    public partial class CreatePlaylistDialog : Window
    {

        public string AlteredData = "";
        public CreatePlaylistDialog()
        {
            InitializeComponent();
        }

        private void Playlist_Name_TextChanged(object sender, TextChangedEventArgs e)
        {
            if(playlist_Name.Text.Length==0)
            {
                NameplaylistHint.Visibility = Visibility.Visible;
            }
            else
            {
                NameplaylistHint.Visibility = Visibility.Hidden;
            }
        }

        private void TextBlock_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            this.DialogResult = false;
        }

        private void Btn_Create_Click(object sender, RoutedEventArgs e)
        {
            AlteredData = playlist_Name.Text;
            this.DialogResult = true;
            this.Close();
        }
    }
}

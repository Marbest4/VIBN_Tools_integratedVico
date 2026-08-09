using System.Windows;
using VIBN_Tools.ContainerGeneration.AI;

namespace VIBN_Tools.Application.View
{
    public partial class NoiseFilterEditor : Window
    {
        public NoiseFilterConfig Config { get; private set; }

        public NoiseFilterEditor()
        {
            InitializeComponent();

            Config = NoiseFilterConfig.Load();
            DataContext = Config;
        }

        private void AddBadWord_Click(object sender, RoutedEventArgs e)
        {
            var word = NewBadWordBox.Text?.Trim();
            if (string.IsNullOrWhiteSpace(word))
                return;

            if (!Config.BadWords.Contains(word))
            {
                Config.BadWords.Add(word);
                BadWordList.Items.Refresh();
            }

            NewBadWordBox.Clear();
        }

        private void DeleteBadWord_Click(object sender, RoutedEventArgs e)
        {
            var selected = BadWordList.SelectedItem as string;
            if (selected == null)
                return;

            Config.BadWords.Remove(selected);
            BadWordList.Items.Refresh();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            Config.Save();
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
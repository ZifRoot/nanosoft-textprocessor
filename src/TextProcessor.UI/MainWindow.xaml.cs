using Microsoft.Win32;
using System.Windows;
using TextProcessor.Lib;

namespace TextProcessor.UI;

public partial class MainWindow : Window
{
    private MainViewModel ViewModel => (MainViewModel)DataContext;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
    }

    private void AddJob_Click(object sender, RoutedEventArgs e) => ViewModel.AddJob();

    private void RemoveJob_Click(object sender, RoutedEventArgs e) =>
        ViewModel.RemoveJob(JobsGrid.SelectedItem as FileJobViewModel);

    private void SelectInput_Click(object sender, RoutedEventArgs e)
    {
        if (JobsGrid.SelectedItem is not FileJobViewModel job)
            return;
        var dialog = new OpenFileDialog { Multiselect = false };
        if (dialog.ShowDialog() == true)
            job.InputPath = dialog.FileName;
    }

    private void SelectOutput_Click(object sender, RoutedEventArgs e)
    {
        if (JobsGrid.SelectedItem is not FileJobViewModel job)
            return;
        var dialog = new SaveFileDialog();
        if (dialog.ShowDialog() == true)
            job.OutputPath = dialog.FileName;
    }
}
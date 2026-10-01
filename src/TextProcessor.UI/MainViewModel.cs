using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using TextProcessor.Lib;

namespace TextProcessor.UI;

public sealed class FileJobViewModel : INotifyPropertyChanged
{
    private string _inputPath = string.Empty;
    private string _outputPath = string.Empty;
    private TextEncodingKind _encoding = TextEncodingKind.Auto;

    public string InputPath { get => _inputPath; set { _inputPath = value; OnPropertyChanged(); } }
    public string OutputPath { get => _outputPath; set { _outputPath = value; OnPropertyChanged(); } }
    public TextEncodingKind Encoding { get => _encoding; set { _encoding = value; OnPropertyChanged(); } }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public sealed class MainViewModel : INotifyPropertyChanged
{
    private int _minWordLength = 3;
    private bool _removePunctuation = true;
    private PunctuationHandling _punctuationHandling = PunctuationHandling.SmartReplace;
    private string _punctuationSet = ".,!?;:—…()[]{}\"'–";
    private bool _keepDigits = true;
    private bool _keepHyphen = true;
    private bool _keepApostrophe = true;
    private string _additionalWordChars = string.Empty;
    private WhitespaceNormalization _whitespaceNormalization = WhitespaceNormalization.Collapse;
    private bool _preserveLineBreaks = true;
    private string _template = "{original}{suffix}{ext}";
    private string _suffix = "_processed";
    private int _parallelism = Math.Max(1, Environment.ProcessorCount / 2);
    private double _progress;
    private string _status = "Готов";
    private string _currentFile = string.Empty;
    private bool _isRunning;

    public ObservableCollection<FileJobViewModel> Jobs { get; } = new() { new FileJobViewModel() };
    public Array EncodingKinds => Enum.GetValues<TextEncodingKind>();
    public Array PunctuationModes => Enum.GetValues<PunctuationHandling>();
    public Array WhitespaceModes => Enum.GetValues<WhitespaceNormalization>();

    public int MinWordLength { get => _minWordLength; set { _minWordLength = value; OnPropertyChanged(); } }
    public bool RemovePunctuation { get => _removePunctuation; set { _removePunctuation = value; OnPropertyChanged(); } }
    public PunctuationHandling PunctuationHandling { get => _punctuationHandling; set { _punctuationHandling = value; OnPropertyChanged(); } }
    public string PunctuationSet { get => _punctuationSet; set { _punctuationSet = value; OnPropertyChanged(); } }
    public bool KeepDigits { get => _keepDigits; set { _keepDigits = value; OnPropertyChanged(); } }
    public bool KeepHyphen { get => _keepHyphen; set { _keepHyphen = value; OnPropertyChanged(); } }
    public bool KeepApostrophe { get => _keepApostrophe; set { _keepApostrophe = value; OnPropertyChanged(); } }
    public string AdditionalWordChars { get => _additionalWordChars; set { _additionalWordChars = value; OnPropertyChanged(); } }
    public WhitespaceNormalization WhitespaceNormalization { get => _whitespaceNormalization; set { _whitespaceNormalization = value; OnPropertyChanged(); } }
    public bool PreserveLineBreaks { get => _preserveLineBreaks; set { _preserveLineBreaks = value; OnPropertyChanged(); } }
    public string Template { get => _template; set { _template = value; OnPropertyChanged(); } }
    public string Suffix { get => _suffix; set { _suffix = value; OnPropertyChanged(); } }
    public int Parallelism { get => _parallelism; set { _parallelism = value; OnPropertyChanged(); } }
    public double Progress { get => _progress; private set { _progress = value; OnPropertyChanged(); } }
    public string Status { get => _status; private set { _status = value; OnPropertyChanged(); } }
    public string CurrentFile { get => _currentFile; private set { _currentFile = value; OnPropertyChanged(); } }
    public bool IsRunning { get => _isRunning; private set { _isRunning = value; OnPropertyChanged(); ((RelayCommand)StartCommand).RaiseCanExecuteChanged(); ((RelayCommand)CancelCommand).RaiseCanExecuteChanged(); } }

    public ObservableCollection<string> Log { get; } = new();
    public ICommand StartCommand { get; }
    public ICommand CancelCommand { get; }
    private CancellationTokenSource? _cancellation;

    public MainViewModel()
    {
        StartCommand = new RelayCommand(async () => await StartAsync(), () => !IsRunning);
        CancelCommand = new RelayCommand(() => _cancellation?.Cancel(), () => IsRunning);
    }

    public void AddJob() => Jobs.Add(new FileJobViewModel());
    public void RemoveJob(FileJobViewModel? job)
    {
        if (Jobs.Count == 1) { Jobs[0].InputPath = string.Empty; Jobs[0].OutputPath = string.Empty; return; }
        if (job is not null) Jobs.Remove(job);
    }

    public void SetProgress(double value) => Progress = Math.Clamp(value, 0, 100);

    private async Task StartAsync()
    {
        if (Jobs.All(x => string.IsNullOrWhiteSpace(x.InputPath)))
        {
            Log.Add("Нет файлов для обработки.");
            return;
        }

        var options = new TextProcessingOptions
        {
            MinWordLength = MinWordLength,
            RemovePunctuation = RemovePunctuation,
            PunctuationSet = new HashSet<char>(PunctuationSet),
            PunctuationHandling = PunctuationHandling,
            KeepDigitsAsPartOfWord = KeepDigits,
            KeepHyphenAsPartOfWord = KeepHyphen,
            KeepApostropheAsPartOfWord = KeepApostrophe,
            AdditionalWordChars = AdditionalWordChars,
            WhitespaceNormalization = WhitespaceNormalization,
            PreserveLineBreaks = PreserveLineBreaks,
            OutputFileNameTemplate = Template,
            OutputFileSuffix = Suffix
        };

        _cancellation = new CancellationTokenSource();
        IsRunning = true;
        Status = "Обработка...";
        Progress = 0;
        Log.Clear();

        try
        {
            var jobs = Jobs.Where(x => !string.IsNullOrWhiteSpace(x.InputPath))
                .Select(x => new FileProcessingJob(x.InputPath, x.OutputPath, x.Encoding))
                .ToArray();

            var progress = new Progress<TextProcessingProgress>(value =>
            {
                CurrentFile = value.FilePath;
                SetProgress(value.Percent);
            });

            var result = await new BatchProcessor().ProcessAsync(
                jobs, options, Parallelism, _cancellation.Token, progress);

            foreach (var item in result.Results)
                Log.Add(item.Success
                    ? $"Готово: {item.InputPath} → {item.OutputPath}"
                    : $"Ошибка: {item.InputPath}: {item.Error}");

            Status = $"Завершено: {result.SuccessfulCount} успешно, {result.FailedCount} с ошибками";
            Progress = 100;
        }
        catch (OperationCanceledException)
        {
            Status = "Отменено";
            Log.Add("Обработка отменена пользователем.");
        }
        finally
        {
            _cancellation.Dispose();
            _cancellation = null;
            IsRunning = false;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
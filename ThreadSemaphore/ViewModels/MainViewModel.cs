namespace ThreadSemaphore.ViewModels;

public class MainViewModel: DependencyObject
{
    public Dispatcher MyDispatcher { get; set; } = Dispatcher.CurrentDispatcher;

    public uint Counter { get; set; } = default;

    public ObservableCollection<Thread> CreatedThreads { get; set; } = new();
    public ObservableCollection<Thread> WaitingThreads { get; set; } = new();
    public ObservableCollection<Thread> WorkingThreads { get; set; } = new();

    public RelayCommand CreateThreadCommand { get; set; }
    public RelayCommand AddToWaitingThreadsCommand { get; set; }
    public RelayCommand DeleteWaitingThreadCommand { get; set; }

    public RelayCommand IncrementSemaphoreCountCommand { get; set; }
    public RelayCommand DecrementSemaphoreCountCommand { get; set; }

    public Semaphore AppSemaphore { get; set; } = new(3, 3);
    
    public byte MaximumSemaphoreValue
    {
        get { return (byte)GetValue(MaximumSemaphoreValueProperty); }
        set { SetValue(MaximumSemaphoreValueProperty, value); AppSemaphore = new(MaximumSemaphoreValue, MaximumSemaphoreValue); }
    }
    public static readonly DependencyProperty MaximumSemaphoreValueProperty =
        DependencyProperty.Register("MaximumSemaphoreValue", typeof(byte), typeof(MainViewModel));


    public MainViewModel()
    {
        CreateThreadCommand = new((sender) => CreateThread());
        AddToWaitingThreadsCommand = new((sender) => AddToWaitingThreads(sender as Thread));
        DeleteWaitingThreadCommand = new((sender) => DeleteWaitingThread(sender as Thread));

        IncrementSemaphoreCountCommand = new((sender) => MaximumSemaphoreValue++, (sender) => WaitingThreads.Count == 0 && WorkingThreads.Count == 0 && MaximumSemaphoreValue < 50);
        DecrementSemaphoreCountCommand = new((sender) => MaximumSemaphoreValue--, (sender) => WaitingThreads.Count == 0 && WorkingThreads.Count == 0 && MaximumSemaphoreValue > 1);

        MaximumSemaphoreValue = 3;
    }


    public void CreateThread()
    {
        var thread = new Thread(DoSomething);
        thread.Name = string.Format("Thread {0}", ++Counter);

        CreatedThreads.Add(thread);
    }

    public void AddToWaitingThreads(Thread? thread)
    {
        ArgumentNullException.ThrowIfNull(thread);

        WaitingThreads.Add(thread);
        CreatedThreads.Remove(thread);

        try
        {
            thread.Start();
        }
        catch { }
    }

    public void DeleteWaitingThread(Thread? thread)
    {
        ArgumentNullException.ThrowIfNull(thread);

        WaitingThreads.Remove(thread);
    }

    public void DoSomething()
    {
        try
        {
            AppSemaphore.WaitOne();

            var currentThread = Thread.CurrentThread;

            if (!WaitingThreads.Contains(currentThread))
            {
                AppSemaphore.Release();
                return;
            }

            MyDispatcher.Invoke(() =>
            {
                WaitingThreads.Remove(currentThread);
                WorkingThreads.Add(currentThread);
            });

            for (int i = 0; i < 1000; i++) Thread.Sleep(3);

            AppSemaphore.Release();

            MyDispatcher.Invoke(() => WorkingThreads.Remove(currentThread));
        }
        catch { }
    } 
}

using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Input;
using System;
using System.IO;

namespace Interaktiivne_töölaud;

public partial class Office : Window
{
    string[] backStack = [];
    string[] redoStack = [];
    public string startDir;
    public Office()
    {
        DataContext = new OfficeModel();
        startDir ??= Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        InitializeComponent();
    }

    private void Return_Click(object? sender, RoutedEventArgs e)
    {
        Inside ins = new();
        ins.Show();
        Close();
    }

    private void Archive_Click(object? sender, RoutedEventArgs e)
    {
        Archive arc = new();
        arc.Show();
        Close();
    }

    private string GetWorkingDirectory()
    {
        // get current url from MainWindowModel
        OfficeModel? ctx = ((OfficeModel?)DataContext);
        if ((ctx == null))
        {
            return "";
        }
        return ctx.url;
    }

    public void NavigateDirectory(string path)
    {
        OfficeModel mwm = new();
        mwm.Navigate(path);
        TopText.Content = new DirectoryInfo(mwm.url).Name;
        this.DataContext = mwm;
    }

    private void Window_Loaded_1(object? sender, RoutedEventArgs e)
    {
        NavigateDirectory(startDir);
    }

    // emulate stack behaviour with lists
    private void PushToBackStack()
    {
        string[] newStack = new string[backStack.Length + 1];
        for (int i = 0; i < backStack.Length; i++)
        {
            newStack[i] = backStack[i];
        }
        newStack[backStack.Length] = GetWorkingDirectory();
        backStack = newStack;
    }

    private void PushToRedoStack()
    {
        string[] newStack = new string[redoStack.Length + 1];
        for (int i = 0; i < redoStack.Length; i++)
        {
            newStack[i] = redoStack[i];
        }
        newStack[redoStack.Length] = GetWorkingDirectory();
        redoStack = newStack;
    }

    private string PullFromBackStack()
    {
        string[] newStack = new string[backStack.Length - 1];
        for (int i = 0; i < backStack.Length - 1; i++)
        {
            newStack[i] = backStack[i];
        }
        string pullVal = backStack[^1];
        backStack = newStack;
        return pullVal;
    }

    private string PullFromRedoStack()
    {
        string[] newStack = new string[redoStack.Length - 1];
        for (int i = 0; i < redoStack.Length - 1; i++)
        {
            newStack[i] = redoStack[i];
        }
        string pullVal = redoStack[^1];
        redoStack = newStack;
        return pullVal;
    }

    private void DataGrid_DoubleTapped(object? sender, TappedEventArgs e)
    {
        if (FileBrowser.SelectedItems.Count > 0)
        {
            string? selFolder = ((Folder?)FileBrowser.SelectedItems[0])?.Name;
            OfficeModel? ctx = ((OfficeModel?)DataContext);
            if ((ctx == null) || (selFolder == null))
            {
                return;
            }
            PushToBackStack();
            redoStack = [];
            string currentDir = ctx.url;
            NavigateDirectory(currentDir + "/" + selFolder);
        }
    }

    private void Back_Click(object? sender, RoutedEventArgs e)
    {
        if (backStack.Length > 0)
        {
            PushToRedoStack();
            NavigateDirectory(PullFromBackStack());
        }
    }

    private void Forward_Click(object? sender, RoutedEventArgs e)
    {
        if (redoStack.Length > 0)
        {
            PushToBackStack();
            NavigateDirectory(PullFromRedoStack());
        }
    }

    private void Up_Click(object? sender, RoutedEventArgs e)
    {
        PushToBackStack();
        OfficeModel mwm = new();
        mwm.Up(GetWorkingDirectory());
        TopText.Content = new DirectoryInfo(mwm.url).Name;
        this.DataContext = mwm;
    }
}
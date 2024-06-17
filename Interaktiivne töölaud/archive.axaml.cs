using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.IO;
using System.Text;

namespace Interaktiivne_töölaud;

public partial class archive : Window
{
    public archive()
    {
        InitializeComponent();
    }

    private void Return_Click(object? sender, RoutedEventArgs e)
    {
        Office ofc = new();
        ofc.Show();
        Close();
    }

    private void Show_DriveList(object? sender, Avalonia.Input.PointerEventArgs e)
    {
        DriveList.Items.Clear();
        foreach (DriveInfo driveInfo in DriveInfo.GetDrives())
        {
            if (!driveInfo.IsReady)
            {
                continue;
            }
            StringBuilder sb = new();
            sb.Append(driveInfo.RootDirectory.Name.Replace("\\", "/")).Append(" - ");
            sb.Append(GetUserFriendlySize(Convert.ToDouble(driveInfo.TotalSize))).Append(' ');
            switch (driveInfo.DriveType)
            {
                case DriveType.CDRom:
                    sb.Append(" (Optiline)");
                    break;
                case DriveType.Fixed:
                    sb.Append(" (Sisemine)");
                    break;
                case DriveType.Removable:
                    sb.Append(" (Eemaldatav)");
                    break;
                case DriveType.Network:
                    sb.Append(" (Võrk)");
                    break;
                case DriveType.Ram:
                    sb.Append(" (Mäluketas)");
                    break;
                default:
                    sb.Append(" (Muu)");
                    break;
            }
            DriveList.Items.Add(sb.ToString());
        }
        DrivesButton.IsVisible = false;
        DriveList.IsVisible = true;
    }

    private void OpenDrive(object? sender, Avalonia.Input.TappedEventArgs e)
    {
        if (DriveList.SelectedItems?.Count > 0)
        {
            StringBuilder mount = new();
            foreach (string word in DriveList.SelectedItems[0].ToString().Split(' '))
            {
                if (word == "-")
                {
                    break ;
                }
                mount.Append(word);
            }
            Office ofc = new()
            {
                startDir = mount.ToString()
            };
            ofc.Show();
            Close();
        }
    }

    private void OpenFolder(object? sender, RoutedEventArgs e)
    {
        if (sender is Button b)
        {
            string startDir;
            switch (b.Content?.ToString())
            {
                case "Pildikogu":
                    startDir = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
                    break;
                case "Dokumendid":
                    startDir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                    break;
                case "Varukoopiad":
                    startDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "/Varukoopiad ja taasted";
                    break;
                case "Markuse videod":
                    startDir = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);
                    break;
                default:
                    return;
            }
            new Office()
            {
                startDir = startDir
            }.Show();
            Close();
        }
    }

    private static string GetUserFriendlySize(double size)
    {
        if (size / 1024 / 1024 / 1024 >= 1024)
        {
            return Math.Round(size / 1024 / 1024 / 1024 / 1024, 2).ToString() + " TB";
        }
        else if (size / 1024 / 1024 >= 1024)
        {
            return Math.Round(size / 1024 / 1024 / 1024, 2).ToString() + " GB";
        }
        else if (size / 1024 >= 1024)
        {
            return Math.Round(size / 1024 / 1024, 2).ToString() + " MB";
        }
        else
        {
            return Math.Round(size / 1024, 2).ToString() + " kB";
        }
    }

    private void ListBox_PointerExited_1(object? sender, Avalonia.Input.PointerEventArgs e)
    {
        DrivesButton.IsVisible = true;
        DriveList.IsVisible = false;
    }
}
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using LibUsbDotNet;
using LibUsbDotNet.Main;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Json.Net;
using Newtonsoft.Json;
using System.Threading;
using Avalonia.Threading;

namespace FlashUnlock;

public partial class FlashDevices : Window
{
    internal bool resultOK = false;
    string cdrive = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

    public FlashDevices()
    {
        InitializeComponent();
    }

    private void Window_Loaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Thread t = new(() =>
        {
            foreach (UsbRegistry usbRegistry in UsbDevice.AllDevices)
            {
                string name = usbRegistry.FullName;
                if (name == "")
                {
                    dynamic Result = GetUsb($"usb-ids?vid={usbRegistry.Vid:X4}&pid={usbRegistry.Pid:X4}");
                    try
                    {
                        name = Result.vendors[0].name.Value;
                        name += " " + Result.vendors[0].devices[0].name.Value;
                    }
                    catch
                    {
                        name = "Tundmatu seade";
                    }
                }
                Dispatcher.UIThread.Post(() =>
                {
                    comboBox1.Items.Add($"{usbRegistry.Vid:X4}:{usbRegistry.Pid:X4} - {name} ({usbRegistry.SymbolicName})");
                });
            }
        });
        t.Start();
    }

    public dynamic? GetUsb(string uri)
    {
        using (var client = new HttpClient(new HttpClientHandler { AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate }))
        {
            client.BaseAddress = new Uri("https://apps.sebastianlang.net/");
            HttpResponseMessage response = client.GetAsync(uri).Result;
            response.EnsureSuccessStatusCode();
            string result = response.Content.ReadAsStringAsync().Result;
            return JsonConvert.DeserializeObject(result);
        }
    }

    private void Button_Click_1(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        File.WriteAllText(cdrive + "/.mas/flash_authenticate", comboBox1.Items[comboBox1.SelectedIndex].ToString().Split('(')[1].Replace(")", ""));
        resultOK = true;
        this.Close();
    }
}
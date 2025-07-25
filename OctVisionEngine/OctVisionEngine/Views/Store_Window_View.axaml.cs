﻿using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using OctVisionEngine.ViewModels;
using CommunityToolkit.Mvvm.Messaging;
using OctVisionEngine.Messages;


namespace OctVisionEngine.Views;

public partial class Store_Window_View : Window
{
    public Store_Window_View()
    {
        InitializeComponent();
        
        if (Design.IsDesignMode)
            return;
        WeakReferenceMessenger.Default.Register<Store_Window_View, Messages_OpenTextWindow>(this, static (w, m) =>
        {
            var dialogStoreWindow = new Text_Window_View()
            {
                DataContext = new Text_Window_ViewModel()
            };
            var owner = w.IsVisible ? w : null;
            m.Reply(dialogStoreWindow.ShowDialog<Text_Window_ViewModel?>(owner));
            // m.Reply(dialogStoreWindow.ShowDialog<Text_Window_ViewModel?>(w));
        });
        
        WeakReferenceMessenger.Default.Register<Store_Window_View, Message_CloseStoreWindow> 
        (this, static (Window, message) =>
            { Window.Close(message.SelectedAlbum); }
            // 这里'Store_Window_View' 是作为对话框打开的（详见MainWindow_View.axaml.cs），
            // 在Avalonia中当一个窗口作为对话框打开时,close()方法可以接受一个返回值参数。这里的返回值是'message.SelectedAlbum'。
            // 这个返回值在MainWindow_View.axaml.cs
        );
    }
}
﻿using OctVisionEngine.ViewModels;
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace OctVisionEngine.Messages;

public class Message_PurchaseToOpenStorePage : AsyncRequestMessage<Album_ViewModel?>;



// public class Message_CloseStoreWindow(Album_ViewModel selectedAlbum)
// {
//     public Album_ViewModel SelectedAlbum { get; } = selectedAlbum;
// }
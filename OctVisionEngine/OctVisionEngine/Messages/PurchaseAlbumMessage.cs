using OctVisionEngine.ViewModels;
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace OctVisionEngine.Messages;

public class Message_OpenStorePage : AsyncRequestMessage<Album_ViewModel?>;

public class Messages_OpenTextWindow : AsyncRequestMessage<Text_Window_ViewModel?>;
using OctVisionEngine.ViewModels;
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace OctVisionEngine.Messages;

public class TestCode_OpenStorePage : AsyncRequestMessage<Album_ViewModel?>;

public class Messages_OpenTextWindow : AsyncRequestMessage<TextWindow_ViewModel?>;
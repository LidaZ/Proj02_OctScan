using OctVisionEngine.Models;
using Avalonia.Media.Imaging;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace OctVisionEngine.ViewModels;

public partial class Album_ViewModel : ViewModelBase
{
    public Album_ViewModel(Album album) // 声明强制初始化（被调用来new一个对象）时，必须传入的参数
    {
        _album = album; 
    }
    
    private readonly Album _album; //因为没有声明{get/set; get/set;}，所以这里是字段field. 字段一般不提供给外部调用的读写功能。
    public string Artist => _album.Artist;
    public string Title => _album.Title;
    [ObservableProperty] public partial Bitmap? Cover { get; private set; }
    //这里是把Bitmap函数（生成的结果）的引用传递给Cover。Class指向的都是引用传递而非值传递。
    //[ObservableProperty]是MVVM的CommunityToolkit中的一个特性，自动实现：
    //1) 声明为属性，方便被外部调用时的读写；
    //2) INotifyPropertyChanded()接口的通知，一但值改变，可以做操作. 

    public async Task LoadCover()
    {
        //`await using` - 确保在使用完 `imageStream` 后自动"异步释放资源",C# 8.0 引入的特性，专门用于处理异步资源的释放
        // 等价于:  if (var!=null) {await var.DisposeAsync()}. 
        await using (var imageStream = await _album.LoadCoverBitmapAsync())
        { Cover = await Task.Run(() => Bitmap.DecodeToWidth(imageStream, 400)); }
        //DecodeToWidth方法来转换图像流，以便在Avalonia UI中显示。
        //此方法可以将一张高分辨率大图像的流转换为一张较小的位图，并保持指定的宽度和纵横比。
        //这意味着即使 Web API 返回相当大的文件，您也不会浪费大量内存来显示专辑封面.
    }

    public async Task SaveToDiskAsync()
    {
        await _album.SaveAsync();
        if (Cover != null)
        {
            var bitmap = Cover;
            await Task.Run(() =>
            {
                using (var fs = _album.SaveCoverBitmapStream())
                {
                    bitmap.Save(fs);
                }
            });
        }
    }



}
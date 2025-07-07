using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using OctVisionEngine.Extensions; // 添加扩展方法的引用



namespace OctVisionEngine.Views
{
    public partial class Text_Window_View : Window
    {
        public Text_Window_View()
        {
            InitializeComponent();
        }

        private void Rectangle_OnPointerPressed(object sender, PointerPressedEventArgs e)
        {
            if (sender is Rectangle rectangle)
            {
                rectangle.Classes.Toggle("selected");
            }
        }
    }
}

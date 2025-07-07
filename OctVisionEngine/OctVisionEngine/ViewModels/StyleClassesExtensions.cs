using Avalonia.Controls;
using Avalonia.Styling;

namespace OctVisionEngine.Extensions
{
    public static class StyleClassesExtensions
    {
        public static void Toggle(this Classes classes, string className)
        {
            if (classes.Contains(className))
            {
                classes.Remove(className);
            }
            else
            {
                classes.Add(className);
            }
        }
    }
}
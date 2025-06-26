using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;

namespace OctVisionEngine.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject
    {
        private double _input;
        private double _result; 
        
        [ObservableProperty] 
        private string greeting = "welcome!";
        
        public MainWindowViewModel()
        {
            // ViewModel initialization logic.
        }

        [RelayCommand]
        private async Task RuntestAsync()
        {
            // Code here will be executed when the buttom being pressed. 
            GetInput();
            CalToOutput();
        }

        public void GetInput()
        {
            Console.WriteLine("Enter your input: ");
            _input = Convert.ToDouble(Console.ReadLine());
        }

        private double Calculate()
        {
            _result = --_input;
            return _result;
        }

        public void CalToOutput()
        {
            Console.WriteLine("Your input is: {0}", Calculate());
        }
    }
}

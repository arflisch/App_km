using System;
using System.Threading;
using Car_kilometer.Services;
using Car_kilometer.NewFolder;

namespace Car_kilometer
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();

            _summary = MauiProgram.ServiceProvider.GetRequiredService<Summary>();
            
        }

        private Summary _summary;


    }

}

using System;
using System.Threading;
using Car_kilometer.Services;
using Car_kilometer.NewFolder;
using AVFoundation;

namespace Car_kilometer
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();

            _summary = MauiProgram.ServiceProvider.GetRequiredService<Summary>();

            TotalTimeValue.Text = _summary.Statistic.TotalSecondDurations.ToString();


        }

        private Summary _summary;

    }

}

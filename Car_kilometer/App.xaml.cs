using Syncfusion.Licensing;

namespace Car_kilometer
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            SyncfusionLicenseProvider.RegisterLicense("Ngo9BigBOggjHTQxAR8/V1NCaF1cXGJCf1FpRmJGdld5fUVHYVZUTXxaS00DNHVRdkdnWXZfdnRTRWReWEdwWEY=");

            MainPage = new AppShell();
        }
    }
}

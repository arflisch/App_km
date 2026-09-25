using Syncfusion.Licensing;

namespace Car_kilometer
{
    public partial class App : Application
    {
        readonly AppShell _shell;

        public App(AppShell shell)
        {
            InitializeComponent();

            SyncfusionLicenseProvider.RegisterLicense("Ngo9BigBOggjHTQxAR8/V1NCaF1cXGJCf1FpRmJGdld5fUVHYVZUTXxaS00DNHVRdkdnWXZfdnRTRWReWEdwWEY=");

            _shell = shell;
        }

        protected override Window CreateWindow(IActivationState? activationState) => new(_shell);
    }
}

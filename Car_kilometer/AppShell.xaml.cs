using Car_kilometer.Views;

namespace Car_kilometer
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            // Pages opened on top of the tabs (as sheets on iOS)
            Routing.RegisterRoute("rideeditor", typeof(RideEditorPage));
            Routing.RegisterRoute("export", typeof(ExportPage));
        }
    }
}

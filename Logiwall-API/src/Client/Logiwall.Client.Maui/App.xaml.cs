[assembly: XamlCompilation(XamlCompilationOptions.Compile)]

namespace Logiwall.Client.Maui;

public partial class App
{
    public App(MainPage mainPage)
    {
        InitializeComponent();

        MainPage = new NavigationPage(mainPage);
    }
}

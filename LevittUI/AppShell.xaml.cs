namespace LevittUI;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();
		
		// Set initial route to login
		Routing.RegisterRoute("login", typeof(Views.LoginPage));
		Routing.RegisterRoute("main", typeof(Views.MainPage));
	}
}

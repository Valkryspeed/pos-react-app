Imports ModernWpf
Imports POS_JAVISH_WPF.Services
Imports System.Globalization
Imports System.Threading
Imports POS_JAVISH_WPF.Helpers

Class Application
    Inherits System.Windows.Application

    Private Async Sub OnAppStartup(sender As Object, e As System.Windows.StartupEventArgs)
        ' Set Culture to Indonesia for Currency Formatting (Rp)
        Dim culture = New CultureInfo("id-ID")
        Thread.CurrentThread.CurrentCulture = culture
        Thread.CurrentThread.CurrentUICulture = culture
        FrameworkElement.LanguageProperty.OverrideMetadata(
            GetType(FrameworkElement),
            New FrameworkPropertyMetadata(
                System.Windows.Markup.XmlLanguage.GetLanguage(culture.IetfLanguageTag)))

        DatabaseInitializer.EnsureCreated()
        ApiService.Start()
        Dim login = New Views.LoginWindow()
        login.Show()
    End Sub
End Class

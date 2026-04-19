Imports System.Windows
Imports POS_JAVISH_WPF.Services
Imports POS_JAVISH_WPF.Helpers

Namespace Views
    Partial Public Class LoginWindow
        Inherits Window

        Public Sub New()
            InitializeComponent()
            UsernameBox.Focus()
        End Sub

        Private Async Sub LoginBtn_Click(sender As Object, e As RoutedEventArgs)
            Dim username = UsernameBox.Text?.Trim()
            Dim password = PasswordBox.Password

            If String.IsNullOrWhiteSpace(username) OrElse String.IsNullOrWhiteSpace(password) Then
                MessageBox.Show("Silakan isi Username dan Password.", "Info", MessageBoxButton.OK, MessageBoxImage.Information)
                Return
            End If

            Try
                Dim auth = New AuthService()
                Dim user = Await auth.LoginAsync(username, password)
                If user Is Nothing Then
                    MessageBox.Show("Login gagal. Periksa kembali Username/Password.", "Gagal", MessageBoxButton.OK, MessageBoxImage.Warning)
                    Return
                End If

                Session.CurrentUser = user
                Dim main = New MainWindow()
                main.Show()
                Me.Close()
            Catch ex As Exception
                MessageBox.Show($"Terjadi kesalahan login: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error)
            End Try
        End Sub
    End Class
End Namespace

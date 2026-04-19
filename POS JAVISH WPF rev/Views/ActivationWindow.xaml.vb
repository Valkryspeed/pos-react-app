Imports System.Windows
Imports POS_JAVISH_WPF.Services

Namespace Views
    Partial Public Class ActivationWindow
        Inherits Window

        Public Sub New()
            InitializeComponent()
            TxtHardwareId.Text = LicensingService.GetHardwareId()
        End Sub

        Private Sub BtnCopyId_Click(sender As Object, e As RoutedEventArgs)
            Clipboard.SetText(TxtHardwareId.Text)
            MessageBox.Show("Hardware ID berhasil disalin!", "Info", MessageBoxButton.OK, MessageBoxImage.Information)
        End Sub

        Private Sub BtnLater_Click(sender As Object, e As RoutedEventArgs)
            Me.DialogResult = False
            Me.Close()
        End Sub

        Private Sub BtnActivate_Click(sender As Object, e As RoutedEventArgs)
            Dim serial = TxtSerial.Text?.Trim()
            
            If String.IsNullOrWhiteSpace(serial) Then
                MessageBox.Show("Silakan masukkan Serial Number.", "Peringatan", MessageBoxButton.OK, MessageBoxImage.Warning)
                Return
            End If

            If LicensingService.ValidateKey(serial) Then
                SettingsService.Current.ActivationKey = LicensingService.EncryptKey(serial)
                SettingsService.Save()
                MessageBox.Show("Aktivasi Berhasil! Terima kasih telah menggunakan POS JAVISH.", "Berhasil", MessageBoxButton.OK, MessageBoxImage.Information)
                Me.DialogResult = True
                Me.Close()
            Else
                MessageBox.Show("Serial Number tidak valid untuk komputer ini.", "Gagal Aktivasi", MessageBoxButton.OK, MessageBoxImage.Error)
            End If
        End Sub
    End Class
End Namespace

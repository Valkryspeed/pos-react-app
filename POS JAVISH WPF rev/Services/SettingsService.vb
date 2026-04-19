Imports System.IO
Imports System.Text.Json

Namespace Services
    Public Class AppSettings
        Public Property StoreName As String = "POS JAVISH"
        Public Property StoreAddress As String = "Jl. Teknologi No. 1"
        Public Property StorePhone As String = "0812-3456-7890"
        Public Property TerminalCode As String = ""
        Public Property PrinterName As String = ""
        Public Property PaperSize As Integer = 58 ' 58 or 80
        Public Property AutoPrint As Boolean = True
        Public Property AutoOpenDrawer As Boolean = True
        Public Property ReceiptFooter As String = "Terima Kasih Atas Kunjungan Anda"
        Public Property ActivationKey As String = ""
        Public Property BarcodeSpeedThresholdMs As Integer = 120
        Public Property BarcodeAutoClearTimeoutMs As Integer = 1000
        Public Property BarcodeMinLength As Integer = 6
        Public Property BarcodeAcceptTabTerminator As Boolean = True
        Public Property BarcodeAutoSubmitOnTimeout As Boolean = True
    End Class

    Public Class SettingsService
        Private Shared ReadOnly ConfigPath As String = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json")
        
        Public Shared Current As AppSettings = Load()

        Public Shared Function Load() As AppSettings
            If File.Exists(ConfigPath) Then
                Try
                    Dim json = File.ReadAllText(ConfigPath)
                    Dim loaded = JsonSerializer.Deserialize(Of AppSettings)(json)
                    If loaded Is Nothing Then
                        Return New AppSettings()
                    End If
                    Return loaded
                Catch
                    Return New AppSettings()
                End Try
            End If
            Return New AppSettings()
        End Function

        Public Shared Sub Save()
            Try
                If Current Is Nothing Then
                    Current = New AppSettings()
                End If
                Dim json = JsonSerializer.Serialize(Current, New JsonSerializerOptions With {.WriteIndented = True})
                File.WriteAllText(ConfigPath, json)
            Catch
            End Try
        End Sub
    End Class
End Namespace

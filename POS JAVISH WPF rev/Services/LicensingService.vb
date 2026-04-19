Imports System.Security.Cryptography
Imports System.Text
Imports System.Management
Imports POS_JAVISH_WPF.Data
Imports Microsoft.EntityFrameworkCore

Namespace Services
    Public Class LicensingService
        Private Const SALT As String = "JAVISH_POS_2024_SECRET"
        Private Const MAX_DEMO_TRANSACTIONS As Integer = 100

        ''' <summary>
        ''' Mendapatkan ID Unik Hardware (CPU ID + Disk Serial).
        ''' </summary>
        Public Shared Function GetHardwareId() As String
            Try
                Dim cpuId As String = ""
                Using searcher As New ManagementObjectSearcher("SELECT ProcessorId FROM Win32_Processor")
                    For Each mo As ManagementObject In searcher.Get()
                        cpuId = mo("ProcessorId")?.ToString()
                        Exit For
                    Next
                End Using

                Dim diskSerial As String = ""
                Using searcher As New ManagementObjectSearcher("SELECT SerialNumber FROM Win32_PhysicalMedia")
                    For Each mo As ManagementObject In searcher.Get()
                        diskSerial = mo("SerialNumber")?.ToString()?.Trim()
                        If Not String.IsNullOrEmpty(diskSerial) Then Exit For
                    Next
                End Using

                Dim combined = $"{cpuId}-{diskSerial}"
                Return GenerateHash(combined).Substring(0, 16).ToUpper()
            Catch
                ' Fallback jika ManagementObjectSearcher gagal
                Return GenerateHash(Environment.MachineName).Substring(0, 16).ToUpper()
            End Try
        End Function

        ''' <summary>
        ''' Membuat Serial Number berdasarkan Hardware ID menggunakan SHA-256.
        ''' </summary>
        Public Shared Function GenerateSerialNumber(hardwareId As String) As String
            Dim raw = $"{hardwareId}-{SALT}"
            Dim hash = GenerateHash(raw)
            ' Format: XXXX-XXXX-XXXX-XXXX-XXXX
            Return $"{hash.Substring(0, 4)}-{hash.Substring(4, 4)}-{hash.Substring(8, 4)}-{hash.Substring(12, 4)}-{hash.Substring(16, 4)}".ToUpper()
        End Function

        ''' <summary>
        ''' Validasi apakah Serial Number cocok dengan Hardware ID komputer ini.
        ''' </summary>
        Public Shared Function ValidateKey(serialNumber As String) As Boolean
            If String.IsNullOrWhiteSpace(serialNumber) Then Return False
            Dim expected = GenerateSerialNumber(GetHardwareId())
            Return serialNumber.Trim().ToUpper() = expected
        End Function

        ''' <summary>
        ''' Cek apakah aplikasi sudah diaktivasi.
        ''' </summary>
        Public Shared Function IsActivated() As Boolean
            Return ValidateKey(DecryptKey(SettingsService.Current.ActivationKey))
        End Function

        ''' <summary>
        ''' Enkripsi kunci sebelum disimpan ke file.
        ''' </summary>
        Public Shared Function EncryptKey(rawKey As String) As String
            If String.IsNullOrWhiteSpace(rawKey) Then Return ""
            Try
                Dim bytes = Encoding.UTF8.GetBytes(rawKey)
                ' XOR sederhana dengan SALT sebagai pengaman dasar penyimpanan
                For i As Integer = 0 To bytes.Length - 1
                    bytes(i) = bytes(i) Xor CByte(Asc(SALT(i Mod SALT.Length)))
                Next
                Return Convert.ToBase64String(bytes)
            Catch
                Return rawKey
            End Try
        End Function

        ''' <summary>
        ''' Dekripsi kunci saat dibaca dari file.
        ''' </summary>
        Public Shared Function DecryptKey(encryptedKey As String) As String
            If String.IsNullOrWhiteSpace(encryptedKey) Then Return ""
            Try
                Dim bytes = Convert.FromBase64String(encryptedKey)
                For i As Integer = 0 To bytes.Length - 1
                    bytes(i) = bytes(i) Xor CByte(Asc(SALT(i Mod SALT.Length)))
                Next
                Return Encoding.UTF8.GetString(bytes)
            Catch
                Return encryptedKey
            End Try
        End Function

        ''' <summary>
        ''' Cek apakah masih bisa melakukan transaksi (Demo Mode).
        ''' </summary>
        Public Shared Async Function CanMakeTransactionAsync() As Task(Of Boolean)
            If IsActivated() Then Return True

            Using db As New AppDbContext()
                ' Pengamanan Ganda: Cek jumlah transaksi asli DAN jumlah transaksi di data Shift
                ' Ini mempersulit user mereset trial hanya dengan menghapus data transaksi
                Dim countTrx = Await db.Transaksi.CountAsync()
                Dim countFromShifts = Await db.Shifts.SumAsync(Function(s) s.TransaksiCount)
                
                Dim effectiveCount = Math.Max(countTrx, countFromShifts)
                Return effectiveCount < MAX_DEMO_TRANSACTIONS
            End Using
        End Function

        ''' <summary>
        ''' Mendapatkan sisa transaksi demo.
        ''' </summary>
        Public Shared Async Function GetRemainingDemoTransactionsAsync() As Task(Of Integer)
            If IsActivated() Then Return -1 ' -1 berarti Unlimited

            Using db As New AppDbContext()
                Dim countTrx = Await db.Transaksi.CountAsync()
                Dim countFromShifts = Await db.Shifts.SumAsync(Function(s) s.TransaksiCount)
                
                Dim effectiveCount = Math.Max(countTrx, countFromShifts)
                Return Math.Max(0, MAX_DEMO_TRANSACTIONS - effectiveCount)
            End Using
        End Function

        Private Shared Function GenerateHash(input As String) As String
            Using sha As SHA256 = SHA256.Create()
                Dim inputBytes = Encoding.UTF8.GetBytes(input)
                Dim hashBytes = sha.ComputeHash(inputBytes)
                Dim sb As New StringBuilder()
                For Each b In hashBytes
                    sb.Append(b.ToString("x2"))
                Next
                Return sb.ToString()
            End Using
        End Function
    End Class
End Namespace

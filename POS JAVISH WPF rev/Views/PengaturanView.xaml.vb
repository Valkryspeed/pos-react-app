Imports System.Windows
Imports System.Windows.Controls
Imports System.Printing
Imports System.IO
Imports Microsoft.Win32
Imports Microsoft.EntityFrameworkCore
Imports POS_JAVISH_WPF.Services
Imports POS_JAVISH_WPF.Data
Imports POS_JAVISH_WPF.Models
Imports POS_JAVISH_WPF.Helpers
Imports System.Text
Imports Microsoft.VisualBasic.FileIO
Imports OfficeOpenXml

Namespace Views
    Partial Public Class PengaturanView
        Inherits UserControl

        Public Sub New()
            InitializeComponent()
            Try
                ExcelPackage.License.SetNonCommercialOrganization("POS JAVISH")
            Catch
            End Try
            AddHandler Me.Loaded, AddressOf OnLoaded
        End Sub

        Private Sub OnLoaded(sender As Object, e As RoutedEventArgs)
            Try
                LoadSettings()
            Catch ex As Exception
                MessageBox.Show("Gagal memuat pengaturan: " & ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error)
            End Try
            Try
                LoadPrinters()
            Catch ex As Exception
                MessageBox.Show("Gagal memuat daftar printer: " & ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error)
            End Try
            Try
                CheckActivation()
                CheckAdminAccess()
            Catch ex As Exception
                MessageBox.Show("Gagal memuat status aktivasi: " & ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error)
            End Try
        End Sub

        Private Sub CheckAdminAccess()
            ' HANYA ADMIN yang bisa backup dan restore manual
            Dim isAdmin = Helpers.Session.CurrentUser IsNot Nothing AndAlso Helpers.Session.CurrentUser.Role = "Admin"
            
            If Not isAdmin Then
                BackupBtn.Visibility = Visibility.Collapsed
                RestoreBtn.Visibility = Visibility.Collapsed
                
                ' Opsional: Beri keterangan kenapa hilang
                ' Kita bisa tambahkan TextBlock di XAML jika ingin memberi info
            End If
        End Sub

        Private Sub CheckActivation()
            Dim activated = LicensingService.IsActivated()
            If activated Then
                StatusAktivasiText.Text = "Status: Teraktivasi (Versi Full)"
                StatusAktivasiText.Foreground = TryCast(Me.FindResource("PosSuccess"), Media.Brush)
                ActivationBtn.Visibility = Visibility.Collapsed
                ResetActivationBtn.Visibility = Visibility.Visible
                
                ' Enable all features
                SaveProfileBtn.IsEnabled = True
                BackupBtn.IsEnabled = True
                RestoreBtn.IsEnabled = True
                ExportBtn.IsEnabled = True
                ImportBtn.IsEnabled = True
                StoreNameBox.IsEnabled = True
                StoreAddressBox.IsEnabled = True
                StorePhoneBox.IsEnabled = True
            Else
                StatusAktivasiText.Text = "Status: Versi Demo (Terbatas 100 Transaksi)"
                StatusAktivasiText.Foreground = TryCast(Me.FindResource("PosError"), Media.Brush)
                ActivationBtn.Visibility = Visibility.Visible
                ResetActivationBtn.Visibility = Visibility.Collapsed
                
                ' Untuk Trial, kita izinkan ganti Profil Toko dan Export/Import Excel 
                ' agar user bisa mencoba fitur utama aplikasi.
                SaveProfileBtn.IsEnabled = True
                StoreNameBox.IsEnabled = True
                StoreAddressBox.IsEnabled = True
                StorePhoneBox.IsEnabled = True
                
                ExportBtn.IsEnabled = True
                ImportBtn.IsEnabled = True

                ' Fitur Backup/Restore Manual Database tetap dikunci (opsional)
                BackupBtn.IsEnabled = False
                RestoreBtn.IsEnabled = False
                
                BackupBtn.ToolTip = "Fitur dinonaktifkan di versi Demo. Silakan Aktivasi Aplikasi."
                RestoreBtn.ToolTip = "Fitur dinonaktifkan di versi Demo. Silakan Aktivasi Aplikasi."
            End If
        End Sub

        Private Sub ActivationBtn_Click(sender As Object, e As RoutedEventArgs)
            Dim w As New ActivationWindow()
            w.Owner = Window.GetWindow(Me)
            If w.ShowDialog() = True Then
                CheckActivation()
            End If
        End Sub

        Private Sub ResetActivationBtn_Click(sender As Object, e As RoutedEventArgs)
            If MessageBox.Show("Apakah Anda yakin ingin mereset aktivasi? Aplikasi akan kembali ke Mode Demo.", "Konfirmasi Reset", MessageBoxButton.YesNo, MessageBoxImage.Warning) = MessageBoxResult.Yes Then
                SettingsService.Current.ActivationKey = ""
                SettingsService.Save()
                CheckActivation()
                MessageBox.Show("Aktivasi berhasil direset. Aplikasi kembali ke Mode Demo.", "Berhasil", MessageBoxButton.OK, MessageBoxImage.Information)
            End If
        End Sub

        Private Sub LoadSettings()
            Dim s = SettingsService.Current

            ' Store Profile
            StoreNameBox.Text = s.StoreName
            StoreAddressBox.Text = s.StoreAddress
            StorePhoneBox.Text = s.StorePhone

            ' Hardware
            AutoPrintCheck.IsChecked = s.AutoPrint
            AutoOpenDrawerCheck.IsChecked = s.AutoOpenDrawer
            ReceiptFooterBox.Text = s.ReceiptFooter

            ' Paper Size
            For Each item As ComboBoxItem In PaperSizeCombo.Items
                If item.Tag.ToString() = s.PaperSize.ToString() Then
                    PaperSizeCombo.SelectedItem = item
                    Exit For
                End If
            Next

            ' API Monitor
            Dim ip = ApiService.GetLocalIPAddress()
            ApiUrlBox.Text = $"http://{ip}:5050"
            CustomerUrlBox.Text = $"http://{ip}:5050/customer"

            BarcodeSpeedBox.Text = s.BarcodeSpeedThresholdMs.ToString()
            BarcodeTimeoutBox.Text = s.BarcodeAutoClearTimeoutMs.ToString()
            BarcodeMinLenBox.Text = s.BarcodeMinLength.ToString()
            BarcodeAcceptTabCheck.IsChecked = s.BarcodeAcceptTabTerminator
            BarcodeAutoSubmitTimeoutCheck.IsChecked = s.BarcodeAutoSubmitOnTimeout
        End Sub

        Private Sub LoadPrinters()
            PrinterCombo.Items.Clear()

            Try
                Dim server As New LocalPrintServer()
                Dim queues = server.GetPrintQueues()

                For Each q In queues
                    PrinterCombo.Items.Add(q.Name)
                Next

                Dim currentPrinter = SettingsService.Current.PrinterName
                If Not String.IsNullOrEmpty(currentPrinter) AndAlso PrinterCombo.Items.Contains(currentPrinter) Then
                    PrinterCombo.SelectedItem = currentPrinter
                Else
                    ' Default to system default printer if configured one not found
                    Try
                        Dim defaultPrinter = server.DefaultPrintQueue?.Name
                        If Not String.IsNullOrEmpty(defaultPrinter) AndAlso PrinterCombo.Items.Contains(defaultPrinter) Then
                            PrinterCombo.SelectedItem = defaultPrinter
                        End If
                    Catch
                    End Try
                End If
            Catch ex As Exception
                MessageBox.Show("Gagal memuat daftar printer: " & ex.Message)
            End Try
        End Sub

        Private Sub SaveProfileBtn_Click(sender As Object, e As RoutedEventArgs)
            Dim s = SettingsService.Current
            s.StoreName = StoreNameBox.Text
            s.StoreAddress = StoreAddressBox.Text
            s.StorePhone = StorePhoneBox.Text

            SettingsService.Save()
            MessageBox.Show("Profil Toko berhasil disimpan!", "Sukses", MessageBoxButton.OK, MessageBoxImage.Information)
        End Sub

        Private Sub SaveHardwareBtn_Click(sender As Object, e As RoutedEventArgs)
            Dim s = SettingsService.Current

            If PrinterCombo.SelectedItem IsNot Nothing Then
                s.PrinterName = PrinterCombo.SelectedItem.ToString()
            End If

            If PaperSizeCombo.SelectedItem IsNot Nothing Then
                Dim item = CType(PaperSizeCombo.SelectedItem, ComboBoxItem)
                If item.Tag IsNot Nothing Then
                    Integer.TryParse(item.Tag.ToString(), s.PaperSize)
                End If
            End If

            s.AutoPrint = AutoPrintCheck.IsChecked.GetValueOrDefault()
            s.AutoOpenDrawer = AutoOpenDrawerCheck.IsChecked.GetValueOrDefault()
            s.ReceiptFooter = ReceiptFooterBox.Text

            SettingsService.Save()
            MessageBox.Show("Pengaturan Perangkat Keras berhasil disimpan!", "Sukses", MessageBoxButton.OK, MessageBoxImage.Information)
        End Sub

        Private Sub SaveBarcodeBtn_Click(sender As Object, e As RoutedEventArgs)
            Dim s = SettingsService.Current

            Dim speed As Integer
            If Not Integer.TryParse(BarcodeSpeedBox.Text, speed) Then speed = s.BarcodeSpeedThresholdMs
            Dim minLen As Integer
            If Not Integer.TryParse(BarcodeMinLenBox.Text, minLen) Then minLen = s.BarcodeMinLength
            Dim timeout As Integer
            If Not Integer.TryParse(BarcodeTimeoutBox.Text, timeout) Then timeout = s.BarcodeAutoClearTimeoutMs

            s.BarcodeSpeedThresholdMs = Math.Max(10, speed)
            s.BarcodeMinLength = Math.Max(1, minLen)
            s.BarcodeAutoClearTimeoutMs = Math.Max(100, timeout)
            s.BarcodeAcceptTabTerminator = BarcodeAcceptTabCheck.IsChecked.GetValueOrDefault(True)
            s.BarcodeAutoSubmitOnTimeout = BarcodeAutoSubmitTimeoutCheck.IsChecked.GetValueOrDefault(True)

            SettingsService.Save()

            BarcodeEngine.Instance.ScannerSpeedThresholdMs = s.BarcodeSpeedThresholdMs
            BarcodeEngine.Instance.MinimumBarcodeLength = s.BarcodeMinLength
            BarcodeEngine.Instance.AutoClearTimeoutMs = s.BarcodeAutoClearTimeoutMs
            BarcodeEngine.Instance.AcceptTabTerminator = s.BarcodeAcceptTabTerminator
            BarcodeEngine.Instance.AutoSubmitOnTimeout = s.BarcodeAutoSubmitOnTimeout

            MessageBox.Show("Pengaturan Barcode Scanner berhasil disimpan!", "Sukses", MessageBoxButton.OK, MessageBoxImage.Information)
        End Sub

        Private Sub BackupBtn_Click(sender As Object, e As RoutedEventArgs)
            ' Keamanan Tambahan: Cek Role Admin
            If Helpers.Session.CurrentUser Is Nothing OrElse Helpers.Session.CurrentUser.Role <> "Admin" Then
                MessageBox.Show("Akses ditolak. Hanya Admin yang dapat melakukan backup manual.", "Peringatan", MessageBoxButton.OK, MessageBoxImage.Stop)
                Return
            End If

            Dim saveDialog As New SaveFileDialog()
            saveDialog.Filter = "Database Backup (*.db)|*.db"
            saveDialog.FileName = $"pos_backup_{DateTime.Now:yyyyMMdd_HHmm}.db"

            If saveDialog.ShowDialog() = True Then
                Try
                    Dim sourcePath = BackupService.GetDbPath()
                    If File.Exists(sourcePath) Then
                        File.Copy(sourcePath, saveDialog.FileName, True)
                        MessageBox.Show("Backup database berhasil!", "Sukses", MessageBoxButton.OK, MessageBoxImage.Information)
                    Else
                        MessageBox.Show("Database sumber tidak ditemukan.", "Error", MessageBoxButton.OK, MessageBoxImage.Error)
                    End If
                Catch ex As Exception
                    MessageBox.Show($"Gagal melakukan backup: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error)
                End Try
            End If
        End Sub

        Private Sub RestoreBtn_Click(sender As Object, e As RoutedEventArgs)
            ' Keamanan Tambahan: Cek Role Admin
            If Helpers.Session.CurrentUser Is Nothing OrElse Helpers.Session.CurrentUser.Role <> "Admin" Then
                MessageBox.Show("Akses ditolak. Hanya Admin yang dapat melakukan restore manual.", "Peringatan", MessageBoxButton.OK, MessageBoxImage.Stop)
                Return
            End If

            If MessageBox.Show("Restore akan menimpa data saat ini. Lanjutkan?", "Konfirmasi", MessageBoxButton.YesNo, MessageBoxImage.Warning) <> MessageBoxResult.Yes Then
                Return
            End If

            Dim openDialog As New OpenFileDialog()
            openDialog.Filter = "Database Backup (*.db)|*.db"

            If openDialog.ShowDialog() = True Then
                Try
                    Dim destPath = BackupService.GetDbPath()
                    ' Pastikan direktori tujuan ada
                    Dim destDir = Path.GetDirectoryName(destPath)
                    If Not Directory.Exists(destDir) Then Directory.CreateDirectory(destDir)

                    File.Copy(openDialog.FileName, destPath, True)
                    MessageBox.Show("Restore database berhasil! Aplikasi akan ditutup.", "Sukses", MessageBoxButton.OK, MessageBoxImage.Information)
                    Application.Current.Shutdown()
                Catch ex As Exception
                    MessageBox.Show($"Gagal melakukan restore: {ex.Message}. Pastikan tidak ada transaksi yang sedang berjalan.", "Error", MessageBoxButton.OK, MessageBoxImage.Error)
                End Try
            End If
        End Sub

        Private Async Sub ExportBtn_Click(sender As Object, e As RoutedEventArgs)
            Dim saveDialog As New SaveFileDialog()
            saveDialog.Filter = "Excel File (*.xlsx)|*.xlsx"
            saveDialog.FileName = $"DataBarang_{DateTime.Now:yyyyMMdd}.xlsx"

            If saveDialog.ShowDialog() = True Then
                Try
                    Using db As New AppDbContext()
                        Dim items = Await db.Barang.AsNoTracking().OrderBy(Function(b) b.NamaBarang).ToListAsync()

                        Using package As New ExcelPackage()
                            Dim ws = package.Workbook.Worksheets.Add("Data Barang")

                            ' Header
                            ws.Cells("A1").Value = "Kode Barang"
                            ws.Cells("B1").Value = "Nama Barang"
                            ws.Cells("C1").Value = "Harga Modal"
                            ws.Cells("D1").Value = "Harga Jual"
                            ws.Cells("E1").Value = "Stok"
                            ws.Cells("F1").Value = "Satuan"

                            Using range = ws.Cells("A1:F1")
                                range.Style.Font.Bold = True
                                range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid
                                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray)
                            End Using

                            ' Data
                            For i As Integer = 0 To items.Count - 1
                                Dim row = i + 2
                                Dim item = items(i)

                                ws.Cells(row, 1).Value = item.KodeBarang
                                ws.Cells(row, 2).Value = item.NamaBarang
                                ws.Cells(row, 3).Value = item.HargaModal
                                ws.Cells(row, 4).Value = item.HargaJual
                                ws.Cells(row, 5).Value = item.Stok
                                ws.Cells(row, 6).Value = item.Satuan
                            Next

                            ' Format Columns
                            ws.Column(3).Style.Numberformat.Format = "#,##0"
                            ws.Column(4).Style.Numberformat.Format = "#,##0"
                            ws.Cells.AutoFitColumns()

                            Dim fileInfo As New FileInfo(saveDialog.FileName)
                            Await package.SaveAsAsync(fileInfo)
                        End Using

                        MessageBox.Show($"Export berhasil! {items.Count} data barang diexport ke Excel.", "Sukses", MessageBoxButton.OK, MessageBoxImage.Information)
                    End Using
                Catch ex As Exception
                    MessageBox.Show($"Gagal export data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error)
                End Try
            End If
        End Sub

        Private Async Sub ImportBtn_Click(sender As Object, e As RoutedEventArgs)
            Dim openDialog As New OpenFileDialog()
            openDialog.Filter = "Excel Files (*.xlsx)|*.xlsx|CSV Files (*.csv)|*.csv"

            If openDialog.ShowDialog() = True Then
                Try
                    Dim extension = Path.GetExtension(openDialog.FileName).ToLower()
                    Dim successCount = 0
                    Dim errorCount = 0

                    Using db As New AppDbContext()
                        If extension = ".xlsx" Then
                            ' Import Excel
                            Using package As New ExcelPackage(New FileInfo(openDialog.FileName))
                                Dim ws = package.Workbook.Worksheets(0)
                                Dim rowCount = ws.Dimension.Rows

                                ' Start from row 2 (skip header)
                                For row = 2 To rowCount
                                    Dim kode = ws.Cells(row, 1).Value?.ToString()
                                    Dim nama = ws.Cells(row, 2).Value?.ToString()

                                    If String.IsNullOrWhiteSpace(kode) OrElse String.IsNullOrWhiteSpace(nama) Then
                                        errorCount += 1
                                        Continue For
                                    End If

                                    Dim modal As Decimal = 0
                                    Dim jual As Decimal = 0
                                    Dim stok As Integer = 0
                                    Dim satuan = ws.Cells(row, 6).Value?.ToString()

                                    Decimal.TryParse(ws.Cells(row, 3).Value?.ToString(), modal)
                                    Decimal.TryParse(ws.Cells(row, 4).Value?.ToString(), jual)
                                    Integer.TryParse(ws.Cells(row, 5).Value?.ToString(), stok)

                                    Await ProcessImportItem(db, kode, nama, modal, jual, stok, satuan)
                                    successCount += 1
                                Next
                            End Using
                        Else
                            ' Import CSV (Legacy Support)
                            Using parser As New TextFieldParser(openDialog.FileName)
                                parser.TextFieldType = FieldType.Delimited
                                parser.SetDelimiters(",", ";")
                                parser.HasFieldsEnclosedInQuotes = True

                                Dim firstRow = True
                                While Not parser.EndOfData
                                    Try
                                        Dim fields = parser.ReadFields()

                                        If firstRow Then
                                            firstRow = False
                                            Continue While
                                        End If

                                        If fields Is Nothing OrElse fields.Length < 6 Then
                                            errorCount += 1
                                            Continue While
                                        End If

                                        Dim kode = fields(0).Trim()
                                        Dim nama = fields(1).Trim()

                                        Dim modal As Decimal = 0
                                        Dim jual As Decimal = 0
                                        Dim stok As Integer = 0

                                        Dim modalStr = fields(2).Replace(".", System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator)
                                        modalStr = modalStr.Replace(",", System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator)
                                        Decimal.TryParse(modalStr, modal)

                                        Dim jualStr = fields(3).Replace(".", System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator)
                                        jualStr = jualStr.Replace(",", System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator)
                                        Decimal.TryParse(jualStr, jual)

                                        Integer.TryParse(fields(4), stok)
                                        Dim satuan = fields(5).Trim()

                                        If String.IsNullOrWhiteSpace(kode) OrElse String.IsNullOrWhiteSpace(nama) Then
                                            errorCount += 1
                                            Continue While
                                        End If

                                        Await ProcessImportItem(db, kode, nama, modal, jual, stok, satuan)
                                        successCount += 1
                                    Catch ex As MalformedLineException
                                        errorCount += 1
                                    End Try
                                End While
                            End Using
                        End If

                        Await db.SaveChangesAsync()
                    End Using

                    MessageBox.Show($"Import selesai! Sukses: {successCount}, Gagal/Skip: {errorCount}", "Info", MessageBoxButton.OK, MessageBoxImage.Information)
                Catch ex As Exception
                    MessageBox.Show($"Gagal import data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error)
                End Try
            End If
        End Sub

        Private Async Function ProcessImportItem(db As AppDbContext, kode As String, nama As String, modal As Decimal, jual As Decimal, stok As Integer, satuan As String) As Task
            Dim existing = Await db.Barang.FirstOrDefaultAsync(Function(b) b.KodeBarang = kode)
            If existing IsNot Nothing Then
                existing.NamaBarang = nama
                existing.HargaModal = modal
                existing.HargaJual = jual
                existing.Stok = stok
                existing.Satuan = satuan
            Else
                db.Barang.Add(New Barang With {
                    .KodeBarang = kode,
                    .NamaBarang = nama,
                    .HargaModal = modal,
                    .HargaJual = jual,
                    .Stok = stok,
                    .Satuan = satuan
                })
            End If
        End Function

        Private Function EscapeCsv(input As String) As String
            If String.IsNullOrEmpty(input) Then Return ""
            ' Handle both comma and semicolon
            If input.Contains(",") OrElse input.Contains(";") OrElse input.Contains("""") OrElse input.Contains(vbCr) OrElse input.Contains(vbLf) Then
                Return $"""{input.Replace("""", """""")}"""
            End If
            Return input
        End Function
    End Class
End Namespace

Imports System.Windows
Imports System.Windows.Controls
Imports Microsoft.EntityFrameworkCore
Imports POS_JAVISH_WPF.Data
Imports POS_JAVISH_WPF.Models
Imports POS_JAVISH_WPF.Services
Imports POS_JAVISH_WPF.Views
Imports POS_JAVISH_WPF.Helpers

Partial Class MainWindow
    Public Sub New()
        InitializeComponent()
        AddHandler Me.Loaded, AddressOf Window_Loaded
        
        ' Register Global Barcode Listener
        AddHandler Me.PreviewKeyDown, AddressOf OnPreviewKeyDownGlobal
        AddHandler Me.PreviewTextInput, AddressOf OnPreviewTextInputGlobal
    End Sub

    Private Sub OnPreviewKeyDownGlobal(sender As Object, e As KeyEventArgs)
        If BarcodeEngine.Instance.IsPaused Then Return

        ' Process barcode key down (especially Enter)
        If BarcodeEngine.Instance.ProcessKeyDown(e) Then
            e.Handled = True
        End If
    End Sub

    Private Sub OnPreviewTextInputGlobal(sender As Object, e As TextCompositionEventArgs)
        If BarcodeEngine.Instance.IsPaused Then Return

        ' Check if it's a barcode scan based on input speed
        If BarcodeEngine.Instance.ProcessTextInput(e.Text) Then
            ' If it's a scan, we prevent it from filling focused TextBoxes
            e.Handled = True
            
            ' If the first character leaked, clean it up
            If BarcodeEngine.Instance.WasLeaked Then
                BarcodeEngine.Instance.ClearLeakedInput(FocusManager.GetFocusedElement(Me))
            End If
        End If
    End Sub

    Private Sub Window_Loaded(sender As Object, e As RoutedEventArgs)
        Dim s = SettingsService.Current
        If s IsNot Nothing Then
            BarcodeEngine.Instance.ScannerSpeedThresholdMs = s.BarcodeSpeedThresholdMs
            BarcodeEngine.Instance.AutoClearTimeoutMs = s.BarcodeAutoClearTimeoutMs
            BarcodeEngine.Instance.MinimumBarcodeLength = s.BarcodeMinLength
            BarcodeEngine.Instance.AcceptTabTerminator = s.BarcodeAcceptTabTerminator
            BarcodeEngine.Instance.AutoSubmitOnTimeout = s.BarcodeAutoSubmitOnTimeout
        End If

        ' Check User Role
        Dim isAdmin As Boolean = Session.CurrentUser IsNot Nothing AndAlso Session.CurrentUser.Role = "Admin"
        MenuUser.Visibility = If(isAdmin, Visibility.Visible, Visibility.Collapsed)

        Dim mp = TryCast(Me.FindName("MenuPembelian"), ListBoxItem)
        If Not Equals(mp, Nothing) Then mp.Visibility = If(isAdmin, Visibility.Visible, Visibility.Collapsed)

        Dim ml = TryCast(Me.FindName("MenuLogPembelian"), ListBoxItem)
        If Not Equals(ml, Nothing) Then ml.Visibility = If(isAdmin, Visibility.Visible, Visibility.Collapsed)

        Dim msr = TryCast(Me.FindName("MenuShiftReport"), ListBoxItem)
        If Not Equals(msr, Nothing) Then msr.Visibility = If(isAdmin, Visibility.Visible, Visibility.Collapsed)

        Dim mmsup = TryCast(Me.FindName("MenuSupplier"), ListBoxItem)
        If Not Equals(mmsup, Nothing) Then mmsup.Visibility = If(isAdmin, Visibility.Visible, Visibility.Collapsed)

        Dim mmexp = TryCast(Me.FindName("MenuExpense"), ListBoxItem)
        If Not Equals(mmexp, Nothing) Then mmexp.Visibility = If(isAdmin, Visibility.Visible, Visibility.Collapsed)

        Dim msh = TryCast(Me.FindName("MenuStockHistory"), ListBoxItem)
        If Not Equals(msh, Nothing) Then msh.Visibility = If(isAdmin, Visibility.Visible, Visibility.Collapsed)

        Dim mlr = TryCast(Me.FindName("MenuLabaRugi"), ListBoxItem)
        If Not Equals(mlr, Nothing) Then mlr.Visibility = If(isAdmin, Visibility.Visible, Visibility.Collapsed)

        Dim displayName As String = "-"
        If Session.CurrentUser IsNot Nothing Then
            Dim username = Session.CurrentUser.Username
            Dim role = Session.CurrentUser.Role
            If Not String.IsNullOrWhiteSpace(username) Then
                If Not String.IsNullOrWhiteSpace(role) Then
                    displayName = $"{username.Trim()} ({role.Trim()})"
                Else
                    displayName = username.Trim()
                End If
            ElseIf Not String.IsNullOrWhiteSpace(Session.CurrentUser.FullName) Then
                If Not String.IsNullOrWhiteSpace(role) Then
                    displayName = $"{Session.CurrentUser.FullName.Trim()} ({role.Trim()})"
                Else
                    displayName = Session.CurrentUser.FullName.Trim()
                End If
            End If
        End If
        If UserNameText IsNot Nothing Then
            UserNameText.Text = displayName
        End If

        ' AUTO BACKUP DATABASE (Setiap kali aplikasi dijalankan)
        BackupService.AutoBackup()

        UpdateTrialStatus()
        NavigateTo("Dashboard")
    End Sub

    Private Async Sub UpdateTrialStatus()
        If Not LicensingService.IsActivated() Then
            TrialInfoCard.Visibility = Visibility.Visible
            Dim remaining = Await LicensingService.GetRemainingDemoTransactionsAsync()
            TrialRemainingText.Text = $"Sisa: {remaining} Transaksi"
            
            If remaining <= 0 Then
                TrialRemainingText.Foreground = New System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Red)
                TrialRemainingText.FontWeight = FontWeights.Bold
            End If
        Else
            TrialInfoCard.Visibility = Visibility.Collapsed
        End If
    End Sub

    Private Sub ActivateTrial_Click(sender As Object, e As RoutedEventArgs)
        Dim w As New ActivationWindow()
        w.Owner = Me
        If w.ShowDialog() = True Then
            UpdateTrialStatus()
        End If
    End Sub

    Private Sub OnNavSelectionChanged(sender As Object, e As SelectionChangedEventArgs)
        Dim item = TryCast(NavList.SelectedItem, ListBoxItem)
        If item Is Nothing Then Return

        Dim tag = TryCast(item.Tag, String)
        If tag IsNot Nothing Then
            NavigateTo(tag)
        End If
    End Sub

    Private Sub NavigateTo(tag As String)
        If ContentRegion Is Nothing Then
            ' Fallback if FindName is needed, though InitializeComponent should handle it
            Dim found = Me.FindName("ContentRegion")
            If found IsNot Nothing Then
                ContentRegion = CType(found, ContentControl)
            End If
        End If
        
        If ContentRegion Is Nothing Then Return

        If PageTitle IsNot Nothing Then
            PageTitle.Text = tag
        End If

        Select Case tag
            Case "Dashboard"
                ContentRegion.Content = New DashboardView()
            Case "Kasir"
                ContentRegion.Content = New KasirView()
            Case "Produk"
                ContentRegion.Content = New ProdukView()
            Case "Pembelian"
                ContentRegion.Content = New PembelianView()
            Case "Supplier"
                ContentRegion.Content = New SupplierView()
                PageTitle.Text = "Manajemen Supplier"
            Case "Expense"
                ContentRegion.Content = New ExpenseView()
                PageTitle.Text = "Manajemen Pengeluaran"
            Case "Laporan"
                ContentRegion.Content = New LaporanView()
            Case "TutupShift"
                ContentRegion.Content = New TutupShiftView()
                PageTitle.Text = "Tutup Shift Kasir"
            Case "ShiftReport"
                ContentRegion.Content = New ShiftReportView()
                PageTitle.Text = "Laporan Shift Kasir"
            Case "UserManagement"
                ContentRegion.Content = New UserManagementView()
            Case "LogPembelian"
                ContentRegion.Content = New LogPembelianView()
                PageTitle.Text = "Log Pembelian"
            Case "StockHistory"
                ContentRegion.Content = New StockHistoryView()
                PageTitle.Text = "Riwayat Perubahan Stok"
            Case "LabaRugi"
                ContentRegion.Content = New LaporanLabaRugiView()
                PageTitle.Text = "Laporan Laba Rugi"
            Case "Pengaturan"
                ContentRegion.Content = New PengaturanView()
            Case Else
                Dim grid = New Grid()
                Dim t1 = New TextBlock() With {.Text = "Halaman " & tag & " belum tersedia", .FontSize = 18, .HorizontalAlignment = HorizontalAlignment.Center, .VerticalAlignment = VerticalAlignment.Center, .Foreground = System.Windows.Media.Brushes.Gray}
                grid.Children.Add(t1)
                ContentRegion.Content = grid
        End Select
    End Sub

    Private Sub LogoutBtn_Click(sender As Object, e As RoutedEventArgs)
        Session.CurrentUser = Nothing
        Dim login = New Views.LoginWindow()
        login.Show()
        Me.Close()
    End Sub


End Class

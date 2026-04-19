Imports System.Windows
Imports System.Windows.Controls
Imports System.Linq
Imports Microsoft.EntityFrameworkCore
Imports POS_JAVISH_WPF.Data
Imports POS_JAVISH_WPF.Models

Namespace Views
    Partial Public Class DashboardView
        Inherits UserControl

        Public Class LowStockViewModel
            Public Property KodeBarang As String
            Public Property NamaBarang As String
            Public Property Stok As Integer
            Public Property StokMinimum As Integer
            Public Property StatusAI As String
            Public Property DailySales As Double
            Public Property StatusText As String
        End Class

        Private WithEvents _timer As New System.Windows.Threading.DispatcherTimer()

        Public Sub New()
            InitializeComponent()
            LoadDashboardData()
            
            ' Setup Timer for Auto Refresh (30 seconds)
            _timer.Interval = TimeSpan.FromSeconds(30)
            _timer.Start()
        End Sub

        Private Sub OnTimerTick(sender As Object, e As EventArgs) Handles _timer.Tick
            LoadDashboardData()
        End Sub

        Private Async Sub LoadDashboardData()
            Try
                Using db As New AppDbContext()
                    ' 1. Sales Today
                    Dim today = Date.Today
                    Dim transactionsToday = Await db.Transaksi.AsNoTracking().
                                                Where(Function(t) t.Tanggal >= today).ToListAsync()

                    Dim salesToday = transactionsToday.Sum(Function(t) t.Total)
                    Dim countToday = transactionsToday.Count

                    SalesTodayText.Text = salesToday.ToString("C0")
                    TransactionCountText.Text = $"{countToday} Transaksi Hari Ini"

                    ' 2. Monthly Sales
                    Dim firstDayMonth = New Date(today.Year, today.Month, 1)
                    Dim transactionsMonth = Await db.Transaksi.AsNoTracking().
                                                Where(Function(t) t.Tanggal >= firstDayMonth).ToListAsync()
                    MonthlySalesText.Text = transactionsMonth.Sum(Function(t) t.Total).ToString("C0")

                    ' 3. Total Products
                    Dim totalProducts = Await db.Barang.CountAsync()
                    TotalProductsText.Text = totalProducts.ToString()

                    ' --- STOK HABIS & MENIPIS ---
                    Dim allBarang = Await db.Barang.AsNoTracking().ToListAsync()
                    
                    ' Stok Habis (stok <= 0)
                    Dim emptyStockList = allBarang.Where(Function(b) b.Stok <= 0).
                        OrderBy(Function(b) b.NamaBarang).
                        Select(Function(b) New LowStockViewModel With {
                            .KodeBarang = b.KodeBarang,
                            .NamaBarang = b.NamaBarang,
                            .Stok = b.Stok,
                            .StokMinimum = b.StokMinimum,
                            .StatusText = "STOK HABIS"
                        }).ToList()
                    
                    EmptyStockText.Text = emptyStockList.Count.ToString()
                    LowStockGrid.ItemsSource = emptyStockList

                    ' Stok Menipis (stok > 0 AND stok <= stok_minimum)
                    Dim lowStockItems = allBarang.Where(Function(b) b.Stok > 0 AndAlso b.Stok <= b.StokMinimum).ToList()
                    LowStockText.Text = lowStockItems.Count.ToString()

                    ' Create Tooltip Text for Low Stock Items
                    Dim tooltipContent As String = ""
                    If lowStockItems.Any() Then
                        Dim tooltipLines = lowStockItems.Select(Function(b) $"• {b.NamaBarang} (Stok: {b.Stok})")
                        tooltipContent = "Daftar Barang Stok Menipis:" & vbCrLf & String.Join(vbCrLf, tooltipLines.Take(15))
                        If lowStockItems.Count > 15 Then
                            tooltipContent &= vbCrLf & "...dan lainnya"
                        End If
                    Else
                        tooltipContent = "Tidak ada barang dengan stok menipis."
                    End If

                    ' Set Tooltip with Deep Red Color
                    LowStockCard.ToolTip = New TextBlock With {
                        .Text = tooltipContent,
                        .Foreground = System.Windows.Media.Brushes.DarkRed,
                        .FontWeight = FontWeights.Bold,
                        .FontSize = 12
                    }

                    ' --- ENTERPRISE AI ANALYTICS ---
                    ' avg_daily_sales = total_penjualan_30hari / 30
                    Dim thirtyDaysAgo = today.AddDays(-30)
                    Dim sales30Days = Await db.TransaksiItems.AsNoTracking().
                        Join(db.Transaksi, Function(i) i.TransaksiId, Function(t) t.Id, Function(i, t) New With {i.BarangId, i.Qty, t.Tanggal}).
                        Where(Function(x) x.Tanggal >= thirtyDaysAgo).
                        ToListAsync()

                    Dim salesByBarang = sales30Days.GroupBy(Function(x) x.BarangId).
                        ToDictionary(Function(g) g.Key, Function(g) g.Sum(Function(x) x.Qty))

                    ' Update AI Chart & Stats
                    Dim topProducts = sales30Days.GroupBy(Function(x) x.BarangId).
                        Select(Function(g) New With {
                            .BarangId = g.Key,
                            .NamaBarang = If(allBarang.FirstOrDefault(Function(b) b.Id = g.Key)?.NamaBarang, "Unknown"),
                            .TotalQty = g.Sum(Function(x) x.Qty)
                        }).
                        OrderByDescending(Function(x) x.TotalQty).
                        Take(10).ToList()
                    
                    Dim maxQty = If(topProducts.Any(), topProducts.Max(Function(x) x.TotalQty), 1)
                    TopProductsList.ItemsSource = topProducts.Select(Function(x) New With {
                        .NamaBarang = x.NamaBarang,
                        .TotalQty = x.TotalQty,
                        .MaxQty = maxQty
                    }).ToList()

                    ' Recent Transactions
                    Dim recentTrx = Await db.Transaksi.AsNoTracking().
                        OrderByDescending(Function(t) t.Tanggal).
                        Take(10).ToListAsync()
                    RecentTransactionsGrid.ItemsSource = recentTrx

                    ' Mobile Activity
                    Dim mobileActivity = Await db.ScannerLog.AsNoTracking().
                        OrderByDescending(Function(l) l.Waktu).
                        Take(10).ToListAsync()
                    MobileActivityGrid.ItemsSource = mobileActivity

                    ' Total Modal (Admin Only)
                    If Helpers.Session.CurrentUser?.Role = "Admin" Then
                        TotalModalCard.Visibility = Visibility.Visible
                        Dim totalModal = allBarang.Sum(Function(b) b.Stok * b.HargaModal)
                        TotalModalText.Text = totalModal.ToString("C0")
                    End If
                End Using
            Catch ex As Exception
                ' Silent fail for dashboard refresh
            End Try
        End Sub

        Private Sub LowStockGrid_MouseDoubleClick(sender As Object, e As System.Windows.Input.MouseButtonEventArgs)
            Dim dg = TryCast(sender, DataGrid)
            If dg Is Nothing Then Return
            Dim item = TryCast(dg.SelectedItem, LowStockViewModel)
            If item Is Nothing Then Return
            If Not (Helpers.Session.CurrentUser IsNot Nothing AndAlso Helpers.Session.CurrentUser.Role = "Admin") Then
                MessageBox.Show("Hanya Admin yang dapat melakukan restock/pembelian.", "Akses Ditolak", MessageBoxButton.OK, MessageBoxImage.Warning)
                Return
            End If
            Dim w = New RestockBarangWindow(item.KodeBarang)
            w.Owner = Window.GetWindow(Me)
            Dim result = w.ShowDialog()
            If result.HasValue AndAlso result.Value Then
                LoadDashboardData()
            End If
        End Sub
    End Class
End Namespace

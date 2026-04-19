Imports System.Windows
Imports System.Windows.Controls
Imports System.Linq
Imports Microsoft.EntityFrameworkCore
Imports POS_JAVISH_WPF.Data
Imports POS_JAVISH_WPF.Models

Namespace Views
    Partial Public Class LaporanLabaRugiView
        Inherits UserControl

        Public Sub New()
            InitializeComponent()
            StartDatePicker.SelectedDate = New Date(DateTime.Now.Year, DateTime.Now.Month, 1)
            EndDatePicker.SelectedDate = DateTime.Now
            LoadReportData()
        End Sub

        Private Async Sub LoadReportData()
            Dim startD = StartDatePicker.SelectedDate.GetValueOrDefault().Date
            Dim endD = EndDatePicker.SelectedDate.GetValueOrDefault().Date.AddDays(1).AddSeconds(-1)

            Using db As New AppDbContext()
                ' 1. Fetch Sales Transactions within date range
                Dim transactions = Await db.Transaksi.AsNoTracking().
                    Include(Function(t) t.Items).
                    Where(Function(t) t.Tanggal >= startD AndAlso t.Tanggal <= endD).
                    ToListAsync()

                ' 2. Calculate Revenue and COGS
                ' Using historical cost stored in SaleItem for 100% accuracy
                Dim totalRevenue As Decimal = 0
                Dim totalCogs As Decimal = 0

                Dim itemProfitSummary = New Dictionary(Of Integer, ItemProfitModel)()

                For Each t In transactions
                    totalRevenue += t.Total
                    For Each it In t.Items
                        ' Use HargaModal from SaleItem (Historical Cost)
                        Dim cogs = it.Qty * it.HargaModal
                        totalCogs += cogs

                        If Not itemProfitSummary.ContainsKey(it.BarangId) Then
                            itemProfitSummary(it.BarangId) = New ItemProfitModel With {
                                .BarangId = it.BarangId,
                                .NamaBarang = it.NamaBarang,
                                .Qty = 0,
                                .TotalRevenue = 0,
                                .TotalCogs = 0
                            }
                        End If

                        Dim summary = itemProfitSummary(it.BarangId)
                        summary.Qty += it.Qty
                        summary.TotalRevenue += it.Subtotal
                        summary.TotalCogs += cogs
                    Next
                Next

                ' 3. Fetch Expenses
                Dim expenses = Await db.Pengeluaran.AsNoTracking().
                    Where(Function(e) e.Tanggal >= startD AndAlso e.Tanggal <= endD).
                    ToListAsync()
                
                Dim totalExpenses = expenses.Sum(Function(e) e.Jumlah)
                Dim grossProfit = totalRevenue - totalCogs
                Dim netProfit = grossProfit - totalExpenses

                ' 4. Update UI
                TotalRevenueText.Text = totalRevenue.ToString("C0")
                TotalCogsText.Text = totalCogs.ToString("C0")
                GrossProfitText.Text = grossProfit.ToString("C0")
                TotalExpensesText.Text = totalExpenses.ToString("C0")
                NetProfitText.Text = netProfit.ToString("C0")

                ItemProfitGrid.ItemsSource = itemProfitSummary.Values.OrderByDescending(Function(x) x.Profit).ToList()
                ExpensesGrid.ItemsSource = expenses.GroupBy(Function(e) e.Kategori).
                    Select(Function(g) New With {.Kategori = g.Key, .Jumlah = g.Sum(Function(e) e.Jumlah)}).
                    OrderByDescending(Function(x) x.Jumlah).ToList()
            End Using
        End Sub

        Private Sub TampilkanBtn_Click(sender As Object, e As RoutedEventArgs)
            LoadReportData()
        End Sub

        Public Class ItemProfitModel
            Public Property BarangId As Integer
            Public Property NamaBarang As String
            Public Property Qty As Integer
            Public Property TotalRevenue As Decimal
            Public Property TotalCogs As Decimal
            Public ReadOnly Property Profit As Decimal
                Get
                    Return TotalRevenue - TotalCogs
                End Get
            End Property
        End Class
    End Class
End Namespace
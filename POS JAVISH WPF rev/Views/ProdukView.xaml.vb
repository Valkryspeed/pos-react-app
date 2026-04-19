Imports System.Windows
Imports System.Windows.Controls
Imports System.Linq
Imports Microsoft.EntityFrameworkCore
Imports POS_JAVISH_WPF.Data
Imports POS_JAVISH_WPF.Models
Imports Microsoft.Win32
Imports POS_JAVISH_WPF.Services
Imports POS_JAVISH_WPF.Helpers

Namespace Views
    Partial Public Class ProdukView
        Inherits UserControl

        Private Products As New System.Collections.ObjectModel.ObservableCollection(Of Barang)()

        Public Sub New()
            InitializeComponent()
            ProductGrid.ItemsSource = Products
            LoadProducts()
            CheckActivation()

            ' Register Barcode Engine Event
            AddHandler BarcodeEngine.Instance.BarcodeScanned, AddressOf OnBarcodeScanned
            AddHandler Me.Unloaded, Sub() RemoveHandler BarcodeEngine.Instance.BarcodeScanned, AddressOf OnBarcodeScanned
        End Sub

        Private Sub OnBarcodeScanned(barcode As String)
            If Not Me.IsVisible Then Return
            SearchBox.Text = barcode
        End Sub

        Private Sub CheckActivation()
            ' Untuk trial, kita izinkan Export/Import Excel agar user bisa mencoba input data masal
            ' Pembatasan utama tetap di jumlah transaksi (100)
            BtnExcelExport.IsEnabled = True
            BtnExcelImport.IsEnabled = True
            
            If Not LicensingService.IsActivated() Then
                BtnExcelExport.ToolTip = "Versi Demo: Export data barang ke Excel"
                BtnExcelImport.ToolTip = "Versi Demo: Import data barang dari Excel"
            End If
        End Sub

        Private Async Sub LoadProducts()
            Using db As New AppDbContext()
                Dim list = Await db.Barang.AsNoTracking().OrderBy(Function(b) b.NamaBarang).ToListAsync()
                Products.Clear()
                For Each item In list
                    Products.Add(item)
                Next
            End Using
        End Sub

        Private Async Sub SearchBox_TextChanged(sender As Object, e As TextChangedEventArgs)
            Dim q = SearchBox.Text?.Trim()
            Using db As New AppDbContext()
                Dim query = db.Barang.AsNoTracking()
                
                If Not String.IsNullOrWhiteSpace(q) Then
                    Dim lowerQ = q.ToLower()
                    query = query.Where(Function(b) b.NamaBarang.ToLower().Contains(lowerQ) Or b.KodeBarang.ToLower().Contains(lowerQ))
                End If

                Dim list = Await query.OrderBy(Function(b) b.NamaBarang).ToListAsync()
                Products.Clear()
                For Each item In list
                    Products.Add(item)
                Next
            End Using
        End Sub

        Private Async Sub ExportExcel_Click(sender As Object, e As RoutedEventArgs)
            Dim sfd As New SaveFileDialog()
            sfd.Filter = "Excel Files (*.xlsx)|*.xlsx"
            sfd.FileName = $"Data_Barang_{Date.Now:yyyyMMdd_HHmm}.xlsx"
            
            If sfd.ShowDialog() = True Then
                Try
                    Dim excelService As New ExcelService()
                    Await excelService.ExportProductsToExcel(sfd.FileName)
                    MessageBox.Show("Data barang berhasil diexport ke Excel.", "Berhasil", MessageBoxButton.OK, MessageBoxImage.Information)
                Catch ex As Exception
                    MessageBox.Show($"Gagal export: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error)
                End Try
            End If
        End Sub

        Private Async Sub ImportExcel_Click(sender As Object, e As RoutedEventArgs)
            Dim ofd As New OpenFileDialog()
            ofd.Filter = "Excel Files (*.xlsx)|*.xlsx"
            
            If ofd.ShowDialog() = True Then
                Try
                    Dim excelService As New ExcelService()
                    Dim result = Await excelService.ImportProductsFromExcel(ofd.FileName)
                    
                    If result.Success Then
                        MessageBox.Show(result.Message, "Berhasil", MessageBoxButton.OK, MessageBoxImage.Information)
                        LoadProducts()
                    Else
                        MessageBox.Show(result.Message, "Gagal", MessageBoxButton.OK, MessageBoxImage.Error)
                    End If
                Catch ex As Exception
                    MessageBox.Show($"Gagal import: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error)
                End Try
            End If
        End Sub

        Private Sub AddProduct_Click(sender As Object, e As RoutedEventArgs)
            Dim form As New ProductFormWindow()
            If form.ShowDialog() = True Then
                LoadProducts()
            End If
        End Sub

        Private Sub EditProduct_Click(sender As Object, e As RoutedEventArgs)
            Dim btn = TryCast(sender, Button)
            Dim item = TryCast(btn.DataContext, Barang)
            If item IsNot Nothing Then
                Dim form As New ProductFormWindow(item.Id)
                If form.ShowDialog() = True Then
                    LoadProducts()
                End If
            End If
        End Sub

        Private Async Sub DeleteProduct_Click(sender As Object, e As RoutedEventArgs)
            Dim btn = TryCast(sender, Button)
            Dim item = TryCast(btn.DataContext, Barang)
            If item IsNot Nothing Then
                If MessageBox.Show($"Yakin hapus '{item.NamaBarang}'?", "Konfirmasi", MessageBoxButton.YesNo, MessageBoxImage.Warning) = MessageBoxResult.Yes Then
                    Using db As New AppDbContext()
                        Dim b = Await db.Barang.FindAsync(item.Id)
                        If b IsNot Nothing Then
                            db.Barang.Remove(b)
                            Await db.SaveChangesAsync()
                            LoadProducts()
                        End If
                    End Using
                End If
            End If
        End Sub
    End Class
End Namespace

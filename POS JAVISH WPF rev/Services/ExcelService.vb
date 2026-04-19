Imports ClosedXML.Excel
Imports POS_JAVISH_WPF.Models
Imports POS_JAVISH_WPF.Data
Imports Microsoft.EntityFrameworkCore

Namespace Services
    Public Class ExcelService
        ''' <summary>
        ''' Export data barang ke file Excel.
        ''' </summary>
        Public Async Function ExportProductsToExcel(filePath As String) As Task
            Using db As New AppDbContext()
                Dim products = Await db.Barang.AsNoTracking().ToListAsync()

                Using workbook As New XLWorkbook()
                    Dim worksheet = workbook.Worksheets.Add("Data Barang")

                    ' Header
                    worksheet.Cell(1, 1).Value = "Kode Barang"
                    worksheet.Cell(1, 2).Value = "Nama Barang"
                    worksheet.Cell(1, 3).Value = "Harga Modal"
                    worksheet.Cell(1, 4).Value = "Harga Jual"
                    worksheet.Cell(1, 5).Value = "Satuan"
                    worksheet.Cell(1, 6).Value = "Stok"
                    worksheet.Cell(1, 7).Value = "Stok Minimum"

                    ' Style Header
                    Dim headerRange = worksheet.Range("A1:G1")
                    headerRange.Style.Font.Bold = True
                    headerRange.Style.Fill.BackgroundColor = XLColor.LightGray

                    ' Data
                    For i As Integer = 0 To products.Count - 1
                        Dim p = products(i)
                        Dim row = i + 2
                        worksheet.Cell(row, 1).Value = p.KodeBarang
                        worksheet.Cell(row, 2).Value = p.NamaBarang
                        worksheet.Cell(row, 3).Value = p.HargaModal
                        worksheet.Cell(row, 4).Value = p.HargaJual
                        worksheet.Cell(row, 5).Value = p.Satuan
                        worksheet.Cell(row, 6).Value = p.Stok
                        worksheet.Cell(row, 7).Value = p.StokMinimum
                    Next

                    worksheet.Columns().AdjustToContents()
                    workbook.SaveAs(filePath)
                End Using
            End Using
        End Function

        ''' <summary>
        ''' Import data barang dari file Excel. 
        ''' Jika kode barang sudah ada, maka data akan diupdate.
        ''' Jika kode barang belum ada, maka data akan ditambah baru.
        ''' </summary>
        Public Async Function ImportProductsFromExcel(filePath As String) As Task(Of (Success As Boolean, Message As String))
            Try
                Using workbook As New XLWorkbook(filePath)
                    Dim worksheet = workbook.Worksheet(1)
                    Dim rows = worksheet.RangeUsed().RowsUsed().Skip(1) ' Lewati header

                    Using db As New AppDbContext()
                        For Each row In rows
                            Dim kode = row.Cell(1).GetValue(Of String)().Trim()
                            If String.IsNullOrWhiteSpace(kode) Then Continue For

                            Dim existing = Await db.Barang.FirstOrDefaultAsync(Function(b) b.KodeBarang = kode)

                            If existing IsNot Nothing Then
                                ' Update
                                existing.NamaBarang = row.Cell(2).GetValue(Of String)()
                                existing.HargaModal = If(row.Cell(3).IsEmpty(), 0D, row.Cell(3).GetValue(Of Decimal)())
                                existing.HargaJual = If(row.Cell(4).IsEmpty(), 0D, row.Cell(4).GetValue(Of Decimal)())
                                existing.Satuan = row.Cell(5).GetValue(Of String)()
                                existing.Stok = If(row.Cell(6).IsEmpty(), 0, row.Cell(6).GetValue(Of Integer)())
                                existing.StokMinimum = If(row.Cell(7).IsEmpty(), 0, row.Cell(7).GetValue(Of Integer)())
                            Else
                                ' Tambah Baru
                                db.Barang.Add(New Barang With {
                                    .KodeBarang = kode,
                                    .NamaBarang = row.Cell(2).GetValue(Of String)(),
                                    .HargaModal = If(row.Cell(3).IsEmpty(), 0D, row.Cell(3).GetValue(Of Decimal)()),
                                    .HargaJual = If(row.Cell(4).IsEmpty(), 0D, row.Cell(4).GetValue(Of Decimal)()),
                                    .Satuan = row.Cell(5).GetValue(Of String)(),
                                    .Stok = If(row.Cell(6).IsEmpty(), 0, row.Cell(6).GetValue(Of Integer)()),
                                    .StokMinimum = If(row.Cell(7).IsEmpty(), 0, row.Cell(7).GetValue(Of Integer)())
                                })
                            End If
                        Next
                        Await db.SaveChangesAsync()
                    End Using
                End Using
                Return (True, "Data barang berhasil diimport.")
            Catch ex As Exception
                Return (False, $"Gagal import: {ex.Message}")
            End Try
        End Function
    End Class
End Namespace

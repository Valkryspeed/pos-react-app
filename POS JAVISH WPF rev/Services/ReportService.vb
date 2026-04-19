Imports ClosedXML.Excel
Imports PdfSharp.Pdf
Imports PdfSharp.Drawing
Imports POS_JAVISH_WPF.Data
Imports Microsoft.EntityFrameworkCore

Namespace Services
    Public Class ReportService
        Public Sub ExportPenjualanHarianKeExcel(path As String, tanggal As Date)
            Using db As New AppDbContext(), wb = New XLWorkbook()
                Dim ws = wb.Worksheets.Add("Penjualan Harian")
                ws.Cell(1, 1).Value = "Tanggal"
                ws.Cell(1, 2).Value = "No Transaksi"
                ws.Cell(1, 3).Value = "Total"
                Dim row = 2
                Dim list = db.Transaksi.AsNoTracking().Where(Function(t) t.Tanggal.Date = tanggal.Date).ToList()
                For Each t In list
                    ws.Cell(row, 1).Value = t.Tanggal
                    ws.Cell(row, 2).Value = t.NoTransaksi
                    ws.Cell(row, 3).Value = t.Total
                    row += 1
                Next
                wb.SaveAs(path)
            End Using
        End Sub

        Public Sub ExportPenjualanHarianKePdf(path As String, tanggal As Date)
            Using db As New AppDbContext()
                Dim doc = New PdfDocument()
                Dim page = doc.AddPage()
                Dim gfx = XGraphics.FromPdfPage(page)
                Dim font = New XFont("Consolas", 12)
                Dim y As Double = 30
                gfx.DrawString($"Penjualan Harian - {tanggal:dd/MM/yyyy}", font, XBrushes.Black, New XPoint(30, y))
                y += 20
                For Each t In db.Transaksi.AsNoTracking().Where(Function(x) x.Tanggal.Date = tanggal.Date).ToList()
                    gfx.DrawString($"{t.NoTransaksi}  {t.Total:C}", font, XBrushes.Black, New XPoint(30, y))
                    y += 18
                Next
                doc.Save(path)
            End Using
        End Sub
    End Class
End Namespace

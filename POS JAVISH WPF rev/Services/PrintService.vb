Imports System.Printing
Imports System.Runtime.InteropServices
Imports System.Text
Imports System.Windows
Imports System.Windows.Controls
Imports System.Windows.Documents
Imports System.Windows.Media
Imports POS_JAVISH_WPF.Helpers
Imports POS_JAVISH_WPF.Models

Namespace Services
    Public Class PrintService
        Public Shared Sub PrintStruk(transactionId As String, items As IEnumerable(Of CartItem), total As Decimal, bayar As Decimal, kembalian As Decimal)
            Dim settings = SettingsService.Current
            Dim printerName = settings.PrinterName
            If Not String.IsNullOrWhiteSpace(printerName) Then
                Dim payload = BuildEscPosPayload(settings, transactionId, items, total, bayar, kembalian)
                If payload IsNot Nothing AndAlso payload.Length > 0 Then
                    If TrySendRaw(printerName, payload) Then Return
                End If
            End If

            Dim doc = CreateFlowDocument(settings, transactionId, items, total, bayar, kembalian)
            PrintFlowDocument(settings, transactionId, doc)
        End Sub

        Private Shared Sub PrintFlowDocument(settings As AppSettings, transactionId As String, doc As FlowDocument)
            Dim pd As New PrintDialog()

            Try
                If Not String.IsNullOrEmpty(settings.PrinterName) Then
                    Dim server = New LocalPrintServer()
                    Dim queue = server.GetPrintQueue(settings.PrinterName)
                    pd.PrintQueue = queue
                End If
            Catch
            End Try

            Dim paginator = CType(doc, IDocumentPaginatorSource).DocumentPaginator
            Dim jobName = $"Struk-{transactionId}"

            Try
                If settings.AutoPrint Then
                    pd.PrintDocument(paginator, jobName)
                Else
                    If pd.ShowDialog().GetValueOrDefault(False) Then
                        pd.PrintDocument(paginator, jobName)
                    End If
                End If
            Catch ex As Exception
                MessageBox.Show($"Gagal mencetak: {ex.Message}", "Print Error", MessageBoxButton.OK, MessageBoxImage.Warning)
            End Try
        End Sub

        Private Shared Function BuildEscPosPayload(settings As AppSettings, transactionId As String, items As IEnumerable(Of CartItem), total As Decimal, bayar As Decimal, kembalian As Decimal) As Byte()
            Dim is80mm = (settings.PaperSize = 80)
            Dim widthChars As Integer = If(is80mm, 48, 32)

            Dim esc = ChrW(&H1B)
            Dim gs = ChrW(&H1D)

            Dim lines = BuildReceiptLines(settings, transactionId, items, total, bayar, kembalian)
            Dim textBody = String.Join(vbLf, lines)

            Dim payload = esc & "@" ' Initialize
            
            ' Buka Cash Drawer jika aktif
            If settings.AutoOpenDrawer Then
                ' ESC p m t1 t2 (Open Drawer)
                ' m = 0 (Pin 2), t1 = 50, t2 = 50
                payload &= esc & "p" & ChrW(0) & ChrW(50) & ChrW(50)
            End If

            payload &= vbLf & textBody & vbLf & vbLf &
                gs & "V" & ChrW(1) ' Cut Paper

            Return Encoding.ASCII.GetBytes(payload)
        End Function

        Public Shared Sub PrintShiftReport(shift As Shift)
            Dim settings = SettingsService.Current
            If String.IsNullOrWhiteSpace(settings.PrinterName) Then Return

            Dim lines As New List(Of String)()
            lines.Add(CenterAlign(settings.StoreName, settings.PaperSize))
            lines.Add(CenterAlign(settings.StoreAddress, settings.PaperSize))
            lines.Add(CenterAlign(settings.StorePhone, settings.PaperSize))
            lines.Add(New String("-"c, GetWidth(settings.PaperSize)))
            lines.Add(CenterAlign("LAPORAN SHIFT KASIR", settings.PaperSize))
            lines.Add(New String("-"c, GetWidth(settings.PaperSize)))
            
            lines.Add($"Mulai   : {shift.ShiftStart:dd/MM/yyyy HH:mm}")
            lines.Add($"Selesai : {shift.ShiftEnd:dd/MM/yyyy HH:mm}")
            lines.Add($"Status  : {shift.Status}")
            lines.Add(New String("-"c, GetWidth(settings.PaperSize)))
            
            lines.Add(FormatLine("Modal Awal", shift.ModalAwal.ToString("N0"), settings.PaperSize))
            lines.Add(FormatLine("Total Penjualan", shift.TotalPenjualan.ToString("N0"), settings.PaperSize))
            lines.Add(FormatLine("Total Cash", shift.TotalCash.ToString("N0"), settings.PaperSize))
            lines.Add(New String("-"c, GetWidth(settings.PaperSize)))
            
            Dim expectedCash = shift.ModalAwal + shift.TotalCash
            lines.Add(FormatLine("Uang di Laci (Seharusnya)", expectedCash.ToString("N0"), settings.PaperSize))
            lines.Add(FormatLine("Uang Fisik", shift.UangFisik.ToString("N0"), settings.PaperSize))
            lines.Add(FormatLine("Selisih", shift.Selisih.ToString("N0"), settings.PaperSize))
            lines.Add(New String("-"c, GetWidth(settings.PaperSize)))
            
            lines.Add(vbLf & vbLf & CenterAlign("TERIMA KASIH", settings.PaperSize) & vbLf & vbLf)

            Dim esc = ChrW(27)
            Dim gs = ChrW(29)
            Dim payload = esc & "@" & String.Join(vbLf, lines) & gs & "V" & ChrW(1)

            TrySendRaw(settings.PrinterName, Encoding.ASCII.GetBytes(payload))
        End Sub

        Private Shared Function GetWidth(paperSize As Integer) As Integer
            Return If(paperSize = 80, 48, 32)
        End Function

        Private Shared Function CenterAlign(text As String, paperSize As Integer) As String
            Dim width = GetWidth(paperSize)
            If String.IsNullOrEmpty(text) Then Return ""
            Dim t = If(text.Length > width, text.Substring(0, width), text)
            Dim pad = Math.Max(0, (width - t.Length) \ 2)
            Return New String(" "c, pad) & t
        End Function

        Private Shared Function FormatLine(left As String, right As String, paperSize As Integer) As String
            Dim width = GetWidth(paperSize)
            left = If(left, "")
            right = If(right, "")
            Dim spaces = Math.Max(1, width - left.Length - right.Length)
            Return left & New String(" "c, spaces) & right
        End Function

        <StructLayout(LayoutKind.Sequential, CharSet:=CharSet.Unicode)>
        Private Structure DOC_INFO_1
            <MarshalAs(UnmanagedType.LPWStr)>
            Public pDocName As String
            <MarshalAs(UnmanagedType.LPWStr)>
            Public pOutputFile As String
            <MarshalAs(UnmanagedType.LPWStr)>
            Public pDatatype As String
        End Structure

        <DllImport("winspool.drv", SetLastError:=True, CharSet:=CharSet.Unicode)>
        Private Shared Function OpenPrinter(pPrinterName As String, ByRef phPrinter As IntPtr, pDefault As IntPtr) As Boolean
        End Function

        <DllImport("winspool.drv", SetLastError:=True)>
        Private Shared Function ClosePrinter(hPrinter As IntPtr) As Boolean
        End Function

        <DllImport("winspool.drv", SetLastError:=True, CharSet:=CharSet.Unicode)>
        Private Shared Function StartDocPrinter(hPrinter As IntPtr, level As Integer, ByRef di As DOC_INFO_1) As Integer
        End Function

        <DllImport("winspool.drv", SetLastError:=True)>
        Private Shared Function EndDocPrinter(hPrinter As IntPtr) As Boolean
        End Function

        <DllImport("winspool.drv", SetLastError:=True)>
        Private Shared Function StartPagePrinter(hPrinter As IntPtr) As Boolean
        End Function

        <DllImport("winspool.drv", SetLastError:=True)>
        Private Shared Function EndPagePrinter(hPrinter As IntPtr) As Boolean
        End Function

        <DllImport("winspool.drv", SetLastError:=True)>
        Private Shared Function WritePrinter(hPrinter As IntPtr, pBytes As IntPtr, dwCount As Integer, ByRef dwWritten As Integer) As Boolean
        End Function

        Private Shared Function TrySendRaw(printerName As String, data As Byte()) As Boolean
            Dim hPrinter As IntPtr = IntPtr.Zero
            If Not OpenPrinter(printerName, hPrinter, IntPtr.Zero) Then Return False

            Dim di As New DOC_INFO_1 With {
                .pDocName = "POS Struk",
                .pDatatype = "RAW",
                .pOutputFile = Nothing
            }

            Dim jobId = StartDocPrinter(hPrinter, 1, di)
            If jobId <= 0 Then
                ClosePrinter(hPrinter)
                Return False
            End If

            If Not StartPagePrinter(hPrinter) Then
                EndDocPrinter(hPrinter)
                ClosePrinter(hPrinter)
                Return False
            End If

            Dim unmanagedPointer = Marshal.AllocCoTaskMem(data.Length)
            Marshal.Copy(data, 0, unmanagedPointer, data.Length)

            Dim written As Integer = 0
            Dim success = WritePrinter(hPrinter, unmanagedPointer, data.Length, written)

            Marshal.FreeCoTaskMem(unmanagedPointer)

            EndPagePrinter(hPrinter)
            EndDocPrinter(hPrinter)
            ClosePrinter(hPrinter)

            Return success AndAlso written = data.Length
        End Function

        Private Shared Function BuildReceiptLines(settings As AppSettings, transactionId As String, items As IEnumerable(Of CartItem), total As Decimal, bayar As Decimal, kembalian As Decimal) As List(Of String)
            Dim is80mm = (settings.PaperSize = 80)
            Dim widthChars As Integer = If(is80mm, 48, 32)

            Dim result As New List(Of String)()
            Dim kasirName = ResolveKasirName()

            result.Add(CenterText(settings.StoreName, widthChars))
            If Not String.IsNullOrWhiteSpace(settings.StoreAddress) Then
                result.Add(CenterText(settings.StoreAddress, widthChars))
            End If
            result.Add($"No: {transactionId}")
            result.Add($"Tgl: {DateTime.Now:dd/MM/yyyy HH:mm}")
            result.Add($"Kasir: {TruncateText(kasirName, widthChars - 7)}")
            result.Add(New String("-"c, widthChars))

            For Each it In items
                Dim name = TruncateText(it.NamaBarang, widthChars)
                result.Add(name)

                Dim left = $"{it.Qty}x{it.HargaJual:N0}"
                Dim right = $"{it.Subtotal:N0}"
                result.Add(LeftRightText(left, right, widthChars))
            Next

            result.Add(New String("-"c, widthChars))
            result.Add(LeftRightText("Total", $"{total:N0}", widthChars))
            result.Add(LeftRightText("Bayar", $"{bayar:N0}", widthChars))
            result.Add(LeftRightText("Kembali", $"{kembalian:N0}", widthChars))
            result.Add(New String("-"c, widthChars))
            
            If Not String.IsNullOrWhiteSpace(settings.ReceiptFooter) Then
                Dim footerLines = settings.ReceiptFooter.Split({vbCrLf, vbLf}, StringSplitOptions.None)
                For Each fl In footerLines
                    result.Add(CenterText(fl, widthChars))
                Next
            End If

            If Not String.IsNullOrWhiteSpace(settings.StorePhone) Then
                result.Add(CenterText(settings.StorePhone, widthChars))
            End If
            result.Add("")

            Return result
        End Function

        Private Shared Function ResolveKasirName() As String
            Dim u = Session.CurrentUser
            Dim fullName = u?.FullName
            If Not String.IsNullOrWhiteSpace(fullName) Then Return fullName.Trim()
            Dim username = u?.Username
            If Not String.IsNullOrWhiteSpace(username) Then Return username.Trim()
            Return "Kasir"
        End Function

        Private Shared Function CenterText(text As String, widthChars As Integer) As String
            If String.IsNullOrEmpty(text) Then Return ""
            Dim t = TruncateText(text, widthChars)
            Dim pad = Math.Max(0, (widthChars - t.Length) \ 2)
            Return New String(" "c, pad) & t
        End Function

        Private Shared Function LeftRightText(left As String, right As String, widthChars As Integer) As String
            left = If(left, "")
            right = If(right, "")
            If right.Length >= widthChars Then
                Return TruncateText(right, widthChars)
            End If

            Dim maxLeft = Math.Max(0, widthChars - right.Length - 1)
            Dim l = TruncateText(left, maxLeft)
            Dim spaces = Math.Max(1, widthChars - l.Length - right.Length)
            Return l & New String(" "c, spaces) & right
        End Function

        Private Shared Function TruncateText(text As String, maxChars As Integer) As String
            If String.IsNullOrEmpty(text) OrElse maxChars <= 0 Then Return ""
            If text.Length <= maxChars Then Return text
            Return text.Substring(0, maxChars)
        End Function

        Private Shared Function CreateFlowDocument(settings As AppSettings, transactionId As String, items As IEnumerable(Of CartItem), total As Decimal, bayar As Decimal, kembalian As Decimal) As FlowDocument
            Dim is80mm = (settings.PaperSize = 80)
            ' Adjust width for thermal printers (58mm is very narrow)
            ' 58mm -> ~48mm printable -> ~180px @ 96dpi
            ' 80mm -> ~72mm printable -> ~270px @ 96dpi
            Dim pageWidth As Double = If(is80mm, 270, 170) 
            
            Dim doc As New FlowDocument() With {
                .PagePadding = New Thickness(0, 0, 0, 0),
                .ColumnWidth = pageWidth,
                .FontFamily = New FontFamily("Courier New"), ' Monospaced font is safer
                .FontSize = If(is80mm, 10, 8), ' Small font for 58mm
                .Background = Brushes.White,
                .TextAlignment = TextAlignment.Left
            }
            
            doc.PageWidth = pageWidth + 5 
            
            ' Header
            doc.Blocks.Add(New Paragraph(New Run(settings.StoreName)) With {
                .TextAlignment = TextAlignment.Center, 
                .FontWeight = FontWeights.Bold, 
                .FontSize = If(is80mm, 12, 10),
                .Margin = New Thickness(0, 5, 0, 0)
            })
            doc.Blocks.Add(New Paragraph(New Run(settings.StoreAddress)) With {
                .TextAlignment = TextAlignment.Center,
                .FontSize = If(is80mm, 9, 8),
                .Margin = New Thickness(0, 0, 0, 5)
            })
            Dim kasirName = ResolveKasirName()
            doc.Blocks.Add(New Paragraph(New Run($"No: {transactionId}" & vbCrLf & $"Tgl: {DateTime.Now:dd/MM/yyyy HH:mm}" & vbCrLf & $"Kasir: {kasirName}")) With {
                .TextAlignment = TextAlignment.Left,
                .FontSize = If(is80mm, 9, 8),
                .Margin = New Thickness(0, 0, 0, 5)
            })
            
            ' Separator (Using Border instead of text dashes to prevent wrapping)
            Dim separator As New BlockUIContainer(New Border With {
                .BorderThickness = New Thickness(0, 1, 0, 0),
                .BorderBrush = Brushes.Black,
                .Margin = New Thickness(2, 5, 2, 5),
                .Height = 1
            })
            doc.Blocks.Add(separator)
            
            ' Items Table
            Dim itemTable As New Table() With {.CellSpacing = 0, .Margin = New Thickness(0)}
            ' Column widths: Star for Qty/Name, Auto for Price/Subtotal
            itemTable.Columns.Add(New TableColumn() With {.Width = New GridLength(1, GridUnitType.Star)}) 
            itemTable.Columns.Add(New TableColumn() With {.Width = New GridLength(1, GridUnitType.Auto)}) 
            
            Dim itemGroup As New TableRowGroup()
            
            For Each item In items
                ' Row 1: Name (Full width, bold)
                Dim rowName As New TableRow()
                Dim cellName As New TableCell(New Paragraph(New Run(item.NamaBarang)) With {.FontWeight = FontWeights.Bold, .Margin = New Thickness(0)}) With {
                    .ColumnSpan = 2,
                    .Padding = New Thickness(0, 2, 0, 0)
                }
                rowName.Cells.Add(cellName)
                itemGroup.Rows.Add(rowName)
                
                ' Row 2: Details (Qty x Price ... Subtotal)
                Dim rowDetails As New TableRow()
                Dim qtyPrice = $"{item.Qty}x{item.HargaJual:N0}" ' Remove spaces to save width
                
                rowDetails.Cells.Add(New TableCell(New Paragraph(New Run(qtyPrice)) With {.Margin = New Thickness(0, 0, 0, 2)}) With {.Padding = New Thickness(0)})
                rowDetails.Cells.Add(New TableCell(New Paragraph(New Run(item.Subtotal.ToString("N0"))) With {.TextAlignment = TextAlignment.Right, .FontWeight = FontWeights.Bold, .Margin = New Thickness(0, 0, 0, 2)}) With {.Padding = New Thickness(0)})
                itemGroup.Rows.Add(rowDetails)
            Next
            
            itemTable.RowGroups.Add(itemGroup)
            doc.Blocks.Add(itemTable)
            
            ' Separator
            Dim separator2 As New BlockUIContainer(New Border With {
                .BorderThickness = New Thickness(0, 1, 0, 0),
                .BorderBrush = Brushes.Black,
                .Margin = New Thickness(2, 5, 2, 5),
                .Height = 1
            })
            doc.Blocks.Add(separator2)
            
            ' Totals Table
            Dim totalTable As New Table() With {.CellSpacing = 0, .Margin = New Thickness(0)}
            totalTable.Columns.Add(New TableColumn() With {.Width = New GridLength(1, GridUnitType.Star)})
            totalTable.Columns.Add(New TableColumn() With {.Width = New GridLength(1, GridUnitType.Auto)})
            
            Dim totalGroup As New TableRowGroup()
            AddRow(totalGroup, "Total", total.ToString("N0"), FontWeights.Bold)
            AddRow(totalGroup, "Bayar", bayar.ToString("N0"), FontWeights.Normal)
            AddRow(totalGroup, "Kembali", kembalian.ToString("N0"), FontWeights.Bold)
            
            totalTable.RowGroups.Add(totalGroup)
            doc.Blocks.Add(totalTable)
            
            ' Footer
            Dim footerText As String = "Terima Kasih" & vbCrLf & "Selamat Belanja Kembali"
            If Not String.IsNullOrWhiteSpace(settings.StorePhone) Then
                footerText &= vbCrLf & settings.StorePhone
            End If

            doc.Blocks.Add(New Paragraph(New Run(footerText)) With {
                .TextAlignment = TextAlignment.Center,
                .Margin = New Thickness(0, 10, 0, 0),
                .FontSize = If(is80mm, 9, 8),
                .FontStyle = FontStyles.Italic
            })

            Return doc
        End Function
        
        Private Shared Sub AddRow(group As TableRowGroup, label As String, value As String, weight As FontWeight)
            Dim row As New TableRow()
            row.Cells.Add(New TableCell(New Paragraph(New Run(label)) With {.FontWeight = weight, .Margin = New Thickness(0)}) With {.Padding = New Thickness(0)})
            row.Cells.Add(New TableCell(New Paragraph(New Run(value)) With {.FontWeight = weight, .TextAlignment = TextAlignment.Right, .Margin = New Thickness(0)}) With {.Padding = New Thickness(0)})
            group.Rows.Add(row)
        End Sub
    End Class
End Namespace

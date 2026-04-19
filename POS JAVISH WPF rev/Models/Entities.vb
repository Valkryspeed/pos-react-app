Imports System.ComponentModel.DataAnnotations
Imports System.ComponentModel.DataAnnotations.Schema

Namespace Models
    <Table("Users")>
    Public Class User
        <Key>
        Public Property Id As Integer
        <Required>
        Public Property Username As String
        <Required>
        Public Property PasswordHash As String
        <Required>
        Public Property Role As String ' Admin, Kasir, Supervisor
        Public Property FullName As String
        Public Property IsActive As Boolean = True
    End Class

    <Table("Suppliers")>
    Public Class Supplier
        <Key>
        Public Property Id As Integer
        <Required>
        Public Property Nama As String
        Public Property Alamat As String
        Public Property Telepon As String
        Public Property Email As String
        Public Property IsActive As Boolean = True
    End Class

    <Table("Pembelian")>
    Public Class Purchase
        <Key>
        Public Property Id As Integer
        <Required>
        Public Property NoFaktur As String
        <Required>
        Public Property Tanggal As Date
        <Required>
        Public Property SupplierId As Integer
        <Column(TypeName:="DECIMAL(15,2)")>
        Public Property Total As Decimal
        Public Property UserId As Integer
        Public Overridable Property Items As List(Of PurchaseItem)
    End Class

    <Table("PembelianItems")>
    Public Class PurchaseItem
        <Key>
        Public Property Id As Integer
        <Required>
        Public Property PurchaseId As Integer
        <Required>
        Public Property BarangId As Integer
        <Required>
        Public Property Qty As Integer
        <Column(TypeName:="DECIMAL(15,2)")>
        Public Property HargaBeli As Decimal
        <Column(TypeName:="DECIMAL(15,2)")>
        Public Property Subtotal As Decimal
    End Class

    <Table("Transaksi")>
    Public Class SaleTransaction
        <Key>
        Public Property Id As Integer
        Public Property NoTransaksi As String
        Public Property Tanggal As Date
        Public Property UserId As Integer
        <Column(TypeName:="DECIMAL(15,2)")>
        Public Property Total As Decimal
        <Column(TypeName:="DECIMAL(15,2)")>
        Public Property Diskon As Decimal
        <Column(TypeName:="DECIMAL(15,2)")>
        Public Property Bayar As Decimal
        <Column(TypeName:="DECIMAL(15,2)")>
        Public Property Kembalian As Decimal
        Public Overridable Property Items As List(Of SaleItem)
    End Class

    <Table("TransaksiItems")>
    Public Class SaleItem
        <Key>
        Public Property Id As Integer
        Public Property TransaksiId As Integer
        Public Property BarangId As Integer
        Public Property NamaBarang As String
        Public Property KodeBarang As String
        Public Property Qty As Integer
        <Column(TypeName:="DECIMAL(15,2)")>
        Public Property HargaJual As Decimal
        <Column(TypeName:="DECIMAL(15,2)")>
        Public Property HargaModal As Decimal
        <Column(TypeName:="DECIMAL(15,2)")>
        Public Property Subtotal As Decimal
    End Class

    <Table("Shifts")>
    Public Class Shift
        <Key>
        Public Property Id As Integer
        Public Property UserId As Integer
        Public Property ShiftStart As Date
        Public Property ShiftEnd As Date?
        <Column(TypeName:="DECIMAL(12,2)")>
        Public Property ModalAwal As Decimal
        <Column(TypeName:="DECIMAL(12,2)")>
        Public Property TotalPenjualan As Decimal
        <Column(TypeName:="DECIMAL(12,2)")>
        Public Property TotalDiskon As Decimal
        <Column(TypeName:="DECIMAL(12,2)")>
        Public Property TotalCash As Decimal
        Public Property TransaksiCount As Integer
        <Column(TypeName:="DECIMAL(12,2)")>
        Public Property UangFisik As Decimal
        <Column(TypeName:="DECIMAL(12,2)")>
        Public Property Selisih As Decimal
        Public Property Status As String ' OPEN, CLOSED
    End Class

    <Table("Pengeluaran")>
    Public Class Expense
        <Key>
        Public Property Id As Integer
        Public Property Tanggal As Date
        Public Property Kategori As String
        Public Property Keterangan As String
        <Column(TypeName:="DECIMAL(15,2)")>
        Public Property Jumlah As Decimal
        Public Property UserId As Integer
    End Class

    <Table("ActivityLog")>
    Public Class ActivityLog
        <Key>
        Public Property Id As Integer
        Public Property UserId As Integer
        Public Property Username As String
        Public Property Activity As String
        Public Property Timestamp As Date
        Public Property Detail As String
    End Class

    <Table("StockLog")>
    Public Class StockLog
        <Key>
        Public Property Id As Integer
        Public Property BarangId As Integer
        Public Property KodeBarang As String
        Public Property NamaBarang As String
        Public Property Tanggal As Date = Date.Now
        Public Property Tipe As String ' PENJUALAN, PEMBELIAN, ADJUSTMENT, VOID, RETUR
        Public Property QtyAwal As Integer
        Public Property QtyPerubahan As Integer ' Positif untuk tambah, negatif untuk kurang
        Public Property QtyAkhir As Integer
        Public Property Keterangan As String
        Public Property UserId As Integer
        Public Property Username As String
    End Class

    <Table("ScannerLog")>
    Public Class ScannerLog
        <Key>
        Public Property Id As Integer
        Public Property Barcode As String
        Public Property DeviceIp As String
        Public Property Mode As String ' kasir, cek_harga, opname, restock
        Public Property Waktu As Date = Date.Now
    End Class

    <Table("StockOpname")>
    Public Class StockOpname
        <Key>
        Public Property Id As Integer
        Public Property KodeBarang As String
        Public Property QtyRak As Integer
        Public Property QtySistem As Integer
        Public Property Selisih As Integer
        Public Property User As String
        Public Property Tanggal As Date = Date.Now
    End Class

    <Table("RestockLog")>
    Public Class RestockLog
        <Key>
        Public Property Id As Integer
        Public Property KodeBarang As String
        Public Property QtyTambah As Integer
        Public Property User As String
        Public Property Tanggal As Date = Date.Now
    End Class

    <Table("InvoiceSequence")>
    Public Class InvoiceSequence
        <Key>
        Public Property Id As Integer
        <Required>
        Public Property DateKey As String
        <Required>
        Public Property Terminal As String
        Public Property LastNumber As Integer
    End Class
End Namespace

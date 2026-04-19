Imports Microsoft.EntityFrameworkCore
Imports POS_JAVISH_WPF.Data
Imports POS_JAVISH_WPF.Models
Imports BCrypt.Net

Namespace Services
    Public Class DatabaseInitializer
        Public Shared Sub EnsureCreated()
            Using db As New AppDbContext()
                ' EnsureCreated() hanya membuat database jika belum ada.
                ' Jika database sudah ada tapi ada tabel baru, kita perlu memicu migrasi atau membuatnya manual.
                db.Database.EnsureCreated()

                ' Buat tabel Shifts jika belum ada (antisipasi jika EnsureCreated tidak menambahkannya ke DB lama)
                db.Database.ExecuteSqlRaw("CREATE TABLE IF NOT EXISTS Shifts (id INTEGER PRIMARY KEY AUTOINCREMENT, UserId INTEGER, ShiftStart DATETIME, ShiftEnd DATETIME, ModalAwal DECIMAL(12,2), TotalPenjualan DECIMAL(12,2), TotalCash DECIMAL(12,2), TotalDiskon DECIMAL(12,2) DEFAULT 0, TransaksiCount INTEGER DEFAULT 0, UangFisik DECIMAL(12,2), Selisih DECIMAL(12,2), Status TEXT);")

                ' Migrasi manual untuk kolom baru di Shifts
                Try
                    db.Database.ExecuteSqlRaw("ALTER TABLE Shifts ADD COLUMN TotalDiskon DECIMAL(12,2) DEFAULT 0;")
                Catch : End Try
                Try
                    db.Database.ExecuteSqlRaw("ALTER TABLE Shifts ADD COLUMN TransaksiCount INTEGER DEFAULT 0;")
                Catch : End Try

                ' Migrasi manual untuk HargaModal di TransaksiItems
                Try
                    db.Database.ExecuteSqlRaw("ALTER TABLE TransaksiItems ADD COLUMN HargaModal DECIMAL(15,2) DEFAULT 0;")
                Catch : End Try

                ' Buat tabel tambahan untuk Mobile Scanner dan Log
                db.Database.ExecuteSqlRaw("CREATE TABLE IF NOT EXISTS ScannerLog (id INTEGER PRIMARY KEY AUTOINCREMENT, Barcode TEXT, DeviceIp TEXT, Mode TEXT, Waktu DATETIME DEFAULT CURRENT_TIMESTAMP);")
                db.Database.ExecuteSqlRaw("CREATE TABLE IF NOT EXISTS StockOpname (id INTEGER PRIMARY KEY AUTOINCREMENT, KodeBarang TEXT, QtyRak INTEGER, QtySistem INTEGER, Selisih INTEGER, User TEXT, Tanggal DATETIME DEFAULT CURRENT_TIMESTAMP);")
                db.Database.ExecuteSqlRaw("CREATE TABLE IF NOT EXISTS RestockLog (id INTEGER PRIMARY KEY AUTOINCREMENT, KodeBarang TEXT, QtyTambah INTEGER, User TEXT, Tanggal DATETIME DEFAULT CURRENT_TIMESTAMP);")
                db.Database.ExecuteSqlRaw("CREATE TABLE IF NOT EXISTS StockLog (id INTEGER PRIMARY KEY AUTOINCREMENT, BarangId INTEGER, KodeBarang TEXT, NamaBarang TEXT, Tanggal DATETIME DEFAULT CURRENT_TIMESTAMP, Tipe TEXT, QtyAwal INTEGER, QtyPerubahan INTEGER, QtyAkhir INTEGER, Keterangan TEXT, UserId INTEGER, Username TEXT);")
                db.Database.ExecuteSqlRaw("CREATE TABLE IF NOT EXISTS Pengeluaran (id INTEGER PRIMARY KEY AUTOINCREMENT, Tanggal DATETIME, Kategori TEXT, Keterangan TEXT, Jumlah DECIMAL(15,2), UserId INTEGER);")
                db.Database.ExecuteSqlRaw("CREATE TABLE IF NOT EXISTS Suppliers (id INTEGER PRIMARY KEY AUTOINCREMENT, Nama TEXT, Alamat TEXT, Telepon TEXT, Email TEXT, IsActive BOOLEAN DEFAULT 1);")
                db.Database.ExecuteSqlRaw("CREATE TABLE IF NOT EXISTS InvoiceSequence (id INTEGER PRIMARY KEY AUTOINCREMENT, DateKey TEXT NOT NULL, Terminal TEXT NOT NULL, LastNumber INTEGER NOT NULL DEFAULT 0);")

                db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS idx_barang_kode ON Barang(kode_barang);")
                db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS idx_barang_nama ON Barang(nama_barang);")
                Try
                    db.Database.ExecuteSqlRaw("CREATE UNIQUE INDEX IF NOT EXISTS ux_invoice_seq_key ON InvoiceSequence(DateKey, Terminal);")
                Catch : End Try
                Try
                    db.Database.ExecuteSqlRaw("CREATE UNIQUE INDEX IF NOT EXISTS ux_transaksi_notrx ON Transaksi(NoTransaksi);")
                Catch : End Try

                If Not db.Users.Any() Then
                    Dim admin = New User() With {
                        .Username = "admin",
                        .PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
                        .Role = "Admin",
                        .FullName = "Administrator",
                        .IsActive = True
                    }
                    db.Users.Add(admin)
                    db.SaveChanges()
                Else
                    ' ONE-TIME FIX: Reset admin password to "admin123" if the hash looks corrupted
                    Dim adminUser = db.Users.FirstOrDefault(Function(u) u.Username = "admin")
                    If adminUser IsNot Nothing AndAlso adminUser.PasswordHash.Contains("6vG.G.e") Then
                        adminUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123")
                        db.SaveChanges()
                    End If
                End If
            End Using
        End Sub
    End Class
End Namespace

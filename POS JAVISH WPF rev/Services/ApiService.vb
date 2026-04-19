Imports Microsoft.AspNetCore.Builder
Imports Microsoft.AspNetCore.Http
Imports Microsoft.Extensions.DependencyInjection
Imports Microsoft.EntityFrameworkCore
Imports POS_JAVISH_WPF.Data
Imports POS_JAVISH_WPF.Models
Imports System.Linq
Imports Microsoft.AspNetCore.Hosting

Namespace Services
    Public Class ScanRequest
        Public Property barcode As String
    End Class

    Public Class OpnameRequest
        Public Property barcode As String
        Public Property qty_rak As Integer
    End Class

    Public Class RestockRequest
        Public Property barcode As String
        Public Property qty_tambah As Integer
    End Class

    Public Class CustomerDisplayData
        Public Property StoreName As String = ""
        Public Property Items As New List(Of CustomerCartItem)()
        Public Property Subtotal As Decimal = 0
        Public Property Diskon As Decimal = 0
        Public Property Total As Decimal = 0
        Public Property Bayar As Decimal = 0
        Public Property Kembalian As Decimal = 0
        Public Property Status As String = "WAITING" ' WAITING, TRANSACTING, SUCCESS
    End Class

    Public Class CustomerCartItem
        Public Property Nama As String = ""
        Public Property Qty As Integer = 0
        Public Property Harga As Decimal = 0
        Public Property Subtotal As Decimal = 0
    End Class

    Public Class MobileScanQueue
        Private Shared _queue As New System.Collections.Concurrent.ConcurrentQueue(Of String)()
        Public Shared Property CurrentDisplay As New CustomerDisplayData()

        Public Shared Sub Enqueue(barcode As String)
            _queue.Enqueue(barcode)
        End Sub
        Public Shared Function Dequeue() As String
            Dim res As String = Nothing
            _queue.TryDequeue(res)
            Return res
        End Function
    End Class

    Public Class ApiService
        Private Shared _app As WebApplication

        Public Shared Sub Start()
            Task.Run(Sub()
                         Try
                             Dim builder = WebApplication.CreateBuilder()
                             
                             builder.WebHost.ConfigureKestrel(Sub(options)
                                                                 options.ListenAnyIP(5050) ' HTTP
                                                             End Sub)

                             ' CORS for mobile/web access
                             builder.Services.AddCors(Sub(options)
                                                         options.AddPolicy("AllowAll", Sub(policy)
                                                                                          policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()
                                                                                      End Sub)
                                                     End Sub)

                             _app = builder.Build()
                             _app.UseCors("AllowAll")

                             ' --- PAGES ---
                             ' Root Dashboard
                             _app.MapGet("/", Function(context As HttpContext)
                                                  context.Response.ContentType = "text/html"
                                                  Return GetDashboardHtml()
                                              End Function)

                             ' Mobile Scanner Page
                             _app.MapGet("/mobile", Function(context As HttpContext)
                                                        context.Response.ContentType = "text/html"
                                                        Return GetMobileScannerHtml()
                                                    End Function)

                             ' Customer Display Page
                             _app.MapGet("/customer", Function(context As HttpContext)
                                                          context.Response.ContentType = "text/html"
                                                          Return GetCustomerDisplayHtml()
                                                      End Function)

                             ' --- UTILITY ---
                             _app.MapGet("/api/ping", Function()
                                                          Console.WriteLine("API: Ping received")
                                                          Return Results.Ok(New With {.status = "ok", .message = "Pong!", .time = DateTime.Now.ToString()})
                                                      End Function)

                             ' --- MOBILE SCANNER API ---
                             Dim validateToken = Function(token As String) As Boolean
                                                     Return token = "POSJAVISH"
                                                 End Function

                             ' 1. Scan Kasir
                             _app.MapPost("/api/scan", Function(data As ScanRequest, token As String, context As HttpContext)
                                                           If Not validateToken(token) Then Return Results.Unauthorized()
                                                           Try
                                                               Using db As New AppDbContext()
                                                                   Dim b = db.Barang.FirstOrDefault(Function(x) x.KodeBarang = data.barcode)
                                                                   If b Is Nothing Then Return Results.NotFound(New With {.message = "Barang tidak ditemukan"})

                                                                   ' Log activity
                                                                   db.ScannerLog.Add(New ScannerLog With {
                                                                       .Barcode = data.barcode,
                                                                       .DeviceIp = context.Connection.RemoteIpAddress?.ToString(),
                                                                       .Mode = "kasir"
                                                                   })
                                                                   db.SaveChanges()

                                                                   If b.Stok <= 0 Then
                                                                       Return Results.Ok(New With {.status = "STOK_HABIS", .message = "STOK HABIS. Barang tidak dapat dijual."})
                                                                   End If

                                                                   ' Trigger event to WPF KasirView
                                                                   MobileScanQueue.Enqueue(data.barcode)

                                                                   Return Results.Ok(New With {.status = "Success", .nama = b.NamaBarang})
                                                               End Using
                                                           Catch ex As Exception
                                                               Return Results.Problem(ex.Message)
                                                           End Try
                                                       End Function)

                             ' 2. Cek Harga
                             _app.MapGet("/api/product/{barcode}", Function(barcode As String, token As String)
                                                                       If Not validateToken(token) Then Return Results.Unauthorized()
                                                                       Using db As New AppDbContext()
                                                                           Dim b = db.Barang.FirstOrDefault(Function(x) x.KodeBarang = barcode)
                                                                           If b Is Nothing Then Return Results.NotFound()
                                                                           Return Results.Ok(New With {
                                                                               .nama = b.NamaBarang,
                                                                               .harga = b.HargaJual,
                                                                               .stok = b.Stok
                                                                           })
                                                                       End Using
                                                                   End Function)

                             ' 3. Stock Opname
                             _app.MapPost("/api/opname", Function(data As OpnameRequest, token As String, context As HttpContext)
                                                             If Not validateToken(token) Then Return Results.Unauthorized()
                                                             Using db As New AppDbContext()
                                                                 Dim b = db.Barang.FirstOrDefault(Function(x) x.KodeBarang = data.barcode)
                                                                 If b Is Nothing Then Return Results.NotFound()

                                                                 Dim selisih = data.qty_rak - b.Stok
                                                                 db.StockOpname.Add(New StockOpname With {
                                                                     .KodeBarang = data.barcode,
                                                                     .QtyRak = data.qty_rak,
                                                                     .QtySistem = b.Stok,
                                                                     .Selisih = selisih,
                                                                     .User = "Mobile Scanner"
                                                                 })
                                                                 db.ScannerLog.Add(New ScannerLog With {
                                                                     .Barcode = data.barcode,
                                                                     .DeviceIp = context.Connection.RemoteIpAddress?.ToString(),
                                                                     .Mode = "opname"
                                                                 })
                                                                 db.SaveChanges()
                                                                 Return Results.Ok(New With {.status = "Success", .selisih = selisih})
                                                             End Using
                                                         End Function)

                             ' 4. Restock
                             _app.MapPost("/api/restock", Function(data As RestockRequest, token As String, context As HttpContext)
                                                              If Not validateToken(token) Then Return Results.Unauthorized()
                                                              Using db As New AppDbContext()
                                                                  Dim b = db.Barang.FirstOrDefault(Function(x) x.KodeBarang = data.barcode)
                                                                  If b Is Nothing Then Return Results.NotFound()

                                                                  b.Stok += data.qty_tambah
                                                                  
                                                                  ' LOG 1: ActivityLog (RESTOCK) for LogPembelianView
                                                                  ' Since this is mobile, we don't have NoFaktur or SupplierId easily, but we should at least log it.
                                                                  Dim detailLog = $"NoFaktur=MOBILE-{DateTime.Now:yyyyMMddHHmmss}; BarangId={b.Id}; Kode={b.KodeBarang}; Nama={b.NamaBarang}; Satuan={If(b.Satuan, "")}; Qty={data.qty_tambah}; StokBefore={b.Stok - data.qty_tambah}; StokAfter={b.Stok}; HargaModal={b.HargaModal}; HargaJual={b.HargaJual}; Total={b.HargaModal * data.qty_tambah}; SupplierId=0; SupplierName=Mobile Scanner; UserId=0; Role=Mobile"
                                                                  db.ActivityLog.Add(New ActivityLog With {
                                                                      .UserId = 0,
                                                                      .Username = "Mobile Scanner",
                                                                      .Activity = "RESTOCK",
                                                                      .Timestamp = DateTime.Now,
                                                                      .Detail = detailLog
                                                                  })

                                                                  ' LOG 2: StockLog (Generic Stock Movement)
                                                                  db.StockLogs.Add(New StockLog With {
                                                                      .BarangId = b.Id,
                                                                      .KodeBarang = b.KodeBarang,
                                                                      .NamaBarang = b.NamaBarang,
                                                                      .Tanggal = DateTime.Now,
                                                                      .Tipe = "PEMBELIAN",
                                                                      .QtyAwal = b.Stok - data.qty_tambah,
                                                                      .QtyPerubahan = data.qty_tambah,
                                                                      .QtyAkhir = b.Stok,
                                                                      .Keterangan = "Mobile Scanner Restock",
                                                                      .UserId = 0,
                                                                      .Username = "Mobile Scanner"
                                                                  })

                                                                  db.RestockLog.Add(New RestockLog With {
                                                                      .KodeBarang = data.barcode,
                                                                      .QtyTambah = data.qty_tambah,
                                                                      .User = "Mobile Scanner"
                                                                  })
                                                                  db.ScannerLog.Add(New ScannerLog With {
                                                                      .Barcode = data.barcode,
                                                                      .DeviceIp = context.Connection.RemoteIpAddress?.ToString(),
                                                                      .Mode = "restock"
                                                                  })
                                                                  db.SaveChanges()
                                                                  Return Results.Ok(New With {.status = "Success", .new_stok = b.Stok})
                                                              End Using
                                                          End Function)

                             ' --- MONITORING DASHBOARD API ---
                             ' 1. Stats (Today/Month)
                             _app.MapGet("/api/dashboard/stats", Function()
                                                                     Console.WriteLine("API: Fetching stats...")
                                                                     Try
                                                                         Using db As New AppDbContext()
                                                                             Dim tglToday = DateTime.Today
                                                                             Dim tglMonth = New DateTime(tglToday.Year, tglToday.Month, 1)

                                                                             Dim listToday = db.Transaksi.AsNoTracking().Where(Function(t) t.Tanggal >= tglToday).ToList()
                                                                             Dim listMonth = db.Transaksi.AsNoTracking().Where(Function(t) t.Tanggal >= tglMonth).ToList()

                                                                             Return Results.Ok(New With {
                                                                                .status = "Success",
                                                                                .today = listToday.Sum(Function(x) x.Total),
                                                                                .month = listMonth.Sum(Function(x) x.Total),
                                                                                .transactionsToday = listToday.Count
                                                                            })
                                                                         End Using
                                                                     Catch ex As Exception
                                                                         Console.WriteLine("API ERROR (Stats): " & ex.Message)
                                                                         Return Results.Json(New With {.status = "Error", .message = ex.Message})
                                                                     End Try
                                                                End Function)

                             ' 2. Top Products
                             _app.MapGet("/api/dashboard/top-products", Function()
                                                                            Console.WriteLine("API: Fetching top products...")
                                                                            Try
                                                                                Using db As New AppDbContext()
                                                                                    Dim top = db.TransaksiItems.AsNoTracking().
                                                                                       GroupBy(Function(i) i.NamaBarang).
                                                                                       Select(Function(g) New With {
                                                                                           .nama = g.Key,
                                                                                           .qty = g.Sum(Function(x) x.Qty)
                                                                                       }).
                                                                                       OrderByDescending(Function(x) x.qty).
                                                                                       Take(10).ToList()
                                                                                    Return Results.Ok(top)
                                                                                End Using
                                                                            Catch ex As Exception
                                                                                Console.WriteLine("API ERROR (Top Products): " & ex.Message)
                                                                                Return Results.Json(New With {.status = "Error", .message = ex.Message})
                                                                            End Try
                                                                        End Function)

                             ' 3. Low Stock
                             _app.MapGet("/api/dashboard/low-stock", Function()
                                                                         Console.WriteLine("API: Fetching low stock...")
                                                                         Try
                                                                             Using db As New AppDbContext()
                                                                                 Dim low = db.Barang.AsNoTracking().
                                                                                    Where(Function(b) b.Stok <= b.StokMinimum).
                                                                                    Select(Function(b) New With {
                                                                                        .nama = b.NamaBarang,
                                                                                        .stok = b.Stok,
                                                                                        .min = b.StokMinimum
                                                                                    }).ToList()
                                                                                 Return Results.Ok(low)
                                                                             End Using
                                                                         Catch ex As Exception
                                                                             Console.WriteLine("API ERROR (Low Stock): " & ex.Message)
                                                                             Return Results.Json(New With {.status = "Error", .message = ex.Message})
                                                                         End Try
                                                                     End Function)

                             ' 4. Recent Sales
                             _app.MapGet("/api/dashboard/recent-sales", Function()
                                                                            Console.WriteLine("API: Fetching recent sales...")
                                                                            Try
                                                                                Using db As New AppDbContext()
                                                                                    Dim recent = db.Transaksi.AsNoTracking().
                                                                                      OrderByDescending(Function(t) t.Tanggal).
                                                                                      Take(10).
                                                                                      Select(Function(t) New With {
                                                                                          .noTrx = t.NoTransaksi,
                                                                                          .waktu = t.Tanggal.ToString("HH:mm"),
                                                                                          .total = t.Total
                                                                                      }).ToList()
                                                                                    Return Results.Ok(recent)
                                                                                End Using
                                                                            Catch ex As Exception
                                                                                Console.WriteLine("API ERROR (Recent Sales): " & ex.Message)
                                                                                Return Results.Json(New With {.status = "Error", .message = ex.Message})
                                                                            End Try
                                                                        End Function)

                             ' 5. Mobile Scanner Activity
                             _app.MapGet("/api/dashboard/mobile-activity", Function()
                                                                               Using db As New AppDbContext()
                                                                                   Return db.ScannerLog.AsNoTracking().OrderByDescending(Function(l) l.Waktu).Take(10).ToList()
                                                                               End Using
                                                                           End Function)

                             ' --- KASIR PULLING ---
                             _app.MapGet("/api/kasir/pull", Function()
                                                                Dim barcode = MobileScanQueue.Dequeue()
                                                                If barcode Is Nothing Then Return Results.NoContent()
                                                                Return Results.Ok(New With {.barcode = barcode})
                                                            End Function)

                             ' --- CUSTOMER DISPLAY API ---
                             _app.MapGet("/api/customer/data", Function()
                                                                    Return Results.Ok(MobileScanQueue.CurrentDisplay)
                                                                End Function)

                             ' --- START SERVER ---
                             _app.Urls.Add("http://0.0.0.0:5050")
                             _app.Run()
                         Catch ex As Exception
                             Try
                                 Dim dir = IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "POS_JAVISH", "logs")
                                 IO.Directory.CreateDirectory(dir)
                                 Dim p = IO.Path.Combine(dir, "api_startup.log")
                                 IO.File.AppendAllText(p, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") & " | " & ex.ToString() & Environment.NewLine)
                             Catch
                             End Try
                             Try
                                 Dim dispatcher = Application.Current?.Dispatcher
                                 If dispatcher IsNot Nothing Then
                                     dispatcher.BeginInvoke(Sub()
                                                                MessageBox.Show("API Service gagal dijalankan. Pastikan Port 5050 tidak dipakai aplikasi lain dan Firewall mengizinkan POS JAVISH.", "API Service Error", MessageBoxButton.OK, MessageBoxImage.Warning)
                                                            End Sub)
                                 End If
                             Catch
                             End Try
                         End Try
                     End Sub)
        End Sub

        Public Shared Sub [Stop]()
            If _app IsNot Nothing Then
                _app.StopAsync()
            End If
        End Sub

        Public Shared Function GetLocalIPAddress() As String
            Try
                Dim host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName())
                For Each ip In host.AddressList
                    If ip.AddressFamily = System.Net.Sockets.AddressFamily.InterNetwork Then
                        Return ip.ToString()
                    End If
                Next
            Catch ex As Exception
                ' Fallback
            End Try
            Return "127.0.0.1"
        End Function

        Private Shared Function GetMobileScannerHtml() As String
            Return "<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>POS JAVISH - Mobile Scanner</title>
    <script src='https://cdn.tailwindcss.com'></script>
    <script src='https://unpkg.com/@zxing/library@latest'></script>
    <link rel='stylesheet' href='https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.0.0/css/all.min.css'>
    <style>
        body { background-color: #0f172a; color: #e2e8f0; }
        .btn-menu { background-color: #1e293b; border: 1px solid #334155; padding: 20px; border-radius: 15px; text-align: center; }
        .btn-active { border-color: #38bdf8; background-color: #0c4a6e; }
        #video { width: 100%; height: auto; border-radius: 10px; background: black; max-height: 60vh; object-fit: cover; }
        .modal { background: rgba(0,0,0,0.8); position: fixed; top:0; left:0; width:100%; height:100%; display:none; flex-direction:column; justify-content:center; padding: 20px; z-index: 100; }
        .error-box { background: #450a0a; border: 1px solid #991b1b; padding: 15px; border-radius: 10px; margin-bottom: 15px; display: none; }
    </style>
</head>
<body class='p-4'>
    <header class='mb-6 text-center'>
        <h1 class='text-xl font-bold text-sky-400'>MOBILE POS SCANNER</h1>
        <p class='text-xs opacity-50' id='status'>Pilih Mode Scan</p>
    </header>

    <div id='error-display' class='error-box'>
        <p class='text-sm font-bold text-red-200 mb-1'><i class='fas fa-exclamation-circle mr-2'></i> KAMERA TIDAK AKTIF</p>
        <p id='error-message' class='text-xs text-red-300'></p>
    </div>

    <!-- Menu Selection -->
    <div id='menu' class='grid grid-cols-2 gap-4 mb-6'>
        <div onclick='setMode(""kasir"")' id='btn-kasir' class='btn-menu'>
            <i class='fas fa-shopping-cart text-2xl mb-2 text-sky-400'></i>
            <p class='text-xs font-bold'>SCAN KASIR</p>
        </div>
        <div onclick='setMode(""cek_harga"")' id='btn-cek' class='btn-menu'>
            <i class='fas fa-tag text-2xl mb-2 text-green-400'></i>
            <p class='text-xs font-bold'>CEK HARGA</p>
        </div>
        <div onclick='setMode(""opname"")' id='btn-opname' class='btn-menu'>
            <i class='fas fa-clipboard-check text-2xl mb-2 text-yellow-400'></i>
            <p class='text-xs font-bold'>STOCK OPNAME</p>
        </div>
        <div onclick='setMode(""restock"")' id='btn-restock' class='btn-menu'>
            <i class='fas fa-plus-circle text-2xl mb-2 text-orange-400'></i>
            <p class='text-xs font-bold'>RESTOCK GUDANG</p>
        </div>
    </div>

    <!-- Scanner View -->
    <div id='scanner-container' class='hidden'>
        <div id='camera-area'>
            <video id='video' playsinline muted></video>
            <div class='mt-4 flex flex-wrap gap-2 justify-between items-center'>
                <button onclick='stopScanner()' class='bg-red-500 text-white px-4 py-2 rounded-lg text-sm'>BATAL</button>
                <p id='mode-label' class='font-bold uppercase text-sky-400'></p>
                <button onclick='switchCamera()' class='bg-slate-700 text-white p-2 rounded-lg'><i class='fas fa-sync'></i></button>
            </div>
        </div>
        
        <!-- Fallback Manual Input -->
        <div class='mt-8 border-t border-slate-700 pt-6'>
            <p class='text-xs opacity-50 mb-2'>Kamera bermasalah? Masukkan manual:</p>
            <div class='flex gap-2'>
                <input type='text' id='manual-barcode' class='flex-1 bg-slate-800 border border-slate-700 p-3 rounded-lg text-sm' placeholder='Ketik Barcode...'>
                <button onclick='handleManualInput()' class='bg-sky-600 px-4 rounded-lg text-xs font-bold'>KIRIM</button>
            </div>
        </div>
    </div>

    <!-- Modals -->
    <div id='modal-input' class='modal'>
        <div class='bg-slate-800 p-6 rounded-xl border border-slate-700'>
            <h3 id='modal-title' class='text-lg font-bold mb-2'>Judul</h3>
            <p id='modal-desc' class='text-sm opacity-70 mb-4'>Deskripsi barang</p>
            <input type='number' id='qty-input' class='w-full bg-slate-900 border border-slate-700 p-3 rounded mb-4' placeholder='Masukkan Jumlah'>
            <div class='flex gap-2'>
                <button onclick='closeModal()' class='flex-1 bg-slate-700 py-2 rounded'>BATAL</button>
                <button onclick='submitInput()' class='flex-1 bg-sky-500 py-2 rounded'>SIMPAN</button>
            </div>
        </div>
    </div>

    <script>
        const token = 'POSJAVISH';
        let currentMode = '';
        let codeReader = null;
        let videoDevices = [];
        let currentDeviceIndex = 0;
        let selectedBarcode = '';
        let lastScan = 0;

        // Auto-check on load
        window.onload = function() {
            const isSecure = window.location.protocol === 'https:' || window.location.hostname === 'localhost';
            const port = window.location.port;

            if (!isSecure && port === '5050') {
                const httpsUrl = 'https://' + window.location.hostname + ':5051/mobile';
                document.getElementById('status').innerHTML = 
                    '<div class=""bg-orange-500/20 p-4 rounded-lg border border-orange-500 mb-4"">' +
                        '<p class=""text-orange-400 font-bold mb-2"">⚠️ PINDAH KE HTTPS WAJIB</p>' +
                        '<p class=""text-xs mb-4"">Browser memblokir kamera di port 5050. Klik tombol di bawah untuk pindah ke jalur aman (Port 5051):</p>' +
                        '<a href=""' + httpsUrl + '"" class=""bg-orange-500 text-white px-6 py-3 rounded-full font-bold inline-block animate-bounce"">PINDAH KE HTTPS (5051)</a>' +
                    '</div>';
            } else if (!isSecure) {
                document.getElementById('status').innerHTML = '<span class=""text-red-400 font-bold"">KONEKSI TIDAK AMAN</span><br>Kamera diblokir browser. Pastikan URL diawali https://';
            }
        };

        async function setMode(mode) {
            currentMode = mode;
            document.querySelectorAll('.btn-menu').forEach(b => b.classList.remove('btn-active'));
            const btnId = 'btn-' + mode.split('_')[0];
            if(document.getElementById(btnId)) document.getElementById(btnId).classList.add('btn-active');
            
            document.getElementById('menu').classList.add('hidden');
            document.getElementById('scanner-container').classList.remove('hidden');
            document.getElementById('mode-label').innerText = mode.replace('_', ' ');
            document.getElementById('error-display').style.display = 'none';
            document.getElementById('status').innerText = 'Memulai kamera...';
            
            await startScanner();
        }

        async function startScanner() {
            try {
                if (codeReader) {
                    await codeReader.reset();
                    codeReader = null;
                }

                codeReader = new ZXing.BrowserMultiFormatReader();
                console.log('ZXing initialized');

                // FORCE BACK CAMERA
                const constraints = { video: { facingMode: ""environment"" } };
                
                document.getElementById('status').innerText = 'Scanner Aktif';
                
                await codeReader.decodeFromConstraints(constraints, 'video', (result, err) => {
                    if (result) {
                        handleScan(result.text);
                    }
                });

            } catch (err) {
                console.error('CRITICAL ERROR:', err);
                showError(err);
            }
        }

        function showError(err) {
            let msg = err.message || 'Gagal mengakses kamera.';
            document.getElementById('error-display').style.display = 'block';
            document.getElementById('error-message').innerText = msg;
            document.getElementById('status').innerText = 'Error Kamera';
            document.getElementById('scanner-container').classList.add('hidden');
            document.getElementById('menu').classList.remove('hidden');
        }

        function stopScanner() {
            if (codeReader) codeReader.reset();
            document.getElementById('menu').classList.remove('hidden');
            document.getElementById('scanner-container').classList.add('hidden');
            document.getElementById('error-display').style.display = 'none';
            document.getElementById('status').innerText = 'Pilih Mode Scan';
        }

        function handleManualInput() {
            const barcode = document.getElementById('manual-barcode').value;
            if (!barcode) return;
            handleScan(barcode);
            document.getElementById('manual-barcode').value = '';
        }

        async function handleScan(barcode) {
            // ANTI-SPAM PROTECTION (1.5 seconds)
            if (Date.now() - lastScan < 1500) return;
            lastScan = Date.now();

            // SUCCESS FEEDBACK: Vibrate & Beep
            if (navigator.vibrate) navigator.vibrate(100);
            new Audio('https://actions.google.com/sounds/v1/cartoon/wood_plank_flicks.ogg').play();

            if (currentMode === 'kasir') {
                const res = await fetch(`/api/scan?token=${token}`, {
                    method: 'POST',
                    headers: {'Content-Type': 'application/json'},
                    body: JSON.stringify({barcode})
                });
                const data = await res.json();
                if (data.status === 'STOK_HABIS') {
                    alert('⚠ ' + data.message);
                } else {
                    console.log('Terkirim ke Kasir:', data.nama);
                }
            } else if (currentMode === 'cek_harga') {
                const res = await fetch(`/api/product/${barcode}?token=${token}`);
                const data = await res.json();
                alert(`NAMA: ${data.nama}\nHARGA: Rp ${data.harga.toLocaleString()}\nSTOK: ${data.stok}`);
            } else {
                selectedBarcode = barcode;
                const res = await fetch(`/api/product/${barcode}?token=${token}`);
                const data = await res.json();
                document.getElementById('modal-title').innerText = currentMode === 'opname' ? 'Stock Opname' : 'Restock Gudang';
                document.getElementById('modal-desc').innerText = data.nama + ' (Stok: ' + data.stok + ')';
                document.getElementById('modal-input').style.display = 'flex';
                if (codeReader) codeReader.reset();
            }
        }

        async function submitInput() {
            const qty = parseInt(document.getElementById('qty-input').value);
            if (isNaN(qty)) return;

            const endpoint = currentMode === 'opname' ? '/api/opname' : '/api/restock';
            const body = currentMode === 'opname' ? {barcode: selectedBarcode, qty_rak: qty} : {barcode: selectedBarcode, qty_tambah: qty};

            const res = await fetch(`${endpoint}?token=${token}`, {
                method: 'POST',
                headers: {'Content-Type': 'application/json'},
                body: JSON.stringify(body)
            });
            const data = await res.json();
            alert('Sukses: ' + data.status);
            closeModal();
            startScanner();
        }

        function closeModal() {
            document.getElementById('modal-input').style.display = 'none';
            document.getElementById('qty-input').value = '';
        }
    </script>
</body>
</html>"
        End Function

        Private Shared Function GetDashboardHtml() As String
            Return "<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>POS JAVISH - Mobile Monitoring</title>
    <script src='https://cdn.tailwindcss.com'></script>
    <link rel='stylesheet' href='https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.0.0/css/all.min.css'>
    <style>
        body { background-color: #0f172a; color: #e2e8f0; font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; }
        .card { background-color: #1e293b; border: 1px solid #334155; border-radius: 12px; padding: 1.5rem; }
        .accent { color: #38bdf8; }
        .bg-accent { background-color: #38bdf8; }
        .success { color: #4ade80; }
        .warning { color: #fbbf24; }
        .error { color: #f87171; }
    </style>
</head>
<body class='p-4 md:p-8'>
    <div class='max-w-4xl mx-auto'>
        <header class='flex justify-between items-center mb-8'>
            <div>
                <h1 class='text-2xl font-bold accent'>POS JAVISH</h1>
                <p class='text-sm opacity-60'>Monitoring Toko Real-time</p>
            </div>
            <div class='text-right'>
                <p id='lastUpdate' class='text-xs opacity-40'>Memuat...</p>
                <button onclick='refreshData()' class='text-sm accent hover:underline'><i class='fas fa-sync-alt mr-1'></i> Refresh</button>
            </div>
        </header>

        <!-- Stats Cards -->
        <div class='grid grid-cols-1 md:grid-cols-2 gap-4 mb-8'>
            <div class='card flex items-center'>
                <div class='bg-blue-500/10 p-4 rounded-full mr-4'>
                    <i class='fas fa-shopping-cart text-2xl text-blue-400'></i>
                </div>
                <div>
                    <p class='text-sm opacity-60'>Penjualan Hari Ini</p>
                    <h2 id='todayTotal' class='text-2xl font-bold'>Rp 0</h2>
                    <p id='todayCount' class='text-xs opacity-40'>0 Transaksi</p>
                </div>
            </div>
            <div class='card flex items-center'>
                <div class='bg-green-500/10 p-4 rounded-full mr-4'>
                    <i class='fas fa-calendar-alt text-2xl text-green-400'></i>
                </div>
                <div>
                    <p class='text-sm opacity-60'>Penjualan Bulan Ini</p>
                    <h2 id='monthTotal' class='text-2xl font-bold'>Rp 0</h2>
                </div>
            </div>
        </div>

        <div class='grid grid-cols-1 md:grid-cols-2 gap-8'>
            <!-- Recent Sales -->
            <section>
                <h3 class='text-lg font-semibold mb-4 flex items-center'>
                    <i class='fas fa-history mr-2 accent'></i> Transaksi Terakhir
                </h3>
                <div class='card p-0 overflow-hidden'>
                    <div id='recentSales' class='divide-y divide-slate-700'>
                        <div class='p-4 text-center opacity-40'>Memuat data...</div>
                    </div>
                </div>
            </section>

            <!-- Top Products -->
            <section>
                <h3 class='text-lg font-semibold mb-4 flex items-center'>
                    <i class='fas fa-crown mr-2 text-yellow-400'></i> Produk Terlaris
                </h3>
                <div class='card p-0 overflow-hidden'>
                    <div id='topProducts' class='divide-y divide-slate-700'>
                        <div class='p-4 text-center opacity-40'>Memuat data...</div>
                    </div>
                </div>
            </section>
        </div>

        <!-- Low Stock Alert -->
        <section class='mt-8'>
            <h3 class='text-lg font-semibold mb-4 flex items-center'>
                <i class='fas fa-exclamation-triangle mr-2 error'></i> Stok Menipis
            </h3>
            <div class='card p-0 overflow-hidden border-red-500/20'>
                <div id='lowStock' class='grid grid-cols-1 md:grid-cols-2 divide-y md:divide-y-0 md:divide-x divide-slate-700'>
                    <div class='p-4 text-center opacity-40'>Memuat data...</div>
                </div>
            </div>
        </section>

        <footer class='mt-12 text-center opacity-30 text-xs'>
            &copy; 2024 POS JAVISH System. Semua data diperbarui otomatis.
        </footer>
    </div>

    <script>
        function formatIDR(num) {
            return 'Rp ' + Number(num).toLocaleString('id-ID');
        }

        async function refreshData() {
             try {
                 // 0. Check connection first
                 const pingRes = await fetch('/api/ping');
                 if (!pingRes.ok) throw new Error('Server unreachable');
                 const pingData = await pingRes.json();
                 console.log('Server Status:', pingData);

                 // 1. Fetch Stats
                 const statsRes = await fetch('/api/dashboard/stats');
                 const stats = await statsRes.json();
                 if (stats.status === 'Success') {
                     document.getElementById('todayTotal').innerText = formatIDR(stats.today);
                     document.getElementById('monthTotal').innerText = formatIDR(stats.month);
                     document.getElementById('todayCount').innerText = stats.transactionsToday + ' Transaksi';
                 } else {
                     console.error('API Error (Stats):', stats.message);
                 }

                 // 2. Fetch Recent Sales
                 const recentRes = await fetch('/api/dashboard/recent-sales');
                 const recent = await recentRes.json();
                 
                 if (Array.isArray(recent)) {
                    const recentHtml = recent.length > 0 ? recent.map(s => `
                        <div class='p-4 flex justify-between items-center hover:bg-slate-800 transition'>
                            <div>
                                <p class='font-medium'>${s.noTrx || 'N/A'}</p>
                                <p class='text-xs opacity-40'>${s.waktu || ''}</p>
                            </div>
                            <p class='font-bold success'>${formatIDR(s.total || 0)}</p>
                        </div>
                    `).join('') : '<div class=\'p-4 text-center opacity-40\'>Belum ada transaksi</div>';
                    document.getElementById('recentSales').innerHTML = recentHtml;
                 } else {
                    document.getElementById('recentSales').innerHTML = `<div class='p-4 text-center error'>Error: ${recent.message || 'Data error'}</div>`;
                 }

                 // 3. Fetch Top Products
                 const topRes = await fetch('/api/dashboard/top-products');
                 const top = await topRes.json();
                 
                 if (Array.isArray(top)) {
                    const topHtml = top.length > 0 ? top.map((p, i) => `
                        <div class='p-4 flex justify-between items-center hover:bg-slate-800 transition'>
                            <div class='flex items-center'>
                                <span class='w-6 h-6 rounded-full bg-slate-700 flex items-center justify-center text-xs mr-3 font-bold'>${i+1}</span>
                                <p class='font-medium'>${p.nama || 'N/A'}</p>
                            </div>
                            <p class='text-sm'><span class='accent font-bold'>${p.qty || 0}</span> <span class='opacity-40'>pcs</span></p>
                        </div>
                    `).join('') : '<div class=\'p-4 text-center opacity-40\'>Belum ada data</div>';
                    document.getElementById('topProducts').innerHTML = topHtml;
                 } else {
                    document.getElementById('topProducts').innerHTML = `<div class='p-4 text-center error'>Error: ${top.message || 'Data error'}</div>`;
                 }

                 // 4. Fetch Low Stock
                 const lowRes = await fetch('/api/dashboard/low-stock');
                 const low = await lowRes.json();
                 
                 if (Array.isArray(low)) {
                    const lowHtml = low.length > 0 ? low.map(l => `
                        <div class='p-4 flex justify-between items-center hover:bg-slate-800 transition'>
                            <p class='font-medium'>${l.nama || 'N/A'}</p>
                            <div class='text-right'>
                                <p class='text-sm error font-bold'>Stok: ${l.stok || 0}</p>
                                <p class='text-[10px] opacity-40'>Min: ${l.min || 0}</p>
                            </div>
                        </div>
                    `).join('') : '<div class=\'p-4 text-center opacity-40 text-green-400\'>Semua stok aman <i class=\'fas fa-check-circle ml-1\'></i></div>';
                    document.getElementById('lowStock').innerHTML = lowHtml;
                 } else {
                    document.getElementById('lowStock').innerHTML = `<div class='p-4 text-center error'>Error: ${low.message || 'Data error'}</div>`;
                 }

                 document.getElementById('lastUpdate').innerText = 'Update: ' + new Date().toLocaleTimeString();
             } catch (err) {
                 console.error('Fetch error:', err);
                 document.getElementById('lastUpdate').innerText = 'Koneksi Terputus: ' + err.message;
             }
         }

        // Initial load and set interval
        refreshData();
        setInterval(refreshData, 30000); // 30 seconds
    </script>
</body>
</html>"
        End Function

        Private Shared Function GetCustomerDisplayHtml() As String
            Return "<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Customer Display - POS JAVISH</title>
    <script src='https://cdn.tailwindcss.com'></script>
    <link rel='stylesheet' href='https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.0.0/css/all.min.css'>
    <style>
        body { background-color: #0f172a; color: #f8fafc; font-family: 'Inter', sans-serif; overflow: hidden; }
        .price-text { color: #38bdf8; }
        .total-box { background: #1e293b; border: 2px solid #334155; }
        .cart-item { border-bottom: 1px solid #334155; }
        .cart-container { height: calc(100vh - 250px); overflow-y: auto; }
        .success-overlay { background: rgba(15, 23, 42, 0.95); }
    </style>
</head>
<body class='h-screen flex flex-col'>
    <header class='p-6 bg-slate-900 flex justify-between items-center border-b border-slate-800'>
        <div class='flex items-center gap-4'>
            <div class='bg-sky-500 p-2 rounded-lg'>
                <i class='fas fa-store text-white text-2xl'></i>
            </div>
            <h1 id='storeName' class='text-2xl font-black tracking-tight'>POS JAVISH</h1>
        </div>
        <div class='text-right'>
            <p class='text-slate-400 text-sm'>Waktu</p>
            <p id='clock' class='text-xl font-bold'>00:00:00</p>
        </div>
    </header>

    <main class='flex-1 flex p-6 gap-6'>
        <!-- Left: Cart -->
        <div class='flex-[2] flex flex-col'>
            <div class='flex justify-between items-end mb-4'>
                <h2 class='text-xl font-bold flex items-center gap-2'>
                    <i class='fas fa-shopping-basket text-sky-400'></i> Keranjang Belanja
                </h2>
                <p id='itemCount' class='text-slate-400'>0 Item</p>
            </div>
            <div class='bg-slate-900 rounded-2xl border border-slate-800 flex-1 flex flex-col overflow-hidden'>
                <div class='grid grid-cols-12 p-4 bg-slate-800/50 text-xs font-bold text-slate-400 uppercase tracking-wider'>
                    <div class='col-span-6'>Produk</div>
                    <div class='col-span-2 text-center'>Qty</div>
                    <div class='col-span-4 text-right'>Subtotal</div>
                </div>
                <div id='cartItems' class='cart-container divide-y divide-slate-800'>
                    <!-- Items injected here -->
                    <div class='h-full flex flex-col items-center justify-center opacity-20'>
                        <i class='fas fa-shopping-cart text-6xl mb-4'></i>
                        <p class='text-xl'>Siap Melayani</p>
                    </div>
                </div>
            </div>
        </div>

        <!-- Right: Totals -->
        <div class='flex-1 flex flex-col gap-6'>
            <div class='total-box rounded-3xl p-8 flex flex-col gap-6 shadow-2xl'>
                <div class='flex justify-between items-center text-slate-400'>
                    <span>Subtotal</span>
                    <span id='subtotal' class='font-bold text-white'>Rp 0</span>
                </div>
                <div class='flex justify-between items-center text-slate-400'>
                    <span>Diskon</span>
                    <span id='diskon' class='font-bold text-red-400'>- Rp 0</span>
                </div>
                <div class='h-px bg-slate-700 my-2'></div>
                <div class='flex flex-col'>
                    <span class='text-slate-400 text-lg mb-1'>Total Bayar</span>
                    <span id='total' class='text-6xl font-black price-text tracking-tighter'>Rp 0</span>
                </div>
            </div>

            <div id='paymentInfo' class='hidden bg-green-500/10 border border-green-500/30 rounded-3xl p-8 flex flex-col gap-4'>
                <div class='flex justify-between items-center'>
                    <span class='text-green-400'>Diterima</span>
                    <span id='bayar' class='text-2xl font-bold text-green-400'>Rp 0</span>
                </div>
                <div class='flex justify-between items-center'>
                    <span class='text-white text-lg'>Kembalian</span>
                    <span id='kembalian' class='text-3xl font-black text-white'>Rp 0</span>
                </div>
            </div>

            <div class='flex-1 flex items-center justify-center'>
                <div class='text-center p-8 border-2 border-dashed border-slate-800 rounded-3xl w-full'>
                    <p class='text-slate-500 text-sm leading-relaxed'>
                        <i class='fas fa-info-circle mb-2 block text-xl'></i>
                        Silakan periksa kembali daftar belanjaan Anda sebelum membayar.
                    </p>
                </div>
            </div>
        </div>
    </main>

    <!-- Success Overlay -->
    <div id='successOverlay' class='success-overlay fixed inset-0 hidden flex flex-col items-center justify-center z-50 animate-in fade-in duration-500'>
        <div class='bg-green-500 w-24 h-24 rounded-full flex items-center justify-center mb-8 animate-bounce'>
            <i class='fas fa-check text-5xl text-white'></i>
        </div>
        <h2 class='text-5xl font-black mb-4'>Terima Kasih!</h2>
        <p class='text-2xl text-slate-400 mb-12'>Transaksi Anda Berhasil</p>
        <div class='flex flex-col items-center gap-2'>
            <p class='text-slate-500'>Kembalian Anda</p>
            <p id='overlayKembalian' class='text-6xl font-black text-green-400'>Rp 0</p>
        </div>
    </div>

    <script>
        function formatIDR(num) {
            return 'Rp ' + Number(num).toLocaleString('id-ID');
        }

        function updateClock() {
            document.getElementById('clock').innerText = new Date().toLocaleTimeString('id-ID');
        }
        setInterval(updateClock, 1000);
        updateClock();

        async function updateDisplay() {
            try {
                const res = await fetch('/api/customer/data');
                const data = await res.json();
                
                document.getElementById('storeName').innerText = data.storeName || 'POS JAVISH';
                document.getElementById('subtotal').innerText = formatIDR(data.subtotal);
                document.getElementById('diskon').innerText = '- ' + formatIDR(data.diskon);
                document.getElementById('total').innerText = formatIDR(data.total);
                document.getElementById('itemCount').innerText = data.items.length + ' Item';

                // Cart Items
                const cartContainer = document.getElementById('cartItems');
                if (data.items.length > 0) {
                    cartContainer.innerHTML = data.items.map(item => `
                        <div class='grid grid-cols-12 p-4 cart-item items-center hover:bg-slate-800/30 transition-colors'>
                            <div class='col-span-6'>
                                <p class='font-bold text-lg'>${item.nama}</p>
                                <p class='text-xs text-slate-500'>${formatIDR(item.harga)} / pcs</p>
                            </div>
                            <div class='col-span-2 text-center'>
                                <span class='bg-slate-800 px-3 py-1 rounded-full font-bold'>${item.qty}</span>
                            </div>
                            <div class='col-span-4 text-right'>
                                <p class='font-black text-lg text-white'>${formatIDR(item.subtotal)}</p>
                            </div>
                        </div>
                    `).join('');
                } else if (data.status !== 'SUCCESS') {
                    cartContainer.innerHTML = `
                        <div class='h-full flex flex-col items-center justify-center opacity-20'>
                            <i class='fas fa-shopping-cart text-6xl mb-4'></i>
                            <p class='text-xl'>Siap Melayani</p>
                        </div>
                    `;
                }

                // Status Handling
                const successOverlay = document.getElementById('successOverlay');
                const paymentInfo = document.getElementById('paymentInfo');

                if (data.status === 'SUCCESS') {
                    document.getElementById('overlayKembalian').innerText = formatIDR(data.kembalian);
                    successOverlay.classList.remove('hidden');
                    paymentInfo.classList.add('hidden');
                } else if (data.status === 'TRANSACTING' && data.bayar > 0) {
                    document.getElementById('bayar').innerText = formatIDR(data.bayar);
                    document.getElementById('kembalian').innerText = formatIDR(data.kembalian);
                    paymentInfo.classList.remove('hidden');
                    successOverlay.classList.add('hidden');
                } else {
                    successOverlay.classList.add('hidden');
                    paymentInfo.classList.add('hidden');
                }

            } catch (err) {
                console.error('Display Update Error:', err);
            }
        }

        setInterval(updateDisplay, 500); // Check every 500ms
        updateDisplay();
    </script>
</body>
</html>"
        End Function
    End Class
End Namespace

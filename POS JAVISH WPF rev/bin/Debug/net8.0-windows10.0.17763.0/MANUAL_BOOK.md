# MANUAL BOOK - POS JAVISH (Versi Trial)

Selamat datang di **POS JAVISH**, solusi Point of Sale (Kasir) modern untuk bisnis Anda. Dokumen ini akan memandu Anda dalam mengoperasikan aplikasi dari awal hingga akhir.

---

## 1. Persiapan Awal
### Cara Menjalankan Aplikasi
1. Buka folder **POS_JAVISH_TRIAL**.
2. Klik dua kali pada file **POS JAVISH WPF.exe**.
3. Aplikasi akan terbuka. Jika muncul peringatan Windows SmartScreen, klik "More Info" lalu "Run Anyway" (karena aplikasi ini bersifat portable).

### Login Pertama Kali
- **Username Default**: `admin`
- **Password Default**: `admin`
*(Disarankan untuk segera mengubah password di menu Manajemen User demi keamanan).*

---

## 2. Manajemen Produk (Data Barang)
Menu ini digunakan untuk mengelola stok barang Anda.
- **Tambah Barang**: Klik tombol "TAMBAH PRODUK", isi kode barcode, nama, harga modal, harga jual, dan stok awal.
- **Edit/Hapus**: Gunakan ikon pensil untuk mengedit atau ikon tempat sampah untuk menghapus di daftar barang.
- **Fitur Excel (Backup/Restore)**:
    - **Export**: Klik "EXPORT" untuk menyimpan data barang Anda ke file Excel (.xlsx). Ini berguna sebagai backup.
    - **Import**: Klik "IMPORT" untuk memasukkan data barang secara massal dari file Excel. Pastikan format kolom sesuai (Kode, Nama, Modal, Jual, Satuan, Stok, Stok Minimum).

---

## 3. Transaksi Kasir
Menu utama untuk melakukan penjualan.
- **Scan Barcode**: Cukup arahkan scanner ke barcode barang, item akan otomatis masuk ke keranjang.
- **Cari Manual**: Ketik nama barang di kotak pencarian jika tidak ada barcode.
- **Pembayaran**: 
    1. Tekan tombol **BAYAR** atau tekan **Enter** pada kotak nominal bayar.
    2. Masukkan jumlah uang pelanggan.
    3. Tekan Enter untuk konfirmasi dan cetak struk.
- **Hapus Item**: Jika ada kesalahan input, klik ikon tempat sampah merah pada baris barang di keranjang belanja.

---

## 4. Fitur Tambahan (Modern)
### HP Jadi Scanner
1. Buka menu **Pengaturan**.
2. Lihat alamat di bagian **Mobile Monitoring**.
3. Buka alamat tersebut di browser HP Anda.
4. Pilih mode **SCAN KASIR** untuk mulai men-scan barang dari HP.

### Layar Pembeli (Customer Display)
1. Siapkan Tablet atau HP bekas, hadapkan ke arah pembeli.
2. Buka browser dan masukkan alamat **Customer Display** yang ada di menu **Pengaturan**.
3. Layar akan otomatis menampilkan daftar belanjaan setiap kali kasir melakukan scan di PC.

---

## 5. Sistem Shift
Aplikasi ini menggunakan sistem Shift untuk memantau uang masuk per sesi kerja.
- **Buka Shift**: Dilakukan di awal hari/sesi. Masukkan modal awal di laci kasir.
- **Tutup Shift**: Dilakukan di akhir sesi. Aplikasi akan menampilkan total penjualan, uang tunai yang seharusnya ada, dan laporan ringkas.

---

## 5. Dashboard & Laporan
- **Dashboard**: Menampilkan ringkasan penjualan hari ini, total produk, barang yang stoknya habis, dan grafik penjualan sederhana.
- **Laporan**: Anda bisa melihat riwayat transaksi, laporan laba rugi, dan riwayat stok masuk di menu-menu terkait (khusus Admin).

---

## 6. Pengaturan & Aktivasi
### Mode Demo (Trial)
- Anda dapat mencoba seluruh fitur utama (Kasir, Excel, Profil Toko).
- Batas maksimal transaksi adalah **100 Transaksi**.
- Sisa kuota transaksi dapat dilihat di bagian bawah Sidebar kiri.

### Aktivasi Versi Full
1. Buka menu **Pengaturan**.
2. Klik tombol **AKTIVASI**.
3. Salin **Hardware ID** Anda dan kirimkan ke Admin/Developer untuk mendapatkan **Serial Number**.
4. Masukkan Serial Number yang diterima dan klik **AKTIFKAN**.

---

## Tips Keamanan
- Jangan bagikan password Admin Anda kepada staf.
- Lakukan **Export Excel** secara berkala sebagai backup data barang Anda.
- Pastikan printer struk sudah terpasang dan dipilih di menu Pengaturan.

---

## 7. Troubleshooting Koneksi (Penting)
Jika HP Scanner atau Layar Pembeli tidak bisa dibuka, kemungkinan besar diblokir oleh **Windows Firewall**.

### Cara Cepat (Rekomendasi):
1. Klik **Start**, ketik **PowerShell**.
2. Klik kanan **Windows PowerShell**, pilih **Run as Administrator**.
3. Salin dan tempel perintah berikut lalu tekan Enter:
   ```powershell
   New-NetFirewallRule -DisplayName "POS JAVISH Web Services" -Direction Inbound -LocalPort 5050,5051 -Protocol TCP -Action Allow
   ```

---
## 8. Troubleshooting Umum

### A. Barcode Scanner tidak masuk ke Keranjang (Menu Kasir)
- Pastikan mode scanner adalah **USB HID Keyboard** (bukan mode Serial/COM).
- Coba scan di **Notepad**. Jika tidak muncul karakter, berarti masalah di scanner/driver.
- Pastikan Anda sedang berada di **Menu Kasir** (bukan di form lain).
- Jika scan terasa lambat atau dianggap ketikan manual, buka **Pengaturan → Barcode Scanner** lalu:
  - Naikkan **Kecepatan Scan (ms)** (contoh 120–200).
  - Aktifkan **Auto Submit jika tidak ada input** (untuk scanner yang tidak kirim Enter).
  - Aktifkan **Terima TAB sebagai Terminator** (jika scanner suffix TAB).

### B. Barcode “ngetik” ke kolom lain / fokus berpindah sendiri
- Pastikan tidak ada aplikasi lain yang mengambil fokus (mis. remote software, input method).
- Jika terjadi di beberapa form input (mis. tambah produk), ini normal karena form tersebut memang menerima input scan langsung di textbox.

### C. Barang Masuk / Pembelian: scan tidak langsung memilih barang
- Pastikan **Kode Barang** di Master Produk sama persis dengan barcode (tanpa spasi).
- Jika barcode belum terdaftar, buat/ubah kode barang di menu **Produk** terlebih dahulu.
- Jika barcode terbaca tetapi hasil pencarian banyak, gunakan kode yang unik (hindari kode yang mirip antar barang).

### D. Gagal cetak struk / printer tidak keluar
- Buka **Pengaturan → Perangkat Keras** lalu pastikan **Printer Struk** sudah dipilih.
- Pastikan driver printer terpasang dan bisa test print di Windows.
- Pastikan ukuran kertas sesuai: **58mm/80mm**.
- Jika muncul pesan “Print Error”, coba:
  - Matikan lalu nyalakan printer.
  - Pastikan printer bukan status “Offline”.
  - Restart layanan Windows: **Print Spooler** (opsional).

### E. Cash Drawer tidak terbuka
- Pastikan kabel cash drawer terhubung ke port drawer pada printer (RJ11).
- Di **Pengaturan**, aktifkan opsi **Buka Cash Drawer Otomatis**.
- Beberapa printer butuh setting drawer-pin berbeda. Jika tetap tidak berfungsi, coba di driver/utility printer.

### F. Aplikasi tertutup sendiri / PC mati saat proses transaksi
- Data transaksi dan stok disimpan dalam transaksi database (atomic). Jika aplikasi mati sebelum selesai, transaksi yang belum commit tidak akan mengurangi stok.
- Jika pembayaran sudah diterima tetapi struk belum tercetak:
  - Buka menu **Laporan**, cari transaksi yang baru terjadi, lalu klik **Cetak Ulang**.

### G. Nomor transaksi “lompat” atau tidak rapi
- Nomor transaksi dibuat per hari dan per terminal (contoh: `TRX-20260418-POS01-00001`).
- Jika Anda memakai lebih dari 1 komputer kasir, atur kode terminal agar rapi dengan mengisi `TerminalCode` di file `settings.json` (mis. `POS01`, `POS02`).

### H. Error database / data tidak muncul
- Pastikan aplikasi dijalankan dengan akses folder normal (jangan dari folder yang dibatasi hak akses).
- Database tersimpan di folder sistem (AppData Local) dengan nama file `sys_data.bin`.
- Jika data hilang setelah restore, pastikan file backup yang dipilih benar dan aplikasi ditutup saat proses restore.

---
**POS JAVISH - Enterprise Point of Sale**
*Solusi cerdas untuk kelola bisnis Anda.*

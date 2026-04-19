# POS React App

Aplikasi Point of Sales modern berbasis React dengan fitur:

- Login/hak akses `admin` dan `kasir`
- Manajemen user lokal untuk admin
- Daftar barang dengan manajemen produk (admin)
- Kasir dengan dukungan barcode scanner / input ID
- Laporan penjualan dan ringkasan pendapatan
- Semua data disimpan lokal di browser (LocalStorage)

## Cara menjalankan

1. Buka terminal di folder `e:\.continue`
2. Jalankan `npm install`
3. Jalankan `npm run dev`
4. Buka `http://localhost:5173`

## Akun default

- admin / admin123
- kasir / kasir123

## File penting

- `src/App.jsx` — logika aplikasi dan UI utama
- `src/styles.css` — gaya aplikasi
- `package.json` — dependensi dan perintah Vite

## Deployment

### ⚠️ **PENTING**: Batasan LocalStorage

Aplikasi ini menggunakan **LocalStorage** untuk penyimpanan data lokal. Ini berarti:

- ❌ **Tidak cocok untuk Vercel** (hosting statis tanpa persistent storage)
- ❌ **Tidak cocok untuk deployment cloud** (data hilang saat refresh/server restart)
- ✅ **Cocok untuk**: GitHub Pages, Netlify, atau hosting statis lainnya
- ✅ **Cocok untuk**: Penggunaan offline di browser lokal

### Opsi Deployment

#### 1. GitHub Pages (Rekomendasi)
```bash
# 1. Update homepage di package.json dengan URL GitHub Pages Anda
# Ganti [your-github-username] dan [repository-name]
"homepage": "https://[your-github-username].github.io/[repository-name]"

# 2. Build aplikasi
npm run build

# 3. Deploy ke GitHub Pages
npm run deploy
```

#### 2. Netlify (Alternatif)
- Upload folder `dist` setelah `npm run build`
- Atau connect langsung dari GitHub repository

#### 3. Vercel (Tidak Direkomendasikan)
- Bisa deploy tapi data LocalStorage hilang setiap kali halaman refresh
- Tidak cocok untuk aplikasi POS yang butuh persistent data

### Migrasi ke Database Cloud (untuk production)

Jika ingin deploy ke Vercel dengan data persistent, perlu migrasi ke:

- **Firebase Firestore** atau **Supabase**
- **MongoDB Atlas** atau **PlanetScale**
- Atau backend API dengan Express.js + MongoDB

Hubungi saya jika perlu bantuan migrasi ke database cloud!

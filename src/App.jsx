import { useEffect, useMemo, useState } from 'react';

const defaultUsers = [
  { id: 'admin', username: 'admin', password: 'admin123', role: 'admin', name: 'Administrator', isActive: true },
  { id: 'kasir', username: 'kasir', password: 'kasir123', role: 'kasir', name: 'Kasir', isActive: true },
];

const defaultProducts = [
  { id: 'P001', barcode: '123456789012', name: 'Kertas A4', price: 12000, stock: 50 },
  { id: 'P002', barcode: '123456789013', name: 'Bolpoin', price: 2500, stock: 100 },
  { id: 'P003', barcode: '123456789014', name: 'Buku Tulis', price: 15000, stock: 80 },
  { id: 'P004', barcode: '123456789015', name: 'Tisu', price: 8000, stock: 60 },
  { id: 'P005', barcode: '123456789016', name: 'Minuman Botol', price: 12000, stock: 120 },
];

const currency = (value) =>
  new Intl.NumberFormat('id-ID', { style: 'currency', currency: 'IDR' }).format(value);

const loadStorage = (key, fallback) => {
  try {
    const raw = window.localStorage.getItem(key);
    return raw ? JSON.parse(raw) : fallback;
  } catch (error) {
    return fallback;
  }
};

const saveStorage = (key, value) => {
  try {
    window.localStorage.setItem(key, JSON.stringify(value));
  } catch (error) {
    console.warn('LocalStorage error', error);
  }
};

const capitalize = (text) => text.charAt(0).toUpperCase() + text.slice(1);

function App() {
  const [auth, setAuth] = useState(null);
  const [view, setView] = useState('dashboard');
  const [products, setProducts] = useState(defaultProducts);
  const [sales, setSales] = useState([]);
  const [cart, setCart] = useState([]);
  const [barcode, setBarcode] = useState('');
  const [productForm, setProductForm] = useState({ id: '', barcode: '', name: '', price: '', stock: '' });
  const [loginState, setLoginState] = useState({ user: '', pass: '' });
  const [message, setMessage] = useState('');
  const [users, setUsers] = useState(defaultUsers);
  const [userForm, setUserForm] = useState({ id: '', username: '', password: '', role: 'kasir', name: '', isActive: true });

  useEffect(() => {
    const savedAuth = loadStorage('pos_auth', null);
    const savedProducts = loadStorage('pos_products', defaultProducts);
    const savedSales = loadStorage('pos_sales', []);
    const savedUsers = loadStorage('pos_users', defaultUsers);
    setAuth(savedAuth);
    setProducts(savedProducts);
    setSales(savedSales);
    setUsers(savedUsers);
  }, []);

  useEffect(() => {
    saveStorage('pos_products', products);
  }, [products]);

  useEffect(() => {
    saveStorage('pos_sales', sales);
  }, [sales]);

  useEffect(() => {
    saveStorage('pos_users', users);
  }, [users]);

  useEffect(() => {
    saveStorage('pos_auth', auth);
  }, [auth]);

  const totals = useMemo(() => {
    const subtotal = cart.reduce((sum, item) => sum + item.price * item.quantity, 0);
    const totalSales = sales.reduce((sum, sale) => sum + sale.total, 0);
    const transactionCount = sales.length;
    const salesByDate = sales.reduce((acc, sale) => {
      const date = new Date(sale.date).toLocaleDateString('id-ID');
      acc[date] = (acc[date] || 0) + sale.total;
      return acc;
    }, {});
    return { subtotal, totalSales, transactionCount, salesByDate };
  }, [cart, sales]);

  const authUsers = users;

  const handleLogin = () => {
    const user = authUsers.find((item) => item.username === loginState.user && item.password === loginState.pass && item.isActive);
    if (!user) {
      setMessage('Username atau password salah.');
      return;
    }
    setAuth(user);
    setView('dashboard');
    setMessage('');
  };

  const handleLogout = () => {
    setAuth(null);
    setCart([]);
    setView('dashboard');
    setMessage('');
    window.localStorage.removeItem('pos_auth');
  };

  const openProduct = (product) => {
    setProductForm({
      id: product.id,
      barcode: product.barcode,
      name: product.name,
      price: product.price,
      stock: product.stock,
    });
    setView('products');
  };

  const resetForm = () => {
    setProductForm({ id: '', barcode: '', name: '', price: '', stock: '' });
  };

  const saveProduct = () => {
    if (!productForm.id || !productForm.barcode || !productForm.name || !productForm.price) {
      setMessage('Isi semua field produk dengan benar.');
      return;
    }
    const existingIndex = products.findIndex((item) => item.id === productForm.id);
    const newProduct = {
      id: productForm.id,
      barcode: productForm.barcode,
      name: productForm.name,
      price: Number(productForm.price),
      stock: Number(productForm.stock),
    };
    if (existingIndex >= 0) {
      const nextProducts = [...products];
      nextProducts[existingIndex] = newProduct;
      setProducts(nextProducts);
      setMessage('Produk diperbarui.');
    } else {
      if (products.some((item) => item.barcode === productForm.barcode)) {
        setMessage('Barcode sudah terdaftar.');
        return;
      }
      setProducts([...products, newProduct]);
      setMessage('Produk baru ditambahkan.');
    }
    resetForm();
  };

  const deleteProduct = (id) => {
    if (!window.confirm('Hapus produk ini?')) return;
    setProducts(products.filter((item) => item.id !== id));
    setMessage('Produk dihapus.');
  };

  const openUser = (user) => {
    setUserForm({
      id: user.id,
      username: user.username,
      password: user.password,
      role: user.role,
      name: user.name,
      isActive: user.isActive,
    });
    setView('users');
  };

  const resetUserForm = () => {
    setUserForm({ id: '', username: '', password: '', role: 'kasir', name: '', isActive: true });
  };

  const saveUser = () => {
    if (!userForm.username || !userForm.password || !userForm.name) {
      setMessage('Isi semua field user dengan benar.');
      return;
    }

    const existingIndex = users.findIndex((item) => item.username === userForm.username);
    const newUser = {
      id: userForm.username,
      username: userForm.username,
      password: userForm.password,
      role: userForm.role,
      name: userForm.name,
      isActive: userForm.isActive,
    };

    if (existingIndex >= 0) {
      const nextUsers = [...users];
      nextUsers[existingIndex] = newUser;
      setUsers(nextUsers);
      setMessage('Data user diperbarui.');
    } else {
      if (users.some((item) => item.username === userForm.username)) {
        setMessage('Username sudah terdaftar.');
        return;
      }
      setUsers([...users, newUser]);
      setMessage('User baru ditambahkan.');
    }
    resetUserForm();
  };

  const deleteUser = (username) => {
    if (username === auth?.username) {
      setMessage('Tidak dapat menghapus user yang sedang login.');
      return;
    }
    if (!window.confirm(`Hapus user ${username}?`)) return;
    setUsers(users.filter((item) => item.username !== username));
    setMessage('User dihapus.');
  };

  const addToCart = (productToAdd) => {
    if (!productToAdd || productToAdd.stock <= 0) {
      setMessage('Produk tidak tersedia atau stok habis.');
      return;
    }
    setCart((current) => {
      const existing = current.find((item) => item.id === productToAdd.id);
      if (existing) {
        return current.map((item) =>
          item.id === productToAdd.id ? { ...item, quantity: item.quantity + 1 } : item
        );
      }
      return [...current, { ...productToAdd, quantity: 1 }];
    });
    setMessage(`${productToAdd.name} ditambahkan ke keranjang.`);
  };

  const updateCartQuantity = (productId, delta) => {
    setCart((current) =>
      current
        .map((item) =>
          item.id === productId
            ? { ...item, quantity: Math.max(1, item.quantity + delta) }
            : item
        )
        .filter((item) => item.quantity > 0)
    );
  };

  const removeCartItem = (productId) => {
    setCart((current) => current.filter((item) => item.id !== productId));
  };

  const completeCheckout = () => {
    if (cart.length === 0) {
      setMessage('Tambahkan produk ke keranjang terlebih dahulu.');
      return;
    }
    const sale = {
      id: `S${Date.now()}`,
      cashier: auth.id,
      date: new Date().toISOString(),
      items: cart,
      total: totals.subtotal,
    };
    setSales([sale, ...sales]);
    setProducts((current) =>
      current.map((product) => {
        const sold = cart.find((item) => item.id === product.id);
        if (!sold) return product;
        return { ...product, stock: Math.max(0, product.stock - sold.quantity) };
      })
    );
    setCart([]);
    setMessage('Transaksi berhasil disimpan.');
  };

  const [paymentAmount, setPaymentAmount] = useState('');
  const [change, setChange] = useState(0);

  const calculateChange = (paid) => {
    const paidAmount = Number(paid) || 0;
    const changeAmount = Math.max(0, paidAmount - totals.subtotal);
    setChange(changeAmount);
    return changeAmount;
  };

  const handlePaymentSubmit = () => {
    if (cart.length === 0) {
      setMessage('Tambahkan produk ke keranjang terlebih dahulu.');
      return;
    }
    const paidAmount = Number(paymentAmount);
    if (!paidAmount || paidAmount < totals.subtotal) {
      setMessage('Jumlah pembayaran tidak cukup.');
      return;
    }
    completeCheckout();
    setPaymentAmount('');
    setChange(0);
  };

  const renderNavItem = (key, label) => (
    <button
      className={view === key ? 'nav-button active' : 'nav-button'}
      onClick={() => setView(key)}
    >
      {label}
    </button>
  );

  const renderHeader = () => (
    <header className="topbar">
      <div>
        <h1>POS React</h1>
        <p>{auth.role === 'admin' ? 'Panel Admin' : 'Panel Kasir'} • {capitalize(auth.role)}</p>
      </div>
      <div className="topbar-actions">
        <span>{auth.name}</span>
        <button className="button small" onClick={handleLogout}>
          Keluar
        </button>
      </div>
    </header>
  );

  const renderDashboard = () => (
    <section>
      <div className="grid cards">
        <article className="card">
          <h3>Pendapatan</h3>
          <strong>{currency(totals.totalSales)}</strong>
        </article>
        <article className="card">
          <h3>Transaksi</h3>
          <strong>{totals.transactionCount}</strong>
        </article>
        <article className="card">
          <h3>Produk</h3>
          <strong>{products.length}</strong>
        </article>
        <article className="card">
          <h3>Stok total</h3>
          <strong>{products.reduce((total, item) => total + item.stock, 0)}</strong>
        </article>
      </div>
      <div className="box">
        <h2>Ringkasan Penjualan Terbaru</h2>
        {sales.length === 0 ? (
          <p>Belum ada transaksi.</p>
        ) : (
          <div className="table-wrapper">
            <table>
              <thead>
                <tr>
                  <th>Waktu</th>
                  <th>Kasir</th>
                  <th>Jumlah</th>
                  <th>Total</th>
                </tr>
              </thead>
              <tbody>
                {sales.slice(0, 6).map((sale) => (
                  <tr key={sale.id}>
                    <td>{new Date(sale.date).toLocaleString('id-ID')}</td>
                    <td>{sale.cashier}</td>
                    <td>{sale.items.length}</td>
                    <td>{currency(sale.total)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </section>
  );

  const renderProducts = () => (
    <section>
      <div className="box">
        <div className="box-header">
          <h2>Daftar Barang</h2>
          {auth.role === 'admin' && (
            <button className="button" onClick={resetForm}>
              Tambah Produk Baru
            </button>
          )}
        </div>
        <div className="table-wrapper">
          <table>
            <thead>
              <tr>
                <th>ID</th>
                <th>Barcode</th>
                <th>Nama</th>
                <th>Harga</th>
                <th>Stok</th>
                <th>Aksi</th>
              </tr>
            </thead>
            <tbody>
              {products.map((product) => (
                <tr key={product.id}>
                  <td>{product.id}</td>
                  <td>{product.barcode}</td>
                  <td>{product.name}</td>
                  <td>{currency(product.price)}</td>
                  <td>{product.stock}</td>
                  <td className="actions-row">
                    <button className="button small" onClick={() => openProduct(product)}>
                      Edit
                    </button>
                    {auth.role === 'admin' && (
                      <button className="button small danger" onClick={() => deleteProduct(product.id)}>
                        Hapus
                      </button>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
      {auth.role === 'admin' && (
        <div className="box">
          <h2>{productForm.id ? 'Edit Produk' : 'Tambah Produk'}</h2>
          <div className="form-grid">
            <label>
              ID Produk
              <input
                value={productForm.id}
                onChange={(event) => setProductForm({ ...productForm, id: event.target.value.toUpperCase() })}
                placeholder="P006"
              />
            </label>
            <label>
              Barcode
              <input
                value={productForm.barcode}
                onChange={(event) => setProductForm({ ...productForm, barcode: event.target.value })}
                placeholder="123456789099"
              />
            </label>
            <label>
              Nama Produk
              <input
                value={productForm.name}
                onChange={(event) => setProductForm({ ...productForm, name: event.target.value })}
                placeholder="Nama produk"
              />
            </label>
            <label>
              Harga
              <input
                type="number"
                value={productForm.price}
                onChange={(event) => setProductForm({ ...productForm, price: event.target.value })}
                placeholder="10000"
              />
            </label>
            <label>
              Stok
              <input
                type="number"
                value={productForm.stock}
                onChange={(event) => setProductForm({ ...productForm, stock: event.target.value })}
                placeholder="50"
              />
            </label>
          </div>
          <div className="form-actions">
            <button className="button" onClick={saveProduct}>
              Simpan Produk
            </button>
            <button className="button secondary" onClick={resetForm}>
              Reset
            </button>
          </div>
        </div>
      )}
    </section>
  );

  const renderUsers = () => (
    <section>
      <div className="box">
        <div className="box-header">
          <h2>Manajemen User</h2>
          <button className="button" onClick={resetUserForm}>
            Tambah User Baru
          </button>
        </div>
        <div className="table-wrapper">
          <table>
            <thead>
              <tr>
                <th>Username</th>
                <th>Nama</th>
                <th>Role</th>
                <th>Status</th>
                <th>Aksi</th>
              </tr>
            </thead>
            <tbody>
              {users.map((user) => (
                <tr key={user.username}>
                  <td>{user.username}</td>
                  <td>{user.name}</td>
                  <td>{capitalize(user.role)}</td>
                  <td>{user.isActive ? 'Aktif' : 'Nonaktif'}</td>
                  <td className="actions-row">
                    <button className="button small" onClick={() => openUser(user)}>
                      Edit
                    </button>
                    {user.username !== auth.username && (
                      <button className="button small danger" onClick={() => deleteUser(user.username)}>
                        Hapus
                      </button>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
      <div className="box">
        <h2>{userForm.username ? 'Edit User' : 'Tambah User'}</h2>
        <div className="form-grid">
          <label>
            Username
            <input
              value={userForm.username}
              onChange={(event) => setUserForm({ ...userForm, username: event.target.value.trim() })}
              placeholder="admin"
            />
          </label>
          <label>
            Nama Lengkap
            <input
              value={userForm.name}
              onChange={(event) => setUserForm({ ...userForm, name: event.target.value })}
              placeholder="Nama lengkap"
            />
          </label>
          <label>
            Password
            <input
              type="password"
              value={userForm.password}
              onChange={(event) => setUserForm({ ...userForm, password: event.target.value })}
              placeholder="Password"
            />
          </label>
          <label>
            Role
            <select
              value={userForm.role}
              onChange={(event) => setUserForm({ ...userForm, role: event.target.value })}
            >
              <option value="kasir">Kasir</option>
              <option value="admin">Admin</option>
            </select>
          </label>
          <label>
            Status
            <select
              value={userForm.isActive ? 'active' : 'inactive'}
              onChange={(event) => setUserForm({ ...userForm, isActive: event.target.value === 'active' })}
            >
              <option value="active">Aktif</option>
              <option value="inactive">Nonaktif</option>
            </select>
          </label>
        </div>
        <div className="form-actions">
          <button className="button" onClick={saveUser}>
            Simpan User
          </button>
          <button className="button secondary" onClick={resetUserForm}>
            Reset
          </button>
        </div>
      </div>
    </section>
  );

  const renderPos = () => (
    <section className="kasir-main">
      {/* Shift Status Panel */}
      <div className="shift-panel">
        <div className="shift-info">
          <h3>Status Shift</h3>
          <div className="shift-details">
            <div className="shift-row">
              <span>Kasir:</span>
              <strong>{auth.name}</strong>
            </div>
            <div className="shift-row">
              <span>Shift:</span>
              <strong>Aktif</strong>
            </div>
            <div className="shift-row">
              <span>Waktu:</span>
              <strong>{new Date().toLocaleTimeString('id-ID')}</strong>
            </div>
          </div>
        </div>
      </div>

      {/* Main Kasir Layout */}
      <div className="kasir-layout">
        {/* Left Panel: Input & Search */}
        <div className="kasir-left-panel">
          <div className="input-section">
            <h3>Input Produk</h3>
            <div className="input-group">
              <label>Kode / Barcode</label>
              <div className="barcode-input-row">
                <input
                  value={barcode}
                  onChange={(event) => setBarcode(event.target.value)}
                  onKeyDown={(event) => event.key === 'Enter' && handleBarcodeSubmit(barcode)}
                  placeholder="Scan barcode atau masukkan ID"
                  className="barcode-input"
                />
                <button className="button primary" onClick={() => handleBarcodeSubmit(barcode)}>
                  Tambah
                </button>
              </div>
              <p className="hint-text">Tekan Enter setelah scan atau klik Tambah</p>
            </div>

            <div className="input-group">
              <label>Cari Manual</label>
              <input
                type="text"
                placeholder="Ketik nama produk..."
                className="search-input"
              />
            </div>
          </div>

          {/* Quick Stats */}
          <div className="stats-section">
            <h4>Statistik Hari Ini</h4>
            <div className="stats-grid">
              <div className="stat-item">
                <span>Total Item</span>
                <strong>{cart.reduce((sum, item) => sum + item.quantity, 0)}</strong>
              </div>
              <div className="stat-item">
                <span>Subtotal</span>
                <strong>{currency(totals.subtotal)}</strong>
              </div>
              <div className="stat-item">
                <span>Transaksi</span>
                <strong>{sales.length}</strong>
              </div>
            </div>
          </div>
        </div>

        {/* Center Panel: Cart */}
        <div className="kasir-center-panel">
          <div className="cart-section">
            <h3>Keranjang Penjualan</h3>
            <div className="cart-table-wrapper">
              <table className="cart-table">
                <thead>
                  <tr>
                    <th>Produk</th>
                    <th>Harga</th>
                    <th>Qty</th>
                    <th>Subtotal</th>
                    <th>Aksi</th>
                  </tr>
                </thead>
                <tbody>
                  {cart.length === 0 ? (
                    <tr>
                      <td colSpan="5" className="empty-cart">Keranjang kosong</td>
                    </tr>
                  ) : (
                    cart.map((item) => (
                      <tr key={item.id}>
                        <td>
                          <div className="product-info">
                            <strong>{item.name}</strong>
                            <small>{item.barcode}</small>
                          </div>
                        </td>
                        <td>{currency(item.price)}</td>
                        <td>
                          <div className="quantity-controls">
                            <button className="qty-btn" onClick={() => updateCartQuantity(item.id, -1)}>-</button>
                            <span className="qty-display">{item.quantity}</span>
                            <button className="qty-btn" onClick={() => updateCartQuantity(item.id, 1)}>+</button>
                          </div>
                        </td>
                        <td>{currency(item.quantity * item.price)}</td>
                        <td>
                          <button className="button small danger" onClick={() => removeCartItem(item.id)}>
                            Hapus
                          </button>
                        </td>
                      </tr>
                    ))
                  )}
                </tbody>
              </table>
            </div>
          </div>
        </div>

        {/* Right Panel: Payment */}
        <div className="kasir-right-panel">
          <div className="payment-section">
            <h3>Pembayaran</h3>

            <div className="payment-summary">
              <div className="summary-item">
                <span>Total Barang:</span>
                <strong>{cart.reduce((sum, item) => sum + item.quantity, 0)}</strong>
              </div>
              <div className="summary-item total">
                <span>Total Bayar:</span>
                <strong>{currency(totals.subtotal)}</strong>
              </div>
            </div>

            <div className="payment-input">
              <label>Jumlah Bayar</label>
              <input
                type="number"
                value={paymentAmount}
                onChange={(event) => {
                  setPaymentAmount(event.target.value);
                  calculateChange(event.target.value);
                }}
                placeholder="0"
                className="payment-input-field"
              />
            </div>

            <div className="change-display">
              <div className="change-item">
                <span>Kembalian:</span>
                <strong className={change > 0 ? 'positive' : ''}>{currency(change)}</strong>
              </div>
            </div>

            <button
              className="button primary payment-btn"
              onClick={handlePaymentSubmit}
              disabled={cart.length === 0 || Number(paymentAmount) < totals.subtotal}
            >
              Bayar & Simpan
            </button>
          </div>

          {/* Quick Actions */}
          <div className="quick-actions">
            <button className="button secondary" onClick={() => setView('dashboard')}>
              Kembali ke Dashboard
            </button>
            <button className="button secondary" onClick={() => setCart([])}>
              Kosongkan Keranjang
            </button>
          </div>
        </div>
      </div>

      {/* Bottom Panel: Quick Products */}
      <div className="quick-products-panel">
        <h3>Produk Cepat</h3>
        <div className="quick-products-grid">
          {products.slice(0, 12).map((product) => (
            <div
              key={product.id}
              className={`quick-product-card ${product.stock <= 0 ? 'out-of-stock' : ''}`}
              onClick={() => product.stock > 0 && addToCart(product)}
            >
              <div className="product-title">
                <h4>{product.name}</h4>
                <small>{product.barcode}</small>
              </div>
              <div className="product-price">{currency(product.price)}</div>
              <div className="product-stock">Stok: {product.stock}</div>
            </div>
          ))}
        </div>
      </div>
    </section>
  );

  const renderReports = () => (
    <section>
      <div className="box">
        <h2>Laporan Penjualan</h2>
        {sales.length === 0 ? (
          <p>Belum ada data penjualan untuk ditampilkan.</p>
        ) : (
          <>
            <div className="grid cards">
              <article className="card">
                <h3>Total Pendapatan</h3>
                <strong>{currency(totals.totalSales)}</strong>
              </article>
              <article className="card">
                <h3>Transaksi</h3>
                <strong>{totals.transactionCount}</strong>
              </article>
            </div>
            <div className="box">
              <h3>Ringkasan Harian</h3>
              <div className="table-wrapper">
                <table>
                  <thead>
                    <tr>
                      <th>Tanggal</th>
                      <th>Total</th>
                    </tr>
                  </thead>
                  <tbody>
                    {Object.entries(totals.salesByDate).map(([date, total]) => (
                      <tr key={date}>
                        <td>{date}</td>
                        <td>{currency(total)}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>
            <div className="box">
              <h3>Daftar Transaksi</h3>
              <div className="table-wrapper">
                <table>
                  <thead>
                    <tr>
                      <th>ID</th>
                      <th>Waktu</th>
                      <th>Kasir</th>
                      <th>Item</th>
                      <th>Total</th>
                    </tr>
                  </thead>
                  <tbody>
                    {sales.map((sale) => (
                      <tr key={sale.id}>
                        <td>{sale.id}</td>
                        <td>{new Date(sale.date).toLocaleString('id-ID')}</td>
                        <td>{sale.cashier}</td>
                        <td>{sale.items.length}</td>
                        <td>{currency(sale.total)}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>
          </>
        )}
      </div>
    </section>
  );

  const renderLayout = () => (
    <div className="app-shell">
      {renderHeader()}
      <div className="layout">
        <aside className="sidebar">
          <div className="section-title">Menu</div>
          {renderNavItem('dashboard', 'Dashboard')}
          {renderNavItem('pos', 'Kasir')}
          {renderNavItem('products', 'Daftar Barang')}
          {auth.role === 'admin' && renderNavItem('users', 'Pengguna')}
          {auth.role === 'admin' && renderNavItem('reports', 'Laporan')}
        </aside>
        <main className="main-content">
          {message && <div className="alert">{message}</div>}
          {view === 'dashboard' && renderDashboard()}
          {view === 'products' && renderProducts()}
          {view === 'pos' && renderPos()}
          {view === 'users' && auth.role === 'admin' && renderUsers()}
          {view === 'reports' && auth.role === 'admin' && renderReports()}
        </main>
      </div>
    </div>
  );

  if (!auth) {
    return (
      <div className="login-screen">
        <div className="login-card">
          <h2>Masuk ke POS</h2>
          <label>
            Username
            <input
              value={loginState.user}
              onChange={(event) => setLoginState({ ...loginState, user: event.target.value })}
              placeholder="admin atau kasir"
            />
          </label>
          <label>
            Password
            <input
              type="password"
              value={loginState.pass}
              onChange={(event) => setLoginState({ ...loginState, pass: event.target.value })}
              placeholder="password"
            />
          </label>
          <button className="button primary" onClick={handleLogin}>
            Masuk
          </button>
          <p className="helper-text">admin/admin123 atau kasir/kasir123</p>
          {message && <div className="alert">{message}</div>}
        </div>
      </div>
    );
  }

  return renderLayout();
}

export default App;

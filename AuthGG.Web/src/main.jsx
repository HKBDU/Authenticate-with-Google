import React from 'react';
import { createRoot } from 'react-dom/client';
import './styles.css';

const API_URL = import.meta.env.VITE_API_URL || 'http://localhost:8000';

function App() {
  const [user, setUser] = React.useState(null);
  const [error, setError] = React.useState('');
  const [loading, setLoading] = React.useState(true);

  const loadUser = async () => {
    try {
      const response = await fetch(`${API_URL}/api/auth/me`, { credentials: 'include' });
      if (response.ok) setUser(await response.json());
    } catch { setError('Không thể kết nối tới máy chủ.'); }
    finally { setLoading(false); }
  };

  React.useEffect(() => { loadUser(); }, []);

  const loginUrl = `${API_URL}/api/auth/google/login`;
  const logout = async () => {
    await fetch(`${API_URL}/api/auth/logout`, { method: 'POST', credentials: 'include' });
    setUser(null);
  };

  return <main className="shell"><section className="card">
    <div className="brand">AuthGG</div>
    <h1>{loading ? 'Đang kiểm tra phiên đăng nhập...' : user ? `Xin chào, ${user.name}` : 'Đăng nhập an toàn'}</h1>
    {!loading && user ? <>
      {user.picture && <img className="avatar" src={user.picture} alt="Ảnh đại diện" referrerPolicy="no-referrer" />}
      <p className="muted">{user.email}</p>
      <p className="success">Bạn đã đăng nhập bằng Google.</p>
      <button className="secondary" onClick={logout}>Đăng xuất</button>
    </> : !loading && <>
      <p className="muted">Sử dụng tài khoản Google để tiếp tục.</p>
      <div className="login"><a className="google-button" href={loginUrl}>Tiếp tục với Google</a></div>
    </>}
    {error && <p className="error" role="alert">{error}</p>}
  </section></main>;
}

createRoot(document.getElementById('root')).render(<React.StrictMode><App /></React.StrictMode>);

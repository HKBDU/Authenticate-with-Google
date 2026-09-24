import React from 'react';
import { createRoot } from 'react-dom/client';
import './styles.css';

const API_URL = import.meta.env.VITE_API_URL || 'http://localhost:8000';

function GoogleIcon() {
  return <svg className="google-icon" viewBox="0 0 24 24" aria-hidden="true">
    <path fill="#4285F4" d="M21.35 12.23c0-.72-.06-1.42-.18-2.09H12v3.96h5.24a4.48 4.48 0 0 1-1.94 2.94v2.45h3.14c1.84-1.7 2.91-4.2 2.91-7.26Z" />
    <path fill="#34A853" d="M12 21.72c2.63 0 4.84-.87 6.45-2.36l-3.14-2.45c-.87.58-1.98.93-3.31.93-2.54 0-4.69-1.72-5.46-4.03H3.3v2.53A9.74 9.74 0 0 0 12 21.72Z" />
    <path fill="#FBBC05" d="M6.54 13.81a5.84 5.84 0 0 1 0-3.62V7.66H3.3a9.73 9.73 0 0 0 0 8.68l3.24-2.53Z" />
    <path fill="#EA4335" d="M12 6.16c1.43 0 2.72.49 3.73 1.45l2.8-2.8C16.83 3.25 14.63 2.28 12 2.28a9.74 9.74 0 0 0-8.7 5.38l3.24 2.53C7.31 7.88 9.46 6.16 12 6.16Z" />
  </svg>;
}

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
    <div className="brand"><span className="brand-mark">G</span><span>AuthGG</span></div>
    <h1>{loading ? 'Đang kiểm tra phiên đăng nhập...' : user ? `Xin chào, ${user.name}` : 'Đăng nhập an toàn'}</h1>
    {!loading && user ? <>
      {user.picture && <img className="avatar" src={user.picture} alt="Ảnh đại diện" referrerPolicy="no-referrer" />}
      <p className="muted">{user.email}</p>
      <p className="success">Bạn đã đăng nhập bằng Google.</p>
      <button className="secondary" onClick={logout}>Đăng xuất</button>
    </> : !loading && <>
      <p className="muted">Sử dụng tài khoản Google để tiếp tục.</p>
      <div className="login"><a className="google-button" href={loginUrl}><GoogleIcon /><span>Đăng nhập bằng Google</span></a></div>
    </>}
    {error && <p className="error" role="alert">{error}</p>}
  </section></main>;
}

createRoot(document.getElementById('root')).render(<React.StrictMode><App /></React.StrictMode>);

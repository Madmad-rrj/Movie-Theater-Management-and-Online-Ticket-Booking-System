import { useEffect, useState } from 'react';
import { HomePage } from './pages/HomePage';
import { MoviesPage } from './pages/MoviesPage';
import { MovieDetailsPage } from './pages/MovieDetailsPage';
import { BookingPage } from './pages/BookingPage';
import { PaymentPage } from './pages/PaymentPage';
import { BookingConfirmationPage } from './pages/BookingConfirmationPage';

type Route =
  | { name: 'home' }
  | { name: 'movies' }
  | { name: 'details'; id: string }
  | { name: 'booking'; id: string }
  | { name: 'payment'; id: string }
  | { name: 'confirmation'; id: string };

function routeFromHash(): Route {
  const parts = window.location.hash.replace(/^#\/?/, '').split('/').filter(Boolean);
  if (parts[0] === 'movies') return { name: 'movies' };
  if (parts[0] === 'movie' && parts[1]) return { name: 'details', id: parts[1] };
  if (parts[0] === 'book' && parts[1]) return { name: 'booking', id: parts[1] };
  if (parts[0] === 'payment' && parts[1]) return { name: 'payment', id: parts[1] };
  if (parts[0] === 'confirmation' && parts[1]) return { name: 'confirmation', id: parts[1] };
  return { name: 'home' };
}

export default function App() {
  const [route, setRoute] = useState<Route>(routeFromHash);
  useEffect(() => {
    const update = () => setRoute(routeFromHash());
    window.addEventListener('hashchange', update);
    return () => window.removeEventListener('hashchange', update);
  }, []);

  const navigate = (path: string) => { window.location.hash = path; window.scrollTo(0, 0); };
  const home = () => navigate('/');
  const showFlow = route.name === 'booking' || route.name === 'payment' || route.name === 'confirmation';
  const activeStep = route.name === 'booking' ? 1 : route.name === 'payment' ? 2 : 3;

  return (
    <div className="site-shell">
      <header className="site-header">
        <button className="brand brand-button" type="button" onClick={home} aria-label="Promethe-Phos trang chủ"><img className="brand-logo" src="/logo.png" alt="" /><span className="brand-wordmark"><b>PROMETHE-PHOS</b><small>NGỌN LỬA ĐIỆN ẢNH</small></span></button>
        <nav className="site-nav" aria-label="Điều hướng chính">
          <button className={route.name === 'home' ? 'is-active' : ''} type="button" onClick={home}>Trang chủ</button>
          <button className={route.name !== 'home' ? 'is-active' : ''} type="button" onClick={() => navigate('/movies')}>Phim</button>
          <button type="button" onClick={() => navigate('/movies')}>Rạp chiếu</button>
          <button type="button" onClick={home}>Tra cứu vé</button>
        </nav>
        <div className="header-actions"><label className="header-search"><span>⌕</span><input aria-label="Tìm phim hoặc rạp chiếu" placeholder="Tìm phim, rạp chiếu..." /></label><button className="login-button" type="button">Đăng nhập</button></div>
      </header>
      {showFlow && <div className="flow-stepper" aria-label="Tiến trình đặt vé">
        {['Thông tin khách hàng', 'Thanh toán', 'Thông tin phim'].map((label, index) => {
          const number = index + 1;
          return <div className={`flow-step ${number <= activeStep ? 'is-active' : ''}`} key={label}><span>{number < activeStep ? '✓' : `0${number}`}</span><b>{label}</b></div>;
        })}
      </div>}
      {route.name === 'home' && <HomePage onMovies={() => navigate('/movies')} onDetails={(id) => navigate(`/movie/${id}`)} onBook={(id) => navigate(`/book/${id}`)} />}
      {route.name === 'movies' && <MoviesPage onDetails={(id) => navigate(`/movie/${id}`)} onBook={(id) => navigate(`/book/${id}`)} />}
      {route.name === 'details' && <MovieDetailsPage movieId={route.id} onBack={() => navigate('/movies')} onBook={() => navigate(`/book/${route.id}`)} />}
      {route.name === 'booking' && <BookingPage movieId={route.id} onHome={home} onPayment={(id) => navigate(`/payment/${id}`)} />}
      {route.name === 'payment' && <PaymentPage bookingId={route.id} onHome={home} onConfirmed={(id) => navigate(`/confirmation/${id}`)} />}
      {route.name === 'confirmation' && <BookingConfirmationPage bookingId={route.id} onHome={home} />}
      <footer className="site-footer"><div className="brand brand--footer"><div className="brand-footer-title"><img className="brand-logo" src="/logo.png" alt="" /><span className="brand-wordmark"><b>PROMETHE-PHOS</b></span></div><p>Điện ảnh, theo một cách khác.</p></div><span>© 2026 Promethe-Phos</span></footer>
    </div>
  );
}

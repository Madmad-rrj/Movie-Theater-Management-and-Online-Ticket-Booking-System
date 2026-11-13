import { useEffect, useState } from 'react';
import { MovieCard } from '../components/MovieCard';
import { StatePanel } from '../components/StatePanel';
import { getCinemas, getMovies, getShowtimes } from '../services/bookingApi';
import type { Cinema, Movie, Showtime } from '../types/api';

function nextDate(): string {
  const date = new Date();
  date.setDate(date.getDate() + 1);
  return date.toISOString().slice(0, 10);
}

export function HomePage({ onMovies, onDetails, onBook }: { onMovies: () => void; onDetails: (id: string) => void; onBook: (id: string) => void }) {
  const [nowShowing, setNowShowing] = useState<Movie[]>([]);
  const [comingSoon, setComingSoon] = useState<Movie[]>([]);
  const [cinemas, setCinemas] = useState<Cinema[]>([]);
  const [quickMovieId, setQuickMovieId] = useState('');
  const [quickCinemaId, setQuickCinemaId] = useState('');
  const [quickDate, setQuickDate] = useState(nextDate);
  const [quickShowtimes, setQuickShowtimes] = useState<Showtime[]>([]);
  const [quickShowtimeId, setQuickShowtimeId] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(true);
  useEffect(() => {
    Promise.all([getMovies('?status=now_showing&page=1&pageSize=12'), getMovies('?status=coming_soon&page=1&pageSize=12'), getCinemas()])
      .then(([now, coming, cinemaResult]) => {
        setNowShowing(now);
        setComingSoon(coming);
        setCinemas(cinemaResult);
        if (now[0]) setQuickMovieId(now[0].movieId);
        if (cinemaResult[0]) setQuickCinemaId(cinemaResult[0].cinemaId);
      })
      .catch((cause) => setError(cause instanceof Error ? cause.message : 'Không thể tải danh sách phim.'))
      .finally(() => setLoading(false));
  }, []);
  useEffect(() => {
    if (!quickMovieId || !quickDate) return;
    getShowtimes(quickMovieId, quickDate, quickCinemaId)
      .then((items) => {
        const available = items.filter((item) => item.bookingAvailable);
        setQuickShowtimes(available);
        setQuickShowtimeId(available[0]?.showtimeId || '');
      })
      .catch(() => { setQuickShowtimes([]); setQuickShowtimeId(''); });
  }, [quickCinemaId, quickDate, quickMovieId]);
  return (
    <div>
      <section className="hero hero--cinematic" id="about">
        <div className="hero__content">
          <p className="eyebrow">MỖI THƯỚC PHIM, MỘT HUYỀN THOẠI</p>
          <h1>THẮP SÁNG ĐIỆN ẢNH</h1>
          <p className="hero__copy">Khám phá những câu chuyện vượt thời gian.</p>
          <div className="hero__actions"><button className="button button--gold" type="button" onClick={onMovies}>Đặt vé ngay →</button><button className="button button--outline" type="button" onClick={onMovies}>Xem lịch chiếu</button></div>
        </div>
        <span className="hero__year">TỪ NĂM 2026</span>
      </section>
      <section className="quick-booking">
        <div className="quick-booking__title"><span>♧</span><b>Đặt vé nhanh</b></div>
        <label>Chọn rạp<select value={quickCinemaId} onChange={(event) => setQuickCinemaId(event.target.value)}>{cinemas.map((cinema) => <option value={cinema.cinemaId} key={cinema.cinemaId}>{cinema.name}</option>)}</select></label>
        <label>Chọn phim<select value={quickMovieId} onChange={(event) => setQuickMovieId(event.target.value)}>{nowShowing.map((movie) => <option value={movie.movieId} key={movie.movieId}>{movie.title}</option>)}</select></label>
        <label>Chọn ngày<input type="date" value={quickDate} onChange={(event) => setQuickDate(event.target.value)} /></label>
        <label>Chọn suất<select value={quickShowtimeId} onChange={(event) => setQuickShowtimeId(event.target.value)}>{quickShowtimes.map((item) => <option value={item.showtimeId} key={item.showtimeId}>{new Date(item.officialStartsAt).toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' })} · {item.roomName}</option>)}</select></label>
        <button className="button button--gold" type="button" disabled={!quickMovieId} onClick={() => onBook(quickMovieId)}>Đặt vé</button>
      </section>
      <section className="movie-section" id="now-showing">
        <div className="section-heading"><div><p className="eyebrow">TUYỂN CHỌN HÔM NAY</p><h2>PHIM ĐANG CHIẾU</h2></div><button className="text-button" type="button" onClick={onMovies}>Tất cả phim ↗</button></div>
        {loading && <div className="movie-grid">{Array.from({ length: 6 }, (_, index) => <div className="movie-skeleton" key={index} />)}</div>}
        {!loading && error && <StatePanel title="Chưa thể tải danh sách phim" message={error} actionLabel="Thử lại" onAction={() => window.location.reload()} />}
        {!loading && !error && nowShowing.length === 0 && <StatePanel title="Chưa có phim đang chiếu" message="Hãy quay lại sau để khám phá những bộ phim mới." />}
        {!loading && !error && nowShowing.length > 0 && <div className="movie-grid">{nowShowing.map((movie) => <MovieCard key={movie.movieId} movie={movie} onDetails={onDetails} onBook={onBook} />)}</div>}
      </section>
      {!loading && !error && comingSoon.length > 0 && <section className="movie-section movie-section--alt"><div className="section-heading"><div><p className="eyebrow">SẮP RA MẮT</p><h2>Coming soon</h2></div></div><div className="movie-grid">{comingSoon.map((movie) => <MovieCard key={movie.movieId} movie={movie} onDetails={onDetails} />)}</div></section>}
    </div>
  );
}

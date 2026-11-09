import { useCallback, useEffect, useState } from 'react';
import { MovieCard } from '../components/MovieCard';
import { StatePanel } from '../components/StatePanel';
import { fetchMovies } from '../services/moviesApi';
import type { Movie } from '../types/movies';

type LoadState = 'loading' | 'success' | 'error';

export function HomePage() {
  const [movies, setMovies] = useState<Movie[]>([]);
  const [state, setState] = useState<LoadState>('loading');
  const [errorMessage, setErrorMessage] = useState('');

  const loadMovies = useCallback(async (signal?: AbortSignal) => {
    setState('loading');
    setErrorMessage('');

    try {
      const response = await fetchMovies(signal);
      setMovies(response.data);
      setState('success');
    } catch (error) {
      if (error instanceof DOMException && error.name === 'AbortError') {
        return;
      }

      setErrorMessage(error instanceof Error ? error.message : 'Không thể tải danh sách phim.');
      setState('error');
    }
  }, []);

  useEffect(() => {
    const controller = new AbortController();
    void loadMovies(controller.signal);
    return () => controller.abort();
  }, [loadMovies]);

  return (
    <div className="site-shell">
      <header className="site-header">
        <a className="brand" href="/" aria-label="Promethe-Phos trang chủ">
          <span className="brand__symbol">✦</span>
          <span>PROMETHE<span>-</span>PHOS</span>
        </a>
        <nav className="site-nav" aria-label="Điều hướng chính">
          <a href="#now-showing">Đang chiếu</a>
          <a href="#about">Về chúng tôi</a>
        </nav>
      </header>

      <main>
        <section className="hero" id="about">
          <div className="hero__glow" aria-hidden="true" />
          <p className="eyebrow">MỘT TRẢI NGHIỆM ĐIỆN ẢNH KHÁC BIỆT</p>
          <h1>Ánh sáng mở ra<br /><em>những thế giới mới.</em></h1>
          <p className="hero__copy">
            Khám phá những câu chuyện đáng nhớ trên màn ảnh tại Promethe-Phos.
          </p>
          <a className="button button--gold" href="#now-showing">Khám phá phim</a>
        </section>

        <section className="movie-section" id="now-showing">
          <div className="section-heading">
            <div>
              <p className="eyebrow">TUYỂN CHỌN HÔM NAY</p>
              <h2>Phim đang chiếu</h2>
            </div>
            {state === 'success' && <span className="movie-count">{movies.length} phim</span>}
          </div>

          {state === 'loading' && (
            <div className="movie-grid" aria-label="Đang tải phim">
              {Array.from({ length: 6 }, (_, index) => <div className="movie-skeleton" key={index} />)}
            </div>
          )}
          {state === 'error' && (
            <StatePanel
              title="Chưa thể tải danh sách phim"
              message={errorMessage}
              actionLabel="Thử lại"
              onAction={() => void loadMovies()}
            />
          )}
          {state === 'success' && movies.length === 0 && (
            <StatePanel title="Chưa có phim đang chiếu" message="Hãy quay lại sau để khám phá những bộ phim mới." />
          )}
          {state === 'success' && movies.length > 0 && (
            <div className="movie-grid">
              {movies.map((movie) => <MovieCard key={movie.movieId} movie={movie} />)}
            </div>
          )}
        </section>
      </main>

      <footer className="site-footer">
        <div className="brand brand--footer"><span className="brand__symbol">✦</span><span>PROMETHE<span>-</span>PHOS</span></div>
        <p>Điện ảnh, theo một cách khác.</p>
        <span>© 2026 Promethe-Phos</span>
      </footer>
    </div>
  );
}

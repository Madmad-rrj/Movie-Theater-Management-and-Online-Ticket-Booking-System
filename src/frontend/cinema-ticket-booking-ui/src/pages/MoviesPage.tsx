import { useEffect, useState } from 'react';
import { MovieCard } from '../components/MovieCard';
import { StatePanel } from '../components/StatePanel';
import { getMovies } from '../services/bookingApi';
import type { Movie } from '../types/api';

export function MoviesPage({ onDetails, onBook }: { onDetails: (id: string) => void; onBook: (id: string) => void }) {
  const [movies, setMovies] = useState<Movie[]>([]);
  const [search, setSearch] = useState('');
  const [status, setStatus] = useState('');
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  async function load() {
    setLoading(true);
    setError('');
    try {
      const params = new URLSearchParams({ page: '1', pageSize: '24' });
      if (search.trim()) params.set('search', search.trim());
      if (status) params.set('status', status);
      setMovies(await getMovies(`?${params}`));
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : 'Không thể tải danh mục phim.');
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => { void load(); }, [status]);

  return (
    <main className="catalogue-page">
      <p className="eyebrow">DANH MỤC PHIM</p>
      <h1>Chọn câu chuyện của bạn.</h1>
      <form className="catalogue-filters" onSubmit={(event) => { event.preventDefault(); void load(); }}>
        <input aria-label="Tìm phim" placeholder="Tìm theo tên phim..." value={search} onChange={(event) => setSearch(event.target.value)} />
        <select aria-label="Trạng thái phim" value={status} onChange={(event) => setStatus(event.target.value)}>
          <option value="">Tất cả</option>
          <option value="now_showing">Đang chiếu</option>
          <option value="coming_soon">Sắp chiếu</option>
        </select>
        <button className="button button--gold" type="submit">Tìm kiếm</button>
      </form>
      {loading && <div className="movie-grid">{Array.from({ length: 6 }, (_, index) => <div className="movie-skeleton" key={index} />)}</div>}
      {!loading && error && <StatePanel title="Không thể tải danh mục" message={error} actionLabel="Thử lại" onAction={() => void load()} />}
      {!loading && !error && movies.length === 0 && <StatePanel title="Không tìm thấy phim" message="Hãy thử từ khóa hoặc bộ lọc khác." />}
      {!loading && !error && movies.length > 0 && <div className="movie-grid">{movies.map((movie) => <MovieCard key={movie.movieId} movie={movie} onDetails={onDetails} onBook={onBook} />)}</div>}
    </main>
  );
}

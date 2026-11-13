import { useEffect, useState } from 'react';
import { getMovie } from '../services/bookingApi';
import type { Movie } from '../types/api';
import { StatePanel } from '../components/StatePanel';

export function MovieDetailsPage({ movieId, onBack, onBook }: { movieId: string; onBack: () => void; onBook: () => void }) {
  const [movie, setMovie] = useState<Movie | null>(null);
  const [error, setError] = useState('');
  useEffect(() => {
    getMovie(movieId).then(setMovie).catch((cause) => setError(cause instanceof Error ? cause.message : 'Không thể tải chi tiết phim.'));
  }, [movieId]);
  if (error) return <div className="page-state"><StatePanel title="Không thể tải phim" message={error} actionLabel="Quay lại" onAction={onBack} /></div>;
  if (!movie) return <div className="page-state"><StatePanel title="Đang tải phim" message="Đang lấy thông tin mới nhất..." /></div>;
  return (
    <main className="details-page">
      <button className="back-link" type="button" onClick={onBack}>← Danh mục phim</button>
      <div className="details-layout">
        <div className="details-poster">{movie.posterUrl ? <img src={movie.posterUrl} alt={`Poster ${movie.title}`} /> : <span>{movie.title.slice(0, 1)}</span>}</div>
        <div>
          <p className="eyebrow">{movie.status === 'coming_soon' ? 'SẮP CHIẾU' : 'ĐANG CHIẾU'}</p>
          <h1>{movie.title}</h1>
          <p className="details-meta">{movie.durationMinutes} phút · {movie.language || 'Đang cập nhật'} · {movie.ageRating || 'Mọi độ tuổi'}</p>
          <p className="details-description">{movie.description || 'Thông tin nội dung đang được cập nhật.'}</p>
          <p className="details-genres">{movie.genres.map((genre) => genre.name).join(' · ')}</p>
          {movie.status !== 'coming_soon' && <button className="button button--gold" type="button" onClick={onBook}>Chọn suất chiếu</button>}
        </div>
      </div>
    </main>
  );
}

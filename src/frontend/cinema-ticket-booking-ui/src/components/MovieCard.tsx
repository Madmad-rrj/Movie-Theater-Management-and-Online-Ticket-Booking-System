import type { Movie } from '../types/movies';

interface MovieCardProps {
  movie: Movie;
}

function Poster({ movie }: MovieCardProps) {
  const fallback = (
    <div className="movie-card__poster movie-card__poster--fallback" aria-label="Poster chưa có">
      <span>{movie.title.slice(0, 1).toUpperCase()}</span>
      <small>PROMETHE-PHOS</small>
    </div>
  );

  if (!movie.posterUrl) {
    return fallback;
  }

  return (
    <img
      className="movie-card__poster"
      src={movie.posterUrl}
      alt={`Poster ${movie.title}`}
      onError={(event) => {
        event.currentTarget.style.display = 'none';
        event.currentTarget.nextElementSibling?.removeAttribute('hidden');
      }}
    />
  );
}

export function MovieCard({ movie }: MovieCardProps) {
  return (
    <article className="movie-card">
      <div className="movie-card__media">
        <Poster movie={movie} />
        {movie.posterUrl && (
          <div className="movie-card__poster movie-card__poster--fallback" hidden aria-label="Poster chưa có">
            <span>{movie.title.slice(0, 1).toUpperCase()}</span>
            <small>PROMETHE-PHOS</small>
          </div>
        )}
        <span className="movie-card__status">ĐANG CHIẾU</span>
      </div>
      <div className="movie-card__body">
        <h3>{movie.title}</h3>
        <div className="movie-card__meta">
          <span>{movie.durationMinutes} phút</span>
          {movie.ageRating && <span>{movie.ageRating}</span>}
        </div>
        <p className="movie-card__genres">
          {movie.genres.length > 0 ? movie.genres.map((genre) => genre.name).join(' · ') : 'Chưa có thể loại'}
        </p>
      </div>
    </article>
  );
}

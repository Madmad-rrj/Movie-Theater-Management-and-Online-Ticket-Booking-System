export interface Genre {
  genreId: string;
  name: string;
  description: string | null;
  isActive: boolean;
  version: string;
}

export interface Movie {
  movieId: string;
  title: string;
  description: string | null;
  durationMinutes: number;
  releaseDate: string | null;
  posterUrl: string | null;
  trailerUrl: string | null;
  language: string | null;
  ageRating: string | null;
  status: string;
  genres: Genre[];
  version: string;
}

export interface Pagination {
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
}

export interface MoviesResponse {
  data: Movie[];
  pagination: Pagination;
}

export interface ApiErrorResponse {
  error?: {
    code?: string;
    message?: string;
  };
}

export type { Genre, Movie, Pagination } from './api';
import type { Movie, Pagination } from './api';

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

import type { ApiErrorResponse, MoviesResponse } from '../types/movies';

export async function fetchMovies(signal?: AbortSignal): Promise<MoviesResponse> {
  const response = await fetch('/api/movies', {
    headers: { Accept: 'application/json' },
    signal,
  });

  if (!response.ok) {
    let message = 'Không thể tải danh sách phim.';

    try {
      const body = (await response.json()) as ApiErrorResponse;
      if (body.error?.message) {
        message = body.error.message;
      }
    } catch {
      // Keep the user-facing message safe when the server does not return JSON.
    }

    throw new Error(message);
  }

  return (await response.json()) as MoviesResponse;
}

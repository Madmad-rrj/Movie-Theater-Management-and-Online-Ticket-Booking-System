import { apiRequest, ensureGuestContext, unwrap } from './apiClient';
import type {
  ApiResponse,
  Booking,
  Cinema,
  CinemaPricing,
  Movie,
  Payment,
  PaymentInitiation,
  Quantities,
  SeatMap,
  Selection,
  Showtime,
  TicketCategory,
} from '../types/api';

export async function getMovies(query = '') {
  return unwrap(await apiRequest<ApiResponse<Movie[]>>(`/api/movies${query}`));
}

export async function getMovie(movieId: string) {
  return unwrap(await apiRequest<ApiResponse<Movie>>(`/api/movies/${encodeURIComponent(movieId)}`));
}

export async function getCinemas() {
  return unwrap(await apiRequest<ApiResponse<Cinema[]>>('/api/cinemas'));
}

export async function getShowtimes(movieId: string, date: string, cinemaId?: string) {
  const params = new URLSearchParams({ date, page: '1', pageSize: '100' });
  if (cinemaId) params.set('cinemaId', cinemaId);
  return unwrap(await apiRequest<ApiResponse<Showtime[]>>(`/api/movies/${encodeURIComponent(movieId)}/showtimes?${params}`));
}

export async function getShowtime(showtimeId: string) {
  return unwrap(await apiRequest<ApiResponse<Showtime>>(`/api/showtimes/${encodeURIComponent(showtimeId)}`));
}

export async function getSeats(showtimeId: string) {
  return unwrap(await apiRequest<ApiResponse<SeatMap>>(`/api/showtimes/${encodeURIComponent(showtimeId)}/seats`));
}

export async function getTicketCategories() {
  return unwrap(await apiRequest<ApiResponse<TicketCategory[]>>('/api/ticket-categories'));
}

export async function getPricing(cinemaId: string) {
  return unwrap(await apiRequest<ApiResponse<CinemaPricing>>(`/api/cinemas/${encodeURIComponent(cinemaId)}/ticket-pricing`));
}

export async function createSelection(showtimeId: string, quantities: Quantities, seatIds: string[]) {
  await ensureGuestContext();
  return unwrap(await apiRequest<ApiResponse<Selection>>('/api/selections', {
    method: 'POST',
    body: JSON.stringify({ showtimeId, quantities, seatIds }),
  }, { mutation: true }));
}

export async function updateSelection(selection: Selection, quantities: Quantities, seatIds: string[]) {
  await ensureGuestContext();
  return unwrap(await apiRequest<ApiResponse<Selection>>(`/api/selections/${selection.selectionId}`, {
    method: 'PUT',
    body: JSON.stringify({ version: selection.version, quantities, seatIds }),
  }, { mutation: true }));
}

export async function getSelection(selectionId: string) {
  await ensureGuestContext();
  return unwrap(await apiRequest<ApiResponse<Selection>>(`/api/selections/${selectionId}`));
}

export async function createBooking(
  selection: Selection,
  showtimeId: string,
  seatIds: string[],
  quantities: Quantities,
  contact: { fullName: string; phone: string; email: string },
) {
  await ensureGuestContext();
  if (!selection.quote) throw new Error('Báo giá lựa chọn không còn khả dụng.');
  return unwrap(await apiRequest<ApiResponse<Booking>>('/api/bookings', {
    method: 'POST',
    body: JSON.stringify({
      selectionId: selection.selectionId,
      selectionVersion: selection.version,
      showtimeId,
      seatIds,
      quantities,
      contact,
      declarations: { termsAccepted: true, ageRequirementsAcknowledged: true },
      priceAcceptance: {
        quoteId: selection.quote.quoteId,
        version: selection.quote.version,
        acceptedTotal: selection.quote.total,
      },
    }),
  }, { mutation: true }));
}

export async function getBooking(bookingId: string) {
  await ensureGuestContext();
  return unwrap(await apiRequest<ApiResponse<Booking>>(`/api/bookings/${bookingId}`));
}

export async function createPayment(bookingId: string) {
  await ensureGuestContext();
  return unwrap(await apiRequest<ApiResponse<PaymentInitiation>>(`/api/bookings/${bookingId}/payments`, {
    method: 'POST',
    body: JSON.stringify({ provider: 'MockPay' }),
  }, { mutation: true }));
}

export async function getPayment(paymentId: string) {
  await ensureGuestContext();
  return unwrap(await apiRequest<ApiResponse<Payment>>(`/api/payments/${paymentId}`));
}

export interface ApiError {
  error?: {
    code?: string;
    message?: string;
    details?: {
      violations?: Array<{ field?: string; reason?: string; message?: string }>;
    };
    traceId?: string;
  };
}

export interface ApiResponse<T> {
  data: T;
  pagination?: Pagination;
}

export interface Pagination {
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
}

export interface Money {
  amount: number;
  currency: string;
}

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

export interface Cinema {
  cinemaId: string;
  name: string;
  address: string;
  timezone: string;
  isActive: boolean;
  minimumTurnaroundMinutes: number;
  version: string;
}

export interface Showtime {
  showtimeId: string;
  movieId: string;
  movieTitle: string;
  cinemaId: string;
  cinemaName: string;
  roomId: string;
  roomName: string;
  officialStartsAt: string;
  officialEndsAt: string;
  timezone: string;
  status: string;
  bookingAvailable: boolean;
  serverTime: string;
  version: string;
}

export interface SeatType {
  seatTypeId: string;
  code: string;
  name: string;
  surcharge: Money;
  isActive: boolean;
  version: string;
}

export interface Seat {
  seatId: string;
  rowLabel: string;
  seatNumber: number;
  label: string;
  x: number;
  y: number;
  seatType: SeatType;
  operationalState: string;
  version: string;
}

export interface SeatAvailability {
  seat: Seat;
  availability: 'available' | 'held' | 'sold' | string;
}

export interface SeatMap {
  showtimeId: string;
  roomId: string;
  serverTime: string;
  seats: SeatAvailability[];
}

export interface TicketCategory {
  code: string;
  name: string;
  description: string | null;
  version: string;
}

export interface CinemaPricing {
  cinemaId: string;
  adultBasePrice: Money;
  discountedBasePrice: Money;
  version: string;
  updatedAt: string;
}

export interface Quantities {
  adult: number;
  discounted: number;
}

export interface QuoteSeat {
  seatId: string;
  label: string;
  seatTypeName: string;
  surcharge: Money;
}

export interface Quote {
  quoteId: string;
  version: string;
  quantities: Quantities;
  adultBasePrice: Money;
  discountedBasePrice: Money;
  seats: QuoteSeat[];
  total: Money;
  quotedAt: string;
}

export interface Selection {
  selectionId: string;
  showtimeId: string;
  quantities: Quantities;
  seatIds: string[];
  quote: Quote | null;
  selectionDeadline: string;
  serverTime: string;
  state: string;
  bookingId: string | null;
  version: string;
}

export interface Contact {
  fullName: string;
  phone: string;
  email: string;
}

export interface Booking {
  bookingId: string;
  bookingCode: string;
  status: string;
  showtime: Showtime;
  purchasedStartAt: string;
  seats: QuoteSeat[];
  quantities: Quantities;
  admitted: Quantities;
  remaining: Quantities;
  financialSnapshot: Quote;
  contact: Contact;
  selectionDeadline: string;
  paymentDeadline: string;
  activePaymentId: string | null;
  cancellation: {
    requestId: string;
    status: string;
    refundId: string | null;
    blocksAdmission: boolean;
  } | null;
  createdAt: string;
  confirmedAt: string | null;
  version: string;
}

export interface Payment {
  paymentId: string;
  bookingId: string;
  provider: string;
  status: string;
  appliedAt: string | null;
  reconciliationStatus: string;
  amount: Money;
  paymentDeadline: string;
  graceUntil: string | null;
  lastVerifiedAt: string | null;
  serverTime: string;
}

export interface PaymentInitiation {
  payment: Payment;
  redirectUrl: string | null;
  nextAction: string;
}

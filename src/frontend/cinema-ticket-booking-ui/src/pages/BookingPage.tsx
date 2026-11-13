import { useEffect, useMemo, useState } from 'react';
import {
  createBooking,
  createSelection,
  getCinemas,
  getMovie,
  getPricing,
  getSeats,
  getShowtimes,
  getTicketCategories,
  updateSelection,
} from '../services/bookingApi';
import type { Cinema, CinemaPricing, Movie, Quantities, SeatMap, Selection, Showtime, TicketCategory } from '../types/api';
import { StatePanel } from '../components/StatePanel';

interface BookingPageProps {
  movieId: string;
  onHome: () => void;
  onPayment: (bookingId: string) => void;
}

const initialQuantities: Quantities = { adult: 1, discounted: 0 };

function localDate(): string {
  return new Date().toISOString().slice(0, 10);
}

function money(amount: number, currency = 'VND'): string {
  return new Intl.NumberFormat('vi-VN', { style: 'currency', currency }).format(amount);
}

export function BookingPage({ movieId, onHome, onPayment }: BookingPageProps) {
  const [movie, setMovie] = useState<Movie | null>(null);
  const [cinemas, setCinemas] = useState<Cinema[]>([]);
  const [categories, setCategories] = useState<TicketCategory[]>([]);
  const [pricing, setPricing] = useState<CinemaPricing | null>(null);
  const [date, setDate] = useState(localDate());
  const [cinemaId, setCinemaId] = useState('');
  const [showtimes, setShowtimes] = useState<Showtime[]>([]);
  const [showtime, setShowtime] = useState<Showtime | null>(null);
  const [seatMap, setSeatMap] = useState<SeatMap | null>(null);
  const [selectedSeats, setSelectedSeats] = useState<string[]>([]);
  const [quantities, setQuantities] = useState<Quantities>(initialQuantities);
  const [selection, setSelection] = useState<Selection | null>(null);
  const [contact, setContact] = useState({ fullName: '', phone: '', email: '' });
  const [step, setStep] = useState<'showtime' | 'seats' | 'contact'>('showtime');
  const [loading, setLoading] = useState(true);
  const [working, setWorking] = useState(false);
  const [error, setError] = useState('');

  const ticketCount = quantities.adult + quantities.discounted;
  const total = selection?.quote?.total.amount
    ?? (pricing
      ? quantities.adult * pricing.adultBasePrice.amount + quantities.discounted * pricing.discountedBasePrice.amount
      : 0);

  useEffect(() => {
    let active = true;
    Promise.all([getMovie(movieId), getCinemas(), getTicketCategories()])
      .then(([movieResult, cinemaResult, categoryResult]) => {
        if (!active) return;
        setMovie(movieResult);
        setCinemas(cinemaResult);
        setCategories(categoryResult);
        if (cinemaResult[0]) setCinemaId(cinemaResult[0].cinemaId);
      })
      .catch((cause) => active && setError(cause instanceof Error ? cause.message : 'Không thể tải dữ liệu đặt vé.'))
      .finally(() => active && setLoading(false));
    return () => { active = false; };
  }, [movieId]);

  useEffect(() => {
    if (!cinemaId) return;
    let active = true;
    setPricing(null);
    setShowtime(null);
    setSeatMap(null);
    setSelectedSeats([]);
    setSelection(null);
    setStep('showtime');
    Promise.all([getPricing(cinemaId), getShowtimes(movieId, date, cinemaId)])
      .then(([pricingResult, showtimeResult]) => {
        if (!active) return;
        setPricing(pricingResult);
        setShowtimes(showtimeResult.filter((item) => item.bookingAvailable));
      })
      .catch((cause) => active && setError(cause instanceof Error ? cause.message : 'Không thể tải suất chiếu.'))
    return () => { active = false; };
  }, [cinemaId, date, movieId]);

  async function chooseShowtime(next: Showtime) {
    setWorking(true);
    setError('');
    try {
      setShowtime(next);
      setSeatMap(await getSeats(next.showtimeId));
      setSelectedSeats([]);
      setSelection(null);
      setStep('seats');
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : 'Không thể tải sơ đồ ghế.');
    } finally {
      setWorking(false);
    }
  }

  function toggleSeat(seatId: string) {
    setSelectedSeats((current) => current.includes(seatId)
      ? current.filter((id) => id !== seatId)
      : current.length < ticketCount ? [...current, seatId] : current);
  }

  async function continueToContact() {
    if (!showtime || selectedSeats.length !== ticketCount) {
      setError(`Vui lòng chọn đúng ${ticketCount} ghế cho số lượng vé.`);
      return;
    }
    setWorking(true);
    setError('');
    try {
      const nextSelection = selection
        ? await updateSelection(selection, quantities, selectedSeats)
        : await createSelection(showtime.showtimeId, quantities, selectedSeats);
      setSelection(nextSelection);
      setStep('contact');
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : 'Không thể giữ ghế. Vui lòng tải lại sơ đồ ghế.');
      setSeatMap(await getSeats(showtime.showtimeId).catch(() => seatMap));
    } finally {
      setWorking(false);
    }
  }

  async function submitBooking(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!selection || !showtime) return;
    setWorking(true);
    setError('');
    try {
      const booking = await createBooking(selection, showtime.showtimeId, selectedSeats, quantities, contact);
      sessionStorage.setItem('promethe-phos.booking', JSON.stringify(booking));
      onPayment(booking.bookingId);
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : 'Không thể tạo đặt vé.');
    } finally {
      setWorking(false);
    }
  }

  const groupedSeats = useMemo(() => {
    const groups = new Map<string, typeof seatMap extends null ? never : NonNullable<typeof seatMap>['seats']>();
    seatMap?.seats.forEach((entry) => {
      const row = entry.seat.rowLabel;
      const current = groups.get(row) || [];
      current.push(entry);
      groups.set(row, current);
    });
    return [...groups.entries()];
  }, [seatMap]);

  if (loading) return <div className="page-state"><StatePanel title="Đang chuẩn bị phòng chiếu" message="Đang tải thông tin phim, rạp và suất chiếu..." /></div>;
  if (error && !movie) return <div className="page-state"><StatePanel title="Không thể bắt đầu đặt vé" message={error} actionLabel="Về trang chủ" onAction={onHome} /></div>;
  if (!movie) return null;

  return (
    <main className="booking-page">
      <button className="back-link" type="button" onClick={onHome}>← Về trang chủ</button>
      <section className="booking-detail-hero">
        <div>{movie.posterUrl ? <img className="booking-detail-hero__poster" src={movie.posterUrl} alt={`Poster ${movie.title}`} /> : <div className="details-poster"><span>{movie.title.slice(0, 1)}</span></div>}</div>
        <div className="booking-detail-hero__content">
          <p className="eyebrow">{movie.status === 'coming_soon' ? 'SẮP CHIẾU' : 'ĐANG CHIẾU'} · KHỞI CHIẾU {movie.releaseDate || 'ĐANG CẬP NHẬT'}</p>
          <h1>{movie.title}</h1>
          <div className="booking-detail-hero__meta"><strong>{movie.ageRating || 'K'}</strong><span>{movie.genres.map((genre) => genre.name).join(' · ') || 'Điện ảnh'}</span><span>{movie.durationMinutes} phút</span><span>{movie.language || 'Đang cập nhật'}</span></div>
          <p>{movie.description || 'Thông tin nội dung phim đang được cập nhật.'}</p>
        </div>
      </section>
      <div className="booking-layout">
        <section>
          <div className="booking-title">
            <p className="eyebrow">ĐẶT VÉ TRỰC TUYẾN</p>
            <h1>{movie.title}</h1>
            <p>{movie.durationMinutes} phút · {movie.language || 'Đang cập nhật'} {movie.ageRating && `· ${movie.ageRating}`}</p>
          </div>
          <div className="step-tabs" aria-label="Các bước đặt vé">
            <span className={step === 'showtime' ? 'active' : ''}>1. Suất chiếu</span>
            <span className={step === 'seats' ? 'active' : ''}>2. Ghế & vé</span>
            <span className={step === 'contact' ? 'active' : ''}>3. Thông tin</span>
          </div>

          {error && <div className="inline-error" role="alert">{error}</div>}
          {step === 'showtime' && (
            <div className="booking-section">
              <label className="field-label" htmlFor="cinema">Chọn rạp</label>
              <select id="cinema" value={cinemaId} onChange={(event) => setCinemaId(event.target.value)}>
                {cinemas.map((cinema) => <option value={cinema.cinemaId} key={cinema.cinemaId}>{cinema.name} · {cinema.address}</option>)}
              </select>
              <label className="field-label" htmlFor="date">Ngày xem</label>
              <input id="date" type="date" value={date} min={localDate()} onChange={(event) => setDate(event.target.value)} />
              <h2>Suất chiếu</h2>
              {showtimes.length === 0
                ? <StatePanel title="Chưa có suất phù hợp" message="Vui lòng thử ngày hoặc rạp khác." />
                : <div className="showtime-list">{showtimes.map((item) => (
                  <button className="showtime-button" type="button" key={item.showtimeId} onClick={() => void chooseShowtime(item)}>
                    <strong>{new Date(item.officialStartsAt).toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' })}</strong>
                    <span>{item.roomName}</span>
                  </button>
                ))}</div>}
            </div>
          )}

          {step === 'seats' && seatMap && (
            <div className="booking-section">
              <button className="back-link" type="button" onClick={() => setStep('showtime')}>← Đổi suất chiếu</button>
              <div className="screen-label">MÀN HÌNH</div>
              <div className="seat-map" aria-label="Sơ đồ ghế">
                {groupedSeats.map(([row, seats]) => (
                  <div className="seat-row" key={row}>
                    <span className="row-label">{row}</span>
                    {seats.sort((a, b) => a.seat.seatNumber - b.seat.seatNumber).map((entry) => {
                      const unavailable = entry.availability !== 'available' || entry.seat.operationalState !== 'active';
                      const selected = selectedSeats.includes(entry.seat.seatId);
                      return <button
                        className={`seat ${unavailable ? 'unavailable' : ''} ${selected ? 'selected' : ''}`}
                        disabled={unavailable || working}
                        type="button"
                        aria-label={`${entry.seat.label}, ${entry.availability}`}
                        key={entry.seat.seatId}
                        onClick={() => toggleSeat(entry.seat.seatId)}
                      >{entry.seat.label}</button>;
                    })}
                  </div>
                ))}
              </div>
              <div className="seat-legend"><span><i className="available" /> Trống</span><span><i className="selected" /> Đang chọn</span><span><i className="unavailable" /> Không thể chọn</span></div>
              <div className="ticket-quantities">
                <h2>Loại vé</h2>
                {(['adult', 'discounted'] as const).map((code) => {
                  const category = categories.find((item) => item.code.toLowerCase() === code);
                  return <div className="quantity-row" key={code}>
                    <span>{category?.name || (code === 'adult' ? 'Adult' : 'Discounted')}<small>{money(code === 'adult' ? pricing?.adultBasePrice.amount || 0 : pricing?.discountedBasePrice.amount || 0)}</small></span>
                    <span className="quantity-control"><button type="button" onClick={() => setQuantities((current) => ({ ...current, [code]: Math.max(code === 'adult' ? 1 : 0, current[code] - 1) }))}>−</button><b>{quantities[code]}</b><button type="button" onClick={() => ticketCount < 8 && setQuantities((current) => ({ ...current, [code]: current[code] + 1 }))}>+</button></span>
                  </div>;
                })}
              </div>
              <button className="button button--gold full-width" type="button" disabled={working} onClick={() => void continueToContact()}>Tiếp tục với {selectedSeats.length}/{ticketCount} ghế</button>
            </div>
          )}

          {step === 'contact' && selection && (
            <form className="booking-section contact-form" onSubmit={(event) => void submitBooking(event)}>
              <button className="back-link" type="button" onClick={() => setStep('seats')}>← Đổi ghế</button>
              <h2>Thông tin liên hệ</h2>
              <label className="field-label" htmlFor="fullName">Họ và tên</label>
              <input id="fullName" required value={contact.fullName} onChange={(event) => setContact({ ...contact, fullName: event.target.value })} />
              <label className="field-label" htmlFor="phone">Số điện thoại</label>
              <input id="phone" required pattern="^[0-9+ ()-]{8,20}$" value={contact.phone} onChange={(event) => setContact({ ...contact, phone: event.target.value })} />
              <label className="field-label" htmlFor="email">Email</label>
              <input id="email" required type="email" value={contact.email} onChange={(event) => setContact({ ...contact, email: event.target.value })} />
              <label className="consent"><input required type="checkbox" /> Tôi đồng ý với điều khoản đặt vé và xác nhận đủ điều kiện độ tuổi của phim.</label>
              <button className="button button--gold full-width" disabled={working} type="submit">{working ? 'Đang tạo đặt vé...' : 'Tạo đặt vé pending'}</button>
            </form>
          )}
        </section>

        <aside className="booking-summary">
          <p className="eyebrow">TÓM TẮT</p>
          <h2>{movie.title}</h2>
          {showtime && <p>{showtime.cinemaName} · {showtime.roomName}<br />{new Date(showtime.officialStartsAt).toLocaleString('vi-VN')}</p>}
          <hr />
          <p>{quantities.adult} Adult · {quantities.discounted} Discounted</p>
          <p>{selectedSeats.length ? `Ghế: ${selectedSeats.map((id) => seatMap?.seats.find((entry) => entry.seat.seatId === id)?.seat.label || id).join(', ')}` : 'Chưa chọn ghế'}</p>
          <strong className="summary-total">{money(total)}</strong>
          {selection?.selectionDeadline && <small>Giữ ghế đến {new Date(selection.selectionDeadline).toLocaleTimeString('vi-VN')}</small>}
        </aside>
      </div>
    </main>
  );
}

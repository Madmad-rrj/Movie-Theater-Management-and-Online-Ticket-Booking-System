import { useEffect, useState } from 'react';
import { getBooking } from '../services/bookingApi';
import type { Booking } from '../types/api';
import { StatePanel } from '../components/StatePanel';

export function BookingConfirmationPage({ bookingId, onHome }: { bookingId: string; onHome: () => void }) {
  const [booking, setBooking] = useState<Booking | null>(null);
  const [error, setError] = useState('');

  useEffect(() => {
    getBooking(bookingId)
      .then(setBooking)
      .catch((cause) => setError(cause instanceof Error ? cause.message : 'Không thể tải thông tin đặt vé.'));
  }, [bookingId]);

  if (error) return <div className="page-state"><StatePanel title="Không thể tải xác nhận" message={error} actionLabel="Về trang chủ" onAction={onHome} /></div>;
  if (!booking) return <div className="page-state"><StatePanel title="Đang tải xác nhận" message="Đang lấy thông tin mới nhất từ hệ thống rạp..." /></div>;

  return (
    <main className="confirmation-page">
      <div className="confirmation-mark">✓</div>
      <p className="eyebrow">THANH TOÁN ĐÃ XÁC NHẬN</p>
      <h1>Đặt vé thành công</h1>
      <p className="confirmation-copy">Hệ thống rạp đã xác nhận giao dịch và giữ chỗ cho bạn.</p>
      <section className="confirmation-card">
        <div className="confirmation-header"><span>MÃ ĐẶT VÉ</span><strong>{booking.bookingCode}</strong></div>
        <h2>{booking.showtime.movieTitle}</h2>
        <p>{booking.showtime.cinemaName} · {booking.showtime.roomName}</p>
        <p>{new Date(booking.showtime.officialStartsAt).toLocaleString('vi-VN')}</p>
        <hr />
        <div className="confirmation-grid">
          <span>Ghế</span><strong>{booking.seats.map((seat) => seat.label).join(', ')}</strong>
          <span>Liên hệ</span><strong>{booking.contact.fullName}<br />{booking.contact.email}</strong>
          <span>Trạng thái vé</span><strong>{booking.status}</strong>
          <span>Tổng thanh toán</span><strong>{new Intl.NumberFormat('vi-VN', { style: 'currency', currency: booking.financialSnapshot.total.currency }).format(booking.financialSnapshot.total.amount)}</strong>
        </div>
        <div className="ticket-note">Vé điện tử và mã vào cửa chưa được phát hành bởi backend. Vui lòng giữ mã đặt vé để đối chiếu tại rạp.</div>
      </section>
      <button className="button button--gold" type="button" onClick={onHome}>Về trang chủ</button>
    </main>
  );
}

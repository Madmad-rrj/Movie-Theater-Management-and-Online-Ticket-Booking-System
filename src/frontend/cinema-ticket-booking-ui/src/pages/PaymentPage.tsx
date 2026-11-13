import { useEffect, useRef, useState } from 'react';
import QRCode from 'qrcode';
import { createPayment, getBooking, getPayment } from '../services/bookingApi';
import { ApiRequestError } from '../services/apiClient';
import type { Booking, Payment } from '../types/api';
import { StatePanel } from '../components/StatePanel';

interface PaymentPageProps {
  bookingId: string;
  onConfirmed: (bookingId: string) => void;
  onHome: () => void;
}

function money(amount: number, currency: string): string {
  return new Intl.NumberFormat('vi-VN', { style: 'currency', currency }).format(amount);
}

export function PaymentPage({ bookingId, onConfirmed, onHome }: PaymentPageProps) {
  const [booking, setBooking] = useState<Booking | null>(null);
  const [payment, setPayment] = useState<Payment | null>(null);
  const [redirectUrl, setRedirectUrl] = useState('');
  const [qrDataUrl, setQrDataUrl] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(true);
  const [secondsLeft, setSecondsLeft] = useState<number | null>(null);
  const polling = useRef(false);
  const initialization = useRef<{
    bookingId: string;
    promise: Promise<{ booking: Booking; payment: Payment; redirectUrl: string }>;
  } | null>(null);

  useEffect(() => {
    let active = true;
    const initialize = async (): Promise<{ booking: Booking; payment: Payment; redirectUrl: string }> => {
      const currentBooking = await getBooking(bookingId);
      const stored = sessionStorage.getItem(`promethe-phos.payment.${bookingId}`);
      const storedPayment = stored ? JSON.parse(stored) as { paymentId: string; redirectUrl: string } : null;
      let currentPayment = currentBooking.activePaymentId
        ? await getPayment(currentBooking.activePaymentId)
        : storedPayment ? await getPayment(storedPayment.paymentId) : null;
      let paymentUrl = storedPayment?.redirectUrl || '';

      if (!currentPayment) {
        const initiated = await createPayment(bookingId);
        currentPayment = initiated.payment;
        paymentUrl = initiated.redirectUrl || '';
        if (paymentUrl) {
          sessionStorage.setItem(`promethe-phos.payment.${bookingId}`, JSON.stringify({
            paymentId: initiated.payment.paymentId,
            redirectUrl: paymentUrl,
          }));
        }
      }

      return { booking: currentBooking, payment: currentPayment, redirectUrl: paymentUrl };
    };

    const existing = initialization.current?.bookingId === bookingId ? initialization.current.promise : null;
    const promise = existing || initialize();
    if (!existing) initialization.current = { bookingId, promise };

    promise.then((result) => {
      if (!active) return;
      setBooking(result.booking);
      setPayment(result.payment);
      if (result.redirectUrl) setRedirectUrl(result.redirectUrl);
    }).catch(async (cause: unknown) => {
      if (active && cause instanceof ApiRequestError && cause.status === 409) {
        try {
          const recoveredBooking = await getBooking(bookingId);
          if (recoveredBooking.activePaymentId) {
            const recoveredPayment = await getPayment(recoveredBooking.activePaymentId);
            if (active) {
              setBooking(recoveredBooking);
              setPayment(recoveredPayment);
              return;
            }
          }
        } catch {
          // Preserve the original conflict message when no recoverable payment exists.
        }
      }
      if (active) setError(cause instanceof Error ? cause.message : 'Không thể khởi tạo thanh toán.');
    }).finally(() => {
      if (active) setLoading(false);
    });

    return () => {
      active = false;
    };
  }, [bookingId]);

  useEffect(() => {
    if (!payment) return;
    let active = true;
    const deadline = new Date(payment.paymentDeadline).getTime();
    const timer = window.setInterval(() => {
      const remaining = Math.max(0, Math.ceil((deadline - Date.now()) / 1000));
      setSecondsLeft(remaining);
      if (remaining === 0) window.clearInterval(timer);
    }, 1000);
    setSecondsLeft(Math.max(0, Math.ceil((deadline - Date.now()) / 1000)));
    if (redirectUrl) {
      QRCode.toDataURL(redirectUrl, { width: 280, margin: 2, color: { dark: '#171411', light: '#fffaf0' } })
        .then(setQrDataUrl)
        .catch(() => setError('Không thể tạo mã QR thanh toán.'));
    }
    const terminal = new Set(['succeeded', 'failed', 'cancelled']);
    const poll = window.setInterval(async () => {
      if (!active || polling.current || terminal.has(payment.status)) return;
      polling.current = true;
      try {
        const next = await getPayment(payment.paymentId);
        if (!active) return;
        setPayment(next);
        if (next.status === 'succeeded') {
          const confirmed = await getBooking(bookingId);
          if (confirmed.status === 'confirmed') onConfirmed(bookingId);
          else setError('Thanh toán đã nhận nhưng đặt vé chưa được xác nhận. Hệ thống đang đối soát, vui lòng thử lại sau.');
        } else if (['failed', 'cancelled'].includes(next.status)) {
          setError('Giao dịch thanh toán không thành công. Bạn có thể quay về trang chủ để đặt lại.');
        }
      } catch {
        // A temporary polling failure must not be interpreted as payment success.
      } finally {
        polling.current = false;
      }
    }, 3000);
    return () => {
      active = false;
      window.clearInterval(timer);
      window.clearInterval(poll);
    };
  }, [bookingId, onConfirmed, payment, redirectUrl]);

  if (loading) return <div className="page-state"><StatePanel title="Đang chuẩn bị thanh toán" message="Đang kiểm tra đặt vé và giao dịch hiện tại..." /></div>;
  if (error && !payment) return <div className="page-state"><StatePanel title="Không thể tiếp tục thanh toán" message={error} actionLabel="Về trang chủ" onAction={onHome} /></div>;
  if (!booking || !payment) return null;

  return (
    <main className="payment-page">
      <p className="eyebrow">THANH TOÁN MOCKPAY</p>
      <h1>Quét mã để hoàn tất đặt vé</h1>
      <p className="payment-intro">Mở camera trên điện thoại và quét mã QR. Kết quả chỉ được xác nhận sau khi hệ thống rạp đối soát thành công.</p>
      <div className="payment-layout">
        <section className="qr-card">
          {qrDataUrl ? <img src={qrDataUrl} alt="Mã QR thanh toán MockPay" /> : <div className="qr-placeholder">Đang tạo mã QR...</div>}
          {redirectUrl && <a className="button button--outline" href={redirectUrl} target="_blank" rel="noreferrer">Mở MockPay trên thiết bị này</a>}
          <strong className="countdown">{secondsLeft === null ? '--:--' : `${Math.floor(secondsLeft / 60).toString().padStart(2, '0')}:${(secondsLeft % 60).toString().padStart(2, '0')}`}</strong>
          <span>Thời gian thanh toán còn lại</span>
          <p className="payment-status">Trạng thái hệ thống: <b>{payment.status}</b></p>
          {error && <div className="inline-error" role="alert">{error}</div>}
        </section>
        <aside className="booking-summary">
          <p className="eyebrow">ĐẶT VÉ PENDING</p>
          <h2>{booking.showtime.movieTitle}</h2>
          <p>{booking.showtime.cinemaName} · {booking.showtime.roomName}<br />{new Date(booking.showtime.officialStartsAt).toLocaleString('vi-VN')}</p>
          <hr />
          <p>Mã đặt vé: <b>{booking.bookingCode}</b></p>
          <p>Ghế: {booking.seats.map((seat) => seat.label).join(', ')}</p>
          <strong className="summary-total">{money(payment.amount.amount, payment.amount.currency)}</strong>
        </aside>
      </div>
    </main>
  );
}

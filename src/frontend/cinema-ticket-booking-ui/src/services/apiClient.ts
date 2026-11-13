import type { ApiError, ApiResponse } from '../types/api';

const csrfStorageKey = 'promethe-phos.csrf-token';

export class ApiRequestError extends Error {
  readonly status: number;
  readonly code?: string;
  readonly details?: ApiError['error'] extends infer T ? T : never;

  constructor(status: number, message: string, code?: string, details?: ApiError['error']) {
    super(message);
    this.name = 'ApiRequestError';
    this.status = status;
    this.code = code;
    this.details = details;
  }
}

function requestId(): string {
  return crypto.randomUUID();
}

function userMessage(status: number, code?: string, fallback?: string): string {
  if (status === 409) {
    if (code === 'PAYMENT_NOT_ELIGIBLE') return 'Booking hiện không đủ điều kiện thanh toán. Vui lòng tạo booking mới.';
    if (code === 'STATE_CONFLICT') return 'Booking đang được xử lý bởi một yêu cầu khác. Vui lòng đợi vài giây rồi thử lại.';
    if (code === 'SEAT_CONFLICT') return 'Ghế đã thay đổi trạng thái. Vui lòng chọn lại ghế.';
    if (fallback) return code ? `[${code}] ${fallback}` : fallback;
    return 'Thông tin đặt vé vừa thay đổi. Vui lòng tải lại lựa chọn và thử lại.';
  }
  if (status === 410 || status === 422) return 'Lựa chọn hoặc thời hạn thanh toán đã hết. Vui lòng bắt đầu lại.';
  if (status === 401 || status === 403) return 'Phiên khách đã hết hạn. Vui lòng tải lại trang và thử lại.';
  if (status === 503) return 'Dịch vụ thanh toán hoặc dữ liệu đang tạm thời không khả dụng.';
  return fallback || 'Không thể hoàn tất yêu cầu. Vui lòng thử lại.';
}

async function parseError(response: Response): Promise<ApiRequestError> {
  let body: ApiError = {};
  try {
    body = (await response.json()) as ApiError;
  } catch {
    // The status-based message below is still actionable when no JSON is returned.
  }
  const error = body.error;
  return new ApiRequestError(
    response.status,
    userMessage(response.status, error?.code, error?.message),
    error?.code,
    error,
  );
}

export async function apiRequest<T>(
  path: string,
  init: RequestInit = {},
  options: { mutation?: boolean; idempotency?: boolean } = {},
): Promise<{ value: T; response: Response }> {
  const headers = new Headers(init.headers);
  headers.set('Accept', 'application/json');
  if (init.body && !headers.has('Content-Type')) headers.set('Content-Type', 'application/json');
  if (options.mutation) {
    const csrf = sessionStorage.getItem(csrfStorageKey);
    if (csrf) headers.set('X-CSRF-Token', csrf);
    headers.set('Idempotency-Key', requestId());
  } else if (options.idempotency) {
    headers.set('Idempotency-Key', requestId());
  }

  const response = await fetch(path, {
    ...init,
    headers,
    credentials: 'include',
  });
  const csrf = response.headers.get('X-CSRF-Token');
  if (csrf) sessionStorage.setItem(csrfStorageKey, csrf);
  if (!response.ok) throw await parseError(response);
  if (response.status === 204) return { value: undefined as T, response };
  return { value: (await response.json()) as T, response };
}

export async function ensureGuestContext(): Promise<void> {
  const context = sessionStorage.getItem('promethe-phos.guest-context');
  if (context) return;
  const { value } = await apiRequest<ApiResponse<{ contextId: string; expiresAt: string }>>(
    '/api/guest-context',
    { method: 'POST' },
    { mutation: true, idempotency: true },
  );
  sessionStorage.setItem('promethe-phos.guest-context', JSON.stringify(value.data));
}

export function unwrap<T>(result: { value: ApiResponse<T> }): T {
  return result.value.data;
}

export function clearGuestContext(): void {
  sessionStorage.removeItem('promethe-phos.guest-context');
  sessionStorage.removeItem(csrfStorageKey);
}

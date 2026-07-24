export const BASE_URL = import.meta.env.VITE_API_BASE_URL || 'https://localhost:7216';

export interface ApiResponse<T = any> {
  messages: string[];
  isSuccessful: boolean;
  statusCode: number;
  data?: T;
}

async function request<T = any>(
  endpoint: string,
  options: RequestInit = {}
): Promise<ApiResponse<T>> {
  const url = `${BASE_URL.replace(/\/$/, '')}/${endpoint.replace(/^\//, '')}`;

  const headers = new Headers(options.headers);
  if (!headers.has('Content-Type') && !(options.body instanceof FormData)) {
    headers.set('Content-Type', 'application/json');
  }

  // Inject token if present
  const token = localStorage.getItem('token');
  if (token && !headers.has('Authorization')) {
    headers.set('Authorization', `Bearer ${token}`);
  }

  const config: RequestInit = {
    ...options,
    headers,
  };

  try {
    const response = await fetch(url, config);
    let data: any = null;

    const contentType = response.headers.get('content-type');
    if (contentType && contentType.includes('application/json')) {
      data = await response.json();
    }

    if (response.ok) {
      return {
        messages: data?.messages || [],
        isSuccessful: data?.isSuccessful ?? true,
        statusCode: response.status,
        data: data?.data ?? data,
      };
    }

    // Handled failure status (e.g. 400, 403, 500)
    return {
      messages: data?.messages || ['An error occurred.'],
      isSuccessful: false,
      statusCode: response.status,
      data: data?.data ?? data,
    };
  } catch (error) {
    console.error('API Client Error:', error);
    return {
      messages: ['Failed to connect to the server. Please verify the backend is running.'],
      isSuccessful: false,
      statusCode: 503, // Service Unavailable
    };
  }
}

export const api = {
  get: <T = any>(endpoint: string, options?: RequestInit) =>
    request<T>(endpoint, { ...options, method: 'GET' }),

  post: <T = any>(endpoint: string, body?: any, options?: RequestInit) =>
    request<T>(endpoint, {
      ...options,
      method: 'POST',
      body: body ? JSON.stringify(body) : undefined,
    }),

  put: <T = any>(endpoint: string, body?: any, options?: RequestInit) =>
    request<T>(endpoint, {
      ...options,
      method: 'PUT',
      body: body ? JSON.stringify(body) : undefined,
    }),

  delete: <T = any>(endpoint: string, options?: RequestInit) =>
    request<T>(endpoint, { ...options, method: 'DELETE' }),
};
export interface DecodedToken {
  id: string;
  email: string;
  fullName: string;
  phoneNumber: string;
  roles: string[];
  permissions: string[];
  exp: number;
}

export function decodeToken(token: string): DecodedToken | null {
  try {
    const parts = token.split('.');
    if (parts.length !== 3) return null;

    const base64Url = parts[1];
    const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
    
    // Add padding if necessary
    const pad = base64.length % 4;
    const paddedBase64 = pad ? base64 + '='.repeat(4 - pad) : base64;

    const jsonPayload = decodeURIComponent(
      window
        .atob(paddedBase64)
        .split('')
        .map((c) => '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2))
        .join('')
    );

    const payload = JSON.parse(jsonPayload);

    // Mappings
    const id = payload["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier"] || payload["nameidentifier"] || payload["sub"] || '';
    const email = payload["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress"] || payload["email"] || '';
    const fullName = payload["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name"] || payload["unique_name"] || payload["name"] || '';
    const phoneNumber = payload["http://schemas.xmlsoap.org/ws/2008/06/identity/claims/mobilephone"] || payload["mobilephone"] || '';
    
    // Roles normalization - MUST use exact key as specified by user
    const rawRoles = payload["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"] || payload["role"] || [];
    const roles = Array.isArray(rawRoles) ? rawRoles : rawRoles ? [rawRoles] : [];

    // Permissions normalization
    const rawPermissions = payload["permission"] || [];
    const permissions = Array.isArray(rawPermissions) ? rawPermissions : rawPermissions ? [rawPermissions] : [];

    const exp = payload["exp"] || 0;

    return {
      id,
      email,
      fullName,
      phoneNumber,
      roles,
      permissions,
      exp,
    };
  } catch (error) {
    console.error('Error decoding JWT token:', error);
    return null;
  }
}

export function isTokenExpired(token: string | null): boolean {
  if (!token) return true;
  const decoded = decodeToken(token);
  if (!decoded) return true;
  // Check if expired, with a 30-second buffer
  const currentTime = Math.floor(Date.now() / 1000);
  return decoded.exp < currentTime + 30;
}
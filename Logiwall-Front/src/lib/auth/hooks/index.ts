import Cookies from 'js-cookie';

// ----------------------------------------------------------------------

export function jwtDecode(token: string) {
  try {
    if (!token) return null;

    const parts = token.split('.');
    if (parts.length < 2) {
      throw new Error('Invalid token!');
    }

    const base64Url = parts[1];
    const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
    return JSON.parse(atob(base64));
  } catch (error) {
    console.error('Error decoding token:', error);
    throw error;
  }
}

// ----------------------------------------------------------------------

// ----------------------------------------------------------------------
export function isValidToken(accessToken: string) {
  if (!accessToken) {
    return false;
  }

  try {
    const decoded = jwtDecode(accessToken);

    if (!decoded || !('exp' in decoded)) {
      return false;
    }

    const currentTime = Date.now() / 1000;
    return decoded.exp > currentTime;
  } catch (error) {
    console.error('Error during token validation:', error);
    return false;
  }
}

//----------------------------------------------------------------------------
export const TokenExpired = () => {
  const timeLeft: number = +Number(localStorage.getItem('expTime')) * 1000;
  setTimeout(() => {
    try {
      localStorage.removeItem('token');
      localStorage.removeItem('expTime');
      window.location.href = '/auth/login';
    } catch (error) {
      console.error('Error during token expiration:', error);
      throw error;
    }
  }, timeLeft);
};
//-----------------------------------------------------------------------------
export const SetToken = (accessToken: string | null, expTime?: any) => {
  if (accessToken) {
    localStorage.setItem('token', accessToken);
    if (expTime) {
      localStorage.setItem('expTime', expTime);
    }
    TokenExpired();
  } else {
    localStorage.removeItem('token');
    localStorage.removeItem('expTime');
  }
};

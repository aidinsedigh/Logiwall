import useQueryParam from '../../../hooks/use-query-param';
import { useAppSelector } from '../../../store/store';
import { useNavigate } from 'react-router';
import { FC, useCallback, useEffect, useState } from 'react';
import { useLocation, useSearchParams } from 'react-router-dom';
import { Ichildren } from '../../../types/global';
import LgAnimateLoading from '../../../components/ui/loading/animate-loading';
import { useAuthContext } from '../context/use-auth-context';
import { isValidToken } from './index';
//------------------------------------------------------------------------
export const AuthGuard: FC<Ichildren> = ({ children }) => {
  const { pathname } = useLocation();
  const [isChecking, setIsChecking] = useState(true);
  const { authenticated, loading } = useAuthContext();
  const navigate = useNavigate();
  const searchParams = useSearchParams();
  const access_token: any = localStorage.getItem('token');
  const { setQuery, GetQuery } = useQueryParam();

  const createQueryString = useCallback(
    (name: string, value: string) => {
      const params = new URLSearchParams(searchParams.toString());
      params.set(name, value);

      return params.toString();
    },
    [searchParams]
  );
  const checkPermissions = async () => {
    if (loading) {
      return;
    }
    if (!authenticated && !isValidToken(access_token)) {
      const href = `/auth/login?${createQueryString('returnTo', pathname)}`;
      navigate(href, { replace: true });
      return;
    }

    setIsChecking(false);
  };
  useEffect(() => {
    if (GetQuery?.channel) {
      sessionStorage.setItem('channel', GetQuery?.channel);
    }
  }, []);
  useEffect(() => {
    checkPermissions();
  }, [authenticated]);

  if (isChecking) {
    return <LgAnimateLoading />;
  }
  return <>{children}</>;
};

// ----------------------------------------------------------------------

export const GuestGuard: FC<Ichildren> = ({ children }) => {
  const { loading, authenticated } = useAuthContext();

  const returnTo = '/app/home';
  const navigate = useNavigate();
  const { setQuery, GetQuery } = useQueryParam();
  const access_token: any = localStorage.getItem('token');

  const checkPermissions = async () => {
    if (loading) {
      return;
    }
    if (authenticated && isValidToken(access_token)) {
      if (GetQuery?.channel) {
        navigate(returnTo, { replace: true });
        setQuery(GetQuery);
      } else {
        navigate(returnTo, { replace: true });
      }
      return;
    } else {
      if (GetQuery?.channel) {
        navigate('/auth/login', { replace: true });
        setQuery(GetQuery);
      } else {
        navigate('/auth/login', { replace: true });
      }
    }
  };
  useEffect(() => {
    if (GetQuery?.channel) {
      sessionStorage.setItem('channel', GetQuery?.channel);
    }
  }, []);
  useEffect(() => {
    checkPermissions();
  }, [authenticated, loading]);

  return <>{children}</>;
};

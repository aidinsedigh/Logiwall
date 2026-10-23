import Cookies from 'js-cookie';
import { useNavigate } from 'react-router';
import { setAuthStatus, setToken } from '../store/slices/auth-slice';
import { useAppDispatch } from '../store/store';
import { SetToken } from '../lib/auth/hooks';

//------------------------------------------------------------------------

const useAuthLogin = () => {
  const dispatch = useAppDispatch();
  const navigate = useNavigate();

  //login
  const Login = async (token: string, expTime: number) => {
    SetToken(token, expTime);
    dispatch(setAuthStatus(true));
    navigate('/app/home', { replace: true });
  };

  return { Login };
};
export default useAuthLogin;

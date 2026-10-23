import { useNavigate } from 'react-router';
import { SetToken } from '../lib/auth/hooks';

const useLogout = () => {
  const navigate = useNavigate();
  const Logout = () => {
    SetToken(null);
    navigate('/auth/login', { replace: true });
  };
  return { Logout };
};
export default useLogout;

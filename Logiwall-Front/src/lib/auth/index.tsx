import { useAppDispatch, useAppSelector } from '../../store/store';
import { FC, useCallback, useEffect, useMemo, useState } from 'react';
import { Ichildren } from '../../types/global';
import { isValidToken, SetToken } from './hooks';
import { AuthContext } from './context/auth-context';

//--------------------------------------------------------------------------
export function isEqual(a: any, b: any) {
  if (a === null || a === undefined || b === null || b === undefined) {
    return a === b;
  }
}
export function useSetState(initialState: any) {
  const [state, set] = useState(initialState);

  const canReset = !isEqual(state, initialState);

  const setState = useCallback((updateState: any) => {
    set((prevValue: any) => ({ ...prevValue, ...updateState }));
  }, []);

  const setField = useCallback(
    (name: any, updateValue: any) => {
      setState({
        [name]: updateValue,
      });
    },
    [setState]
  );

  const onResetState = useCallback(() => {
    set(initialState);
  }, [initialState]);

  return useMemo(
    () => ({
      state,
      setState,
      setField,
      onResetState,
      canReset,
    }),
    [canReset, onResetState, setField, setState, state]
  );
}

export const AuthProvider: FC<Ichildren> = ({ children }) => {
  const dispatch = useAppDispatch();
  const accessToken = localStorage.getItem('token');
  const auth = useAppSelector((state) => state.AuthSlice.authenticated);

  const { state, setState } = useSetState({
    user: null,
    loading: true,
  });
  const checkUserToken = useCallback(async () => {
    try {
      if (accessToken && isValidToken(accessToken as string)) {
        await SetToken(accessToken as string);
        setState({ user: auth, loading: false });
      } else {
        await SetToken(null);
        setState({ user: null, loading: false });
      }
    } catch (error) {
      setState({ user: null, loading: false });
      console.log(error);
    }
  }, [setState, auth]);

  useEffect(() => {
    checkUserToken();
  }, []);
  const checkAuthenticated = accessToken ? 'authenticated' : 'unauthenticated';

  const status = state.loading ? 'loading' : checkAuthenticated;
  const memorizedValue: any = useMemo(
    () => ({
      checkUserToken,
      loading: status === 'loading',
      authenticated: status === 'authenticated',
      unauthenticated: status === 'unauthenticated',
    }),
    [checkUserToken, status, state.user]
  );

  return <AuthContext.Provider value={memorizedValue}>{children}</AuthContext.Provider>;
};

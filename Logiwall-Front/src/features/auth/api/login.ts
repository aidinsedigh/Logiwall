import { useForm } from 'react-hook-form';
import { UseBaseMutationResult, useMutation, UseMutationResult } from '@tanstack/react-query';
import { axiosAuth } from 'lib/axios';
import useApiEndpoints from 'hooks/use-api-endpoints';
import useQueryParam from 'hooks/use-query-param';
import useAuthLogin from 'hooks/use-login';
import { useEffect } from 'react';
import * as Yup from 'yup';
import { yupResolver } from '@hookform/resolvers/yup';
import { useAppDispatch } from 'store/store';
import { setChannel } from 'store/slices/auth-slice';
import { IloginPayload, IloginResponse } from '../../../types/auth';

//------------------------------------------------------------------------------------------

const schema = Yup.object().shape({
  userName: Yup.string().required('نام کاربری یا شماره همراه خود را وارد نمایید'),
  password: Yup.string().required('رمز عبور خود را وارد نمایید'),
  rememberMe: Yup.boolean(),
  channel: Yup.string(),
  loginProvider: Yup.number(),
});

//-------------------------------------------------------------------------------------

const useLogin = () => {
  const { GetQuery } = useQueryParam();
  const methods = useForm({
    resolver: yupResolver(schema),
    defaultValues: {
      rememberMe: false,
      userName: GetQuery?.phoneNumber ?? '',
      password: '',
      channel: GetQuery?.channel ?? 'pwa',
      loginProvider: GetQuery?.channel ? 21 : 20,
    },
  });
  const { Login } = useAuthLogin();
  const dispatch = useAppDispatch();

  const { handleSubmit, control } = methods;
  const endpoint = useApiEndpoints();
  const { mutateAsync: GetLogin, isPending: LoginPending } = useMutation({
    mutationKey: ['login'],
    mutationFn: (params) => {
      return axiosAuth.post(endpoint.AUTH.POST_LOGIN, params).then((res) => res.data);
    },
  }) as UseMutationResult<IloginResponse>;

  const HandleSubmitLogin = handleSubmit(async (payload: IloginPayload) => {
    try {
      const response = await GetLogin(payload);
      Login(response.accessToken, response.expiresIn);
    } catch (error) {
      console.log(error);
    }
  });

  useEffect(() => {
    if (GetQuery?.theme) {
      localStorage.setItem('theme', GetQuery?.theme);
    }
    if (GetQuery?.channel) {
      dispatch(setChannel(GetQuery?.channel));
    }
  }, [GetQuery]);

  return { methods, control, HandleSubmitLogin, LoginPending };
};
export default useLogin;
